// Generic modding plugin. Loads WASM mods, runs their `setup`, then dispatches the
// systems they registered into the matching Bevy Stage each frame. Runtime-agnostic:
// this file has ZERO wasmtime references (compiles under WasmGuest) — the backends own the
// concrete runtime: Component/ComponentModBackend.cs (desktop: wasm32-wasip2 component
// mods on an embedded wasmtime) or ModRelayBackend.cs over ModdingConfig.WasmExecutor
// (a wasm guest relaying mods to its native host over abi/mod-abi.fbs). See EnsureBackend.
//
// Game-agnostic: the host supplies a ModComponentRegistry and per-mod hooks
// through ModdingConfig (registered before this plugin's Startup runs). The lib
// knows no concrete game component, no networking, no input device — only the
// generic ECS + registry contract.
//
// Mods live in the ModFolder (in the working dir / next to the exe) for the component
// backend — one folder per mod: `<ModFolder>/<mod>/{mod.json, *.wasm}`. The relay has
// no filesystem scan: discovery comes from ModdingConfig.WasmManifestSource instead (see
// SetupEcsMods). Either way, if no mods are found the plugin is a no-op, so it is always
// safe to install.

using System.Buffers;
using System.IO;
using System.Linq;
using System.Text.Json;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Collections;

namespace TinyEcs.Bevy.Modding;

internal sealed class ModRuntime
{
    public ModManifest Manifest = null!;
    // The runtime-backed mod instance (wasmtime today; backend chosen by config).
    public IModInstance Instance = null!;
    public ModHostContext Ctx = null!;
    // Lifecycle (host enable/disable/reload via ModControl). Disabled mods are
    // skipped by the per-stage runner; Slot scopes their entities for teardown;
    // WasmPath lets Reload re-read the component bytes from disk (component backend
    // only — empty under the relay, which re-instantiates by slot instead).
    public bool Enabled = true;
    public int Slot;
    public string WasmPath = "";
    // The host-visible mirror of this runtime (same index in ModControl.Mods), so a
    // failure recorded deep in a dispatch path can flip Enabled + publish LastError
    // without every caller threading it through.
    public ModInfo? Info;
    // Guest failures (system / observer / a host's inline export), cumulative until
    // an explicit Enable/Reload. At MaxModFailures the mod is auto-disabled — a
    // trapping guest otherwise retries every frame forever.
    public int FailureCount;
    public string LastError = "";
    // Observer fires buffered by host global observers, drained by FlushObservers
    // at a safe point (end of frame) so guest callbacks can mutate via the bridge.
    public readonly ModObserverQueue ObserverFires = new();
}

/// FIFO of observer fires whose JSON payloads live in one grow-only byte arena (a
/// string per fire was the old shape). Reset once drained.
internal sealed class ModObserverQueue
{
    private readonly List<(string Name, ulong Entity, int Off, int Len, bool Binary)> _fires = new();
    private byte[] _bytes = new byte[512];
    private int _len;
    private int _head;

    public int Count => _fires.Count - _head;

    public void Enqueue(string name, ulong entity, ReadOnlySpan<byte> json, bool binary = false)
    {
        if (_len + json.Length > _bytes.Length)
            Array.Resize(ref _bytes, Math.Max(_bytes.Length * 2, _len + json.Length));
        json.CopyTo(_bytes.AsSpan(_len));
        _fires.Add((name, entity, _len, json.Length, binary));
        _len += json.Length;
    }

    /// The JSON span stays valid until the next TryDequeue — fires enqueued meanwhile
    /// (the guest call it feeds may trigger more) append past it; the arena is only
    /// rewound by the TryDequeue that finds the queue drained.
    public bool TryDequeue(out string name, out ulong entity, out ReadOnlySpan<byte> json)
        => TryDequeue(out name, out entity, out json, out _);

    /// `binary`: the payload is Encoding.Typed bytes (ModObserverSpec.Binary), not JSON.
    public bool TryDequeue(out string name, out ulong entity, out ReadOnlySpan<byte> json, out bool binary)
    {
        if (_head == _fires.Count)
        {
            Clear();
            name = null!;
            entity = 0;
            json = default;
            binary = false;
            return false;
        }
        var f = _fires[_head++];
        name = f.Name;
        entity = f.Entity;
        json = _bytes.AsSpan(f.Off, f.Len);
        binary = f.Binary;
        return true;
    }

    public void Clear()
    {
        _fires.Clear();
        _len = 0;
        _head = 0;
    }
}

/// One loaded mod as a host sees it (name, enabled state, its host context).
public readonly struct LoadedMod
{
    private readonly ModRuntime _rt;
    internal LoadedMod(ModRuntime rt) => _rt = rt;

    public bool Enabled => _rt.Enabled;
    public string Name => _rt.Manifest.Name;

    /// The mod's host context (what a host function receives as `mod`).
    public ModHostContext Context => _rt.Ctx;

    /// Report a failed inline export call so it counts toward the auto-disable budget
    /// (the lib already does this for the systems + observers it drives itself).
    public void RecordFailure(string what, Exception e) => ModdingPlugin.NoteFailure(_rt, what, e);
}

/// Loaded mod runtimes (public so a host can drive synchronous guest calls — see
/// LoadedMod). Fields stay internal; the host iterates via Count + the indexer. Calls
/// here re-enter a mod instance, so drive them ONLY from a SingleThreaded host system.
public sealed class ModRuntimes
{
    // The wasm runtime hosting the mods — see ModdingPlugin.EnsureBackend.
    internal IModBackend Backend = null!;
    internal readonly List<ModRuntime> Runtimes = new();

    // Dispose the backend's engine (instances are owned by the runtimes). No
    // app-teardown hook wires this today (the process outlives the mods and frees the
    // native engine on exit); it exists so a future teardown has one entry point.
    internal void DisposeBackends()
    {
        Backend?.Dispose();
        Backend = null!;
    }

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
    /// Message of the most recent guest failure ("" when the mod never failed).
    public string LastError = "";
    /// Host features this mod declared it replaces (mod.json's top-level `replaces`).
    /// See ModControl.IsReplaced.
    public string[] Replaces = Array.Empty<string>();
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
    internal readonly Queue<(int Index, ModAction Action, string Dir)> Pending = new();

    /// True when some ENABLED mod declared it replaces `feature` (mod.json's top-level
    /// `replaces`). A host feature that has a mod-facing equivalent asks this
    /// before building its own UI, so installing the mod is all it takes — and
    /// disabling or unloading the mod brings the built-in one straight back.
    public bool IsReplaced(string feature)
    {
        // Linear over a handful of mods, called from a spawn guard, not a hot loop.
        for (var i = 0; i < Mods.Count; i++)
        {
            var mod = Mods[i];
            if (!mod.Enabled)
                continue;
            var replaces = mod.Replaces;
            for (var j = 0; j < replaces.Length; j++)
                if (string.Equals(replaces[j], feature, StringComparison.Ordinal))
                    return true;
        }
        return false;
    }

    public void Enable(int index) => Pending.Enqueue((index, ModAction.Enable, ""));
    public void Disable(int index) => Pending.Enqueue((index, ModAction.Disable, ""));
    public void Reload(int index) => Pending.Enqueue((index, ModAction.Reload, ""));

    /// Load a mod folder (`<dir>/mod.json` + the wasm it names) into the running app,
    /// appending it to Mods/Runtimes. Same deferred point as the other actions — a mod
    /// store installs a folder, then queues this.
    public void Load(string modDir) => Pending.Enqueue((-1, ModAction.Load, modDir));

    /// Tear a mod down completely (dispose the instance, despawn its entities) and
    /// REMOVE it from Mods/Runtimes. Unlike Disable this shifts the indices of every
    /// mod after it — a host UI keyed on index must rebuild from Mods afterwards.
    public void Unload(int index) => Pending.Enqueue((index, ModAction.Unload, ""));
}

internal enum ModAction : byte { Enable, Disable, Reload, Load, Unload }

/// Host-supplied configuration for the modding plugin. Register an instance with
/// `app.AddResource(new ModdingConfig { ... })` BEFORE adding the plugin; the
/// plugin reads it at Startup. If absent the plugin uses an empty default (mods
/// load but see no registered components/resources and no game-specific imports).
public sealed class ModdingConfig
{
    /// Components + resources the host exposes to mods, keyed by WIT type-path.
    public ModComponentRegistry Registry = new();

    /// Mod relay for a host that can't embed wasmtime — a wasm guest (MOD_RELAY_GUEST)
    /// supplies its relay executor here, and its native host runs the actual
    /// component mods behind the abi/mod-abi.fbs wire. Null (the default) on desktop
    /// means the embedded component backend. Internal: only a host in this assembly's
    /// InternalsVisibleTo friend list (TinyEcsModdingFriends) constructs an
    /// IModWasmExecutor.
    internal IModWasmExecutor? WasmExecutor;

    /// Mod DISCOVERY override — a JSON manifest-array provider for a host with no
    /// filesystem scan of its own (the relay guest: its native host lists the mods).
    /// Null (the default) means the filesystem scan (DiscoverFolderMods) is used.
    internal Func<string>? WasmManifestSource;

    /// Folder (relative to the exe + cwd) scanned for wasm32-wasip2 component mods.
    /// Ignored when WasmManifestSource is set (discovery comes from it instead).
    public string ModFolder = "ecs-mods";

    /// Per-mod hook run once per loaded mod right after its ModHostContext is created
    /// (before Load), for EVERY backend. Use to wire host capabilities onto the ctx —
    /// e.g. ConsumeMouse/ConsumeKeyboard, the game-specific host imports
    /// (ctx.HostImportModule + ctx.HostImports descriptors — see ModHostImports.cs),
    /// a filter delegate. There is no separate linker hook.
    public readonly List<Action<ModHostContext>> PerModContext = new();

    /// Per-mod hook run when a mod stops running: on DISABLE, and on RELOAD before the
    /// fresh instance's setup. The argument is the manifest name. The lib already
    /// despawns the mod's entities itself; this is for host state a mod publishes
    /// OUTSIDE the World (its registered hotkeys, a cached binding table) which would
    /// otherwise keep firing for a mod that is no longer running. Not called on enable
    /// — the mod re-publishes from its own ModStartup.
    public readonly List<Action<string>> OnModTeardown = new();

#if !WASM_GUEST
    /// Run once when the component backend is created, to
    /// define the host's own WIT interfaces on the shared component linker (see
    /// Component/ComponentModImports.cs). The generic `tinyecs:modding/ecs` is built in.
    public readonly List<Action<ComponentModImports>> ComponentImports = new();
#endif
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
    /// The Startup system that discovers + loads + sets up the mods. A host orders its
    /// own Startup work (anything reading the loaded set) After this.
    public const string Loader = "tinyecs:mod_loader";

    /// The mod wire-ABI version this host speaks (abi/mod-abi.fbs, stamped into every
    /// Handshake by ModAbiRunner). Public so a host that installs mods from a registry
    /// can refuse a module built against a different ABI BEFORE instantiating it.
    public const uint AbiVersion = ModAbiRunner.AbiVersion;

    public void Build(App app)
    {
        if (!app.HasResource<App>())
            app.AddResource(app);
        if (!app.HasResource<ModdingConfig>())
            app.AddResource(new ModdingConfig());
        var runtimesRes = new ModRuntimes();
        app.AddResource(runtimesRes);
        app.AddResource(new ModControl());
        // Host functions (env.mod_call): the host adds its own at build time.
        if (!app.HasResource<ModHostFunctions>())
            app.AddResource(new ModHostFunctions());
        if (!app.HasResource<ModPacketChain>())
            app.AddResource(new ModPacketChain());
        app.GetResource<ModPacketChain>().Runtimes = runtimesRes;

        var setupFn = SetupEcsMods;
        app.AddSystem(setupFn).InStage(Stage.Startup).Label(Loader).Build();

        // One dispatcher per Stage. SingleThreaded: mod systems touch the World
        // directly through the bridge (see GuestBridge), so they must not share a
        // parallel batch.
        AddRunner(app, Stage.First, ModSchedule.First, RunnerFirst);
        AddRunner(app, Stage.PreUpdate, ModSchedule.PreUpdate, RunnerPreUpdate);
        AddRunner(app, Stage.Update, ModSchedule.Update, RunnerUpdate);
        AddRunner(app, Stage.PostUpdate, ModSchedule.PostUpdate, RunnerPostUpdate);
        AddRunner(app, Stage.Last, ModSchedule.Last, RunnerLast);

        // Pointer bridges: Bevy.UI's click / right-click / enter / leave triggers on a
        // MOD-OWNED entity are re-emitted as the mod-facing events (ModClick,
        // ModRightClick, ModHover — Markers.cs) on that entity, so a mod observes them
        // like any other event instead of polling a marker. Host entities are
        // ignored: a mod-wide observer would otherwise wake on every host click.
        app.AddObserver<On<UiClick>, Commands, Query<Data<ModEntity>>>((trigger, commands, modQ) =>
        {
            if (modQ.Contains(trigger.EntityId))
                commands.Entity(trigger.EntityId).EmitTrigger(new ModClick());
        });
        // The trigger's payload Position is the right-PRESS point (latched in
        // InteractionSystem); a mod context menu opens there.
        app.AddObserver<On<UiRightClick>, Commands, Query<Data<ModEntity>>>((trigger, commands, modQ) =>
        {
            if (modQ.Contains(trigger.EntityId))
                commands.Entity(trigger.EntityId).EmitTrigger(new ModRightClick
                {
                    X = trigger.Event.Position.X,
                    Y = trigger.Event.Position.Y,
                });
        });
        app.AddObserver<On<UiOver>, Commands, Query<Data<ModEntity>>>((trigger, commands, modQ) =>
        {
            if (modQ.Contains(trigger.EntityId))
                commands.Entity(trigger.EntityId).EmitTrigger(new ModHover { Over = true });
        });
        app.AddObserver<On<UiOut>, Commands, Query<Data<ModEntity>>>((trigger, commands, modQ) =>
        {
            if (modQ.Contains(trigger.EntityId))
                commands.Entity(trigger.EntityId).EmitTrigger(new ModHover { Over = false });
        });

        // Drain buffered observer fires into the guest callbacks at end of frame,
        // after the Last runner — a safe single-threaded point (no mid-mutation
        // re-entry). Events fired during Last reach the guest next frame.
        app.AddSystem((ResMut<ModRuntimes> runtimes) =>
            {
                foreach (var rt in runtimes.Value.Runtimes)
                    if (rt.Enabled)
                        FlushObservers(rt);
                    else
                        // A disabled mod still has its host global observers wired
                        // (TinyEcs can't unregister them), so fires keep arriving.
                        // Drop them: dispatching would tick a disabled guest, and
                        // buffering would grow unbounded and replay stale fires on
                        // re-enable.
                        rt.ObserverFires.Clear();
            })
            .InStage(Stage.Last).After(RunnerLast).SingleThreaded().Build();

        // Apply queued enable/disable/reload requests at the same safe point. These
        // dispose wasm instances + spawn/despawn entities directly, so they must run
        // single-threaded and outside any mod system call (no re-entry).
        var processControlFn = ProcessModControl;
        app.AddSystem(processControlFn)
            .InStage(Stage.Last).After(RunnerLast).SingleThreaded().Build();
    }

    private static void AddRunner(App app, Stage stage, ModSchedule which, string label)
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

        var mods = config.WasmManifestSource != null ? DiscoverFromManifestJson(config.WasmManifestSource())
            : DiscoverFolderMods(config.ModFolder);
        if (mods.Length == 0)
            return;

        var runtimes = runtimesRes.Value;
        EnsureBackend(runtimes, config);

        var failedMods = new List<string>();
        foreach (var (manifest, wasmPath) in mods)
        {
            if (!LoadOne(appRes.Value, runtimes, controlRes.Value, config, manifest, wasmPath, out var error))
                failedMods.Add(manifest.Name);
        }

        Console.WriteLine("[ecs-mod] backend={0}: {1}/{2} mods loaded{3}",
            config.WasmExecutor != null ? "relay" : "component", runtimes.Runtimes.Count, mods.Length,
            failedMods.Count > 0 ? $" — FAILED: {string.Join(", ", failedMods)}" : "");
    }

    // The wasm runtime is created lazily on the first load: a host with an empty mod
    // folder at boot can still install one later (ModControl.Load) and needs a backend
    // then. Idempotent — repeat calls keep the existing engine.
    private static void EnsureBackend(ModRuntimes runtimes, ModdingConfig config)
    {
        if (runtimes.Backend != null)
            return;
        // A relay executor when the host supplies one (the relay guest, whose native
        // host runs the component mods), else the embedded component backend (desktop).
#if !WASM_GUEST || MOD_RELAY_GUEST
        if (config.WasmExecutor != null)
        {
            runtimes.Backend = new ModRelayBackend(config.WasmExecutor);
            return;
        }
#endif
#if !WASM_GUEST
        runtimes.Backend = new ComponentModBackend(config.ComponentImports);
#else
        throw new InvalidOperationException("a WasmGuest build loads mods only through ModdingConfig.WasmExecutor (-p:ModRelayGuest=true)");
#endif
    }

    // The lowest slot no live runtime holds. Slots are NOT list indices: Unload removes
    // a runtime from the middle of the list while later mods keep the slot their
    // entities (ModEntity.Slot) and the executor's instance table are keyed by, so a
    // fresh load must claim a genuinely free one, not Runtimes.Count.
    private static int FreeSlot(ModRuntimes runtimes)
    {
        for (var slot = 0; ; slot++)
        {
            var taken = false;
            foreach (var rt in runtimes.Runtimes)
                if (rt.Slot == slot) { taken = true; break; }
            if (!taken)
                return slot;
        }
    }

    // Instantiate one discovered mod and register it (runtime + host-visible ModInfo).
    // Shared by the boot scan and ModControl.Load so an installed-at-runtime mod goes
    // through byte-identical setup. Returns false (and the message) on failure.
    private static bool LoadOne(App app, ModRuntimes runtimes, ModControl control, ModdingConfig config,
        ModManifest manifest, string wasmPath, out string error)
    {
        error = "";
        var world = app.GetWorld();
        {
            try
            {
                var slot = FreeSlot(runtimes);
                var ctx = new ModHostContext { World = world, Registry = config.Registry, App = app, Slot = slot, Name = manifest.Name };
                foreach (var hook in config.PerModContext)
                    hook(ctx);

                // Bytes stay null for the slot-keyed relay: the native host holds the
                // component, the guest never sees raw bytes at all.
                var source = config.WasmManifestSource != null
                    ? new ModSource(manifest.Name, null)
                    : ReadModBytes(manifest.Name, wasmPath);
                RejectCoreModule(manifest, source.Bytes);
                var instance = runtimes.Backend.Load(in source, ctx);
                instance.Setup();

                var rt = new ModRuntime
                {
                    Manifest = manifest,
                    Instance = instance,
                    Ctx = ctx,
                    Slot = slot,
                    WasmPath = wasmPath,
                };
                runtimes.Runtimes.Add(rt);
                rt.Info = new ModInfo
                {
                    Name = manifest.Name,
                    Version = manifest.Version,
                    Enabled = true,
                    Replaces = ModManifest.CleanFeatures(manifest.Replaces),
                };
                control.Mods.Add(rt.Info);

                Console.WriteLine("[ecs-mod] loaded {0} v{1} ({2} systems, {3} observers)",
                    manifest.Name, manifest.Version, ctx.Systems.Count, ctx.Observers.Count + ctx.PacketObservers.Count);

                // Wire the observers the mod registered during setup to host globals.
                RegisterModObservers(app, rt);

                // mod-startup systems run once, now.
                RunSystemsForStage(rt, ModSchedule.ModStartup);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                Console.WriteLine("[ecs-mod] failed to load {0}: {1}", manifest.Name, e);
                return false;
            }
        }
    }

    // Mods are wasm32-wasip2 components only. Bytes are read off disk only for the
    // embedded component backend (the relay is slot-keyed, bytes null; its native host
    // does its own check), so
    // that is where a core module (layer bytes 00 00 at offset 6-7; a component has
    // 01 00) is turned away — before wasmtime gives a less helpful compile error.
    private static void RejectCoreModule(ModManifest manifest, byte[]? bytes)
    {
        if (bytes is { Length: >= 8 } && bytes[6] == 0x00 && bytes[7] == 0x00)
            throw new NotSupportedException(
                $"{manifest.Name} is a core-wasm (p1) module; mods must be wasm32-wasip2 components — rebuild against the current SDK");
    }

    private static ModSource ReadModBytes(string name, string wasmPath)
        => new ModSource(name, File.ReadAllBytes(wasmPath));

    // Filesystem discovery (wasmtime backend). Look both next to the exe (deployed
    // alongside the build) and in the working dir, so launch location doesn't
    // matter. Each mod is a subfolder with a mod.json manifest; dedup by manifest
    // name (exe dir wins over cwd).
    private static (ModManifest Manifest, string WasmPath)[] DiscoverFolderMods(string modFolder)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, modFolder),
            Path.Combine(Directory.GetCurrentDirectory(), modFolder),
        };
        return candidates
            .Where(Directory.Exists)
            .SelectMany(Directory.GetDirectories)
            .Select(LoadManifest)
            .OfType<(ModManifest Manifest, string WasmPath)>()
            .GroupBy(m => m.Manifest.Name)
            .Select(g => g.First())
            .ToArray();
    }

    // JSON discovery (ModdingConfig.WasmManifestSource — the relay guest's native host
    // lists the mods): no filesystem access. WasmPath is unused (empty) — the relay
    // re-instantiates by slot, never by re-reading bytes.
    private static (ModManifest Manifest, string WasmPath)[] DiscoverFromManifestJson(string json)
    {
        ModManifest[]? manifests;
        try
        {
            manifests = JsonSerializer.Deserialize(json, ModManifestJsonContext.Default.ModManifestArray);
        }
        catch (Exception e)
        {
            Console.WriteLine("[ecs-mod] bad manifest-list payload: {0}", e.Message);
            return Array.Empty<(ModManifest, string)>();
        }
        if (manifests == null)
            return Array.Empty<(ModManifest, string)>();

        return manifests
            .Where(m => !string.IsNullOrEmpty(m.Name))
            .GroupBy(m => m.Name)
            .Select(g => (g.First(), ""))
            .ToArray();
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

    private static void RunStage(ModRuntimes runtimes, ModSchedule which)
    {
        foreach (var rt in runtimes.Runtimes)
            if (rt.Enabled)
            {
                // Drain observer fires buffered since the previous stage BEFORE this
                // stage's systems run: a UiClick / OnInsert that fired in PreUpdate then
                // reaches the guest in time for its Update systems to react the SAME
                // frame (it used to wait for the Stage.Last flush = one frame of lag).
                // The Last flush stays — it catches fires produced during Last itself.
                FlushObservers(rt);
                RunSystemsForStage(rt, which);
            }
    }

    // Internal (not private): the lib tests drive one stage of one runtime directly to
    // cover the interval gate without standing up a wasm runtime.
    internal static void RunSystemsForStage(ModRuntime rt, ModSchedule which)
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
                NoteFailure(rt, $"system '{sys.Name}'", e);
            }
        }
    }

    // Idle-skip for systems that did not opt into run-on-change (see ShouldSkipRun;
    // used by the backends' RunSystem AFTER snapshots are built, so
    // the query evaluation is never paid twice): don't cross the component
    // boundary for a system whose every query matched zero entities LAST tick
    // too. The first all-empty tick always runs so the guest sees one empty
    // result set (e.g. ecs-ui's hover diff emits MouseLeave off it). Every 8th
    // idle tick still runs as a safety net for guest-side timer pumps that
    // piggyback on the call (~130ms at 60fps — tooltip-delay-scale latency).
    // Query-less systems (Commands-only) never skip — no signal to gate on.
    internal const int IdleSafetyRunPeriod = 8;

    /// Guest failures tolerated (cumulative until enable/reload) before a mod is
    /// switched off.
    internal const int MaxModFailures = 10;

    // The FIRST failure logs the whole exception (stack included — that is the one
    // worth debugging); later ones log only the message, because a trapping guest
    // fails every frame and would flood the log.
    internal static void NoteFailure(ModRuntime rt, string what, Exception e)
    {
        rt.LastError = e.Message;
        if (rt.Info != null)
            rt.Info.LastError = e.Message;
        var first = rt.FailureCount == 0;
        rt.FailureCount++;
        Console.WriteLine("[ecs-mod] {0} '{1}' failed: {2}", what, rt.Manifest.Name, first ? e.ToString() : e.Message);

        if (rt.FailureCount < MaxModFailures || !rt.Enabled)
            return;
        rt.Enabled = false;
        if (rt.Info != null)
            rt.Info.Enabled = false;
        rt.ObserverFires.Clear();
        Console.WriteLine("[ecs-mod] mod '{0}' disabled after {1} failures", rt.Manifest.Name, MaxModFailures);
    }

    /// The one skip decision every backend makes, AFTER it built the run's inputs
    /// (query snapshots, resource values, event swap) and BEFORE it crosses into the
    /// guest. A run-on-change system (`system.run-on-change`) is skipped when it has at
    /// least one input parameter (query / res / events — commands don't count), every
    /// query matched zero rows, no event arrived and every resource equals what its
    /// previous run saw; its first run never skips. Every other system keeps the
    /// idle-skip. Skipping never loses a change: the Changed / Added window closes only
    /// over a zero-row evaluation, and events are only consumed when there are none.
    internal static bool ShouldSkipRun(ModSystemSpec sys, bool idleGated, bool hasInputs, bool anyRows, bool resChanged)
    {
        if (!sys.RunOnChange)
            return ShouldSkipIdle(sys, idleGated, anyRows);
        if (sys.HasRun && hasInputs && !anyRows && !resChanged)
            return true;
        sys.HasRun = true;
        return false;
    }

    internal static bool ShouldSkipIdle(ModSystemSpec sys, bool hasQuery, bool anyRows)
    {
        if (!hasQuery || anyRows)
        {
            sys.EmptyStreak = 0;
            return false;
        }
        sys.EmptyStreak++;
        // Streak 1 = the transition tick — run it. Then run every Nth.
        return sys.EmptyStreak > 1 && (sys.EmptyStreak % IdleSafetyRunPeriod) != 0;
    }

    // Drain host enable/disable/reload requests at a safe single-threaded point.
    private static void ProcessModControl(ResMut<ModRuntimes> runtimesRes, ResMut<ModControl> controlRes, Res<App> appRes, Res<ModdingConfig> configRes)
    {
        var control = controlRes.Value;
        if (control.Pending.Count == 0)
            return;

        var runtimes = runtimesRes.Value;
        while (control.Pending.Count > 0)
        {
            var (idx, action, dir) = control.Pending.Dequeue();
            // Load is index-free (the mod isn't in the list yet) — handle it before
            // the index bounds check the in-place actions need.
            if (action == ModAction.Load)
            {
                try { LoadFolder(runtimes, control, appRes.Value, configRes.Value, dir); }
                catch (Exception e) { Console.WriteLine("[ecs-mod] load '{0}' failed: {1}", dir, e); }
                continue;
            }
            if (idx < 0 || idx >= runtimes.Runtimes.Count)
                continue;
            var rt = runtimes.Runtimes[idx];
            var info = idx < control.Mods.Count ? control.Mods[idx] : null;
            try
            {
                switch (action)
                {
                    case ModAction.Disable: DisableMod(rt, info, configRes.Value); break;
                    case ModAction.Enable: EnableMod(rt, info); break;
                    case ModAction.Reload: ReloadMod(runtimes, rt, appRes.Value, info, configRes.Value); break;
                    case ModAction.Unload: UnloadMod(runtimes, control, rt, info, configRes.Value); break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[ecs-mod] {0} on '{1}' failed: {2}", action, rt.Manifest.Name, e);
            }
        }
    }

    // Load a mod folder into the running app (a mod store just installed it). Reads
    // `<dir>/mod.json` the same way the boot scan does, so an installed mod is
    // indistinguishable from one that was there at startup.
    private static void LoadFolder(ModRuntimes runtimes, ModControl control, App app, ModdingConfig config, string dir)
    {
        var found = LoadManifest(dir);
        if (found == null)
            return;
        var (manifest, wasmPath) = found.Value;
        if (control.Mods.Exists(m => string.Equals(m.Name, manifest.Name, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("[ecs-mod] load '{0}': already loaded — unload it first", manifest.Name);
            return;
        }
        EnsureBackend(runtimes, config);
        LoadOne(app, runtimes, control, config, manifest, wasmPath, out _);
    }

    // Full teardown: stop the mod, drop its entities, dispose the wasm instance and
    // forget it. Disable keeps the instance alive for a cheap re-enable; this is the
    // uninstall path, where the .wasm on disk is about to go away.
    private static void UnloadMod(ModRuntimes runtimes, ModControl control, ModRuntime rt, ModInfo? info, ModdingConfig config)
    {
        // Enabled=false first: the mod's host global observers can't be unregistered
        // (TinyEcs has no removal), and their `wanted` closure reads this flag — so a
        // fire arriving after the runtime is gone is dropped instead of queued.
        var wasEnabled = rt.Enabled;
        rt.Enabled = false;
        if (info != null) info.Enabled = false;
        rt.ObserverFires.Clear();
        DespawnModEntities(rt.Ctx.World, rt.Slot);
        if (wasEnabled)
            NotifyTeardown(config, rt.Manifest.Name);
        try { rt.Instance.Dispose(); }
        catch (Exception e) { Console.WriteLine("[ecs-mod] dispose '{0}': {1}", rt.Manifest.Name, e.Message); }
        runtimes.Runtimes.Remove(rt);
        if (info != null)
            control.Mods.Remove(info);
        rt.Info = null;
        Console.WriteLine("[ecs-mod] unloaded {0}", rt.Manifest.Name);
    }

    // Stop a mod ticking and remove everything it spawned. The wasm instance stays
    // loaded; re-enable re-runs its startup. Only its host entities are despawned.
    private static void DisableMod(ModRuntime rt, ModInfo? info, ModdingConfig config)
    {
        if (!rt.Enabled)
            return;
        rt.Enabled = false;
        if (info != null) info.Enabled = false;
        rt.ObserverFires.Clear();
        DespawnModEntities(rt.Ctx.World, rt.Slot);
        NotifyTeardown(config, rt.Manifest.Name);
    }

    // Let the host drop per-mod state it holds outside the World (see
    // ModdingConfig.OnModTeardown). A throwing hook must not abort the
    // disable/reload it is reacting to.
    private static void NotifyTeardown(ModdingConfig config, string modName)
    {
        foreach (var hook in config.OnModTeardown)
        {
            try { hook(modName); }
            catch (Exception e) { Console.WriteLine("[ecs-mod] teardown hook for '{0}' failed: {1}", modName, e); }
        }
    }

    // Resume ticking and re-run the mod's startup so it rebuilds its UI. A mod whose
    // startup is one-shot-guarded won't rebuild until a reload; the React UI mod does.
    private static void EnableMod(ModRuntime rt, ModInfo? info)
    {
        if (rt.Enabled)
            return;
        rt.Enabled = true;
        if (info != null) info.Enabled = true;
        ResetFailures(rt, info);
        RunSystemsForStage(rt, ModSchedule.ModStartup);
    }

    // An explicit enable/reload is the host saying "try again" — forget the budget.
    private static void ResetFailures(ModRuntime rt, ModInfo? info)
    {
        rt.FailureCount = 0;
        rt.LastError = "";
        if (info != null) info.LastError = "";
    }

    // Tear the mod down and re-instantiate it (picks up a rebuilt .wasm). Reuses the
    // same ModRuntime + ModHostContext AND the existing Linker + bridge (wasmtime): the
    // host import functions never change between loads, and the fork registers every
    // linker.Define'd function in a STATIC, process-global, never-freed table
    // (ComponentExport.RegisterFunction). Re-Define'ing on reload would leak a full set
    // of slots each time, so we do NOT build a new Linker — we only recompile the component and instantiate it on a fresh Store
    // with the original linker. Zero new function registrations. Under the relay there
    // is no Linker or bytes at all: the native host re-instantiates by slot.
    //
    // CEILING (inherent, no clean fix here): TinyEcs has no global-observer removal,
    // so observers wired at first load persist (packet observers are exempt: the packet
    // chain reads ctx.PacketObservers live, so a reload rewires them fully). Same-named exports on the new
    // instance still receive them, but observers a mod registers ONLY on reload are
    // not wired.
    private static void ReloadMod(ModRuntimes runtimes, ModRuntime rt, App app, ModInfo? info, ModdingConfig config)
    {
        DespawnModEntities(rt.Ctx.World, rt.Slot);
        NotifyTeardown(config, rt.Manifest.Name);

        var observersBefore = ObserverSignatures(rt.Ctx.Observers);

        // Re-setup repopulates these from the fresh instance's setup() call.
        rt.Ctx.Systems.Clear();
        rt.Ctx.SystemsByStage.Clear();
        rt.Ctx.Observers.Clear();
        rt.Ctx.PacketObservers.Clear();
        Array.Clear(rt.Ctx.PacketInterest);
        foreach (var buffer in rt.Ctx.EventBuffers)
            buffer.Dead = true;
        rt.Ctx.EventBuffers.Clear();

        // Backend tears down + re-instantiates (reusing host imports where it applies)
        // and re-runs setup, which repopulates ctx.Systems via the guest.
        var source = config.WasmManifestSource != null
            ? new ModSource(rt.Manifest.Name, null)
            : ReadModBytes(rt.Manifest.Name, rt.WasmPath);
        RejectCoreModule(rt.Manifest, source.Bytes);
        rt.Instance.Reload(in source);

        // The CEILING above, made visible: the host globals wired at first load stay
        // bound to the observer set the mod declared THEN. A reload declaring a
        // different set silently loses the new ones, which reads as a dead mod.
        if (!ObserverSignatures(rt.Ctx.Observers).SetEquals(observersBefore))
            Console.WriteLine(
                "[ecs-mod] '{0}': observer set changed on reload — observers added on reload are NOT wired until a restart",
                rt.Manifest.Name);

        rt.Enabled = true;
        if (info != null) info.Enabled = true;
        ResetFailures(rt, info);

        RunSystemsForStage(rt, ModSchedule.ModStartup);
    }

    private static HashSet<string> ObserverSignatures(List<ModObserverSpec> observers)
    {
        var set = new HashSet<string>();
        foreach (var o in observers)
            set.Add($"{o.Kind}|{o.TypePath}|{o.Name}");
        return set;
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
        // A disabled mod's fire queue is cleared on disable and never drained, so both
        // the enqueue AND the component serialization feeding it are dead work — ask
        // rt.Enabled first (component mappers check it BEFORE serializing).
        var wanted = () => rt.Enabled;
        foreach (var obs in rt.Ctx.Observers)
            RegisterObserver(app, obs, rt.Ctx.Registry, wanted,
                (string name, ulong e, ReadOnlySpan<byte> json, bool binary) => rt.ObserverFires.Enqueue(name, e, json, binary));
    }

    /// A buffered observer fire (payload valid for the call only): UTF8 JSON, or
    /// Encoding.Typed bytes when `binary`.
    internal delegate void ModObserverFire(string name, ulong entity, ReadOnlySpan<byte> json, bool binary);

    // Internal + testable: maps one observer spec to the matching host global
    // observer. Component events resolve their type-path via the registry.
    internal static void RegisterObserver(App app, ModObserverSpec obs, ModComponentRegistry registry, Action<string, ulong, string> onFire)
        => RegisterObserver(app, obs, registry, static () => true, onFire);

    internal static void RegisterObserver(App app, ModObserverSpec obs, ModComponentRegistry registry, Func<bool> wanted, Action<string, ulong, string> onFire)
        => RegisterObserver(app, obs, registry, wanted,
            (string name, ulong e, ReadOnlySpan<byte> json, bool _) => onFire(name, e, System.Text.Encoding.UTF8.GetString(json)));

    internal static void RegisterObserver(App app, ModObserverSpec obs, ModComponentRegistry registry, Func<bool> wanted, ModObserverFire onFire)
    {
        switch (obs.Kind)
        {
            case ModObserverKind.Spawn:
                app.AddObserver<OnSpawn>(t => { if (wanted()) onFire(obs.Name, t.EntityId, default, false); });
                break;
            case ModObserverKind.Despawn:
                app.AddObserver<OnDespawn>(t => { if (wanted()) onFire(obs.Name, t.EntityId, default, false); });
                break;
            case ModObserverKind.Insert:
                if (obs.TypePath == null)
                    break;
                // Typed when asked for and the mapper hands the payload typed; else its JSON.
                if (obs.Binary && registry.TryGetBinary(obs.TypePath, out var bi) && bi is IModBinaryTrigger ti
                    && ti.TryObserveInsert(app, wanted, (e, bytes) => onFire(obs.Name, e, bytes, true)))
                    break;
                if (registry.TryGet(obs.TypePath, out var ci))
                    ci.RegisterInsertObserverUtf8(app, wanted, (e, json) => onFire(obs.Name, e, json, false));
                break;
            case ModObserverKind.Remove:
                if (obs.TypePath == null)
                    break;
                if (obs.Binary && registry.TryGetBinary(obs.TypePath, out var br) && br is IModBinaryTrigger tr
                    && tr.TryObserveRemove(app, wanted, (e, bytes) => onFire(obs.Name, e, bytes, true)))
                    break;
                if (registry.TryGet(obs.TypePath, out var cr))
                    cr.RegisterRemoveObserverUtf8(app, wanted, (e, json) => onFire(obs.Name, e, json, false));
                break;
            case ModObserverKind.Custom:
                if (obs.TypePath == null)
                    break;
                // `wanted` first, so a disabled mod's fire costs no serialization.
                if (obs.Binary && registry.TryGetBinaryEvent(obs.TypePath, out var be))
                    be.Observe(app, wanted, (e, bytes) => onFire(obs.Name, e, bytes, true));
                else if (registry.TryGetEvent(obs.TypePath, out var ev))
                    ev.RegisterObserverUtf8(app, wanted, (e, json) => onFire(obs.Name, e, json, false));
                break;
        }
    }

    // Dispatch buffered observer fires to the guest. Each fire calls the guest
    // export `name` with (entity: u64, json: string). Not yet exercised by a test
    // fixture — needs a mod that exports an observer callback.
    private static void FlushObservers(ModRuntime rt)
    {
        while (rt.ObserverFires.TryDequeue(out var name, out var entity, out var json, out var binary))
        {
            try
            {
                if (binary)
                    rt.Instance.CallObserverBinary(name, entity, json);
                else
                    rt.Instance.CallObserver(name, entity, json);
            }
            catch (Exception e)
            {
                NoteFailure(rt, $"observer '{name}'", e);
            }
        }
    }

    // Builds the matching-entity snapshot into an ArrayPool-rented buffer (caller
    // owns it: returned in the backend's RunSystem finally after the wasm Call).
    // Returns the buffer; `matched` is the valid prefix length. `candidates` is
    // method-scoped scratch (PooledList). Runtime-agnostic — used by every backend.
    internal static ulong[] BuildSnapshot(ModHostContext ctx, ModQuerySpec q, uint sinceTick, out int matched)
    {
        matched = 0;

        // Resolved once per spec — no type-path hashing per term per entity below.
        var mappers = q.TermMappers(ctx.Registry);

        if (!q.SnapshotPlanBuilt)
        {
            q.SnapshotPlan = ModSnapshotPlan.Build(ctx.World, mappers);
            q.SnapshotPlanBuilt = true;
        }
        if (q.SnapshotPlan != null)
            return q.SnapshotPlan.Run(ctx.World, mappers, sinceTick, out matched);

        // Driver = first present-required term (ref/mut/with/changed/added) that is
        // registered. An Added driver collects by presence; the term loop filters it.
        IModComponent? driver = null;
        var driverChanged = false;
        var driverIndex = -1;
        for (var ti = 0; ti < mappers.Length; ti++)
            if (mappers[ti].Kind != ModQueryTermKind.Without && mappers[ti].Comp != null)
            {
                driver = mappers[ti].Comp;
                driverChanged = mappers[ti].Kind == ModQueryTermKind.Changed;
                driverIndex = ti;
                break;
            }
        if (driver == null)
            return ArrayPool<ulong>.Shared.Rent(1);

        // Per-candidate term order: the tick-filtered terms (Changed / Added) first —
        // they reject almost every candidate of a presence-collected scan, so the
        // presence lookups after them run only for the few that changed. A presence
        // driver (Ref / Mut / With) is skipped: collection already proved it.
        Span<int> order = mappers.Length <= 32 ? stackalloc int[mappers.Length] : new int[mappers.Length];
        var orderCount = 0;
        for (var ti = 0; ti < mappers.Length; ti++)
            if (mappers[ti].Kind is ModQueryTermKind.Changed or ModQueryTermKind.Added)
                order[orderCount++] = ti;
        for (var ti = 0; ti < mappers.Length; ti++)
            if (mappers[ti].Kind is not (ModQueryTermKind.Changed or ModQueryTermKind.Added)
                && !(ti == driverIndex && !driverChanged && mappers[ti].Comp != null))
                order[orderCount++] = ti;

        // Not `using` — CollectEntities needs `candidates` by ref (Add may grow),
        // and a using-variable can't be passed by ref (CS1657). Dispose by hand.
        var candidates = new PooledList<ulong>(16);
        try
        {
            // A Changed driver narrows the scan itself (tick-filtered collect); every
            // other driver collects by presence and the per-term loop below filters.
            if (driverChanged)
                driver.CollectChangedEntities(ctx.World, sinceTick, ref candidates);
            else
                driver.CollectEntities(ctx.World, ref candidates);

            // matched ⊆ candidates, so a candidates-sized buffer never overflows.
            var result = ArrayPool<ulong>.Shared.Rent(candidates.Count == 0 ? 1 : candidates.Count);
            for (var ci = 0; ci < candidates.Count; ci++)
            {
                var id = candidates[ci];
                var ok = true;
                for (var oi = 0; oi < orderCount; oi++)
                {
                    var (comp, kind) = mappers[order[oi]];
                    if (comp == null)
                    {
                        if (kind != ModQueryTermKind.Without) { ok = false; break; }
                        continue;
                    }
                    var has = kind switch
                    {
                        ModQueryTermKind.Changed => comp.ChangedSince(ctx.World, id, sinceTick),
                        ModQueryTermKind.Added => comp.AddedSince(ctx.World, id, sinceTick),
                        _ => comp.Has(ctx.World, id),
                    };
                    if (kind == ModQueryTermKind.Without && has) { ok = false; break; }
                    if (kind != ModQueryTermKind.Without && !has) { ok = false; break; }
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
