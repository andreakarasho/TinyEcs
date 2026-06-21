// Generic component-model modding plugin (tinyecs:modding WIT, via the
// wasmtime-dotnet fork). Loads WASM *component* mods from a folder, runs their
// `setup`, then dispatches the systems they registered into the matching Bevy
// Stage each frame — mirroring wasvy's dynamic_system.
//
// Game-agnostic: the host supplies a ModComponentRegistry and per-mod linker
// hooks through ModdingConfig (registered before this plugin's Startup runs).
// The lib knows no concrete game component, no networking, no input device —
// only the generic ECS+UI contract in tinyecs.wit.
//
// Mods are loaded from the ModFolder in the working dir / next to the exe
// (*.wasm). If the folder is absent the plugin is a no-op, so it is always safe
// to install.

using System.Buffers;
using System.IO;
using System.Linq;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Collections;
using Wasmtime;
using G = Wit.Tinyecs.Modding.GuestImports;
using WitApp = Wit.Tinyecs.Modding.App;

namespace TinyEcs.Bevy.Modding;

internal sealed class ModRuntime
{
    public Linker Linker = null!;
    public Store Store = null!;
    public ComponentInstance Instance = null!;
    public GuestBridge Imports = null!;
    public Wit.Tinyecs.Modding.GuestExports Exports = null!;
    public ModHostContext Ctx = null!;
    // Reused per CallSystem: the ArrayPool-rented query snapshots in flight for
    // one system call, returned in CallSystem's finally after the wasm Call.
    // Safe to share (one buffer per runtime): the stage runners are
    // SingleThreaded and CallSystem is never re-entered during Instance.Call.
    public readonly List<ulong[]> SnapshotScratch = new();
}

internal sealed class ModRuntimes
{
    public Engine Engine = null!;
    public readonly List<ModRuntime> Runtimes = new();
}

/// Host-supplied configuration for the modding plugin. Register an instance with
/// `app.AddResource(new ModdingConfig { ... })` BEFORE adding the plugin; the
/// plugin reads it at Startup. If absent the plugin uses an empty default (mods
/// load but see no registered components/resources and no game-specific imports).
public sealed class ModdingConfig
{
    /// Components + resources the host exposes to mods, keyed by WIT type-path.
    public ModComponentRegistry Registry = new();

    /// Folder (relative to the exe + cwd) scanned for *.wasm component mods.
    public string ModFolder = "ecs-mods";

    /// Per-mod hooks run once for each loaded mod after the generic `app` bridge
    /// is defined and before the component is instantiated. Use to `Define` extra
    /// game-specific linker imports (other WIT packages a mod consumes) and to
    /// wire host capabilities onto the ModHostContext (e.g. ConsumeMouse).
    public readonly List<Action<Linker, ModHostContext>> PerMod = new();
}

public readonly struct ModdingPlugin : IPlugin
{
    // Stable labels for the per-stage mod runners so a host can order its own
    // systems relative to mod dispatch (e.g. drain inputs Before the First
    // runner, clear one-frame state After the Last runner).
    public const string RunnerFirst = "tinyecs:mod_runner_first";
    public const string RunnerPreUpdate = "tinyecs:mod_runner_pre_update";
    public const string RunnerUpdate = "tinyecs:mod_runner_update";
    public const string RunnerPostUpdate = "tinyecs:mod_runner_post_update";
    public const string RunnerLast = "tinyecs:mod_runner_last";

    public void Build(App app)
    {
        if (!app.HasResource<App>())
            app.AddResource(app);
        if (!app.HasResource<ModdingConfig>())
            app.AddResource(new ModdingConfig());
        app.AddResource(new ModRuntimes());

        var setupFn = SetupEcsMods;
        app.AddSystem(setupFn).InStage(Stage.Startup).Build();

        // One dispatcher per Stage. SingleThreaded: mod systems touch the World
        // directly through the bridge (see GuestBridge), so they must not share a
        // parallel batch.
        AddRunner(app, Stage.First, WitApp.Schedule.Case.First, RunnerFirst);
        AddRunner(app, Stage.PreUpdate, WitApp.Schedule.Case.PreUpdate, RunnerPreUpdate);
        AddRunner(app, Stage.Update, WitApp.Schedule.Case.Update, RunnerUpdate);
        AddRunner(app, Stage.PostUpdate, WitApp.Schedule.Case.PostUpdate, RunnerPostUpdate);
        AddRunner(app, Stage.Last, WitApp.Schedule.Case.Last, RunnerLast);

        // Click bridge: Bevy.UI fires On<UiClick> (observer); mods poll. When a
        // mod-owned entity is clicked, tag it ModClicked so a mod query sees it.
        app.AddObserver<On<UiClick>, Commands, Query<Data<ModEntity>>>((trigger, commands, modQ) =>
        {
            if (modQ.Contains(trigger.EntityId))
                commands.Entity(trigger.EntityId).Insert(new ModClicked());
        });

        // Clear ModClicked AFTER the Update runner (read-then-clear, same frame).
        // NOT Stage.Last: UiClick fires after Update, so a Last clear would strip
        // the tag before the mod's NEXT Update poll ever sees it.
        var clearFn = ClearClicks;
        app.AddSystem(clearFn).InStage(Stage.Update).After(RunnerUpdate).Build();
    }

    private static void ClearClicks(Commands commands, Query<Data<ModClicked>> q)
    {
        foreach ((var e, var _) in q)
            commands.Entity(e.Ref).Remove<ModClicked>();
    }

    private static void AddRunner(App app, Stage stage, WitApp.Schedule.Case which, string label)
    {
        app.AddSystem((ResMut<ModRuntimes> runtimes) => RunStage(runtimes.Value, which))
            .InStage(stage)
            .SingleThreaded()
            .Label(label)
            .Build();
    }

    private static void SetupEcsMods(Res<App> appRes, ResMut<ModRuntimes> runtimesRes, Res<ModdingConfig> configRes)
    {
        var config = configRes.Value;

        // Look both next to the exe (deployed alongside the build) and in the
        // working dir, so launch location doesn't matter. Dedup by file name.
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, config.ModFolder),
            Path.Combine(Directory.GetCurrentDirectory(), config.ModFolder),
        };
        var files = candidates
            .Where(Directory.Exists)
            .SelectMany(d => Directory.GetFiles(d, "*.wasm"))
            .GroupBy(Path.GetFileName)
            .Select(g => g.First())
            .ToArray();
        if (files.Length == 0)
            return;

        var world = appRes.Value.GetWorld();
        var runtimes = runtimesRes.Value;
        runtimes.Engine = new Engine();

        foreach (var file in files)
        {
            try
            {
                var ctx = new ModHostContext { World = world, Registry = config.Registry, App = appRes.Value };
                var linker = new Linker(runtimes.Engine);
                linker.AddWasiP2();
                var imports = new GuestBridge(ctx);
                linker.Define(imports);
                // Host-specific imports + per-mod context wiring (game bridges, input).
                foreach (var hook in config.PerMod)
                    hook(linker, ctx);

                var store = new Store(runtimes.Engine);
                store.AddWasiP2(inheritStdout: true, inheritStderr: true);

                var component = Component.Compile(runtimes.Engine, File.ReadAllBytes(file));
                var instance = store.GetComponentInstance(component, linker);
                var exports = new Wit.Tinyecs.Modding.GuestExports(instance, store);

                var appHandle = imports.RegisterApp(new AppImpl(ctx));
                exports.Setup(appHandle);

                var rt = new ModRuntime
                {
                    Linker = linker,
                    Store = store,
                    Instance = instance,
                    Imports = imports,
                    Exports = exports,
                    Ctx = ctx,
                };
                runtimes.Runtimes.Add(rt);

                Console.WriteLine("[ecs-mod] loaded {0} ({1} systems)", Path.GetFileName(file), ctx.Systems.Count);

                // mod-startup systems run once, now.
                RunSystemsForStage(rt, WitApp.Schedule.Case.ModStartup);
            }
            catch (Exception e)
            {
                Console.WriteLine("[ecs-mod] failed to load {0}: {1}", Path.GetFileName(file), e);
            }
        }
    }

    private static void RunStage(ModRuntimes runtimes, WitApp.Schedule.Case which)
    {
        foreach (var rt in runtimes.Runtimes)
            RunSystemsForStage(rt, which);
    }

    private static void RunSystemsForStage(ModRuntime rt, WitApp.Schedule.Case which)
    {
        if (!rt.Ctx.SystemsByStage.TryGetValue(which, out var systems))
            return;
        foreach (var sys in systems)
        {
            try
            {
                CallSystem(rt, sys);
            }
            catch (Exception e)
            {
                Console.WriteLine("[ecs-mod] system '{0}' failed: {1}", sys.Name, e.Message);
            }
        }
    }

    private static void CallSystem(ModRuntime rt, ModSystemSpec sys)
    {
        var ctx = rt.Ctx;
        var count = sys.Params.Count;
        // Param counts are tiny; stack the common case, heap-fall-back for the rare large one.
        Span<ComponentValue> vals = count <= 16 ? stackalloc ComponentValue[count] : new ComponentValue[count];
        rt.SnapshotScratch.Clear();

        for (var i = 0; i < count; i++)
        {
            var p = sys.Params[i];
            if (p.Kind == ModParamKind.Commands)
            {
                var h = rt.Imports.RegisterCommands(new CommandsImpl(ctx));
                vals[i] = ComponentValue.CreateOwnResource(rt.Store, h, G.CommandsTypeId);
            }
            else
            {
                var snapshot = BuildSnapshot(ctx, p.Query!, out var matched);
                rt.SnapshotScratch.Add(snapshot);
                var h = rt.Imports.RegisterQuery(new QueryImpl(ctx, snapshot, matched, p.Query!.Components));
                vals[i] = ComponentValue.CreateOwnResource(rt.Store, h, G.QueryTypeId);
            }
        }

        try
        {
            using var _ = rt.Instance.Call(sys.Name, 0, vals);
        }
        finally
        {
            for (var i = 0; i < count; i++)
                vals[i].Dispose(rt.Store);
            // Return query snapshots only AFTER the Call: the guest iterates them
            // synchronously within Call and drops the resource at fn end. ulong is
            // unmanaged → no clear needed.
            foreach (var arr in rt.SnapshotScratch)
                ArrayPool<ulong>.Shared.Return(arr);
            rt.SnapshotScratch.Clear();
        }
    }

    // Builds the matching-entity snapshot into an ArrayPool-rented buffer (caller
    // owns it: returned in CallSystem's finally). Returns the buffer; `matched` is
    // the valid prefix length. `candidates` is method-scoped scratch (PooledList).
    private static ulong[] BuildSnapshot(ModHostContext ctx, ModQuerySpec q, out int matched)
    {
        matched = 0;

        // Driver = first present-required term (ref/mut/with) that is registered.
        IModComponent? driver = null;
        foreach (var (typePath, _, kind) in q.Terms)
            if (kind != WitApp.QueryFor.Case.Without && ctx.Registry.TryGet(typePath, out driver))
                break;
        if (driver == null)
            return ArrayPool<ulong>.Shared.Rent(1);

        // Not `using` — CollectEntities needs `candidates` by ref (Add may grow),
        // and a using-variable can't be passed by ref (CS1657). Dispose by hand.
        var candidates = new PooledList<ulong>(16);
        try
        {
            driver.CollectEntities(ctx.World, ref candidates);

            // matched ⊆ candidates, so a candidates-sized buffer never overflows.
            var result = ArrayPool<ulong>.Shared.Rent(candidates.Count == 0 ? 1 : candidates.Count);
            for (var ci = 0; ci < candidates.Count; ci++)
            {
                var id = candidates[ci];
                var ok = true;
                foreach (var (typePath, _, kind) in q.Terms)
                {
                    if (!ctx.Registry.TryGet(typePath, out var comp))
                    {
                        if (kind != WitApp.QueryFor.Case.Without) { ok = false; break; }
                        continue;
                    }
                    var has = comp.Has(ctx.World, id);
                    if (kind == WitApp.QueryFor.Case.Without && has) { ok = false; break; }
                    if (kind != WitApp.QueryFor.Case.Without && !has) { ok = false; break; }
                }
                if (ok)
                    result[matched++] = id;
            }

            return result;
        }
        finally
        {
            candidates.Dispose();
        }
    }
}
