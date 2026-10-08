// Desktop IModWasmExecutor: hosts core-module (NOT Component Model) mods on the
// UPSTREAM official Wasmtime NuGet (aliased UpstreamWt in the csproj comment),
// separate from the fork's component-model host (long removed). One Wasmtime
// Engine per process; Load() creates a per-mod Linker + Store + Instance and
// returns an index into _slots as the opaque handle every other call takes.
//
// SPAN RULE: the guest memory can grow on any guest call or alloc, which relocates
// its backing pointer and invalidates a previously-taken Span. NEVER hold a
// Memory.GetSpan() across a guest call or an alloc — re-acquire it immediately
// before each read/write. WriteInputToArena / CopyPackedOut / the host import
// glue below all obey this.
//
// Desktop only: excluded from the browser guest build (see the csproj) — the
// browser host cannot embed wasmtime in-process; a GUEST_CORE guest instead relays
// through flat env imports to JS (see the planned guest-relay executor).

using System.Buffers;
using Wasmtime;

namespace TinyEcs.Bevy.Modding;

internal sealed class WasmtimeModWasmExecutor : IModWasmExecutor
{
    // EXECUTION LIMITS: a guest that loops forever would otherwise hang the frame
    // loop with no way out. Epoch interruption is the cheap escape (no per-block fuel
    // accounting): a background timer bumps the engine epoch every EpochPeriodMs, and
    // every guest call arms a relative deadline first, so an overrun TRAPS and surfaces
    // as an exception in the caller's per-system catch (which counts it toward the
    // mod's auto-disable budget).
    private const int EpochPeriodMs = 10;
    // ~1s of wall clock: orders of magnitude above a real per-frame call (tens of
    // microseconds), still bounded.
    private const ulong CallDeadlineTicks = 100;
    // Instantiation + mod_setup do real work in a NativeAOT C# guest (static ctors,
    // the whole registry handshake), so they get a much wider budget.
    private const ulong SetupDeadlineTicks = 3000;
    // 1 GiB linear memory ceiling: a runaway guest fails its memory.grow instead of
    // taking the process's address space with it.
    private const long MemoryLimitBytes = 1L << 30;

    // ONE engine + epoch timer per process (Engine is built to be shared). A per-executor
    // pair leaked: App has no dispose path, and the live Timer rooted the executor — its
    // engine, compiled code and every guest Store — for the process lifetime. A test
    // suite booting a 44 MB NativeAOT mod per test grew to 11 GB that way.
    private static readonly Engine _engine = new(new Config().WithEpochInterruption(true));
    private static readonly System.Threading.Timer _epochTimer =
        new(_ => _engine.IncrementEpoch(), null, EpochPeriodMs, EpochPeriodMs);

    // Compiled modules by mod name, reused while the bytes are unchanged: a cranelift
    // compile of a large guest costs seconds, and every new App (a reboot, a test)
    // would otherwise pay it again. A changed hash (hot reload) replaces the entry.
    private static readonly Dictionary<string, (byte[] Hash, Module Module)> _modules = new();

    private static Module Compile(string name, byte[] bytes)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        lock (_modules)
        {
            if (_modules.TryGetValue(name, out var hit) && hit.Hash.AsSpan().SequenceEqual(hash))
                return hit.Module;
            var module = Module.FromBytes(_engine, name, bytes);
            _modules[name] = (hash, module);
            return module;
        }
    }

    // Indexed by the caller-supplied slot (== ModHostContext.Slot); Load grows the
    // list as needed rather than relying on Count to already equal `slot` (a prior
    // mod's Load can fail without ever reaching this executor).
    private readonly List<Slot?> _slots = new();

    // Cached exports (re-resolved on reload); optional ones stay null when absent.
    internal sealed class Slot
    {
        public required Linker Linker;
        public required Store Store;
        public required Instance Instance;
        public required string Name;
        public required IModImportSink Sink;
        public required Imports Imports; // the strong ref the import callbacks only hold weakly

        public Memory Memory = null!;
        public Func<int, int> Alloc = null!;
        public Action ArenaReset = null!;
        public Func<int, int, long> SetupFn = null!;
        public Func<int, int, int, long>? RunFn;
        public Func<int, long, int, int, long>? ObserverFn;
        public Func<int, int, int, long>? OnPacketFn;
        public Action<int, int>? SpawnedFn;

        // Grow-only copy-out buffer for the guest's packed reply — see CopyPackedOut.
        public byte[] Reply = new byte[1024];
    }

    public int Load(in ModSource source, int slot, IModImportSink sink, string importModule, IReadOnlyList<ModHostImport> hostImports)
    {
        var linker = new Linker(_engine);
        linker.DefineWasi();
        var imports = new Imports(sink, hostImports);
        DefineImports(linker, importModule, new WeakReference<Imports>(imports), hostImports);

        var store = CreateStore(_engine);
        var module = Compile(source.Name, source.Bytes!);
        var instance = linker.Instantiate(store, module);

        var entry = new Slot { Linker = linker, Store = store, Instance = instance, Name = source.Name, Sink = sink, Imports = imports };
        // Before CacheExports: the reactor's _initialize may already mod_call.
        imports.Owner = entry;
        CacheExports(entry);

        while (_slots.Count <= slot)
            _slots.Add(null);
        _slots[slot] = entry;
        return slot;
    }

    public Memory<byte> CallSetup(int handle, ReadOnlySpan<byte> handshake)
    {
        var slot = _slots[handle]!;
        slot.Store.SetEpochDeadline(SetupDeadlineTicks);
        var ptr = WriteInputToArena(slot, handshake);
        var packed = (ulong)slot.SetupFn(ptr, handshake.Length);
        return CopyPackedOut(slot, packed);
    }

    public Memory<byte> CallRun(int handle, uint sysId, ReadOnlySpan<byte> input)
    {
        var slot = _slots[handle]!;
        if (slot.RunFn == null)
            return default;
        slot.Store.SetEpochDeadline(CallDeadlineTicks);
        var ptr = WriteInputToArena(slot, input);
        var packed = (ulong)slot.RunFn((int)sysId, ptr, input.Length);
        return CopyPackedOut(slot, packed);
    }

    public Memory<byte> CallObserver(int handle, uint obsId, ulong entity, ReadOnlySpan<byte> input)
    {
        var slot = _slots[handle]!;
        if (slot.ObserverFn == null)
            return default;
        slot.Store.SetEpochDeadline(CallDeadlineTicks);
        var ptr = WriteInputToArena(slot, input);
        var packed = (ulong)slot.ObserverFn((int)obsId, (long)entity, ptr, input.Length);
        return CopyPackedOut(slot, packed);
    }

    // mod_on_packet(dir, ptr, len) -> i64: 0 pass, 1 block, else len<<32|ptr of the
    // replacement bytes in the guest arena (copied out into the reply buffer).
    public ModPacketVerdict CallOnPacket(int handle, ModPacketDirection dir, ReadOnlySpan<byte> data, out ReadOnlySpan<byte> replacement)
    {
        replacement = default;
        var slot = _slots[handle]!;
        if (slot.OnPacketFn == null)
            return ModPacketVerdict.Pass;
        slot.Store.SetEpochDeadline(CallDeadlineTicks);
        var ptr = WriteInputToArena(slot, data);
        var packed = slot.OnPacketFn((int)dir, ptr, data.Length);
        if (packed == 0)
            return ModPacketVerdict.Pass;
        if (packed == 1)
            return ModPacketVerdict.Block;
        replacement = CopyPackedOut(slot, (ulong)packed).Span;
        return replacement.IsEmpty ? ModPacketVerdict.Pass : ModPacketVerdict.Replace;
    }

    public void CallSpawned(int handle, ReadOnlySpan<byte> input)
    {
        var slot = _slots[handle]!;
        if (slot.SpawnedFn == null)
            return;
        slot.Store.SetEpochDeadline(CallDeadlineTicks);
        var ptr = WriteInputToArena(slot, input);
        slot.SpawnedFn(ptr, input.Length);
    }

    // Tear down + re-instantiate, REUSING the Linker (the host imports never
    // change; re-defining would re-register callbacks — the fork's static,
    // process-global, capped function table leaked a slot per re-Define, which is
    // why ModdingPlugin.ReloadMod never builds a new Linker either).
    public void Reload(int handle, in ModSource source)
    {
        var slot = _slots[handle]!;
        try { slot.Store.Dispose(); } catch { /* already torn down */ }

        slot.Store = CreateStore(_engine);
        // The old store's memory is gone; mod_call falls back to the caller's until
        // CacheExports binds the new one.
        slot.Memory = null!;
        var module = Compile(slot.Name, source.Bytes!);
        slot.Instance = slot.Linker.Instantiate(slot.Store, module);
        CacheExports(slot);
    }

    public void DisposeInstance(int handle)
    {
        var slot = _slots[handle];
        if (slot == null)
            return;
        try { slot.Store.Dispose(); } catch { /* already torn down */ }
        _slots[handle] = null;
    }

    // The engine, timer and module cache are process-wide; only the stores are ours.
    public void Dispose()
    {
        for (var i = 0; i < _slots.Count; i++)
            DisposeInstance(i);
    }

    internal static Store CreateStore(Engine engine)
    {
        var store = new Store(engine);
        store.SetWasiConfiguration(new WasiConfiguration()
            .WithInheritedStandardOutput()
            .WithInheritedStandardError());
        store.SetLimits(memorySize: MemoryLimitBytes);
        // Covers instantiate + the reactor's _initialize, which run before the first
        // CallSetup gets to arm a deadline of its own.
        store.SetEpochDeadline(SetupDeadlineTicks);
        return store;
    }

    private static void CacheExports(Slot slot)
    {
        var instance = slot.Instance;
        slot.Memory = instance.GetMemory("memory") ?? throw new InvalidOperationException("core mod exports no 'memory'");
        slot.Alloc = instance.GetFunction<int, int>("mod_alloc") ?? throw new InvalidOperationException("core mod exports no 'mod_alloc'");
        slot.ArenaReset = instance.GetAction("mod_arena_reset") ?? throw new InvalidOperationException("core mod exports no 'mod_arena_reset'");
        slot.SetupFn = instance.GetFunction<int, int, long>("mod_setup") ?? throw new InvalidOperationException("core mod exports no 'mod_setup'");
        slot.RunFn = instance.GetFunction<int, int, int, long>("mod_run");
        slot.ObserverFn = instance.GetFunction<int, long, int, int, long>("mod_observer");
        slot.OnPacketFn = instance.GetFunction<int, int, int, long>("mod_on_packet");
        slot.SpawnedFn = instance.GetAction<int, int>("mod_spawned");
        // WASI reactor init (globals / component ctors) — before any other export.
        instance.GetAction("_initialize")?.Invoke();
    }

    // Reset the bump arena, allocate `len`, copy the input in. Returns the guest
    // pointer. SPAN RULE: memory is re-acquired AFTER alloc.
    private static int WriteInputToArena(Slot slot, ReadOnlySpan<byte> input)
    {
        slot.ArenaReset();
        var ptr = slot.Alloc(input.Length);
        if (input.Length > 0)
            input.CopyTo(slot.Memory.GetSpan(ptr, input.Length));
        return ptr;
    }

    // Packed guest return = len&lt;&lt;32 | ptr, 0 = none. The bytes live in the guest
    // arena until the next arena_reset, so they must be copied out before the caller
    // (ModAbiRunner) parses them — into the slot's GROW-ONLY reply buffer, not a
    // right-sized array: this runs per system per stage per frame. The returned slice
    // is valid until the next call on this handle (the IModWasmExecutor contract).
    private static Memory<byte> CopyPackedOut(Slot slot, ulong packed)
    {
        if (packed == 0)
            return default;
        var ptr = (int)(packed & 0xFFFFFFFFUL);
        var len = (int)(packed >> 32);
        if (len <= 0)
            return default;
        if (slot.Reply.Length < len)
            slot.Reply = new byte[Math.Max(slot.Reply.Length * 2, len)];
        slot.Memory.GetSpan(ptr, len).CopyTo(slot.Reply);
        return new Memory<byte>(slot.Reply, 0, len);
    }

    // The host imports (see abi/mod-abi.fbs header): mid-run RPCs. `log` and
    // `env.mod_call` route to `sink` (ModAbiBacking); any legacy host-described
    // ModHostImport descriptors are matched to wasm signatures by Kind. All under the
    // host's import module, all resolving THIS mod's own ptr/len against the
    // Wasmtime Caller's memory.
    // Wasmtime-dotnet roots every import callback in a strong GCHandle that is freed only
    // when the native linker goes. A callback capturing the sink (-> backend -> App) closed
    // that cycle, so a dropped App — Store, guest linear memory and all — never became
    // collectable. Callbacks reach the targets through this weak hop; the Slot holds the
    // strong ref, so they stay live exactly as long as the executor does.
    internal sealed class Imports(IModImportSink sink, IReadOnlyList<ModHostImport> host)
    {
        public readonly IModImportSink Sink = sink;
        public readonly IReadOnlyList<ModHostImport> Host = host;
        // The slot whose arena mod_call results are written into (set at Load).
        public Slot? Owner;
    }

    // The p1 host-function import: env.mod_call(name_ptr, name_len, args_ptr, args_len)
    // -> i64 (0 = unit, else len<<32|ptr of UTF-8 JSON in the guest arena, valid until
    // the next arena_reset). An unknown name / malformed args throws -> trap.
    internal const string ModCallModule = "env";
    internal const string ModCallName = "mod_call";

    private static Imports Get(WeakReference<Imports> weak)
        => weak.TryGetTarget(out var t) ? t : throw new ObjectDisposedException(nameof(Imports));

    private static void DefineImports(Linker linker, string module, WeakReference<Imports> w, IReadOnlyList<ModHostImport> hostImports)
    {
        CallerAction<int, int> log = (caller, ptr, len) => Get(w).Sink.Log(ReadUtf8(caller, ptr, len));
        linker.DefineFunction(module, "log", log);

        CallerFunc<int, int, int, int, long> modCall = (caller, namePtr, nameLen, argsPtr, argsLen) =>
        {
            var imports = Get(w);
            var slot = imports.Owner;
            // The slot's cached export (set before _initialize can call in): a
            // Caller.GetMemory per call is a fresh Memory wrapper on the heap.
            var memory = slot?.Memory ?? caller.GetMemory("memory")!;
            // The host body never calls the guest, so these spans stay valid for it.
            var result = imports.Sink.ModCall(
                nameLen > 0 ? memory.GetSpan(namePtr, nameLen) : default,
                argsLen > 0 ? memory.GetSpan(argsPtr, argsLen) : default);
            if (result.IsEmpty)
                return 0L;
            if (slot == null)
                throw new InvalidOperationException("mod_call before the mod finished loading");
            var len = result.Length;
            var ptr = slot.Alloc(len);
            result.CopyTo(slot.Memory.GetSpan(ptr, len)); // SPAN RULE: after alloc
            return ((long)len << 32) | (uint)ptr;
        };
        linker.DefineFunction(ModCallModule, ModCallName, modCall);

        for (var idx = 0; idx < hostImports.Count; idx++)
        {
            var i = idx;
            var name = hostImports[i].Name;
            switch (hostImports[i].Kind)
            {
                case ModHostImportKind.BytesIn:
                {
                    CallerAction<int, int> fn = (caller, ptr, len) =>
                        Get(w).Host[i].BytesIn!(len > 0 ? caller.GetMemory("memory")!.GetSpan(ptr, len) : default);
                    linker.DefineFunction(module, name, fn);
                    break;
                }
                case ModHostImportKind.U32ToU64:
                {
                    CallerFunc<int, long> fn = (caller, arg) => (long)Get(w).Host[i].U32ToU64!((uint)arg);
                    linker.DefineFunction(module, name, fn);
                    break;
                }
                case ModHostImportKind.U32ToU32:
                {
                    CallerFunc<int, int> fn = (caller, arg) => (int)Get(w).Host[i].U32ToU32!((uint)arg);
                    linker.DefineFunction(module, name, fn);
                    break;
                }
                case ModHostImportKind.U32TextToU32:
                {
                    CallerFunc<int, int, int, int> fn = (caller, arg, ptr, len) =>
                        (int)Get(w).Host[i].U32TextToU32!((uint)arg, ReadUtf8(caller, ptr, len));
                    linker.DefineFunction(module, name, fn);
                    break;
                }
                case ModHostImportKind.U32ToTextOut:
                {
                    CallerFunc<int, int, int, int> fn = (caller, arg, outPtr, cap) =>
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(Get(w).Host[i].U32ToTextOut!((uint)arg));
                        if (bytes.Length > 0 && bytes.Length <= cap)
                            bytes.CopyTo(caller.GetMemory("memory")!.GetSpan(outPtr, bytes.Length));
                        return bytes.Length;
                    };
                    linker.DefineFunction(module, name, fn);
                    break;
                }
            }
        }
    }

    private static string ReadUtf8(Caller caller, int ptr, int len)
        => len <= 0 ? string.Empty
            : System.Text.Encoding.UTF8.GetString(caller.GetMemory("memory")!.GetSpan(ptr, len));
}
