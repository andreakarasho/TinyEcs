using System.Buffers;
using FlatSharp;
using ModAbi;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Encoding.Typed over the relay: the one value encoding. Every registered path is listed
// typed in the Handshake (SetupReply.typed_types is ignored), every value crosses as its
// binary encoding both ways, a tag as empty bytes; a Json-encoded value in a command
// buffer is rejected (that command only), and a path with no codec fails where it is
// used. Canned guest replies, real World.
public class ModAbiTypedTests
{
    private sealed class CaptureExecutor : IModWasmExecutor
    {
        public byte[]? SetupReplyBytes;
        public byte[]? RunReplyBytes;
        public byte[]? Handshake;
        public readonly List<byte[]> RunInputs = new();

        public int Load(in ModSource source, int slot, IModImportSink sink) => slot;
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

    private static (App App, ModHostContext Ctx, ModComponentRegistry Reg) Host()
    {
        var app = new App(ThreadingMode.Single);
        var reg = new ModComponentRegistry()
            .Component("t:tag", new ModComponent<V3Tag>(), Codecs.WriteV3Tag, Codecs.ReadV3Tag)
            .Component("t:link", new ModComponent<V3Link>(static (v, resolve) => { v.Target = resolve(v.Target); return v; }),
                Codecs.WriteV3Link, Codecs.ReadV3Link)
            .Tag<WitTag>("t:marker", new ModComponent<WitTag>())
            .Resource("t:score", new ModResource<V3Score>(), Codecs.WriteV3Score, Codecs.ReadV3Score)
            .Event<V3Ping>("t:ping", Codecs.WriteV3Ping, Codecs.ReadV3Ping);
        reg.Register("t:ping-comp", new ModComponent<V3Ping>()); // no codec
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

    private static QueryTerm Ref(ModComponentRegistry reg, string path) => new() { Kind = QueryTermKind.Ref, TypeId = Id(reg, path) };

    private static CaptureExecutor Guest(ModComponentRegistry reg, CommandBuffer? reply = null, params QueryTerm[] terms) => new()
    {
        SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
        {
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
                            Query = new QueryDecl { Terms = terms.Length > 0 ? terms.ToList() : [Ref(reg, "t:tag")] },
                        },
                        new() { Kind = ParamKind.Res, TypeId = Id(reg, "t:score") },
                        new() { Kind = ParamKind.Commands },
                    },
                },
            },
        }),
        RunReplyBytes = reply != null ? Bytes(CommandBuffer.Serializer, reply) : null,
    };

    private static CompValue Typed(ModComponentRegistry reg, string path, byte[] data)
        => new() { TypeId = Id(reg, path), Encoding = ModAbi.Encoding.Typed, Data = data };

    // Runs the one system, returning what it logged.
    private static string Run(ModAbiRunner runner, ModHostContext ctx)
    {
        var output = new StringWriter();
        var stdout = Console.Out;
        Console.SetOut(output);
        try { runner.RunSystem(ctx.Systems[0]); }
        finally { Console.SetOut(stdout); }
        return output.ToString();
    }

    [Fact]
    public void Handshake_lists_every_registered_path_typed()
    {
        var (_, ctx, reg) = Host();
        var exec = Guest(reg);
        new ModAbiRunner(exec, 0, new ModRelayState(), ctx).Setup();
        var hs = Handshake.Serializer.Parse(exec.Handshake!);
        var ids = hs.TypePaths!.Select(t => t.Id).OrderBy(x => x).ToArray();
        Assert.Equal(reg.Entries.Count(), ids.Length);
        Assert.Equal(ids, hs.TypedTypes!.OrderBy(x => x));
        Assert.Equal(Id(reg, "t:ping"), hs.TypePaths!.Single(t => t.Path == "t:ping").Id);
    }

    [Fact]
    public void Rows_and_resources_cross_typed_and_a_tag_is_empty_bytes()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        app.GetWorld().Entity().Set(new V3Tag { X = -5 }).Set(new WitTag());
        var exec = Guest(reg, null, Ref(reg, "t:tag"), Ref(reg, "t:marker"));
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        runner.RunSystem(ctx.Systems[0]);

        var input = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        var row = Assert.Single(input.Queries![0].Rows!);
        Assert.All(row.Comps!, c => Assert.Equal(ModAbi.Encoding.Typed, c.Encoding));
        Assert.Equal(Bin.S32(-5), row.Comps![0].Data!.Value.ToArray());
        Assert.Equal(Id(reg, "t:marker"), row.Comps[1].TypeId);
        Assert.Equal(0, row.Comps[1].Data?.Length ?? 0);
        Assert.Equal(ModAbi.Encoding.Typed, input.Resources![0].Value!.Encoding);
        Assert.Equal(Bin.S32(7), input.Resources[0].Value!.Data!.Value.ToArray());

        // An unchanged resource is just the flag.
        runner.RunSystem(ctx.Systems[0]);
        var second = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Null(second.Resources![0].Value);
        Assert.True(second.Resources[0].Unchanged);
    }

    [Fact]
    public void An_absent_resource_is_absent_not_bytes()
    {
        var (_, ctx, reg) = Host();
        var exec = Guest(reg);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        runner.RunSystem(ctx.Systems[0]);
        var input = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Null(input.Resources![0].Value);
        Assert.False(input.Resources[0].Unchanged);
    }

    // A reading term on a path the host gave no codec fails the run, naming the path.
    [Fact]
    public void A_reading_term_without_a_codec_fails_the_run_by_name()
    {
        var (app, ctx, reg) = Host();
        Assert.Equal(new[] { "t:ping-comp" }, reg.MissingCodecs());
        app.GetWorld().Entity().Set(new V3Ping { N = 3 });
        var exec = Guest(reg, null, Ref(reg, "t:ping-comp"));
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        var e = Assert.Throws<InvalidOperationException>(() => runner.RunSystem(ctx.Systems[0]));
        Assert.Contains("'t:ping-comp' has no binary codec", e.Message);
        Assert.Empty(exec.RunInputs);
    }

    [Fact]
    public void Typed_commands_apply_through_the_codec_and_resolve_placeholders()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 1 });
        var hostSaw = new List<int>();
        app.AddObserver<On<V3Ping>>(t => hostSaw.Add(t.Event.N));
        var reply = new CommandBuffer
        {
            Cmds = new List<Cmd>
            {
                new(new SpawnCmd { TempId = 0, Comps = new List<CompValue> { Typed(reg, "t:tag", Bin.S32(42)) } }),
                new(new SpawnCmd
                {
                    TempId = 1,
                    Comps = new List<CompValue>
                    {
                        Typed(reg, "t:link", Bin.U64(ModEntityRef.PlaceholderBit | 0UL)),
                        Typed(reg, "t:tag", Bin.S32(9)),
                        Typed(reg, "t:marker", []),
                    },
                }),
                // A path with no codec: this command fails, the rest still apply.
                new(new InsertCmd { Entity = -2, Comps = new List<CompValue> { Typed(reg, "t:ping-comp", Bin.S32(1)) } }),
                new(new ResourceSetCmd { Value = Typed(reg, "t:score", Bin.S32(77)) }),
                new(new EmitEventCmd { EventName = "t:ping", Encoding = ModAbi.Encoding.Typed, Data = Bin.S32(5) }),
            },
        };
        var exec = Guest(reg, reply);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        var log = Run(runner, ctx);

        var world = app.GetWorld();
        var tags = new Dictionary<ulong, int>();
        foreach (var (e, tag) in world.Query<Data<V3Tag>>())
            tags[e.Ref] = tag.Ref.X;
        Assert.Equal(new[] { 9, 42 }, tags.Values.OrderBy(x => x));
        var first = tags.Single(kv => kv.Value == 42).Key;
        var second = tags.Single(kv => kv.Value == 9).Key;
        Assert.Equal(first, world.Get<V3Link>(second).Target);
        Assert.True(world.Has<WitTag>(second));
        Assert.False(world.Has<V3Ping>(second));
        Assert.Contains("cmd #2 (InsertCmd) failed: component 't:ping-comp' has no binary codec", log);
        Assert.Equal(77, app.GetResource<V3Score>().Value);
        Assert.Equal(new[] { 5 }, hostSaw);
    }

    // Encoding.Json is retired: a Json value in a command buffer is rejected and logged
    // per command, and the commands after it still apply.
    [Fact]
    public void Json_encoding_in_a_command_buffer_is_rejected_and_the_next_command_still_applies()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 1 });
        var hostSaw = new List<int>();
        app.AddObserver<On<V3Ping>>(t => hostSaw.Add(t.Event.N));
        var target = app.GetWorld().Entity().ID;
        var json = "{\"X\":9}"u8.ToArray();
        var reply = new CommandBuffer
        {
            Cmds = new List<Cmd>
            {
                new(new InsertCmd { Entity = (long)target, Comps = new List<CompValue> { new() { TypeId = Id(reg, "t:tag"), Encoding = ModAbi.Encoding.Json, Data = json } } }),
                // Json is the wire default: an encoding left unset is rejected too.
                new(new ResourceSetCmd { Value = new CompValue { TypeId = Id(reg, "t:score"), Data = Bin.S32(50) } }),
                new(new EmitEventCmd { EventName = "t:ping", Encoding = ModAbi.Encoding.Json, Data = Bin.S32(4) }),
                new(new InsertCmd { Entity = (long)target, Comps = new List<CompValue> { Typed(reg, "t:tag", Bin.S32(3)) } }),
                new(new EmitEventCmd { EventName = "t:ping", Encoding = ModAbi.Encoding.Typed, Data = Bin.S32(6) }),
            },
        };
        var exec = Guest(reg, reply);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        var log = Run(runner, ctx);

        Assert.Contains("cmd #0 (InsertCmd) failed: t:tag: value encoding Json (only Typed is accepted)", log);
        Assert.Contains("cmd #1 (ResourceSetCmd) failed: t:score: value encoding Json", log);
        Assert.Contains("cmd #2 (EmitEventCmd) failed: t:ping: value encoding Json", log);
        Assert.Equal(3, app.GetWorld().Get<V3Tag>(target).X);
        Assert.Equal(1, app.GetResource<V3Score>().Value);
        Assert.Equal(new[] { 6 }, hostSaw);
    }

    // A truncated value fails its own command only (the per-command guard).
    [Fact]
    public void A_truncated_value_fails_its_command_only()
    {
        var (app, ctx, reg) = Host();
        var target = app.GetWorld().Entity().ID;
        var reply = new CommandBuffer
        {
            Cmds = new List<Cmd>
            {
                new(new InsertCmd { Entity = (long)target, Comps = new List<CompValue> { Typed(reg, "t:link", [1, 2, 3]) } }),
                new(new InsertCmd { Entity = (long)target, Comps = new List<CompValue> { Typed(reg, "t:tag", Bin.S32(8)) } }),
            },
        };
        var exec = Guest(reg, reply);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        var log = Run(runner, ctx);

        Assert.Contains("cmd #0 (InsertCmd) failed: typed value truncated", log);
        Assert.False(app.GetWorld().Has<V3Link>(target));
        Assert.Equal(8, app.GetWorld().Get<V3Tag>(target).X);
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
        reg.Register("t:tag", new ModComponent<V3Tag>());
        Assert.Throws<InvalidOperationException>(() => reg.RegisterBinaryComponent<V3Link>("t:tag", Codecs.WriteV3Link, Codecs.ReadV3Link));
        Assert.Throws<InvalidOperationException>(() => reg.RegisterBinaryComponent<V3Tag>("t:none", Codecs.WriteV3Tag, Codecs.ReadV3Tag));
        Assert.Throws<InvalidOperationException>(() => reg.RegisterBinaryResource<V3Score>("t:tag", Codecs.WriteV3Score, Codecs.ReadV3Score));
        Assert.Throws<InvalidOperationException>(() => reg.RegisterBinaryEvent<V3Ping>("t:tag", Codecs.WriteV3Ping, Codecs.ReadV3Ping));
    }

    [Fact]
    public void Binary_lookups_name_an_unregistered_path_and_a_missing_codec()
    {
        var (_, _, reg) = Host();
        Assert.Contains("unregistered component 't:nope'", Assert.Throws<InvalidOperationException>(() => reg.Binary("t:nope")).Message);
        Assert.Contains("'t:ping-comp' has no binary codec", Assert.Throws<InvalidOperationException>(() => reg.Binary("t:ping-comp")).Message);
        Assert.Contains("unregistered resource 't:nope'", Assert.Throws<InvalidOperationException>(() => reg.BinaryResource("t:nope")).Message);
        Assert.Contains("unregistered event 't:nope'", Assert.Throws<InvalidOperationException>(() => reg.BinaryEvent("t:nope")).Message);
    }
}
