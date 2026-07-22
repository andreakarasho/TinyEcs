// Core-wasm mod backend: hosts core-module (NOT Component Model) mods via the
// IModWasmExecutor seam. Executor-agnostic by construction — WasmtimeModWasmExecutor
// (upstream Wasmtime NuGet) on desktop, a GUEST_CORE guest-relay executor (flat env
// imports to JS instead of an embedded runtime, supplied by the embedding host's
// guest build) in the browser core-module guest. Either way the wire
// contract is the FlatSharp ModAbi graph derived from abi/mod-abi.fbs — a fixed
// set of guest exports (mod_setup/run/observer/filter + alloc/arena_reset
// over a bump arena) and host imports (mid-run RPCs; module name + game-specific
// entries come from the host via ModHostContext).
//
// This file is now just the glue: take an executor, build the per-mod codec
// (ModAbiBacking = the generic import backing, ModAbiRunner = the FlatSharp
// build/parse + CommandBuffer applier), and hand IModInstance to the scheduler.
// See ModWasmExecutor.cs for the seam itself and WasmtimeModWasmExecutor.cs for
// the wasm mechanics this used to inline.
//
// Needs FlatSharp (ModAbiRunner) — see the csproj's UseFlatSharp: desktop AND the
// GUEST_CORE guest, never the plain Jco guest (Component Model mods, no FlatSharp).

namespace TinyEcs.Bevy.Modding;

internal sealed class CoreWasmModBackend : IModBackend
{
    private readonly IModWasmExecutor _executor;

    public CoreWasmModBackend(IModWasmExecutor executor) => _executor = executor;

    public IModInstance Load(in ModSource source, ModHostContext ctx)
    {
        var state = new CoreModState();
        var sink = new ModAbiBacking(ctx, state, source.Name);
        var handle = _executor.Load(in source, ctx.Slot, sink, ctx.HostImportModule, ctx.HostImports);
        return new ModAbiRunner(_executor, handle, state, ctx);
    }

    public void Dispose() => _executor.Dispose();
}
