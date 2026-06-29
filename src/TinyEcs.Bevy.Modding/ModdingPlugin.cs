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
using WitApp = Wit.Tinyecs.Modding.App;

namespace TinyEcs.Bevy.Modding;

internal sealed class ModRuntime
{
    public ModManifest Manifest = null!;
    // The runtime-backed mod instance (wasmtime today; backend chosen by config).
    public IModInstance Instance = null!;
    public ModHostContext Ctx = null!;
    // Lifecycle (host enable/disable/reload via ModControl). Disabled mods are
    // skipped by the per-stage runner; Slot scopes their entities for teardown;
    // WasmPath lets Reload re-read the component bytes from disk.
    public bool Enabled = true;
    public int Slot;
    public string WasmPath = "";
    // Observer fires buffered by host global observers, drained by FlushObservers
    // at a safe point (end of frame) so guest callbacks can mutate via the bridge.
    public readonly Queue<(string Name, ulong Entity, string Json)> ObserverFires = new();
}

/// One loaded mod as a host sees it, for hosts that must call a guest export inline
/// (outside the per-frame scheduler — e.g. a synchronous predicate hook). The lib owns
/// normal per-stage dispatch; this is the escape hatch. Meaning-free: the host names the
/// export and decides what a true return means.
public readonly struct LoadedMod
{
    private readonly ModRuntime _rt;
    internal LoadedMod(ModRuntime rt) => _rt = rt;

    public bool Enabled => _rt.Enabled;
    public string Name => _rt.Manifest.Name;

    /// Invoke `export(arg: u8, data: list&lt;u8&gt;) -> bool` on this mod; false if absent.
    public bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data)
        => _rt.Instance.TryInvokeBoolExport(export, arg, data);
}

/// Loaded mod runtimes (public so a host can drive synchronous guest calls — see
/// LoadedMod). Fields stay internal; the host iterates via Count + the indexer. Calls
/// here re-enter a mod instance, so drive them ONLY from a SingleThreaded host system.
public sealed class ModRuntimes
{
    internal IModBackend Backend = null!;
    internal readonly List<ModRuntime> Runtimes = new();

    /// Loaded mods in load order (index stable, matches the control list). Iterate with
    /// a plain for-loop — LoadedMod is a struct, so no per-call allocation in hot paths.
    public int Count => Runtimes.Count;
    public LoadedMod this[int index] => new(Runtimes[index]);
}

/// One loaded mod's host-visible state. Enabled flips as the host enables/disables
/// (the actual runtime work is deferred — see ModControl).
public sealed class ModInfo
{
    public string Name = "";
    public string Version = "";
    public bool Enabled = true;
}

/// Host-facing control surface for the loaded mods: the list to render (Mods, in
/// load order — index is stable and matches the runtime) plus queued enable/disable
/// /reload requests. Requests are NOT applied inline: they enqueue and a lib system
/// drains them at a safe single-threaded point (after the Last runner), where it can
/// dispose wasm instances and mutate the World directly. Always registered (empty
/// when no mods are present).
public sealed class ModControl
{
    public readonly List<ModInfo> Mods = new();
    internal readonly Queue<(int Index, ModAction Action)> Pending = new();

    public void Enable(int index) => Pending.Enqueue((index, ModAction.Enable));
    public void Disable(int index) => Pending.Enqueue((index, ModAction.Disable));
    public void Reload(int index) => Pending.Enqueue((index, ModAction.Reload));
}

internal enum ModAction : byte { Enable, Disable, Reload }

/// Host-supplied configuration for the modding plugin. Register an instance with
/// `app.AddResource(new ModdingConfig { ... })` BEFORE adding the plugin; the
/// plugin reads it at Startup. If absent the plugin uses an empty default (mods
/// load but see no registered components/resources and no game-specific imports).
public sealed class ModdingConfig
{
    /// Components + resources the host exposes to mods, keyed by WIT type-path.
    public ModComponentRegistry Registry = new();

    /// Which wasm runtime hosts the mods. Wasmtime (native) is the desktop default.
    public WasmBackend Backend = WasmBackend.Wasmtime;

    /// Folder (relative to the exe + cwd) scanned for *.wasm component mods.
    public string ModFolder = "ecs-mods";

    /// Per-mod hooks run once for each loaded mod after the generic `app` bridge
    /// is defined and before the component is instantiated. Use to `Define` extra
    /// game-specific linker imports (other WIT packages a mod consumes) and to
    /// wire host capabilities onto the ModHostContext (e.g. ConsumeMouse).
    /// Wasmtime backend only.
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
        app.AddResource(new ModControl());

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

        // Hover bridge: mirror Bevy.UI's single HoveredEntity onto a sparse marker
        // so mods stop scanning every interactive element's Interaction byte each
        // frame. UiOver/UiOut fire once per enter/leave on the topmost entity, so
        // ModHovered lives on at most one entity at a time — no clear system, no
        // refcount, no enter-before-leave ordering hazard (Over and Out target
        // different entities). The mod walks ancestors itself (DOM mouseenter).
        app.AddObserver<On<UiOver>, Commands, Query<Data<ModEntity>>>((trigger, commands, modQ) =>
        {
            if (modQ.Contains(trigger.EntityId))
                commands.Entity(trigger.EntityId).Insert(new ModHovered());
        });
        app.AddObserver<On<UiOut>, Commands, Query<Data<ModHovered>>>((trigger, commands, hoveredQ) =>
        {
            if (hoveredQ.Contains(trigger.EntityId))
                commands.Entity(trigger.EntityId).Remove<ModHovered>();
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

        // Apply queued enable/disable/reload requests at the same safe point. These
        // dispose wasm instances + spawn/despawn entities directly, so they must run
        // single-threaded and outside any mod system call (no re-entry).
        var processControlFn = ProcessModControl;
        app.AddSystem(processControlFn)
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

    private static void SetupEcsMods(Res<App> appRes, ResMut<ModRuntimes> runtimesRes, Res<ModdingConfig> configRes, ResMut<ModControl> controlRes)
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
        runtimes.Backend = config.Backend switch
        {
            WasmBackend.Wasmtime => new WasmtimeModBackend(config.PerMod),
            _ => throw new NotSupportedException($"mod backend {config.Backend} is not implemented"),
        };

        var failedMods = new List<string>();
        foreach (var (manifest, file) in mods)
        {
            try
            {
                var slot = runtimes.Runtimes.Count;
                var ctx = new ModHostContext { World = world, Registry = config.Registry, App = appRes.Value, Slot = slot };

                var instance = runtimes.Backend.Load(File.ReadAllBytes(file), ctx);
                instance.Setup();

                var rt = new ModRuntime
                {
                    Manifest = manifest,
                    Instance = instance,
                    Ctx = ctx,
                    Slot = slot,
                    WasmPath = file,
                };
                runtimes.Runtimes.Add(rt);
                controlRes.Value.Mods.Add(new ModInfo { Name = manifest.Name, Version = manifest.Version, Enabled = true });

                Console.WriteLine("[ecs-mod] loaded {0} v{1} ({2} systems, {3} observers)",
                    manifest.Name, manifest.Version, ctx.Systems.Count, ctx.Observers.Count);

                // Wire the observers the mod registered during setup to host globals.
                RegisterModObservers(appRes.Value, rt);

                // mod-startup systems run once, now.
                RunSystemsForStage(rt, WitApp.Schedule.Case.ModStartup);
            }
            catch (Exception e)
            {
                failedMods.Add(manifest.Name);
                Console.WriteLine("[ecs-mod] failed to load {0}: {1}", manifest.Name, e);
            }
        }

        Console.WriteLine("[ecs-mod] backend={0}: {1}/{2} mods loaded{3}",
            config.Backend, runtimes.Runtimes.Count, mods.Length,
            failedMods.Count > 0 ? $" — FAILED: {string.Join(", ", failedMods)}" : "");
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
            if (rt.Enabled)
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
                rt.Instance.RunSystem(sys);
            }
            catch (Exception e)
            {
                Console.WriteLine("[ecs-mod] system '{0}' failed: {1}", sys.Name, e.Message);
            }
        }
    }

    // Drain host enable/disable/reload requests at a safe single-threaded point.
    private static void ProcessModControl(ResMut<ModRuntimes> runtimesRes, ResMut<ModControl> controlRes, Res<App> appRes)
    {
        var control = controlRes.Value;
        if (control.Pending.Count == 0)
            return;

        var runtimes = runtimesRes.Value;
        while (control.Pending.Count > 0)
        {
            var (idx, action) = control.Pending.Dequeue();
            if (idx < 0 || idx >= runtimes.Runtimes.Count)
                continue;
            var rt = runtimes.Runtimes[idx];
            var info = idx < control.Mods.Count ? control.Mods[idx] : null;
            try
            {
                switch (action)
                {
                    case ModAction.Disable: DisableMod(rt, info); break;
                    case ModAction.Enable: EnableMod(rt, info); break;
                    case ModAction.Reload: ReloadMod(runtimes, rt, appRes.Value, info); break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[ecs-mod] {0} on '{1}' failed: {2}", action, rt.Manifest.Name, e);
            }
        }
    }

    // Stop a mod ticking and remove everything it spawned. The wasm instance stays
    // loaded; re-enable re-runs its startup. Only its host entities are despawned.
    private static void DisableMod(ModRuntime rt, ModInfo? info)
    {
        if (!rt.Enabled)
            return;
        rt.Enabled = false;
        if (info != null) info.Enabled = false;
        DespawnModEntities(rt.Ctx.World, rt.Slot);
    }

    // Resume ticking and re-run the mod's startup so it rebuilds its UI. A mod whose
    // startup is one-shot-guarded won't rebuild until a reload; the React UI mod does.
    private static void EnableMod(ModRuntime rt, ModInfo? info)
    {
        if (rt.Enabled)
            return;
        rt.Enabled = true;
        if (info != null) info.Enabled = true;
        RunSystemsForStage(rt, WitApp.Schedule.Case.ModStartup);
    }

    // Tear the mod down and re-instantiate it from disk (picks up a rebuilt .wasm).
    // Reuses the same ModRuntime + ModHostContext AND the existing Linker + bridge:
    // the host import functions never change between loads, and the fork registers
    // every linker.Define'd function in a STATIC, process-global, never-freed table
    // capped at 1024 (ComponentExport.RegisterFunction). Re-Define'ing on reload
    // leaked a full set of slots each time and overflowed almost immediately, so we
    // do NOT build a new Linker — we only recompile the component and instantiate it
    // on a fresh Store with the original linker. Zero new function registrations.
    //
    // CEILING (inherent, no clean fix here): TinyEcs has no global-observer removal,
    // so observers wired at first load persist. Same-named exports on the new
    // instance still receive them, but observers a mod registers ONLY on reload are
    // not wired. (The 1024-function cap still bounds how many mods can be LOADED at
    // once — Define count scales with mod count — but reload no longer consumes it.)
    private static void ReloadMod(ModRuntimes runtimes, ModRuntime rt, App app, ModInfo? info)
    {
        DespawnModEntities(rt.Ctx.World, rt.Slot);

        // Re-setup repopulates these from the fresh instance's setup() call.
        rt.Ctx.Systems.Clear();
        rt.Ctx.SystemsByStage.Clear();
        rt.Ctx.Observers.Clear();
        rt.Ctx.ChildrenQuery = null;

        // Backend tears down + re-instantiates from fresh bytes (reusing host imports)
        // and re-runs setup, which repopulates ctx.Systems via the guest.
        rt.Instance.Reload(File.ReadAllBytes(rt.WasmPath));

        rt.Enabled = true;
        if (info != null) info.Enabled = true;

        RunSystemsForStage(rt, WitApp.Schedule.Case.ModStartup);
    }

    // Delete every entity this mod spawned (ModEntity.Slot == slot). Deleting a root
    // cascades to its children, so already-gone children are skipped by Exists.
    private static void DespawnModEntities(World world, int slot)
    {
        var q = world.QueryBuilder().With<ModEntity>().Build();
        var ids = new List<ulong>();
        var it = q.Iter();
        while (it.Next())
            foreach (var ev in it.Entities())
                if (world.Get<ModEntity>(ev.ID).Slot == slot)
                    ids.Add(ev.ID);
        foreach (var id in ids)
            if (world.Exists(id))
                world.Delete(id);
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
            try
            {
                rt.Instance.CallObserver(name, entity, json);
            }
            catch (Exception e)
            {
                Console.WriteLine("[ecs-mod] observer '{0}' failed: {1}", name, e.Message);
            }
        }
    }

    // Builds the matching-entity snapshot into an ArrayPool-rented buffer (caller
    // owns it: returned in the backend's RunSystem finally after the wasm Call).
    // Returns the buffer; `matched` is the valid prefix length. `candidates` is
    // method-scoped scratch (PooledList). Runtime-agnostic — used by every backend.
    internal static ulong[] BuildSnapshot(ModHostContext ctx, ModQuerySpec q, out int matched)
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
