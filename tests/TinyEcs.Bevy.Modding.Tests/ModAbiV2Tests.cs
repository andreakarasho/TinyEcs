using FlatSharp;
using ModAbi;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// ABI v2 additions: real entity ids back to the guest (mod_spawned), the Changed query
// term, and the per-system run interval. Drives the internal seams directly (via
// InternalsVisibleTo) — no wasm runtime, no game glue.
public class ModAbiV2Tests
{
    // ── idle-skip policy ────────────────────────────────────────────────────────

    [Fact]
    public void ShouldSkipIdle_runs_the_transition_tick_then_every_Nth()
    {
        var sys = new ModSystemSpec();

        // A query-less (Commands-only) system never skips — no signal to gate on.
        Assert.False(ModdingPlugin.ShouldSkipIdle(sys, hasQuery: false, anyRows: false));

        // First all-empty tick still runs (the guest sees one empty result set).
        Assert.False(ModdingPlugin.ShouldSkipIdle(sys, hasQuery: true, anyRows: false));
        // Then it skips until the safety period comes round.
        for (var i = 2; i < ModdingPlugin.IdleSafetyRunPeriod; i++)
            Assert.True(ModdingPlugin.ShouldSkipIdle(sys, hasQuery: true, anyRows: false));
        Assert.False(ModdingPlugin.ShouldSkipIdle(sys, hasQuery: true, anyRows: false));

        // Any row resets the streak, so the next empty tick runs again.
        Assert.False(ModdingPlugin.ShouldSkipIdle(sys, hasQuery: true, anyRows: true));
        Assert.Equal(0, sys.EmptyStreak);
        Assert.False(ModdingPlugin.ShouldSkipIdle(sys, hasQuery: true, anyRows: false));
    }

    // ── per-system run interval ─────────────────────────────────────────────────

    [Fact]
    public void Interval_gate_throttles_by_host_time_and_zero_means_every_tick()
    {
        var app = new App(ThreadingMode.Single);
        app.AddResource(new Time());

        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry(), App = app };

        var throttled = new ModSystemSpec { Name = "throttled", Stage = ModSchedule.Update, IntervalMs = 100 };
        var everyTick = new ModSystemSpec { Name = "every", Stage = ModSchedule.Update };
        ctx.SystemsByStage[ModSchedule.Update] = new List<ModSystemSpec> { throttled, everyTick };

        var instance = new CountingInstance();
        var rt = new ModRuntime { Manifest = new ModManifest(), Instance = instance, Ctx = ctx };

        // t = 0: both run (LastRunTime starts at -inf so the first tick is never gated).
        ModdingPlugin.RunSystemsForStage(rt, ModSchedule.Update);
        Assert.Equal(1, instance.Runs["throttled"]);
        Assert.Equal(1, instance.Runs["every"]);

        // t = 50ms: inside the window — only the ungated system runs.
        app.GetResourceRef<Time>().Total = 50f;
        ModdingPlugin.RunSystemsForStage(rt, ModSchedule.Update);
        Assert.Equal(1, instance.Runs["throttled"]);
        Assert.Equal(2, instance.Runs["every"]);

        // t = 100ms: window elapsed.
        app.GetResourceRef<Time>().Total = 100f;
        ModdingPlugin.RunSystemsForStage(rt, ModSchedule.Update);
        Assert.Equal(2, instance.Runs["throttled"]);
        Assert.Equal(3, instance.Runs["every"]);
    }

    private sealed class CountingInstance : IModInstance
    {
        public readonly Dictionary<string, int> Runs = new();
        public void Setup() { }
        public void RunSystem(ModSystemSpec sys) =>
            Runs[sys.Name] = Runs.TryGetValue(sys.Name, out var n) ? n + 1 : 1;
        public void CallObserver(string export, ulong entity, string json) { }
        public bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data) => false;
        public void Reload(in ModSource source) { }
        public void Dispose() { }
    }

    // ── Changed query term ──────────────────────────────────────────────────────

    private static ModHostContext ChangedCtx(World world)
    {
        var reg = new ModComponentRegistry();
        reg.Register("test/pos", new ModComponent<WitPos>(WitJsonContext.Default.WitPos));
        return new ModHostContext { World = world, Registry = reg };
    }

    private static ModQuerySpec Query(params (string Path, ModQueryTermKind Kind)[] terms)
    {
        var si = new SystemImpl("q");
        var built = new ModQueryTerm[terms.Length];
        for (var i = 0; i < terms.Length; i++)
            built[i] = new ModQueryTerm(terms[i].Kind, terms[i].Path);
        si.AddQuery(built);
        return si.Spec.Params[0].Query!;
    }

    [Fact]
    public void Changed_driver_term_matches_only_entities_written_since_the_tick()
    {
        using var world = new World();
        var ctx = ChangedCtx(world);

        var stale = world.Entity().ID;
        world.Set(stale, new WitPos { X = 1 });
        var since = world.Update(); // everything written above is now "before since"

        var fresh = world.Entity().ID;
        world.Set(fresh, new WitPos { X = 2 });

        var q = Query(("test/pos", ModQueryTermKind.Changed));
        var snap = ModdingPlugin.BuildSnapshot(ctx, q, since, out var matched);
        Assert.Equal(1, matched);
        Assert.Equal(fresh, snap[0]);

        // sinceTick 0 = "everything" — both rows come back.
        _ = ModdingPlugin.BuildSnapshot(ctx, q, 0, out var all);
        Assert.Equal(2, all);
    }

    [Fact]
    public void Changed_is_a_read_term_too_so_the_row_carries_the_component()
    {
        var q = Query(("test/pos", ModQueryTermKind.Changed), ("test/other", ModQueryTermKind.With));
        Assert.Equal(2, q.Terms.Count);
        // Only Changed lands in Components (With is filter-only), and it is not mutable.
        Assert.Single(q.Components);
        Assert.Equal("test/pos", q.Components[0].typePath);
        Assert.False(q.Components[0].mut);
    }

    [Fact]
    public void Non_driver_Changed_term_filters_a_presence_driver()
    {
        using var world = new World();
        var ctx = ChangedCtx(world);
        ctx.Registry.Register("test/tag", new ModPresence<WitTag>());

        var a = world.Entity().ID;
        var b = world.Entity().ID;
        world.Set(a, new WitPos { X = 1 });
        world.Set(b, new WitPos { X = 1 });
        world.Set(a, new WitTag());
        world.Set(b, new WitTag());
        var since = world.Update();

        world.Set(a, new WitPos { X = 9 }); // only `a` is dirty now

        // Driver is the presence term; the Changed term filters the candidates.
        var q = Query(("test/tag", ModQueryTermKind.With), ("test/pos", ModQueryTermKind.Changed));
        var snap = ModdingPlugin.BuildSnapshot(ctx, q, since, out var matched);
        Assert.Equal(1, matched);
        Assert.Equal(a, snap[0]);
    }

    [Fact]
    public void ModPresence_degrades_Changed_to_presence_via_the_interface_default()
    {
        using var world = new World();
        var id = world.Entity().ID;
        world.Set(id, new WitTag());

        var comp = (IModComponent)new ModPresence<WitTag>();
        // A tag carries no column tick, so the default keeps it matching on presence.
        Assert.True(comp.ChangedSince(world, id, world.CurrentTick + 1000));
    }

    [Fact]
    public void World_GetChangedTick_tracks_writes_and_is_zero_for_absent()
    {
        using var world = new World();
        var id = world.Entity().ID;
        Assert.Equal(0u, world.GetChangedTick<WitPos>(id));

        world.Set(id, new WitPos { X = 1 });
        var first = world.GetChangedTick<WitPos>(id);
        var t = world.Update();
        world.Set(id, new WitPos { X = 2 });
        Assert.True(world.GetChangedTick<WitPos>(id) > first);
        Assert.True(world.GetChangedTick<WitPos>(id) >= t);
    }

    // ── mod_spawned: real ids back to the guest ─────────────────────────────────

    [Fact]
    public void ApplyCommandBuffer_pushes_resolved_spawn_ids_back_through_mod_spawned()
    {
        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry() };
        var exec = new ScriptedExecutor
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
                    new(new SpawnCmd { TempId = 1 }),
                },
            }),
        };

        var runner = new ModAbiRunner(exec, 0, new CoreModState(), ctx);
        runner.Setup();
        Assert.Single(ctx.Systems);

        runner.RunSystem(ctx.Systems[0]);

        Assert.NotNull(exec.SpawnedBytes);
        var spawned = SpawnedInput.Serializer.Parse(exec.SpawnedBytes!);
        Assert.Equal(2, spawned.Spawned!.Count);
        Assert.Equal(0u, spawned.Spawned[0].TempId);
        Assert.Equal(1u, spawned.Spawned[1].TempId);
        Assert.True(world.Exists(spawned.Spawned[0].Entity));
        Assert.True(world.Exists(spawned.Spawned[1].Entity));
        Assert.NotEqual(spawned.Spawned[0].Entity, spawned.Spawned[1].Entity);
    }

    [Fact]
    public void No_spawn_means_no_mod_spawned_call()
    {
        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry() };
        var target = world.Entity().ID;
        var exec = new ScriptedExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
            {
                Systems = new List<SystemDecl>
                {
                    new()
                    {
                        Id = 0,
                        Name = "despawner",
                        Schedule = Schedule.Update,
                        Params = new List<ParamDecl> { new() { Kind = ParamKind.Commands } },
                    },
                },
            }),
            RunReplyBytes = Bytes(CommandBuffer.Serializer, new CommandBuffer
            {
                Cmds = new List<Cmd> { new(new DespawnCmd { Entity = (long)target }) },
            }),
        };

        var runner = new ModAbiRunner(exec, 0, new CoreModState(), ctx);
        runner.Setup();
        runner.RunSystem(ctx.Systems[0]);

        Assert.Null(exec.SpawnedBytes);
        Assert.False(world.Exists(target));
    }

    [Fact]
    public void SystemDecl_interval_ms_reaches_the_spec()
    {
        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry() };
        var exec = new ScriptedExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
            {
                Systems = new List<SystemDecl>
                {
                    new() { Id = 0, Name = "slow", Schedule = Schedule.Update, IntervalMs = 500 },
                },
            }),
        };

        new ModAbiRunner(exec, 0, new CoreModState(), ctx).Setup();
        Assert.Equal(500u, ctx.Systems[0].IntervalMs);
    }

    [Fact]
    public void Handshake_stamps_abi_version_2()
    {
        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry() };
        var exec = new ScriptedExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply()),
        };

        new ModAbiRunner(exec, 0, new CoreModState(), ctx).Setup();

        var hs = Handshake.Serializer.Parse(exec.HandshakeBytes!);
        Assert.Equal(2u, hs.AbiVersion);
    }

    private static byte[] Bytes<T>(ISerializer<T> serializer, T value) where T : class
    {
        var buf = new byte[serializer.GetMaxSize(value)];
        var n = serializer.Write(buf, value);
        return buf.AsSpan(0, n).ToArray();
    }

    // Canned guest: replays fixed reply bytes and records the mod_spawned push.
    private sealed class ScriptedExecutor : IModWasmExecutor
    {
        public byte[]? SetupReplyBytes;
        public byte[]? RunReplyBytes;
        public byte[]? SpawnedBytes;
        public byte[]? HandshakeBytes;

        public int Load(in ModSource source, int slot, IModImportSink sink, string importModule, IReadOnlyList<ModHostImport> hostImports) => slot;
        public Memory<byte> CallSetup(int handle, ReadOnlySpan<byte> handshake)
        {
            HandshakeBytes = handshake.ToArray();
            return SetupReplyBytes;
        }
        public Memory<byte> CallRun(int handle, uint sysId, ReadOnlySpan<byte> input) => RunReplyBytes;
        public Memory<byte> CallObserver(int handle, uint obsId, ulong entity, ReadOnlySpan<byte> input) => default;
        public bool CallFilter(int handle, byte arg, ReadOnlySpan<byte> data) => false;
        public void CallSpawned(int handle, ReadOnlySpan<byte> input) => SpawnedBytes = input.ToArray();
        public void Reload(int handle, in ModSource source) { }
        public void DisposeInstance(int handle) { }
        public void Dispose() { }
    }
}
