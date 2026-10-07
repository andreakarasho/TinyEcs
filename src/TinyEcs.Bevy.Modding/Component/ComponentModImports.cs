// The seam a HOST APP uses to give component-model mods its own imports. The lib
// defines only the generic `tinyecs:modding/ecs` interface (ComponentModBackend); a game's
// world (its WIT `include`s tinyecs:modding/guest and adds `import <game>:modding/...`) is
// satisfied by the host defining those interfaces on the SAME component linker, from a
// ModdingConfig.ComponentImports callback. Either way works:
//   - dynamic:  var i = imports.DefineInstance("game:mod/host@0.1.0");
//               i.DefineFunction("log", (state, args, results, cx) => ..., state);
//   - generated: the fork's WIT source generator emits an `IComponentImports` whose
//               Register(Linker) does the same — call it with imports.Linker.
// Handlers run synchronously inside a guest call; `Current` names the mod making it,
// so one linker serves every loaded mod.

extern alias WasmtimeCm;

using Cm = WasmtimeCm::Wasmtime;

namespace TinyEcs.Bevy.Modding;

/// Handed to every ModdingConfig.ComponentImports callback once, when the component
/// backend is created (before the first component mod is instantiated).
public sealed class ComponentModImports
{
    private readonly ComponentModBackend _backend;

    internal ComponentModImports(ComponentModBackend backend) => _backend = backend;

    /// The component linker every component mod is instantiated with (WASI p2 and the
    /// `tinyecs:modding/ecs` interface are already defined on it).
    public Cm.Linker Linker => _backend.Linker;

    /// The mod whose guest call is on the stack; null outside a guest call.
    public ModHostContext? Current => _backend.Current?.Ctx;

    /// Defines (or opens) an interface instance, e.g. "game:mod/assets@0.1.0".
    public Cm.LinkerInstance DefineInstance(string interfaceName) => _backend.Linker.DefineInstance(interfaceName);

    /// First resource type id this lib does NOT use. A host defining its own resources
    /// must keep its ids out of [ComponentModBackend.TypeIdBase, ReservedTypeIdEnd).
    public const uint ReservedTypeIdEnd = ComponentModBackend.TypeIdBase + 0x100;

    /// Calls a game-specific export (one the generic world doesn't know, e.g. a packet
    /// hook) on the component mod behind `ctx`. Returns false when that mod isn't a
    /// component or doesn't export `name`. `args` are disposed either way;
    /// `onResults` (optional) reads the results before they are released.
    public bool TryCallExport(ModHostContext ctx, string name, ReadOnlySpan<Cm.ComponentValue> args,
        int resultCount = 0, ComponentResultsReader? onResults = null)
        => _backend.TryCallExport(ctx, name, args, resultCount, onResults);

    /// Where ModPacketChain's IModInstance.OnPacket lands for a component mod. The
    /// generic world has no packet export, so the host maps it onto its own (typically
    /// via TryCallExport). Unset = every component mod passes.
    public ComponentPacketHook? OnPacket
    {
        get => _backend.PacketHook;
        set => _backend.PacketHook = value;
    }
}

/// A component mod's verdict on one intercepted message (see ModPacketChain.Run).
/// For Replace, `replacement` must stay valid until the next call.
public delegate ModPacketVerdict ComponentPacketHook(ModHostContext ctx, ModPacketDirection dir,
    ReadOnlySpan<byte> packet, out ReadOnlySpan<byte> replacement);

/// Reads the results of ComponentModImports.TryCallExport (valid only inside the call).
public delegate void ComponentResultsReader(Cm.ComponentCallResults results);
