using System.Buffers;
using System.Buffers.Binary;
using FlatSharp;
using ModAbi;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Encoding.Typed over the relay: the host offers the type ids it has a binary codec for
// (Handshake.typed_types), the guest accepts a subset (SetupReply.typed_types); exactly
// those cross as bytes, both ways. Everything else, and a guest that accepts nothing,
// stays JSON. Canned guest replies, real World.
public class ModAbiTypedTests
{
    private sealed class CaptureExecutor : IModWasmExecutor
    {
        public byte[]? SetupReplyBytes;
        public byte[]? RunReplyBytes;
        public byte[]? Handshake;
        public readonly List<byte[]> RunInputs = new();

        public int Load(in ModSource source, int slot, IModImportSink sink, string importModule, IReadOnlyList<ModHostImport> hostImports) => slot;
        public Memory<byte> CallSetup(int handle, ReadOnlySpan<byte> handshake)
        {
            Handshake = handshake.ToArray();
            return SetupReplyBytes;
        }
        public Memory<byte> CallRun(int handle, uint sysId, ReadOnlySpan<byte> input)
        {
            RunInputs.Add(input.ToArray());
            return RunReplyBytes;
        }
        public Memory<byte> CallObserver(int handle, uint obsId, ulong entity, ReadOnlySpan<byte> input) => default;
        public void CallSpawned(int handle, ReadOnlySpan<byte> input) { }
        public void Reload(int handle, in ModSource source) { }
        public void DisposeInstance(int handle) { }
        public void Dispose() { }
    }

    private static byte[] Bytes<T>(ISerializer<T> serializer, T value) where T : class
    {
        var buf = new byte[serializer.GetMaxSize(value)];
        var n = serializer.Write(buf, value);
        return buf.AsSpan(0, n).ToArray();
    }

    // The binary codecs a host would generate from its WIT records.
    private static void WriteTag(in V3Tag v, IBufferWriter<byte> w) => ModBinary.S32(w, v.X);
    private static V3Tag ReadTag(ref ModBinaryReader r, ModEntityResolver? resolve) => new() { X = r.S32() };
    private static void WriteLink(in V3Link v, IBufferWriter<byte> w) => ModBinary.U64(w, v.Target);
    private static V3Link ReadLink(ref ModBinaryReader r, ModEntityResolver? resolve) => new() { Target = r.Entity(resolve) };
    private static void WriteScore(in V3Score v, IBufferWriter<byte> w) => ModBinary.S32(w, v.Value);
    private static V3Score ReadScore(ref ModBinaryReader r, ModEntityResolver? resolve) => new() { Value = r.S32() };

    private static (App App, ModHostContext Ctx, ModComponentRegistry Reg) Host()
    {
        var app = new App(ThreadingMode.Single);
        var reg = new ModComponentRegistry();
        reg.Register("t:tag", new ModComponent<V3Tag>(V3JsonContext.Default.V3Tag));
        reg.Register("t:link", new ModComponent<V3Link>(V3JsonContext.Default.V3Link,
            static (v, resolve) => { v.Target = resolve(v.Target); return v; }));
        reg.Register("t:ping-comp", new ModComponent<V3Ping>(V3JsonContext.Default.V3Ping)); // no codec
        reg.RegisterResource("t:score", new ModResource<V3Score>(V3JsonContext.Default.V3Score));
        reg.RegisterBinaryComponent<V3Tag>("t:tag", WriteTag, ReadTag);
        reg.RegisterBinaryComponent<V3Link>("t:link", WriteLink, ReadLink);
        reg.RegisterBinaryResource<V3Score>("t:score", WriteScore, ReadScore);
        var ctx = new ModHostContext { World = app.GetWorld(), Registry = reg, App = app, Name = "typed" };
        return (app, ctx, reg);
    }

    private static ushort Id(ModComponentRegistry reg, string path)
    {
        ushort i = 0;
        foreach (var (p, _) in reg.Entries)
        {
            if (p == path)
                return i;
            i++;
        }
        throw new KeyNotFoundException(path);
    }

    private static CaptureExecutor Guest(ModComponentRegistry reg, List<ushort>? accepted, CommandBuffer? reply = null) => new()
    {
        SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
        {
            TypedTypes = accepted,
            ResUnchanged = true,
            Systems = new List<SystemDecl>
            {
                new()
                {
                    Id = 0, Name = "s", Schedule = Schedule.Update,
                    Params = new List<ParamDecl>
                    {
                        new()
                        {
                            Kind = ParamKind.Query,
                            Query = new QueryDecl
                            {
                                Terms = new List<QueryTerm>
                                {
                                    new() { Kind = QueryTermKind.Ref, TypeId = Id(reg, "t:tag") },
                                    new() { Kind = QueryTermKind.Ref, TypeId = Id(reg, "t:ping-comp") },
                                },
                            },
                        },
                        new() { Kind = ParamKind.Res, TypeId = Id(reg, "t:score") },
                        new() { Kind = ParamKind.Commands },
                    },
                },
            },
        }),
        RunReplyBytes = reply != null ? Bytes(CommandBuffer.Serializer, reply) : null,
    };

    private static byte[] S32(int v)
    {
        var b = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(b, v);
        return b;
    }

    private static string Utf8(CompValue v) => System.Text.Encoding.UTF8.GetString(v.Data!.Value.Span);

    [Fact]
    public void Handshake_offers_exactly_the_paths_with_a_binary_codec()
    {
        var (_, ctx, reg) = Host();
        var exec = Guest(reg, accepted: null);
        new ModAbiRunner(exec, 0, new ModRelayState(), ctx).Setup();
        var hs = Handshake.Serializer.Parse(exec.Handshake!);
        Assert.Equal(new[] { Id(reg, "t:tag"), Id(reg, "t:link"), Id(reg, "t:score") }.OrderBy(x => x), hs.TypedTypes!.OrderBy(x => x));
    }

    [Fact]
    public void Accepted_type_ids_cross_typed_and_the_rest_stays_json()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        app.GetWorld().Entity().Set(new V3Tag { X = -5 }).Set(new V3Ping { N = 3 });
        var exec = Guest(reg, [Id(reg, "t:tag"), Id(reg, "t:score"), Id(reg, "t:ping-comp")]);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        runner.RunSystem(ctx.Systems[0]);

        var input = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        var row = Assert.Single(input.Queries![0].Rows!);
        Assert.Equal(ModAbi.Encoding.Typed, row.Comps![0].Encoding);
        Assert.Equal(S32(-5), row.Comps[0].Data!.Value.ToArray());
        // Accepted, but the host has no codec for it (never offered): JSON.
        Assert.Equal(ModAbi.Encoding.Json, row.Comps[1].Encoding);
        Assert.Equal("{\"N\":3}", Utf8(row.Comps[1]));
        Assert.Equal(ModAbi.Encoding.Typed, input.Resources![0].Value!.Encoding);
        Assert.Equal(S32(7), input.Resources[0].Value!.Data!.Value.ToArray());

        // An unchanged typed resource is still just the flag.
        runner.RunSystem(ctx.Systems[0]);
        var second = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Null(second.Resources![0].Value);
    }

    [Fact]
    public void A_guest_that_accepts_nothing_keeps_getting_json()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        app.GetWorld().Entity().Set(new V3Tag { X = -5 }).Set(new V3Ping { N = 3 });
        var exec = Guest(reg, accepted: null);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        runner.RunSystem(ctx.Systems[0]);

        var input = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        var row = Assert.Single(input.Queries![0].Rows!);
        Assert.Equal(ModAbi.Encoding.Json, row.Comps![0].Encoding);
        Assert.Equal("{\"X\":-5}", Utf8(row.Comps[0]));
        Assert.Equal(ModAbi.Encoding.Json, input.Resources![0].Value!.Encoding);
        Assert.Equal("{\"Value\":7}", Utf8(input.Resources[0].Value!));
    }

    [Fact]
    public void An_absent_typed_resource_is_absent_not_bytes()
    {
        var (_, ctx, reg) = Host();
        var exec = Guest(reg, [Id(reg, "t:score")]);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        runner.RunSystem(ctx.Systems[0]);
        var input = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Null(input.Resources![0].Value);
        Assert.False(input.Resources[0].Unchanged);
    }

    [Fact]
    public void Typed_commands_apply_through_the_codec_and_resolve_placeholders()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 1 });
        var target = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(target, ModEntityRef.PlaceholderBit | 0UL);
        var reply = new CommandBuffer
        {
            Cmds = new List<Cmd>
            {
                new(new SpawnCmd
                {
                    TempId = 0,
                    Comps = new List<CompValue> { new() { TypeId = Id(reg, "t:tag"), Encoding = ModAbi.Encoding.Typed, Data = S32(42) } },
                }),
                new(new SpawnCmd
                {
                    TempId = 1,
                    Comps = new List<CompValue>
                    {
                        new() { TypeId = Id(reg, "t:link"), Encoding = ModAbi.Encoding.Typed, Data = target },
                        // Typed for a path with no codec: skipped, the rest still applies.
                        new() { TypeId = Id(reg, "t:ping-comp"), Encoding = ModAbi.Encoding.Typed, Data = S32(1) },
                        new() { TypeId = Id(reg, "t:tag"), Encoding = ModAbi.Encoding.Json, Data = System.Text.Encoding.UTF8.GetBytes("{\"X\":9}") },
                    },
                }),
                new(new ResourceSetCmd { Value = new CompValue { TypeId = Id(reg, "t:score"), Encoding = ModAbi.Encoding.Typed, Data = S32(77) } }),
            },
        };
        var exec = Guest(reg, [Id(reg, "t:tag"), Id(reg, "t:link")], reply);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        runner.RunSystem(ctx.Systems[0]);

        var world = app.GetWorld();
        var tags = new Dictionary<ulong, int>();
        foreach (var (e, tag) in world.Query<Data<V3Tag>>())
            tags[e.Ref] = tag.Ref.X;
        Assert.Equal(new[] { 9, 42 }, tags.Values.OrderBy(x => x));
        var first = tags.Single(kv => kv.Value == 42).Key;
        var second = tags.Single(kv => kv.Value == 9).Key;
        Assert.Equal(first, world.Get<V3Link>(second).Target);
        Assert.False(world.Has<V3Ping>(second));
        Assert.Equal(77, app.GetResource<V3Score>().Value);
    }

    [Fact]
    public void Binary_reader_rejects_a_truncated_value()
    {
        var bytes = new byte[] { 5, 0, 0, 0, (byte)'a' }; // a 5-byte string with 1 byte
        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new ModBinaryReader(bytes);
            r.String();
        });
        var w = new ArrayBufferWriter<byte>();
        ModBinary.String(w, "hé");
        ModBinary.F32(w, 1.5f);
        ModBinary.Bool(w, true);
        var rr = new ModBinaryReader(w.WrittenSpan);
        Assert.Equal("hé", rr.String());
        Assert.Equal(1.5f, rr.F32());
        Assert.True(rr.Bool());
        Assert.Equal(0, rr.Remaining);
    }

    [Fact]
    public void A_codec_needs_a_typed_mapper_of_its_payload()
    {
        var reg = new ModComponentRegistry();
        reg.Register("t:tag", new ModComponent<V3Tag>(V3JsonContext.Default.V3Tag));
        Assert.Throws<InvalidOperationException>(() => reg.RegisterBinaryComponent<V3Link>("t:tag", WriteLink, ReadLink));
        Assert.Throws<InvalidOperationException>(() => reg.RegisterBinaryComponent<V3Tag>("t:none", WriteTag, ReadTag));
    }
}
