// Wasmtime-only half of ModdingConfig: the per-mod Linker hook, which needs the
// fork's Wasmtime.Linker type. Split out of ModdingPlugin.cs (rather than living
// alongside the rest of the config) so ModdingPlugin.cs — and everything else that
// touches ModdingConfig — compiles with zero wasmtime references under WasmGuest
// (this file is excluded from that build; see the csproj). The runtime-neutral
// per-mod hook (packet-filter wiring, input-consume — anything that doesn't need
// the Linker) lives in ModdingConfig.PerModContext instead and runs for EVERY
// backend, wired in ModdingPlugin.cs.

using Wasmtime;

namespace TinyEcs.Bevy.Modding;

public sealed partial class ModdingConfig
{
    /// Per-mod hooks run once for each loaded mod, after the generic `app` bridge
    /// is defined and before the component is instantiated. Use to `Define` extra
    /// game-specific linker imports (other WIT packages a mod consumes). Wasmtime
    /// backend only — has no effect under WasmBackend.Jco (the JS glue is the
    /// linker there; see ModdingConfig.PerModContext for the backend-agnostic hook).
    public readonly List<Action<Linker, ModHostContext>> PerModLinker = new();
}
