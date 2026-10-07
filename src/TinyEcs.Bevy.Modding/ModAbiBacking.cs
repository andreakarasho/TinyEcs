// IModImportSink over one mod's ModHostContext + CoreModState — the exact
// logic CoreWasmModBackend's import glue used to inline against a Wasmtime
// Caller, now runtime-neutral (see ModWasmExecutor.cs's doc comment): every
// executor's own guest-import glue resolves its own ptr/len into a Span (or
// already has one, in the guest-relay case) BEFORE calling in here. Only the
// generic imports live here (log + mod_call, which routes to the host's
// ModHostFunctions table); legacy shape-described ModHostImport descriptors
// (ModHostImports.cs) are defined alongside by the executor.

using TinyEcs;

namespace TinyEcs.Bevy.Modding;

// Per-mod, shared between the host import backing below (which reads IdToEntry
// at call time) and ModAbiRunner (which reads PathToId when serializing
// SystemInput). One object per loaded mod, reused across reloads. Lives here (not
// ModAbiRunner.cs, which is FlatSharp/desktop-only) because it must compile
// everywhere — a guest-relay executor's backing needs it too.
internal sealed class CoreModState
{
    // Shared u16 id space over every registered path (components + resources +
    // events), interned from ModComponentRegistry.Entries in the Handshake.
    public readonly Dictionary<string, ushort> PathToId = new();
    public readonly Dictionary<ushort, (string Path, ModRegistryKind Kind)> IdToEntry = new();
}

internal sealed class ModAbiBacking : IModImportSink
{
    private readonly ModHostContext _ctx;
    private readonly string _modName;

    public ModAbiBacking(ModHostContext ctx, CoreModState state, string modName)
    {
        _ctx = ctx;
        _modName = modName;
    }

    public void Log(string message) => Console.WriteLine("[mod:{0}] {1}", _modName, message);

    public ReadOnlySpan<byte> ModCall(ReadOnlySpan<byte> name, ReadOnlySpan<byte> args)
    {
        if (_ctx.App == null || !_ctx.App.HasResource<ModHostFunctions>())
            throw new ModCallException("mod_call: this host registers no host functions");
        return _ctx.App.GetResource<ModHostFunctions>().Call(_ctx, name, args);
    }
}
