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
    private readonly Engine _engine = new();
    // Indexed by the caller-supplied slot (== ModHostContext.Slot); Load grows the
    // list as needed rather than relying on Count to already equal `slot` (a prior
    // mod's Load can fail without ever reaching this executor).
    private readonly List<Slot?> _slots = new();

    // Cached exports (re-resolved on reload); optional ones stay null when absent.
    private sealed class Slot
    {
        public required Linker Linker;
        public required Store Store;
        public required Instance Instance;
        public required string Name;
        public required IModImportSink Sink;

        public Memory Memory = null!;
        public Func<int, int> Alloc = null!;
        public Action ArenaReset = null!;
        public Func<int, int, long> SetupFn = null!;
        public Func<int, int, int, long>? RunFn;
        public Func<int, long, int, int, long>? ObserverFn;
        public Func<int, int, int, int>? FilterFn;
    }

    public int Load(in ModSource source, int slot, IModImportSink sink, string importModule, IReadOnlyList<ModHostImport> hostImports)
    {
        var linker = new Linker(_engine);
        linker.DefineWasi();
        DefineImports(linker, importModule, sink, hostImports);

        var store = CreateStore(_engine);
        var module = Module.FromBytes(_engine, source.Name, source.Bytes!);
        var instance = linker.Instantiate(store, module);

        var entry = new Slot { Linker = linker, Store = store, Instance = instance, Name = source.Name, Sink = sink };
        CacheExports(entry);

        while (_slots.Count <= slot)
            _slots.Add(null);
        _slots[slot] = entry;
        return slot;
    }

    public byte[]? CallSetup(int handle, byte[] handshake)
    {
        var slot = _slots[handle]!;
        var ptr = WriteInputToArena(slot, handshake);
        var packed = (ulong)slot.SetupFn(ptr, handshake.Length);
        return CopyPackedOut(slot, packed);
    }

    public byte[]? CallRun(int handle, uint sysId, byte[] input)
    {
        var slot = _slots[handle]!;
        if (slot.RunFn == null)
            return null;
        var ptr = WriteInputToArena(slot, input);
        var packed = (ulong)slot.RunFn((int)sysId, ptr, input.Length);
        return CopyPackedOut(slot, packed);
    }

    public byte[]? CallObserver(int handle, uint obsId, ulong entity, byte[] input)
    {
        var slot = _slots[handle]!;
        if (slot.ObserverFn == null)
            return null;
        var ptr = WriteInputToArena(slot, input);
        var packed = (ulong)slot.ObserverFn((int)obsId, (long)entity, ptr, input.Length);
        return CopyPackedOut(slot, packed);
    }

    public bool CallFilter(int handle, byte arg, ReadOnlySpan<byte> data)
    {
        var slot = _slots[handle]!;
        if (slot.FilterFn == null)
            return false;
        slot.ArenaReset();
        var ptr = slot.Alloc(data.Length);
        if (data.Length > 0)
            data.CopyTo(slot.Memory.GetSpan(ptr, data.Length)); // SPAN RULE: after alloc
        return slot.FilterFn(arg, ptr, data.Length) != 0;
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
        var module = Module.FromBytes(_engine, slot.Name, source.Bytes!);
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

    public void Dispose() => _engine.Dispose();

    internal static Store CreateStore(Engine engine)
    {
        var store = new Store(engine);
        store.SetWasiConfiguration(new WasiConfiguration()
            .WithInheritedStandardOutput()
            .WithInheritedStandardError());
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
        slot.FilterFn = instance.GetFunction<int, int, int, int>("mod_filter");
        // WASI reactor init (globals / component ctors) — before any other export.
        instance.GetAction("_initialize")?.Invoke();
    }

    // Reset the bump arena, allocate `len`, copy the input in. Returns the guest
    // pointer. SPAN RULE: memory is re-acquired AFTER alloc.
    private static int WriteInputToArena(Slot slot, byte[] input)
    {
        slot.ArenaReset();
        var ptr = slot.Alloc(input.Length);
        if (input.Length > 0)
            input.AsSpan().CopyTo(slot.Memory.GetSpan(ptr, input.Length));
        return ptr;
    }

    // Packed guest return = len&lt;&lt;32 | ptr, 0 = none. The bytes live in the guest
    // arena until the next arena_reset — copy out into a right-sized array before
    // the caller (ModAbiRunner) parses it with FlatSharp.
    private static byte[]? CopyPackedOut(Slot slot, ulong packed)
    {
        if (packed == 0)
            return null;
        var ptr = (int)(packed & 0xFFFFFFFFUL);
        var len = (int)(packed >> 32);
        if (len <= 0)
            return null;
        return slot.Memory.GetSpan(ptr, len).ToArray();
    }

    // The host imports (see abi/mod-abi.fbs header): mid-run RPCs. The generic ECS
    // ones route to `sink` (ModAbiBacking); the game-specific ones are host-described
    // ModHostImport descriptors matched to wasm signatures by Kind. All under the
    // host's import module, all resolving THIS mod's own ptr/len against the
    // Wasmtime Caller's memory.
    private static void DefineImports(Linker linker, string module, IModImportSink sink, IReadOnlyList<ModHostImport> hostImports)
    {
        CallerAction<int, int> log = (caller, ptr, len) => sink.Log(ReadUtf8(caller, ptr, len));
        linker.DefineFunction(module, "log", log);

        CallerFunc<long, long> entityParent = (caller, entity) => (long)sink.EntityParent((ulong)entity);
        linker.DefineFunction(module, "entity_parent", entityParent);

        CallerFunc<long, int, int, int> entityChildren = (caller, entity, outPtr, cap) =>
            sink.EntityChildren((ulong)entity, caller.GetMemory("memory")!.GetSpan(outPtr, cap * 8));
        linker.DefineFunction(module, "entity_children", entityChildren);

        CallerFunc<long, int, int, int, int> componentGet = (caller, entity, typeId, outPtr, cap) =>
            sink.ComponentGet((ulong)entity, (ushort)typeId, caller.GetMemory("memory")!.GetSpan(outPtr, cap));
        linker.DefineFunction(module, "component_get", componentGet);

        CallerFunc<int, int, int, int> resourceGet = (caller, typeId, outPtr, cap) =>
            sink.ResourceGet((ushort)typeId, caller.GetMemory("memory")!.GetSpan(outPtr, cap));
        linker.DefineFunction(module, "resource_get", resourceGet);

        foreach (var import in hostImports)
        {
            var h = import;
            switch (h.Kind)
            {
                case ModHostImportKind.BytesIn:
                {
                    CallerAction<int, int> fn = (caller, ptr, len) =>
                        h.BytesIn!(len > 0 ? caller.GetMemory("memory")!.GetSpan(ptr, len) : default);
                    linker.DefineFunction(module, h.Name, fn);
                    break;
                }
                case ModHostImportKind.U32ToU64:
                {
                    CallerFunc<int, long> fn = (caller, arg) => (long)h.U32ToU64!((uint)arg);
                    linker.DefineFunction(module, h.Name, fn);
                    break;
                }
                case ModHostImportKind.U32ToU32:
                {
                    CallerFunc<int, int> fn = (caller, arg) => (int)h.U32ToU32!((uint)arg);
                    linker.DefineFunction(module, h.Name, fn);
                    break;
                }
                case ModHostImportKind.U32TextToU32:
                {
                    CallerFunc<int, int, int, int> fn = (caller, arg, ptr, len) =>
                        (int)h.U32TextToU32!((uint)arg, ReadUtf8(caller, ptr, len));
                    linker.DefineFunction(module, h.Name, fn);
                    break;
                }
                case ModHostImportKind.U32ToTextOut:
                {
                    CallerFunc<int, int, int, int> fn = (caller, arg, outPtr, cap) =>
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(h.U32ToTextOut!((uint)arg));
                        if (bytes.Length > 0 && bytes.Length <= cap)
                            bytes.CopyTo(caller.GetMemory("memory")!.GetSpan(outPtr, bytes.Length));
                        return bytes.Length;
                    };
                    linker.DefineFunction(module, h.Name, fn);
                    break;
                }
            }
        }
    }

    private static string ReadUtf8(Caller caller, int ptr, int len)
        => len <= 0 ? string.Empty
            : System.Text.Encoding.UTF8.GetString(caller.GetMemory("memory")!.GetSpan(ptr, len));
}
