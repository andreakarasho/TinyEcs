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

    public ModAbiBacking(ModHostContext ctx, CoreModState state, string modName)
    {
        _ctx = ctx;
        _state = state;
        _modName = modName;
    }

    public void Log(string message) => Console.WriteLine("[mod:{0}] {1}", _modName, message);

    public ulong EntityParent(ulong entity)
    {
        var p = (ulong)_ctx.World.GetParent(entity);
        return p != 0 && _ctx.World.Exists(p) ? p : 0UL;
    }

    // Enumerate an entity's children by the Parent relationship (mirrors
    // EntityImpl.Children). Two-phase: collect host-side, then one write into the
    // caller-supplied span — the caller (an executor) owns whatever SPAN RULE
    // applies to its own memory (e.g. no guest call between GetSpan() and this).
    public int EntityChildren(ulong entity, Span<byte> outBytes)
    {
        var q = _ctx.ChildrenQuery ??= _ctx.World.QueryBuilder().With<Parent>().Build();
        var ids = new List<ulong>();
        var it = q.Iter();
        while (it.Next())
            foreach (var ev in it.Entities())
                if ((ulong)_ctx.World.Get<Parent>(ev.ID).Id == entity)
                    ids.Add(ev.ID);

        var cap = outBytes.Length / 8;
        var writeCount = Math.Min(ids.Count, cap);
        for (var i = 0; i < writeCount; i++)
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(outBytes.Slice(i * 8, 8), ids[i]);
        return ids.Count;
    }

    public int ComponentGet(ulong entity, ushort typeId, Span<byte> outBytes)
    {
        if (!_state.IdToEntry.TryGetValue(typeId, out var e)
            || !_ctx.Registry.TryGet(e.Path, out var comp)
            || !comp.Has(_ctx.World, entity))
            return 0;
        return WriteOut(outBytes, comp.GetJson(_ctx.World, entity));
    }

    public int ResourceGet(ushort typeId, Span<byte> outBytes)
    {
        if (_ctx.App == null
            || !_state.IdToEntry.TryGetValue(typeId, out var e)
            || !_ctx.Registry.TryGetResource(e.Path, out var res))
            return 0;
        return WriteOut(outBytes, res.GetJson(_ctx.App));
    }

    private static int WriteOut(Span<byte> outBytes, string json)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        if (bytes.Length > 0 && bytes.Length <= outBytes.Length)
            bytes.CopyTo(outBytes);
        return bytes.Length;
    }
}
