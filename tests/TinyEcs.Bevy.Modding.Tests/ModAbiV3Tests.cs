using System.Text.Json.Serialization;
using FlatSharp;
using ModAbi;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

public struct V3Score { public int Value; }
public struct V3Ping { public int N; }
public struct V3Link { public ulong Target; }
public struct V3Tag { public int X; }

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(V3Score))]
[JsonSerializable(typeof(V3Ping))]
[JsonSerializable(typeof(V3Link))]
[JsonSerializable(typeof(V3Tag))]
internal partial class V3JsonContext : JsonSerializerContext { }

// ABI v3: Res / ResMut / Events params, the Added term, observers with params, and
// placeholder entity ids in component JSON. Canned guest replies, real World.
public class ModAbiV3Tests
{
    private sealed class CaptureExecutor : IModWasmExecutor
    {
        public byte[]? SetupReplyBytes;
        public byte[]? RunReplyBytes;
        public readonly List<byte[]> RunInputs = new();
        public readonly List<byte[]> ObserverInputs = new();

        public int Load(in ModSource source, int slot, IModImportSink sink, string importModule, IReadOnlyList<ModHostImport> hostImports) => slot;
        public Memory<byte> CallSetup(int handle, ReadOnlySpan<byte> handshake) => SetupReplyBytes;
        public Memory<byte> CallRun(int handle, uint sysId, ReadOnlySpan<byte> input)
        {
            RunInputs.Add(input.ToArray());
            return RunReplyBytes;
        }
        public Memory<byte> CallObserver(int handle, uint obsId, ulong entity, ReadOnlySpan<byte> input)
        {
            ObserverInputs.Add(input.ToArray());
            return default;
        }
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
        var reg = new ModComponentRegistry();
        reg.Register("t:tag", new ModComponent<V3Tag>(V3JsonContext.Default.V3Tag));
        reg.Register("t:link", new ModComponent<V3Link>(V3JsonContext.Default.V3Link,
            static (v, resolve) => { v.Target = resolve(v.Target); return v; }));
        reg.RegisterResource("t:score", new ModResource<V3Score>(V3JsonContext.Default.V3Score));
        reg.RegisterResource("t:missing", new ModResource<V3Ping>(V3JsonContext.Default.V3Ping));
        reg.RegisterEvent("t:ping", new ModEvent<V3Ping>(V3JsonContext.Default.V3Ping));
        var ctx = new ModHostContext { World = app.GetWorld(), Registry = reg, App = app, Name = "v3" };
        return (app, ctx, reg);
    }

    // The handshake interns paths in registry order; resolve one the way a guest would.
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

    private static string Json(CompValue? v) => v?.Data is { } d ? System.Text.Encoding.UTF8.GetString(d.Span) : "<absent>";

    private static CaptureExecutor ResSystem(ModComponentRegistry reg, bool resUnchanged) => new()
    {
        SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
        {
            ResUnchanged = resUnchanged,
            Systems = new List<SystemDecl>
            {
                new()
                {
                    Id = 0, Name = "s", Schedule = Schedule.Update,
                    Params = new List<ParamDecl>
                    {
                        new() { Kind = ParamKind.Res, TypeId = Id(reg, "t:score") },
                        new() { Kind = ParamKind.Res, TypeId = Id(reg, "t:missing") },
                    },
                },
            },
        }),
    };

    [Fact]
    public void Unchanged_resource_crosses_as_a_flag_only_to_a_guest_that_keeps_it()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        var exec = ResSystem(reg, resUnchanged: true);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        runner.RunSystem(ctx.Systems[0]);
        var first = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Equal("{\"Value\":7}", Json(first.Resources![0].Value));
        Assert.False(first.Resources[0].Unchanged);
        // Absent stays absent, never "unchanged".
        Assert.Null(first.Resources[1].Value);
        Assert.False(first.Resources[1].Unchanged);

        runner.RunSystem(ctx.Systems[0]);
        var second = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Null(second.Resources![0].Value);
        Assert.True(second.Resources[0].Unchanged);
        Assert.False(second.Resources[1].Unchanged);

        app.GetResourceRef<V3Score>().Value = 8;
        runner.RunSystem(ctx.Systems[0]);
        var third = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Equal("{\"Value\":8}", Json(third.Resources![0].Value));
        Assert.False(third.Resources[0].Unchanged);
    }

    [Fact]
    public void Resources_are_always_sent_in_full_to_a_guest_that_did_not_opt_in()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        var exec = ResSystem(reg, resUnchanged: false);
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        runner.RunSystem(ctx.Systems[0]);
        runner.RunSystem(ctx.Systems[0]);
        var second = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Equal("{\"Value\":7}", Json(second.Resources![0].Value));
        Assert.False(second.Resources[0].Unchanged);
    }

    [Fact]
    public void A_failed_guest_run_resends_resources_in_full()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        var exec = new ThrowingExecutor(ResSystem(reg, resUnchanged: true));
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        exec.Throw = true;
        Assert.ThrowsAny<Exception>(() => runner.RunSystem(ctx.Systems[0]));
        exec.Throw = false;
        runner.RunSystem(ctx.Systems[0]);
        var input = SystemInput.Serializer.Parse(exec.Inner.RunInputs[^1]);
        Assert.Equal("{\"Value\":7}", Json(input.Resources![0].Value));
        Assert.False(input.Resources[0].Unchanged);
    }

    private sealed class ThrowingExecutor(CaptureExecutor inner) : IModWasmExecutor
    {
        public readonly CaptureExecutor Inner = inner;
        public bool Throw;
        public int Load(in ModSource source, int slot, IModImportSink sink, string importModule, IReadOnlyList<ModHostImport> hostImports) => slot;
        public Memory<byte> CallSetup(int handle, ReadOnlySpan<byte> handshake) => Inner.CallSetup(handle, handshake);
        public Memory<byte> CallRun(int handle, uint sysId, ReadOnlySpan<byte> input)
            => Throw ? throw new InvalidOperationException("trap") : Inner.CallRun(handle, sysId, input);
        public Memory<byte> CallObserver(int handle, uint obsId, ulong entity, ReadOnlySpan<byte> input) => default;
        public void CallSpawned(int handle, ReadOnlySpan<byte> input) { }
        public void Reload(int handle, in ModSource source) { }
        public void DisposeInstance(int handle) { }
        public void Dispose() { }
    }

    [Fact]
    public void Res_and_events_params_are_pushed_and_absent_resources_stay_absent()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        var exec = new CaptureExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
            {
                Systems = new List<SystemDecl>
                {
                    new()
                    {
                        Id = 3, Name = "s", Schedule = Schedule.Update,
                        Params = new List<ParamDecl>
                        {
                            new() { Kind = ParamKind.Res, TypeId = Id(reg, "t:score") },
                            new() { Kind = ParamKind.ResMut, TypeId = Id(reg, "t:missing") },
                            new() { Kind = ParamKind.Events, TypeId = Id(reg, "t:ping") },
                        },
                    },
                },
            }),
        };
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        app.GetWorld().EmitTrigger(0, new V3Ping { N = 1 });
        app.GetWorld().EmitTrigger(0, new V3Ping { N = 2 });
        runner.RunSystem(ctx.Systems[0]);

        var input = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Equal(2, input.Resources!.Count);
        Assert.Equal(0u, input.Resources[0].ParamIndex);
        Assert.Equal("{\"Value\":7}", Json(input.Resources[0].Value));
        Assert.Equal(1u, input.Resources[1].ParamIndex);
        Assert.Null(input.Resources[1].Value);
        var events = Assert.Single(input.Events!);
        Assert.Equal(2u, events.ParamIndex);
        Assert.Equal(new[] { "{\"N\":1}", "{\"N\":2}" }, events.Values!.Select(Json).ToArray());

        // Consumed: the next run sees no events.
        runner.RunSystem(ctx.Systems[0]);
        input = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.Empty(input.Events![0].Values ?? new List<CompValue>());
    }

    [Fact]
    public void Added_term_matches_only_entities_that_got_the_component_since_the_last_run()
    {
        var (app, ctx, reg) = Host();
        var world = app.GetWorld();
        var exec = new CaptureExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
            {
                Systems = new List<SystemDecl>
                {
                    new()
                    {
                        Id = 0, Name = "added", Schedule = Schedule.Update,
                        Params = new List<ParamDecl>
                        {
                            new()
                            {
                                Kind = ParamKind.Query,
                                Query = new QueryDecl { Terms = new List<QueryTerm> { new() { Kind = QueryTermKind.Added, TypeId = Id(reg, "t:tag") } } },
                            },
                        },
                    },
                },
            }),
        };
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        SystemTicks.Advance(world);              // a host system writes...
        var first = world.Entity().ID;
        world.Set(first, new V3Tag { X = 1 });
        SystemTicks.Advance(world);              // ...then the mod runner runs
        runner.RunSystem(ctx.Systems[0]);
        var rows = SystemInput.Serializer.Parse(exec.RunInputs[^1]).Queries![0].Rows!;
        Assert.Equal(first, Assert.Single(rows).Entity);

        SystemTicks.Advance(world);
        var second = world.Entity().ID;
        world.Set(second, new V3Tag { X = 2 });
        world.Set(first, new V3Tag { X = 3 }); // a change, not an add
        SystemTicks.Advance(world);
        runner.RunSystem(ctx.Systems[0]);
        rows = SystemInput.Serializer.Parse(exec.RunInputs[^1]).Queries![0].Rows!;
        Assert.Equal(second, Assert.Single(rows).Entity);
    }

    [Fact]
    public void Observer_params_are_evaluated_per_fire()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 9 });
        var tagged = app.GetWorld().Entity().ID;
        app.GetWorld().Set(tagged, new V3Tag { X = 5 });
        var exec = new CaptureExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
            {
                Observers = new List<ObserverDecl>
                {
                    new()
                    {
                        Id = 4, Kind = ObserverKind.Custom, TypeId = 0xFFFF, EventName = "t:ping",
                        Params = new List<ParamDecl>
                        {
                            new()
                            {
                                Kind = ParamKind.Query,
                                Query = new QueryDecl { Terms = new List<QueryTerm> { new() { Kind = QueryTermKind.Ref, TypeId = Id(reg, "t:tag") } } },
                            },
                            new() { Kind = ParamKind.Res, TypeId = Id(reg, "t:score") },
                        },
                    },
                },
            }),
        };
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        runner.CallObserver("4", 0, "{\"N\":3}");

        var input = ObserverInput.Serializer.Parse(exec.ObserverInputs[^1]);
        Assert.Equal("{\"N\":3}", Json(input.Value));
        Assert.Equal(Id(reg, "t:ping"), input.Value!.TypeId);
        var row = Assert.Single(input.Queries![0].Rows!);
        Assert.Equal(tagged, row.Entity);
        Assert.Equal("{\"X\":5}", Json(row.Comps![0]));
        Assert.Equal("{\"Value\":9}", Json(input.Resources![0].Value));
    }

    [Fact]
    public void Placeholder_entity_ids_in_component_json_resolve_to_this_buffers_spawns()
    {
        var (app, ctx, reg) = Host();
        var placeholder = ModEntityRef.PlaceholderBit | 0UL;
        var exec = new CaptureExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
            {
                Systems = new List<SystemDecl>
                {
                    new()
                    {
                        Id = 0, Name = "spawner", Schedule = Schedule.Update,
                        Params = new List<ParamDecl> { new() { Kind = ParamKind.Commands } },
                    },
                },
            }),
            RunReplyBytes = Bytes(CommandBuffer.Serializer, new CommandBuffer
            {
                Cmds = new List<Cmd>
                {
                    new(new SpawnCmd { TempId = 0 }),
                    new(new SpawnCmd
                    {
                        TempId = 1,
                        Comps = new List<CompValue>
                        {
                            new() { TypeId = Id(reg, "t:link"), Data = System.Text.Encoding.UTF8.GetBytes($"{{\"Target\":{placeholder}}}") },
                        },
                    }),
                    // A placeholder nothing in this buffer spawned resolves to 0.
                    new(new InsertCmd
                    {
                        Entity = -1, // temp 0
                        Comps = new List<CompValue>
                        {
                            new() { TypeId = Id(reg, "t:link"), Data = System.Text.Encoding.UTF8.GetBytes($"{{\"Target\":{ModEntityRef.PlaceholderBit | 9UL}}}") },
                        },
                    }),
                },
            }),
        };
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        runner.RunSystem(ctx.Systems[0]);

        var world = app.GetWorld();
        var links = new List<(ulong Entity, ulong Target)>();
        foreach (var (e, link) in world.Query<Data<V3Link>>())
            links.Add((e.Ref, link.Ref.Target));
        Assert.Equal(2, links.Count);
        var spawned0 = links.Single(l => l.Target == 0).Entity;   // the InsertCmd target (temp 0)
        var spawned1 = links.Single(l => l.Target != 0);
        Assert.Equal(spawned0, spawned1.Target);                  // temp 1 links to temp 0's real id
    }

    [Fact]
    public void A_param_naming_an_unregistered_resource_fails_setup_by_name()
    {
        var (_, ctx, reg) = Host();
        var exec = new CaptureExecutor
        {
            SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
            {
                Systems = new List<SystemDecl>
                {
                    new()
                    {
                        Id = 0, Name = "bad", Schedule = Schedule.Update,
                        // t:tag is a component, not a resource.
                        Params = new List<ParamDecl> { new() { Kind = ParamKind.Res, TypeId = Id(reg, "t:tag") } },
                    },
                },
            }),
        };
        var e = Assert.Throws<InvalidOperationException>(() => new ModAbiRunner(exec, 0, new ModRelayState(), ctx).Setup());
        Assert.Contains("bad", e.Message);
    }

    // ── run-on-change (SystemDecl.run_on_change) over the relay ──────────────────────

    private static CaptureExecutor OnChangeSystem(ModComponentRegistry reg, bool runOnChange, params ParamDecl[] ps) => new()
    {
        SetupReplyBytes = Bytes(SetupReply.Serializer, new SetupReply
        {
            ResUnchanged = true,
            Systems = new List<SystemDecl>
            {
                new() { Id = 0, Name = "s", Schedule = Schedule.Update, RunOnChange = runOnChange, Params = ps.ToList() },
            },
        }),
    };

    private static ParamDecl[] WatchParams(ModComponentRegistry reg) =>
    [
        new() { Kind = ParamKind.Res, TypeId = Id(reg, "t:score") },
        new() { Kind = ParamKind.Query, Query = new QueryDecl { Terms = new List<QueryTerm> { new() { Kind = QueryTermKind.Ref, TypeId = Id(reg, "t:tag") } } } },
        new() { Kind = ParamKind.Events, TypeId = Id(reg, "t:ping") },
        new() { Kind = ParamKind.Res, TypeId = Id(reg, "t:missing") },
    ];

    [Fact]
    public void Run_on_change_skips_the_guest_call_until_an_input_changes()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        var exec = OnChangeSystem(reg, runOnChange: true, WatchParams(reg));
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        var sys = Assert.Single(ctx.Systems);
        Assert.True(sys.RunOnChange);

        runner.RunSystem(sys); // the first run never skips
        runner.RunSystem(sys); // nothing changed (an absent resource stays absent)
        Assert.Single(exec.RunInputs);

        app.GetResourceRef<V3Score>().Value = 8; // a resource change
        runner.RunSystem(sys);
        Assert.Equal(2, exec.RunInputs.Count);
        Assert.Equal("{\"Value\":8}", Json(SystemInput.Serializer.Parse(exec.RunInputs[^1]).Resources![0].Value));
        runner.RunSystem(sys);
        Assert.Equal(2, exec.RunInputs.Count);

        Assert.True(reg.TryGetEvent("t:ping", out var ping)); // an event
        ping.Emit(app.GetWorld(), 0, "{\"N\":1}");
        runner.RunSystem(sys);
        Assert.Equal(3, exec.RunInputs.Count);
        var input = SystemInput.Serializer.Parse(exec.RunInputs[^1]);
        Assert.True(input.Resources![0].Unchanged); // the unchanged value still crosses as a flag
        Assert.Single(input.Events![0].Values!);
        runner.RunSystem(sys); // the event was consumed
        Assert.Equal(3, exec.RunInputs.Count);

        app.GetWorld().Entity().Set(new V3Tag { X = 1 }); // a matching row, on every run
        runner.RunSystem(sys);
        runner.RunSystem(sys);
        Assert.Equal(5, exec.RunInputs.Count);
    }

    [Fact]
    public void Without_run_on_change_the_same_system_keeps_running()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        var exec = OnChangeSystem(reg, runOnChange: false, WatchParams(reg));
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        for (var i = 0; i < 3; i++)
            runner.RunSystem(ctx.Systems[0]);
        Assert.Equal(3, exec.RunInputs.Count); // a resource param opts out of the idle-skip
    }

    [Fact]
    public void Run_on_change_never_skips_a_system_without_input_params()
    {
        var (_, ctx, reg) = Host();
        var exec = OnChangeSystem(reg, runOnChange: true, new ParamDecl { Kind = ParamKind.Commands });
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        for (var i = 0; i < 3; i++)
            runner.RunSystem(ctx.Systems[0]);
        Assert.Equal(3, exec.RunInputs.Count);
    }

    [Fact]
    public void Run_on_change_runs_again_after_a_failed_call()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        var exec = new ThrowingExecutor(OnChangeSystem(reg, runOnChange: true, WatchParams(reg))) { Throw = true };
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();
        var sys = ctx.Systems[0];

        Assert.Throws<InvalidOperationException>(() => runner.RunSystem(sys));
        exec.Throw = false;
        runner.RunSystem(sys); // nothing changed, but the guest never took the last run in
        Assert.Single(exec.Inner.RunInputs);
        Assert.Equal("{\"Value\":7}", Json(SystemInput.Serializer.Parse(exec.Inner.RunInputs[^1]).Resources![0].Value));
        runner.RunSystem(sys);
        Assert.Single(exec.Inner.RunInputs);
    }

    // Bevy's first run of a system sees every earlier change; so does a mod system's,
    // even for a component stamped before any system ran (world tick 0).
    [Fact]
    public void A_first_run_sees_changes_made_before_it()
    {
        var (app, ctx, reg) = Host();
        var early = app.GetWorld().Entity().Set(new V3Tag { X = 1 }).ID;
        var exec = OnChangeSystem(reg, runOnChange: true,
            new ParamDecl { Kind = ParamKind.Query, Query = new QueryDecl { Terms = new List<QueryTerm> { new() { Kind = QueryTermKind.Changed, TypeId = Id(reg, "t:tag") } } } },
            new ParamDecl { Kind = ParamKind.Query, Query = new QueryDecl { Terms = new List<QueryTerm> { new() { Kind = QueryTermKind.Added, TypeId = Id(reg, "t:tag") } } } });
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        runner.RunSystem(ctx.Systems[0]);
        var input = SystemInput.Serializer.Parse(Assert.Single(exec.RunInputs));
        Assert.Equal(early, Assert.Single(input.Queries![0].Rows!).Entity);
        Assert.Equal(early, Assert.Single(input.Queries[1].Rows!).Entity);
    }

    [Fact]
    public void Resource_set_naming_a_non_resource_is_refused()
    {
        var (app, ctx, reg) = Host();
        app.AddResource(new V3Score { Value = 7 });
        var exec = OnChangeSystem(reg, runOnChange: false, new ParamDecl { Kind = ParamKind.Commands });
        exec.RunReplyBytes = Bytes(CommandBuffer.Serializer, new CommandBuffer
        {
            Cmds = new List<Cmd>
            {
                new(new ResourceSetCmd { Value = new CompValue { TypeId = Id(reg, "t:tag"), Data = "{\"X\":1}"u8.ToArray() } }),
                new(new ResourceSetCmd { Value = new CompValue { TypeId = Id(reg, "t:score"), Data = "{\"Value\":9}"u8.ToArray() } }),
            },
        });
        var runner = new ModAbiRunner(exec, 0, new ModRelayState(), ctx);
        runner.Setup();

        var output = new StringWriter();
        var stdout = Console.Out;
        Console.SetOut(output);
        try { runner.RunSystem(ctx.Systems[0]); }
        finally { Console.SetOut(stdout); }

        Assert.Contains($"type id {Id(reg, "t:tag")} is not a resource", output.ToString());
        Assert.Equal(9, app.GetResource<V3Score>().Value); // the next command still applied
    }
}
