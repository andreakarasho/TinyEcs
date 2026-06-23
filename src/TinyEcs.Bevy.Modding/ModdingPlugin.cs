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
// Mods live in the ModFolder (in the working dir / next to the exe), one folder
// per mod: `<ModFolder>/<mod>/{mod.json, *.wasm}`. The `mod.json` manifest names
// the WASM component to load (plus name/version/ruleset). If the folder is absent
// the plugin is a no-op, so it is always safe to install.

using System.Buffers;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    public ModManifest Manifest = null!;
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
    // Observer fires buffered by host global observers, drained by FlushObservers
    // at a safe point (end of frame) so guest callbacks can mutate via the bridge.
    public readonly Queue<(string Name, ulong Entity, string Json)> ObserverFires = new();
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

        // Drain buffered observer fires into the guest callbacks at end of frame,
        // after the Last runner — a safe single-threaded point (no mid-mutation
        // re-entry). Events fired during Last reach the guest next frame.
        app.AddSystem((ResMut<ModRuntimes> runtimes) =>
            {
                foreach (var rt in runtimes.Value.Runtimes)
                    FlushObservers(rt);
            })
            .InStage(Stage.Last).After(RunnerLast).SingleThreaded().Build();
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
        // working dir, so launch location doesn't matter. Each mod is a subfolder
        // with a mod.json manifest; dedup by manifest name (exe dir wins over cwd).
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, config.ModFolder),
            Path.Combine(Directory.GetCurrentDirectory(), config.ModFolder),
        };
        var mods = candidates
            .Where(Directory.Exists)
            .SelectMany(Directory.GetDirectories)
            .Select(LoadManifest)
            .OfType<(ModManifest Manifest, string WasmPath)>()
            .GroupBy(m => m.Manifest.Name)
            .Select(g => g.First())
            .ToArray();
        if (mods.Length == 0)
            return;

        var world = appRes.Value.GetWorld();
        var runtimes = runtimesRes.Value;
        runtimes.Engine = new Engine();

        foreach (var (manifest, file) in mods)
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
                    Manifest = manifest,
                    Linker = linker,
                    Store = store,
                    Instance = instance,
                    Imports = imports,
                    Exports = exports,
                    Ctx = ctx,
                };
                runtimes.Runtimes.Add(rt);

                Console.WriteLine("[ecs-mod] loaded {0} v{1} ({2} systems, {3} observers)",
                    manifest.Name, manifest.Version, ctx.Systems.Count, ctx.Observers.Count);

                // Wire the observers the mod registered during setup to host globals.
                RegisterModObservers(appRes.Value, rt);

                // mod-startup systems run once, now.
                RunSystemsForStage(rt, WitApp.Schedule.Case.ModStartup);
            }
            catch (Exception e)
            {
                Console.WriteLine("[ecs-mod] failed to load {0}: {1}", manifest.Name, e);
            }
        }
    }

    // Read `<dir>/mod.json` and resolve the WASM it names (relative to the mod's
    // own folder). Returns null (skipping the folder) if there is no manifest, it
    // doesn't parse, names no wasm, or the wasm is missing.
    private static (ModManifest Manifest, string WasmPath)? LoadManifest(string dir)
    {
        var manifestPath = Path.Combine(dir, "mod.json");
        if (!File.Exists(manifestPath))
            return null;

        ModManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize(File.ReadAllText(manifestPath), ModManifestJsonContext.Default.ModManifest);
        }
        catch (Exception e)
        {
            Console.WriteLine("[ecs-mod] bad manifest {0}: {1}", manifestPath, e.Message);
            return null;
        }

        if (manifest == null || string.IsNullOrEmpty(manifest.Wasm))
        {
            Console.WriteLine("[ecs-mod] manifest {0} names no 'wasm'", manifestPath);
            return null;
        }

        var wasmPath = Path.Combine(dir, manifest.Wasm);
        if (!File.Exists(wasmPath))
        {
            Console.WriteLine("[ecs-mod] {0}: wasm '{1}' not found in {2}", manifest.Name, manifest.Wasm, dir);
            return null;
        }

        if (string.IsNullOrEmpty(manifest.Name))
            manifest.Name = Path.GetFileName(dir);

        return (manifest, wasmPath);
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

    // Wire every observer the mod registered to a host global observer that
    // buffers fires onto the runtime's queue (no World mutation in the callback —
    // re-entrancy-safe; the guest is called later in FlushObservers).
    private static void RegisterModObservers(App app, ModRuntime rt)
    {
        foreach (var obs in rt.Ctx.Observers)
            RegisterObserver(app, obs, rt.Ctx.Registry, (name, e, json) => rt.ObserverFires.Enqueue((name, e, json)));
    }

    // Internal + testable: maps one observer spec to the matching host global
    // observer. Component events resolve their type-path via the registry.
    internal static void RegisterObserver(App app, ModObserverSpec obs, ModComponentRegistry registry, Action<string, ulong, string> onFire)
    {
        switch (obs.Kind)
        {
            case WitApp.ObserverEvent.Case.Spawn:
                app.AddObserver<OnSpawn>(t => onFire(obs.Name, t.EntityId, ""));
                break;
            case WitApp.ObserverEvent.Case.Despawn:
                app.AddObserver<OnDespawn>(t => onFire(obs.Name, t.EntityId, ""));
                break;
            case WitApp.ObserverEvent.Case.Insert:
                if (obs.TypePath != null && registry.TryGet(obs.TypePath, out var ci))
                    ci.RegisterInsertObserver(app, (e, json) => onFire(obs.Name, e, json));
                break;
            case WitApp.ObserverEvent.Case.Remove:
                if (obs.TypePath != null && registry.TryGet(obs.TypePath, out var cr))
                    cr.RegisterRemoveObserver(app, (e, json) => onFire(obs.Name, e, json));
                break;
            case WitApp.ObserverEvent.Case.Custom:
                if (obs.TypePath != null && registry.TryGetEvent(obs.TypePath, out var ev))
                    ev.RegisterObserver(app, (e, json) => onFire(obs.Name, e, json));
                break;
        }
    }

    // Dispatch buffered observer fires to the guest. Each fire calls the guest
    // export `name` with (entity: u64, json: string). Not yet exercised by a test
    // fixture — needs a mod that exports an observer callback.
    private static void FlushObservers(ModRuntime rt)
    {
        while (rt.ObserverFires.Count > 0)
        {
            var (name, entity, json) = rt.ObserverFires.Dequeue();
            Span<ComponentValue> args = stackalloc ComponentValue[2];
            args[0] = ComponentValue.CreateUInt64(entity);
            args[1] = ComponentValue.CreateString(json, true);
            try
            {
                using var _ = rt.Instance.Call(name, 0, args);
            }
            catch (Exception e)
            {
                Console.WriteLine("[ecs-mod] observer '{0}' failed: {1}", name, e.Message);
            }
            finally
            {
                args[0].Dispose(rt.Store);
                args[1].Dispose(rt.Store);
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
