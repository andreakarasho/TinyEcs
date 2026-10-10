// IModImportSink over one mod's ModHostContext + ModRelayState: the generic mid-run
// import (log), runtime-neutral (see ModWasmExecutor.cs's doc comment). A mod's game
// host calls don't come through here: the host's own typed dispatch serves them.

using TinyEcs;

namespace TinyEcs.Bevy.Modding;

// Per-mod, shared between the host import backing below (which reads IdToEntry
// at call time) and ModAbiRunner (which reads PathToId when serializing
// SystemInput). One object per loaded mod, reused across reloads. Lives here (not
// ModAbiRunner.cs, which is FlatSharp/desktop-only) because it must compile
// everywhere — a guest-relay executor's backing needs it too.
internal sealed class ModRelayState
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

    public ModAbiBacking(ModHostContext ctx, ModRelayState state, string modName)
    {
        _ctx = ctx;
        _modName = modName;
    }

    public void Log(string message) => Console.WriteLine("[mod:{0}] {1}", _modName, message);
}
