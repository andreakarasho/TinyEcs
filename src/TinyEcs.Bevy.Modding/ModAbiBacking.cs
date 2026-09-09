// IModImportSink over one mod's ModHostContext + CoreModState — the exact
// logic CoreWasmModBackend's import glue used to inline against a Wasmtime
// Caller, now runtime-neutral (see ModWasmExecutor.cs's doc comment): every
// executor's own guest-import glue resolves its own ptr/len into a Span (or
// already has one, in the guest-relay case) BEFORE calling in here. Only the
// generic ECS imports live here; game-specific imports are host-described
// ModHostImport descriptors (ModHostImports.cs) the executor defines alongside.

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
    private readonly CoreModState _state;
    private readonly string _modName;
    // Reused UTF8 staging buffer for the two out-buffer imports (a mod polls these
    // mid-run, so a string + byte[] per call was per-frame garbage).
    private readonly ModJsonBuffer _json = new();

    public ModAbiBacking(ModHostContext ctx, CoreModState state, string modName)
    {
        _ctx = ctx;
        _state = state;
        _modName = modName;
    }

    public void Log(string message) => Console.WriteLine("[mod:{0}] {1}", _modName, message);

    public ulong EntityParent(ulong entity)
    {
        if (!_ctx.World.Exists(entity))
            return 0UL;

        var p = (ulong)_ctx.World.GetParent(entity);
        return p != 0 && _ctx.World.Exists(p) ? p : 0UL;
    }

    // Enumerate an entity's children. TinyEcs's relationship mapper already keeps the
    // ordered child list on the parent as a `Children` component, so this reads it
    // directly instead of scanning every entity that has a Parent (mirrors
    // EntityImpl.Children). Written straight into the caller-supplied span — the
    // caller (an executor) owns whatever SPAN RULE applies to its own memory (e.g.
    // no guest call between GetSpan() and this).
    public int EntityChildren(ulong entity, Span<byte> outBytes)
    {
        // A guest holds entity ids from a PUSHED snapshot, so they routinely outlive
        // the entity (the host right-click-closes a mod window between two ticks).
        // World.Has PANICS on a dead id, and that throw traps the guest mid-system —
        // "no children" is the honest answer at this boundary.
        if (!_ctx.World.Exists(entity) || !_ctx.World.Has<Children>(entity))
            return 0;

        var cap = outBytes.Length / 8;
        var count = 0;
        foreach (var child in _ctx.World.Get<Children>(entity))
        {
            if (count < cap)
                System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(
                    outBytes.Slice(count * 8, 8), (ulong)child);
            count++;
        }
        return count;
    }

    public int ComponentGet(ulong entity, ushort typeId, Span<byte> outBytes)
    {
        // Exists() FIRST — see EntityChildren: a stale id would panic inside
        // IModComponent.Has and abort the calling guest system every tick.
        if (!_ctx.World.Exists(entity)
            || !_state.IdToEntry.TryGetValue(typeId, out var e)
            || !_ctx.Registry.TryGet(e.Path, out var comp)
            || !comp.Has(_ctx.World, entity))
            return 0;
        _json.Reset();
        comp.GetJsonUtf8(_ctx.World, entity, _json);
        return WriteOut(outBytes, _json.WrittenSpan);
    }

    public int ResourceGet(ushort typeId, Span<byte> outBytes)
    {
        if (_ctx.App == null
            || !_state.IdToEntry.TryGetValue(typeId, out var e)
            || !_ctx.Registry.TryGetResource(e.Path, out var res))
            return 0;
        _json.Reset();
        IModComponent.WriteUtf8(_json, res.GetJson(_ctx.App));
        return WriteOut(outBytes, _json.WrittenSpan);
    }

    private static int WriteOut(Span<byte> outBytes, ReadOnlySpan<byte> json)
    {
        if (json.Length > 0 && json.Length <= outBytes.Length)
            json.CopyTo(outBytes);
        return json.Length;
    }
}
