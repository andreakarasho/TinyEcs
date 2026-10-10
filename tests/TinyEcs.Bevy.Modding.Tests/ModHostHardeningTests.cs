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
        rt.ObserverFires.Enqueue("obs", 1UL, ReadOnlySpan<byte>.Empty);
        app.GetResource<ModRuntimes>().Runtimes.Add(rt);

        app.Update();

        Assert.Equal(0, instance.ObserverCalls);
        Assert.Equal(0, rt.ObserverFires.Count); // dropped, not buffered until re-enable

        // Control: enabled, the same fire reaches the guest.
        rt.Enabled = true;
        rt.ObserverFires.Enqueue("obs", 1UL, ReadOnlySpan<byte>.Empty);
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
        rt.ObserverFires.Enqueue("obs", 1UL, ReadOnlySpan<byte>.Empty);

        for (var i = 0; i < ModdingPlugin.MaxModFailures - 1; i++)
            ModdingPlugin.RunSystemsForStage(rt, ModSchedule.Update);
        Assert.True(rt.Enabled);

        ModdingPlugin.RunSystemsForStage(rt, ModSchedule.Update);

        Assert.False(rt.Enabled);
        Assert.False(info.Enabled);
        Assert.Equal(ModdingPlugin.MaxModFailures, rt.FailureCount);
        Assert.Equal(0, rt.ObserverFires.Count);
        Assert.Contains("guest trap", rt.LastError);
        Assert.Contains("guest trap", info.LastError);
    }

    // ── an unregistered query term fails setup instead of shifting the rows ──────

    [Fact]
    public void Setup_fails_naming_the_system_when_a_query_term_type_is_unregistered()
    {
        using var world = new World();
        var reg = new ModComponentRegistry().Flag();
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
            () => new ModAbiRunner(exec, 0, new ModRelayState(), ctx).Setup());
        Assert.Contains("reads_a_ghost", e.Message);
        Assert.Contains("#1", e.Message);
        Assert.Empty(ctx.Systems); // the mod registered nothing -> the loader skips it
    }

    // ── a Mut term on a read-only component fails setup, naming the path ─────────

    private static SetupReply MutOn(ushort typeId, bool asObserver)
    {
        var query = new ParamDecl
        {
            Kind = ParamKind.Query,
            Query = new QueryDecl { Terms = new List<QueryTerm> { new() { Kind = QueryTermKind.Mut, TypeId = typeId } } },
        };
        return asObserver
            ? new SetupReply
            {
                Observers = new List<ObserverDecl>
                {
                    new() { Id = 7, Kind = ObserverKind.Insert, TypeId = 0, Params = new List<ParamDecl> { query } },
                },
            }
            : new SetupReply
            {
                Systems = new List<SystemDecl>
                {
                    new() { Id = 0, Name = "writes_flag", Schedule = Schedule.Update, Params = new List<ParamDecl> { query } },
                },
            };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Setup_fails_when_a_mut_term_names_a_read_only_component(bool asObserver)
    {
        using var world = new World();
        var reg = new ModComponentRegistry().Flag(readOnly: true);
        var ctx = new ModHostContext { World = world, Registry = reg, Name = "rwmod" };
        var exec = new CannedExecutor { SetupReplyBytes = Bytes(SetupReply.Serializer, MutOn(0, asObserver)) };

        var e = Assert.Throws<InvalidOperationException>(
            () => new ModAbiRunner(exec, 0, new ModRelayState(), ctx).Setup());
        Assert.Contains("mod 'rwmod'", e.Message);
        Assert.Contains(asObserver ? "observer 7" : "writes_flag", e.Message);
        Assert.Contains("declares Mut on read-only 'test/flag'", e.Message);
        Assert.Empty(ctx.Systems);
        Assert.Empty(ctx.Observers);
    }

    [Fact]
    public void A_mut_term_on_a_writable_component_still_sets_up()
    {
        using var world = new World();
        var reg = new ModComponentRegistry().Flag();
        var ctx = new ModHostContext { World = world, Registry = reg, Name = "rwmod" };
        var exec = new CannedExecutor { SetupReplyBytes = Bytes(SetupReply.Serializer, MutOn(0, asObserver: false)) };

        new ModAbiRunner(exec, 0, new ModRelayState(), ctx).Setup();

        Assert.Single(ctx.Systems);
    }

    [Fact]
    public void Presence_mappers_are_read_only()
        => Assert.True(((IModComponent)new ModPresence<HardFlag>()).ReadOnly);

    // ── one bad command doesn't take the rest of the buffer with it ──────────────

    [Fact]
    public void A_failing_command_does_not_drop_the_commands_after_it()
    {
        using var world = new World();
        var reg = new ModComponentRegistry().Flag();
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
                    // Registered component, truncated payload (a bool needs 1 byte) -> the
                    // codec throws.
                    new(new InsertCmd
                    {
                        Entity = (long)victim,
                        Comps = new List<CompValue> { new() { TypeId = 0, Encoding = ModAbi.Encoding.Typed, Data = Array.Empty<byte>() } },
                    }),
                    new(new SpawnCmd { TempId = 1 }),
                },
            }),
        };

        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
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

        var reg = new ModComponentRegistry()
            .Resource("test/ro", new ModResource<WitScore>(readOnly: true), Codecs.WriteScore, Codecs.ReadScore)
            .Resource("test/rw", new ModResource<WitScore>(), Codecs.WriteScore, Codecs.ReadScore);

        var ro = reg.BinaryResource("test/ro");
        Assert.True(ro.ReadOnly);
        ro.Set(app, Bin.S32(42), "m");
        Assert.Equal(10, app.GetResource<WitScore>().Value);
        var read = new System.Buffers.ArrayBufferWriter<byte>();
        Assert.True(ro.TryWrite(app, "m", read)); // reads still work
        Assert.Equal(Bin.S32(10), read.WrittenSpan.ToArray());

        var rw = reg.BinaryResource("test/rw");
        Assert.False(rw.ReadOnly);
        rw.Set(app, Bin.S32(42), "m");
        Assert.Equal(42, app.GetResource<WitScore>().Value);
    }

    // ── tag probing: a one-byte struct with a real field is NOT a tag ────────────

    [Fact]
    public void Single_bool_field_struct_round_trips_instead_of_being_read_as_a_tag()
    {
        using var world = new World();
        var codec = new ModComponentRegistry().Flag().Binary("test/flag");

        var a = world.Entity().ID;
        world.Set(a, new HardFlag { On = true });
        var w = new System.Buffers.ArrayBufferWriter<byte>();
        Assert.True(codec.TryWrite(world, a, w));
        Assert.Equal(new byte[] { 1 }, w.WrittenSpan.ToArray());

        var b = world.Entity().ID;
        codec.Set(world, b, [1], null);
        Assert.True(world.Get<HardFlag>(b).On);

        // ...and a truly fieldless struct still is one (World.Get would panic on it): its
        // value is empty bytes.
        var tag = new ModComponentRegistry().Tag<WitTag>("test/tag", new ModComponent<WitTag>()).Binary("test/tag");
        var t = world.Entity().Set(new WitTag()).ID;
        var tw = new System.Buffers.ArrayBufferWriter<byte>();
        Assert.True(tag.TryWrite(world, t, tw));
        Assert.Equal(0, tw.WrittenCount);
        var u = world.Entity().ID;
        tag.Set(world, u, [], null);
        Assert.True(world.Has<WitTag>(u));
    }

    // ── stale entity ids from the guest ──────────────────────────────────────────

    // A guest's command routinely names an entity that died since its snapshot: the
    // entity commands are no-ops, never a trap. (Reads never see a dead id: both backends
    // drop dead snapshot rows before writing a column.)
    [Fact]
    public void Commands_and_reads_on_a_despawned_entity_are_no_ops_instead_of_trapping()
    {
        using var world = new World();
        var reg = new ModComponentRegistry().Flag();
        var ctx = new ModHostContext { World = world, Registry = reg, Name = "stale" };

        var parent = world.Entity().ID;
        var child = world.Entity().ID;
        world.AddChild(parent, child);
        world.Set(child, new HardFlag { On = true });
        var codec = reg.Binary("test/flag");
        Assert.True(codec.TryWrite(world, child, new System.Buffers.ArrayBufferWriter<byte>()));

        world.Delete(child);
        world.Delete(parent);

        var commands = new CommandsImpl(ctx);
        commands.EntityById(child).Insert([new ModPayload("test/flag", new byte[] { 0 }, codec)]);
        commands.EntityById(child).Remove(["test/flag"]);
        commands.EntityById(parent).AddChild(child, 0);
        commands.EntityById(child).Despawn();
        Assert.False(world.Exists(child));
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
        public void CallObserver(string export, ulong entity, ReadOnlySpan<byte> value) => ObserverCalls++;
        public void Reload(in ModSource source) { }
        public void Dispose() { }
    }

    private sealed class ThrowingInstance : IModInstance
    {
        public void Setup() { }
        public void RunSystem(ModSystemSpec sys) => throw new InvalidOperationException("guest trap");
        public void CallObserver(string export, ulong entity, ReadOnlySpan<byte> value) => throw new InvalidOperationException("guest trap");
        public void Reload(in ModSource source) { }
        public void Dispose() { }
    }

    // Canned guest: replays fixed reply bytes and records the mod_spawned push.
    private sealed class CannedExecutor : IModWasmExecutor
    {
        public byte[]? SetupReplyBytes;
        public byte[]? RunReplyBytes;
        public byte[]? SpawnedBytes;

        public int Load(in ModSource source, int slot, IModImportSink sink) => slot;
        public Memory<byte> CallSetup(int handle, ReadOnlySpan<byte> handshake) => SetupReplyBytes;
        public Memory<byte> CallRun(int handle, uint sysId, ReadOnlySpan<byte> input) => RunReplyBytes;
        public Memory<byte> CallObserver(int handle, uint obsId, ulong entity, ReadOnlySpan<byte> input) => default;
        public void CallSpawned(int handle, ReadOnlySpan<byte> input) => SpawnedBytes = input.ToArray();
        public void Reload(int handle, in ModSource source) { }
        public void DisposeInstance(int handle) { }
        public void Dispose() { }
    }
}
