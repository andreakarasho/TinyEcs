using FlatSharp;
using ModAbi;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// ABI v2/v3: real entity ids back to the guest (mod_spawned), the Changed query term,
// the mod_on_packet export. Drives the internal seams directly (via
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

        // Leave tick 0 behind so "sinceTick 0" really does mean "everything".
        world.Update();
        var stale = world.Entity().ID;
        world.Set(stale, new WitPos { X = 1 });

        // The window is EXCLUSIVE at the bottom — (since, now] — so `since` is
        // the tick the stale write already carries, and the fresh write below
        // has to land on a strictly newer one.
        var since = world.CurrentTick;
        world.Update();

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
        var since = world.CurrentTick;
        world.Update(); // the write below lands on a strictly newer tick

        world.Set(a, new WitPos { X = 9 }); // only `a` is dirty now

        // Driver is the presence term; the Changed term filters the candidates.
        var q = Query(("test/tag", ModQueryTermKind.With), ("test/pos", ModQueryTermKind.Changed));
        var snap = ModdingPlugin.BuildSnapshot(ctx, q, since, out var matched);
        Assert.Equal(1, matched);
        Assert.Equal(a, snap[0]);
    }

    // A projection: not one column, so the snapshot evaluates it per entity.
    private sealed class OddX : IModComponent
    {
        public bool Has(World world, ulong entity) => world.Has<WitPos>(entity) && world.Get<WitPos>(entity).X % 2 == 1;
        public void CollectEntities(World world, ref TinyEcs.Collections.PooledList<ulong> into)
        {
            var it = world.QueryBuilder().With<WitPos>().Build().Iter();
            while (it.Next())
                foreach (var e in it.Entities())
                    if (Has(world, e.ID))
                        into.Add(e.ID);
        }
        public string GetJson(World world, ulong entity) => "{}";
        public void SetJson(World world, ulong entity, string json) { }
        public void Remove(World world, ulong entity) { }
        public void RegisterInsertObserver(App app, Action<ulong, string> onFire) { }
        public void RegisterRemoveObserver(App app, Action<ulong, string> onFire) { }
    }

    [Fact]
    public void Archetype_snapshot_combines_column_terms_ticks_and_projection_terms()
    {
        using var world = new World();
        var ctx = ChangedCtx(world);
        ctx.Registry.Register("test/tag", new ModPresence<WitTag>());
        ctx.Registry.Register("test/odd", new OddX());

        world.Update();
        var tagged = new ulong[4];
        for (var i = 0; i < 4; i++)
        {
            tagged[i] = world.Entity().ID;
            world.Set(tagged[i], new WitPos { X = i });
            world.Set(tagged[i], new WitTag());
        }
        var untagged = world.Entity().ID;
        world.Set(untagged, new WitPos { X = 1 });
        var since = world.CurrentTick;
        world.Update();
        world.Set(tagged[1], new WitPos { X = 5 });   // odd, changed, tagged
        world.Set(tagged[2], new WitPos { X = 2 });   // even, changed, tagged
        world.Set(untagged, new WitPos { X = 3 });    // odd, changed, NOT tagged

        // With tag + Changed pos (columns) + a projection term.
        var q = Query(("test/tag", ModQueryTermKind.With), ("test/pos", ModQueryTermKind.Changed), ("test/odd", ModQueryTermKind.With));
        var snap = ModdingPlugin.BuildSnapshot(ctx, q, since, out var matched);
        Assert.Equal(1, matched);
        Assert.Equal(tagged[1], snap[0]);

        // Without on a column term.
        var q2 = Query(("test/pos", ModQueryTermKind.Changed), ("test/tag", ModQueryTermKind.Without));
        snap = ModdingPlugin.BuildSnapshot(ctx, q2, since, out matched);
        Assert.Equal(1, matched);
        Assert.Equal(untagged, snap[0]);

        // A required term naming nothing registered matches nothing.
        var q3 = Query(("test/tag", ModQueryTermKind.With), ("test/nope", ModQueryTermKind.Ref));
        _ = ModdingPlugin.BuildSnapshot(ctx, q3, 0, out matched);
        Assert.Equal(0, matched);
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
    public void Handshake_stamps_abi_version_3()
    {
        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry() };
        var exec = new ScriptedExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply()),
        };

        new ModAbiRunner(exec, 0, new CoreModState(), ctx).Setup();

        var hs = Handshake.Serializer.Parse(exec.HandshakeBytes!);
        Assert.Equal(3u, hs.AbiVersion);
    }

    // ── mod_on_packet through the runner ────────────────────────────────────────

    [Fact]
    public void OnPacket_passes_the_executor_verdict_and_replacement_through()
    {
        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry() };
        var exec = new ScriptedExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply()),
            PacketVerdict = ModPacketVerdict.Replace,
            PacketReplacement = new byte[] { 0x22, 0x01 },
        };
        var runner = new ModAbiRunner(exec, 0, new CoreModState(), ctx);
        runner.Setup();

        var verdict = runner.OnPacket(ModPacketDirection.Outgoing, new byte[] { 0x22, 0x00 }, out var replacement);

        Assert.Equal(ModPacketVerdict.Replace, verdict);
        Assert.Equal(new byte[] { 0x22, 0x01 }, replacement.ToArray());
        Assert.Equal(ModPacketDirection.Outgoing, exec.LastPacketDir);
        Assert.Equal(new byte[] { 0x22, 0x00 }, exec.LastPacket);
    }

    [Fact]
    public void OnPacket_defaults_to_pass_for_an_executor_without_the_export()
    {
        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry() };
        IModWasmExecutor exec = new BareExecutor();
        Assert.Equal(ModPacketVerdict.Pass, exec.CallOnPacket(0, ModPacketDirection.Incoming, new byte[] { 1 }, out var r));
        Assert.True(r.IsEmpty);
    }

    private sealed class BareExecutor : IModWasmExecutor
    {
        public int Load(in ModSource source, int slot, IModImportSink sink, string importModule, IReadOnlyList<ModHostImport> hostImports) => slot;
        public Memory<byte> CallSetup(int handle, ReadOnlySpan<byte> handshake) => default;
        public Memory<byte> CallRun(int handle, uint sysId, ReadOnlySpan<byte> input) => default;
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
        public ModPacketVerdict PacketVerdict;
        public byte[] PacketReplacement = System.Array.Empty<byte>();
        public ModPacketDirection LastPacketDir;
        public byte[]? LastPacket;
        public ModPacketVerdict CallOnPacket(int handle, ModPacketDirection dir, ReadOnlySpan<byte> data, out ReadOnlySpan<byte> replacement)
        {
            LastPacketDir = dir;
            LastPacket = data.ToArray();
            replacement = PacketReplacement;
            return PacketVerdict;
        }
        public void CallSpawned(int handle, ReadOnlySpan<byte> input) => SpawnedBytes = input.ToArray();
        public void Reload(int handle, in ModSource source) { }
        public void DisposeInstance(int handle) { }
        public void Dispose() { }
    }
}
