// Executor seam: separates wasm MECHANICS (compile/instantiate a mod module, call
// its 4 guest exports, arena/span marshalling) from the ABI CODEC (FlatSharp
// build/parse, SetupReply translation, CommandBuffer application, host import
// backing) — see ModAbiRunner.cs / ModAbiBacking.cs, which are byte[]-level and
// therefore identical for every executor. WasmtimeModWasmExecutor.cs (desktop,
// upstream Wasmtime) is the only implementation today; a browser GUEST_CORE
// guest-relay executor (flat env imports to JS instead of an embedded runtime) is
// the planned second one — see CoreWasmModBackend.cs's header comment.
//
// This file itself has ZERO wasm-runtime references (no Wasmtime, no wasm ptr/len)
// so it compiles under every flavor, including GUEST_CORE.

namespace TinyEcs.Bevy.Modding;

/// The generic ECS mid-run RPC backing methods (see abi/mod-abi.fbs header),
/// runtime-neutral: no wasm ptr/len, no Wasmtime Caller. Every executor's own
/// guest-import glue narrows its ptr/len args into a Span (or reads/writes one
/// that already lives in its own address space) BEFORE calling in here, so this
/// interface — and ModAbiBacking, its one implementation — needs no wasm types
/// at all and is identical for the wasmtime executor and a guest-relay executor.
/// Game-specific imports (networking, asset lookups, …) are NOT here: the host
/// describes those as ModHostImport shape descriptors (ModHostImports.cs) and
/// each executor defines them alongside these into the same import module.
internal interface IModImportSink
{
    void Log(string message);
    /// 0 = no parent (or the entity doesn't exist).
    ulong EntityParent(ulong entity);
    /// Writes up to (outBytes.Length / 8) matching child ids (each little-endian
    /// u64) into outBytes; returns the TOTAL child count (may exceed the write
    /// capacity — the caller recalls with a bigger buffer if so).
    int EntityChildren(ulong entity, Span<byte> outBytes);
    /// Returns the needed byte length (0 = component absent/unregistered); writes
    /// the UTF8 JSON into outBytes only if it fits (length &lt;= outBytes.Length).
    int ComponentGet(ulong entity, ushort typeId, Span<byte> outBytes);
    /// Returns the needed byte length (0 = resource absent/unregistered); writes
    /// the UTF8 JSON into outBytes only if it fits (length &lt;= outBytes.Length).
    int ResourceGet(ushort typeId, Span<byte> outBytes);
}

/// Wasm-mechanics seam: compiles/instantiates a mod module and drives its guest
/// exports (mod_setup/run/observer/filter) + reload, entirely over
/// byte[]-in/byte[]-out (or bool, for the filter predicate). Implementations
/// own arena allocation, span re-acquisition (the SPAN RULE — see
/// WasmtimeModWasmExecutor.cs), and packed-return decoding internally; callers
/// (ModAbiRunner) never see a wasm pointer or arena. One executor instance per
/// process (owns the engine/runtime); Load() returns a per-mod handle threaded
/// through every subsequent call — mirrors ModSource/ModHostContext's shape so a
/// backend can construct one executor and Load() every mod through it.
internal interface IModWasmExecutor : IDisposable
{
    /// Compile + instantiate from source.Bytes, defining the host imports — the
    /// generic ECS ones routed to `sink` plus the host-described `hostImports`
    /// descriptors, all under import module `importModule` — for the lifetime of
    /// this handle (reused across Reload). `slot` is the mod's
    /// ModHostContext.Slot (assigned by ModdingPlugin.SetupEcsMods in load order)
    /// — the caller passes it through explicitly rather than letting each
    /// executor mint its own handle, because a guest-relay executor's handle
    /// MUST equal the slot number JS is tracking (its modw_load(slot) call), and a
    /// failed Load for an earlier mod would otherwise desync "Nth successful load"
    /// (ctx.Slot) from "Nth Load call" (an internally-counted handle). Returns the
    /// handle passed to every other method below (implementations may just echo
    /// `slot` back, as WasmtimeModWasmExecutor does).
    int Load(in ModSource source, int slot, IModImportSink sink, string importModule, IReadOnlyList<ModHostImport> hostImports);

    /// Every Call* below is span-IN (the caller's FlatSharp write buffer is reused
    /// and over-sized, so an exact-length copy would be pure garbage) and
    /// reply-buffer-OUT: the returned memory is a slice of a buffer the EXECUTOR
    /// owns and reuses, valid only until the next call on this handle. Empty = no
    /// reply (the guest exports nothing, or returned packed == 0). Memory, not
    /// ReadOnlyMemory: FlatSharp refuses a read-only input buffer for a table that
    /// has a [ubyte] field, because the parsed vector aliases the input.
    /// Call mod_setup(handshake) -> SetupReply bytes.
    Memory<byte> CallSetup(int handle, ReadOnlySpan<byte> handshake);

    /// Call mod_run(sysId, input) -> CommandBuffer bytes.
    Memory<byte> CallRun(int handle, uint sysId, ReadOnlySpan<byte> input);

    /// Call mod_observer(obsId, entity, input) -> CommandBuffer bytes.
    Memory<byte> CallObserver(int handle, uint obsId, ulong entity, ReadOnlySpan<byte> input);

    /// Call mod_filter(arg, data) -> bool; false when the guest exports none.
    bool CallFilter(int handle, byte arg, ReadOnlySpan<byte> data);

    /// Call the OPTIONAL second filter slot mod_filter_out(arg, data) -> bool; false
    /// when the guest exports none. Defaulted so an executor that predates the slot
    /// (or a test double) needs no edit — it simply never filters on it.
    bool CallFilterOut(int handle, byte arg, ReadOnlySpan<byte> data) => false;

    /// Call the OPTIONAL mod_spawned(SpawnedInput) export — no return. Invoked right
    /// after the host applies a CommandBuffer that spawned at least one entity, so the
    /// guest can map its TempIds onto real ecs ids. No-op when the guest doesn't
    /// export it (most mods don't).
    void CallSpawned(int handle, ReadOnlySpan<byte> input);

    /// Tear down + re-instantiate this handle from fresh bytes, reusing the same
    /// host imports (see WasmtimeModWasmExecutor's Reload doc comment for why).
    void Reload(int handle, in ModSource source);

    /// Dispose this handle's per-instance runtime resources. The executor itself
    /// (engine / import template) stays alive for other loaded mods.
    void DisposeInstance(int handle);
}
