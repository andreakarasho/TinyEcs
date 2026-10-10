// Relay mod backend: drives mods through the IModWasmExecutor seam — a wasm guest's
// relay executor (ModdingConfig.WasmExecutor, MOD_RELAY_GUEST build: imports to the
// native host, which runs the real wasm32-wasip2 component mods and translates).
// Desktop hosts mods directly (Component/ComponentModBackend.cs); mods themselves are
// components only. The wire contract is the FlatSharp ModAbi graph derived from abi/mod-abi.fbs — a fixed
// set of per-mod calls (mod_setup/run/observer/spawned) and host imports (mid-run RPCs; module name + game-specific
// entries come from the host via ModHostContext).
//
// This file is now just the glue: take an executor, build the per-mod codec
// (ModAbiBacking = the generic import backing, ModAbiRunner = the FlatSharp
// build/parse + CommandBuffer applier), and hand IModInstance to the scheduler.
// See ModWasmExecutor.cs for the seam itself.
//
// Needs FlatSharp (ModAbiRunner) — see the csproj's UseFlatSharp: desktop AND the
// relay guest.

namespace TinyEcs.Bevy.Modding;

internal sealed class ModRelayBackend : IModBackend
{
    private readonly IModWasmExecutor _executor;

    public ModRelayBackend(IModWasmExecutor executor) => _executor = executor;

    public IModInstance Load(in ModSource source, ModHostContext ctx)
    {
        var state = new ModRelayState();
        var sink = new ModAbiBacking(ctx, state, source.Name);
        var handle = _executor.Load(in source, ctx.Slot, sink, ctx.HostImportModule, ctx.HostImports);
        return new ModAbiRunner(_executor, handle, state, ctx);
    }

    public void Dispose() => _executor.Dispose();
}
