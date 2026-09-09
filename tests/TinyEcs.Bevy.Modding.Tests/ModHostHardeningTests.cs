using System.Text.Json.Serialization;
using FlatSharp;
using ModAbi;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Host hardening: what happens when a mod misbehaves (a trapping system, a malformed
// command buffer, a query term naming nothing) or when the host switches it off. Drives
// the internal seams directly (InternalsVisibleTo) with a canned executor — no wasm
// runtime, no game glue. The limits that need a real guest (epoch deadlines) and the
// host-side packet-filter gate are covered by the parent repo's tests.
public struct HardFlag { public bool On; }

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(HardFlag))]
internal partial class HardeningJsonContext : JsonSerializerContext { }

public class ModHostHardeningTests
{
    // ── a disabled mod is really off ─────────────────────────────────────────────

    [Fact]
    public void Disabled_mod_gets_no_observer_call_and_its_queue_drains()
    {
        var app = new App(ThreadingMode.Single);
        app.AddResource(new ModdingConfig()); // no mods on disk; the plugin still wires
        app.AddPlugin<ModdingPlugin>();
        app.RunStartup();

        var ctx = new ModHostContext { World = app.GetWorld(), Registry = new ModComponentRegistry(), App = app };
        var instance = new RecordingInstance();
        var rt = new ModRuntime
        {
            Manifest = new ModManifest { Name = "off" },
            Instance = instance,
            Ctx = ctx,
            Enabled = false,
        };
        rt.ObserverFires.Enqueue(("obs", 1UL, "{}"));
        app.GetResource<ModRuntimes>().Runtimes.Add(rt);

        app.Update();

        Assert.Equal(0, instance.ObserverCalls);
        Assert.Empty(rt.ObserverFires); // dropped, not buffered until re-enable

        // Control: enabled, the same fire reaches the guest.
        rt.Enabled = true;
        rt.ObserverFires.Enqueue(("obs", 1UL, "{}"));
        app.Update();
        Assert.Equal(1, instance.ObserverCalls);
    }

    // ── a trapping mod is switched off instead of retried forever ────────────────

    [Fact]
    public void Ten_consecutive_failures_auto_disable_the_mod()
    {
        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry() };
        ctx.SystemsByStage[ModSchedule.Update] = new List<ModSystemSpec>
        {
            new() { Name = "boom", Stage = ModSchedule.Update },
        };

        var info = new ModInfo { Name = "bad", Enabled = true };
        var rt = new ModRuntime
        {
            Manifest = new ModManifest { Name = "bad" },
            Instance = new ThrowingInstance(),
            Ctx = ctx,
            Info = info,
        };
        rt.ObserverFires.Enqueue(("obs", 1UL, "{}"));

        for (var i = 0; i < ModdingPlugin.MaxModFailures - 1; i++)
            ModdingPlugin.RunSystemsForStage(rt, ModSchedule.Update);
        Assert.True(rt.Enabled);

        ModdingPlugin.RunSystemsForStage(rt, ModSchedule.Update);

        Assert.False(rt.Enabled);
        Assert.False(info.Enabled);
        Assert.Equal(ModdingPlugin.MaxModFailures, rt.FailureCount);
        Assert.Empty(rt.ObserverFires);
        Assert.Contains("guest trap", rt.LastError);
        Assert.Contains("guest trap", info.LastError);
    }

    // ── an unregistered query term fails setup instead of shifting the rows ──────

    [Fact]
    public void Setup_fails_naming_the_system_when_a_query_term_type_is_unregistered()
    {
        using var world = new World();
        var reg = new ModComponentRegistry();
        reg.Register("test/flag", new ModComponent<HardFlag>(HardeningJsonContext.Default.HardFlag));
        var ctx = new ModHostContext { World = world, Registry = reg, Name = "ghostmod" };

        var exec = new CannedExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
            {
                Systems = new List<SystemDecl>
                {
                    new()
                    {
                        Id = 0,
                        Name = "reads_a_ghost",
                        Schedule = Schedule.Update,
                        Params = new List<ParamDecl>
                        {
                            new()
                            {
                                Kind = ParamKind.Query,
                                Query = new QueryDecl
                                {
                                    Terms = new List<QueryTerm>
                                    {
                                        new() { Kind = QueryTermKind.Ref, TypeId = 0 },      // test/flag
                                        new() { Kind = QueryTermKind.Ref, TypeId = 4242 },   // nothing
                                    },
                                },
                            },
                        },
                    },
                },
            }),
        };

        var e = Assert.Throws<InvalidOperationException>(
            () => new ModAbiRunner(exec, 0, new CoreModState(), ctx).Setup());
        Assert.Contains("reads_a_ghost", e.Message);
        Assert.Contains("#1", e.Message);
        Assert.Empty(ctx.Systems); // the mod registered nothing -> the loader skips it
    }

    // ── one bad command doesn't take the rest of the buffer with it ──────────────

    [Fact]
    public void A_failing_command_does_not_drop_the_commands_after_it()
    {
        using var world = new World();
        var reg = new ModComponentRegistry();
        reg.Register("test/flag", new ModComponent<HardFlag>(HardeningJsonContext.Default.HardFlag));
        var ctx = new ModHostContext { World = world, Registry = reg, Name = "sloppy" };
        var victim = world.Entity().ID;

        var exec = new CannedExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
            {
                Systems = new List<SystemDecl>
                {
                    new()
                    {
                        Id = 0,
                        Name = "spawner",
                        Schedule = Schedule.Update,
                        Params = new List<ParamDecl> { new() { Kind = ParamKind.Commands } },
                    },
                },
            }),
            RunReplyBytes = Bytes(CommandBuffer.Serializer, new CommandBuffer
            {
                Cmds = new List<Cmd>
                {
                    new(new SpawnCmd { TempId = 0 }),
                    // Registered component, unparseable payload -> SetJson throws.
                    new(new InsertCmd
                    {
                        Entity = (long)victim,
                        Comps = new List<CompValue>
                        {
                            new()
                            {
                                TypeId = 0,
                                Encoding = ModAbi.Encoding.Json,
                                Data = System.Text.Encoding.UTF8.GetBytes("{ this is not json"),
                            },
                        },
                    }),
                    new(new SpawnCmd { TempId = 1 }),
                },
            }),
        };

        var runner = new ModAbiRunner(exec, 0, new CoreModState(), ctx);
        runner.Setup();
        runner.RunSystem(ctx.Systems[0]);

        Assert.False(world.Has<HardFlag>(victim)); // cmd #1 was rejected
        var spawned = SpawnedInput.Serializer.Parse(exec.SpawnedBytes!);
        Assert.Equal(2, spawned.Spawned!.Count); // cmd #2 still ran
        Assert.Equal(1u, spawned.Spawned[1].TempId);
    }

    // ── read-only resources ──────────────────────────────────────────────────────

    [Fact]
    public void Write_to_a_read_only_resource_is_ignored()
    {
        var app = new App();
        app.AddResource(new WitScore { Value = 10 });

        var reg = new ModComponentRegistry();
        reg.RegisterResource("test/ro", new ModResource<WitScore>(WitJsonContext.Default.WitScore, readOnly: true));
        reg.RegisterResource("test/rw", new ModResource<WitScore>(WitJsonContext.Default.WitScore));

        Assert.True(reg.TryGetResource("test/ro", out var ro));
        ro.SetJson(app, "{\"Value\":42}");
        Assert.Equal(10, app.GetResource<WitScore>().Value);
        Assert.Contains("\"Value\":10", ro.GetJson(app)); // reads still work

        Assert.True(reg.TryGetResource("test/rw", out var rw));
        rw.SetJson(app, "{\"Value\":42}");
        Assert.Equal(42, app.GetResource<WitScore>().Value);
    }

    // ── tag probing: a one-byte struct with a real field is NOT a tag ────────────

    [Fact]
    public void Single_bool_field_struct_round_trips_instead_of_being_read_as_a_tag()
    {
        using var world = new World();
        var comp = (IModComponent)new ModComponent<HardFlag>(HardeningJsonContext.Default.HardFlag);

        var a = world.Entity().ID;
        world.Set(a, new HardFlag { On = true });
        Assert.Equal("{\"On\":true}", comp.GetJson(world, a));

        var b = world.Entity().ID;
        comp.SetJson(world, b, "{\"On\":true}");
        Assert.True(world.Get<HardFlag>(b).On);

        // ...and a truly fieldless struct still is one (World.Get would panic on it).
        Assert.Equal("{}", ((IModComponent)new ModComponent<WitTag>(WitJsonContext.Default.WitTag))
            .GetJson(world, world.Entity().Set(new WitTag()).ID));
    }

    // ── fixtures ─────────────────────────────────────────────────────────────────

    private static byte[] Bytes<T>(ISerializer<T> serializer, T value) where T : class
    {
        var buf = new byte[serializer.GetMaxSize(value)];
        var n = serializer.Write(buf, value);
        return buf.AsSpan(0, n).ToArray();
    }

    private sealed class RecordingInstance : IModInstance
    {
        public int ObserverCalls;
        public void Setup() { }
        public void RunSystem(ModSystemSpec sys) { }
        public void CallObserver(string export, ulong entity, string json) => ObserverCalls++;
        public bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data) => false;
        public void Reload(in ModSource source) { }
        public void Dispose() { }
    }

    private sealed class ThrowingInstance : IModInstance
    {
        public void Setup() { }
        public void RunSystem(ModSystemSpec sys) => throw new InvalidOperationException("guest trap");
        public void CallObserver(string export, ulong entity, string json) => throw new InvalidOperationException("guest trap");
        public bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data) => false;
        public void Reload(in ModSource source) { }
        public void Dispose() { }
    }

    // Canned guest: replays fixed reply bytes and records the mod_spawned push.
    private sealed class CannedExecutor : IModWasmExecutor
    {
        public byte[]? SetupReplyBytes;
        public byte[]? RunReplyBytes;
        public byte[]? SpawnedBytes;

        public int Load(in ModSource source, int slot, IModImportSink sink, string importModule, IReadOnlyList<ModHostImport> hostImports) => slot;
        public Memory<byte> CallSetup(int handle, ReadOnlySpan<byte> handshake) => SetupReplyBytes;
        public Memory<byte> CallRun(int handle, uint sysId, ReadOnlySpan<byte> input) => RunReplyBytes;
        public Memory<byte> CallObserver(int handle, uint obsId, ulong entity, ReadOnlySpan<byte> input) => default;
        public bool CallFilter(int handle, byte arg, ReadOnlySpan<byte> data) => false;
        public void CallSpawned(int handle, ReadOnlySpan<byte> input) => SpawnedBytes = input.ToArray();
        public void Reload(int handle, in ModSource source) { }
        public void DisposeInstance(int handle) { }
        public void Dispose() { }
    }
}
