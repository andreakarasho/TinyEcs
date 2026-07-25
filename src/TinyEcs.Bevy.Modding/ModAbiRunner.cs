// Runtime-neutral mod instance: FlatSharp build/parse, SetupReply -> ModSystemSpec
// translation, CommandBuffer application — identical for every IModWasmExecutor.
// This is the former CoreWasmModInstance minus the wasm mechanics (arena/span/
// packed-return decode), which now live behind the executor. ModAbiRunner drives
// the executor purely over byte[] (or bool); FlatSharp serialize/parse happens
// here, on the byte[] the executor handed back — the executor never touches a
// FlatSharp type.

using System.Buffers;
using FlatSharp;
using ModAbi;

namespace TinyEcs.Bevy.Modding;

// CoreModState lives in ModAbiBacking.cs — it's shared between that class (the
// host import backing, compiled everywhere) and this runner (FlatSharp, desktop
// only), and must compile everywhere too.

internal sealed class ModAbiRunner : IModInstance
{
    // Canonical ABI version stamped into every Handshake. Bumped only on a
    // breaking wire change; the guest asserts it matches its compiled schema.
    internal const uint AbiVersion = 1;

    private readonly IModWasmExecutor _executor;
    private readonly int _handle;
    private readonly CoreModState _state;
    private readonly ModHostContext _ctx;

    private bool _wantsFilter;

    // guest system id (SystemDecl.id) keyed by the neutral spec the runner passes back.
    private readonly Dictionary<ModSystemSpec, uint> _sysToId = new();
    // observer token (ModObserverSpec.Name = obs id string) -> (obs id, value type id).
    private readonly Dictionary<string, (uint ObsId, ushort TypeId)> _obsByName = new();
    // SpawnCmd.temp_id -> freshly spawned ecs id, per applied CommandBuffer.
    private readonly Dictionary<uint, ulong> _tempTable = new();

    // Reused per RunSystem: ArrayPool query snapshots in flight, returned after the call.
    private readonly List<ulong[]> _snapshotScratch = new();
    // Reused FlatSharp write buffer (grown as needed) — serialized before handing
    // an exact-length copy to the executor.
    private byte[] _writeScratch = new byte[1024];
    // Reused command-apply scratch (bundles/paths are tiny and consumed synchronously).
    private readonly List<(string, string)> _bundleScratch = new();
    private readonly List<string> _pathScratch = new();

    public ModAbiRunner(IModWasmExecutor executor, int handle, CoreModState state, ModHostContext ctx)
    {
        _executor = executor;
        _handle = handle;
        _state = state;
        _ctx = ctx;
    }

    public void Setup()
    {
        _state.PathToId.Clear();
        _state.IdToEntry.Clear();
        _sysToId.Clear();
        _obsByName.Clear();

        // Handshake: intern every registered path into one u16 id space.
        var handshake = new Handshake { AbiVersion = AbiVersion, TypePaths = new List<TypePath>() };
        ushort next = 0;
        foreach (var (path, kind) in _ctx.Registry.Entries)
        {
            handshake.TypePaths.Add(new TypePath { Id = next, Path = path });
            _state.PathToId[path] = next;
            _state.IdToEntry[next] = (path, kind);
            next++;
        }

        var bytes = SerializeToArray(Handshake.Serializer, handshake);
        var replyBytes = _executor.CallSetup(_handle, bytes)
            ?? throw new InvalidOperationException("mod_setup returned no SetupReply");
        var reply = SetupReply.Serializer.Parse(replyBytes);
        TranslateSetup(reply);
        _wantsFilter = reply.WantsFilter;
    }

    // SetupReply.systems -> ctx.Systems/SystemsByStage (via AppImpl.AddSystems, exactly
    // as the component path does) + observers -> ctx.Observers. Declaration order is the
    // list order; after/before are recorded on the spec (the runner uses declaration
    // order, matching how the generic scheduler dispatches).
    private void TranslateSetup(SetupReply reply)
    {
        var appImpl = new AppImpl(_ctx);

        var idToName = new Dictionary<uint, string>();
        if (reply.Systems != null)
            foreach (var sd in reply.Systems)
                idToName[sd.Id] = sd.Name ?? "";

        if (reply.Systems != null)
        {
            var one = new SystemImpl[1];
            foreach (var sd in reply.Systems)
            {
                var si = new SystemImpl(sd.Name ?? "");
                if (sd.Params != null)
                    foreach (var pd in sd.Params)
                    {
                        if (pd.Kind == ParamKind.Commands)
                            si.AddCommands();
                        else
                            si.AddQuery(BuildTerms(pd.Query));
                    }
                if (sd.After != null)
                    foreach (var aid in sd.After)
                        if (idToName.TryGetValue(aid, out var n)) si.Spec.After.Add(n);
                if (sd.Before != null)
                    foreach (var bid in sd.Before)
                        if (idToName.TryGetValue(bid, out var n)) si.Spec.Before.Add(n);

                one[0] = si;
                appImpl.AddSystems((ModSchedule)(byte)sd.Schedule, sd.CustomStage, one);
                _sysToId[si.Spec] = sd.Id;
            }
        }

        if (reply.Observers != null)
            foreach (var od in reply.Observers)
            {
                var kind = (ModObserverKind)(byte)od.Kind;
                var typePath = kind switch
                {
                    ModObserverKind.Insert or ModObserverKind.Remove =>
                        _state.IdToEntry.TryGetValue(od.TypeId, out var e) ? e.Path : null,
                    ModObserverKind.Custom => od.EventName,
                    _ => null,
                };
                var token = od.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _ctx.Observers.Add(new ModObserverSpec { Name = token, Kind = kind, TypePath = typePath });
                var valueType = kind is ModObserverKind.Insert or ModObserverKind.Remove ? od.TypeId : (ushort)0xFFFF;
                _obsByName[token] = (od.Id, valueType);
            }
    }

    private ModQueryTerm[] BuildTerms(QueryDecl? q)
    {
        if (q?.Terms == null || q.Terms.Count == 0)
            return Array.Empty<ModQueryTerm>();
        var terms = new ModQueryTerm[q.Terms.Count];
        for (var i = 0; i < q.Terms.Count; i++)
        {
            var t = q.Terms[i];
            var path = _state.IdToEntry.TryGetValue(t.TypeId, out var e) ? e.Path : "";
            terms[i] = new ModQueryTerm((ModQueryTermKind)(byte)t.Kind, path);
        }
        return terms;
    }

    public void RunSystem(ModSystemSpec sys)
    {
        _snapshotScratch.Clear();
        List<QueryRows>? queries = null;
        var hasQuery = false;
        var anyRows = false;

        for (var pi = 0; pi < sys.Params.Count; pi++)
        {
            var p = sys.Params[pi];
            if (p.Kind != ModParamKind.Query)
                continue;
            hasQuery = true;
            var snapshot = ModdingPlugin.BuildSnapshot(_ctx, p.Query!, out var matched);
            _snapshotScratch.Add(snapshot);
            anyRows |= matched > 0;

            var rows = new List<Row>(matched);
            for (var r = 0; r < matched; r++)
            {
                var entId = snapshot[r];
                if (!_ctx.World.Exists(entId))
                    continue;
                var comps = new List<CompValue>(p.Query!.Components.Count);
                foreach (var (typePath, _) in p.Query!.Components)
                {
                    if (!_ctx.Registry.TryGet(typePath, out var comp))
                        continue;
                    comps.Add(new CompValue
                    {
                        TypeId = _state.PathToId.TryGetValue(typePath, out var tid) ? tid : (ushort)0xFFFF,
                        Encoding = ModAbi.Encoding.Json,
                        Data = System.Text.Encoding.UTF8.GetBytes(comp.GetJson(_ctx.World, entId)),
                    });
                }
                rows.Add(new Row { Entity = entId, Comps = comps });
            }
            (queries ??= new List<QueryRows>()).Add(new QueryRows { ParamIndex = (uint)pi, Rows = rows });
        }

        try
        {
            // Idle-skip (same policy as every backend): every query empty this
            // tick AND last — skip the guest call. The finally still returns snapshots.
            if (ModdingPlugin.ShouldSkipIdle(sys, hasQuery, anyRows))
                return;

            var sysId = _sysToId.TryGetValue(sys, out var sid) ? sid : 0u;
            var input = new SystemInput { SysId = sysId, Tick = CurrentTick(), Queries = queries };
            var bytes = SerializeToArray(SystemInput.Serializer, input);
            var replyBytes = _executor.CallRun(_handle, sysId, bytes);
            if (replyBytes != null)
                ApplyCommandBuffer(CommandBuffer.Serializer.Parse(replyBytes));
        }
        finally
        {
            foreach (var arr in _snapshotScratch)
                ArrayPool<ulong>.Shared.Return(arr);
            _snapshotScratch.Clear();
        }
    }

    public void CallObserver(string export, ulong entity, string json)
    {
        if (!_obsByName.TryGetValue(export, out var obs))
            return; // unknown observer token — no-op

        var input = new ObserverInput
        {
            ObsId = obs.ObsId,
            Entity = entity,
            Value = new CompValue
            {
                TypeId = obs.TypeId,
                Encoding = ModAbi.Encoding.Json,
                Data = string.IsNullOrEmpty(json) ? Array.Empty<byte>() : System.Text.Encoding.UTF8.GetBytes(json),
            },
        };
        var bytes = SerializeToArray(ObserverInput.Serializer, input);
        var replyBytes = _executor.CallObserver(_handle, obs.ObsId, entity, bytes);
        if (replyBytes != null)
            ApplyCommandBuffer(CommandBuffer.Serializer.Parse(replyBytes));
    }

    // The host picks the logical export name; the core backend maps any bool export
    // onto the single mod_filter guest export. Absent export (or the mod didn't ask
    // to filter via wants_filter) = no call, returns false.
    public bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data)
        => _wantsFilter && _executor.CallFilter(_handle, arg, data);

    // Re-instantiate from fresh bytes (the executor reuses whatever host-import
    // wiring it built at Load), then re-run setup — ModdingPlugin.ReloadMod has
    // already cleared ctx.Systems/Observers.
    public void Reload(in ModSource source)
    {
        _executor.Reload(_handle, in source);
        Setup();
    }

    public void Dispose() => _executor.DisposeInstance(_handle);

    private ulong CurrentTick()
        => _ctx.App != null && _ctx.App.HasResource<TinyEcs.Bevy.Time>()
            ? (ulong)_ctx.App.GetResource<TinyEcs.Bevy.Time>().Total
            : 0UL;

    // ── FlatSharp plumbing ────────────────────────────────────────────────────

    // Serialize into the reused scratch buffer, then hand the executor an
    // exact-length copy (the byte[]-in/byte[]-out executor seam owns no knowledge
    // of FlatSharp's max-size-vs-actual-size distinction).
    private byte[] SerializeToArray<T>(ISerializer<T> serializer, T value) where T : class
    {
        var max = serializer.GetMaxSize(value);
        if (_writeScratch.Length < max)
            _writeScratch = new byte[max];
        var len = serializer.Write(_writeScratch, value);
        return _writeScratch.AsSpan(0, len).ToArray();
    }

    // ── CommandBuffer applier ─────────────────────────────────────────────────
    // Walks the cmds in order through the neutral GuestBridge Impl structs. Entity
    // refs are int64: >=0 real ecs id, <0 temp ref (index = -(v)-1 into the temp-id
    // table established by SpawnCmd.temp_id entries of THIS buffer).
    private void ApplyCommandBuffer(CommandBuffer cb)
    {
        var cmds = cb.Cmds;
        if (cmds == null || cmds.Count == 0)
            return;

        var commands = new CommandsImpl(_ctx);
        _tempTable.Clear();

        foreach (var cmd in cmds)
        {
            switch (cmd.Kind)
            {
                case Cmd.ItemKind.SpawnCmd:
                {
                    var sc = cmd.SpawnCmd;
                    var ec = commands.Spawn(BuildBundle(sc.Comps));
                    _tempTable[sc.TempId] = ec.Id().EcsId;
                    break;
                }
                case Cmd.ItemKind.InsertCmd:
                {
                    var ic = cmd.InsertCmd;
                    commands.EntityById(Resolve(ic.Entity)).Insert(BuildBundle(ic.Comps));
                    break;
                }
                case Cmd.ItemKind.RemoveCmd:
                {
                    var rc = cmd.RemoveCmd;
                    commands.EntityById(Resolve(rc.Entity)).Remove(BuildPaths(rc.TypeIds));
                    break;
                }
                case Cmd.ItemKind.DespawnCmd:
                    commands.EntityById(Resolve(cmd.DespawnCmd.Entity)).Despawn();
                    break;
                case Cmd.ItemKind.AddChildCmd:
                {
                    var ac = cmd.AddChildCmd;
                    commands.EntityById(Resolve(ac.Parent))
                        .AddChild(new EntityImpl(_ctx, Resolve(ac.Child)), ac.Index);
                    break;
                }
                case Cmd.ItemKind.ResourceSetCmd:
                {
                    var v = cmd.ResourceSetCmd.Value;
                    if (v != null && _state.IdToEntry.TryGetValue(v.TypeId, out var e))
                    {
                        if (IsApplicable(v.Encoding, e.Path))
                            commands.ResourceSet(e.Path, Utf8(v.Data));
                    }
                    break;
                }
                case Cmd.ItemKind.EmitEventCmd:
                {
                    var ee = cmd.EmitEventCmd;
                    commands.EmitEvent(ee.EventName ?? string.Empty, ee.Entity, Utf8(ee.Data));
                    break;
                }
                case Cmd.ItemKind.ConsumeMouseCmd:
                    commands.InputConsumeMouse(cmd.ConsumeMouseCmd.Button);
                    break;
                case Cmd.ItemKind.ConsumeKeyCmd:
                    commands.InputConsumeKeyboard(cmd.ConsumeKeyCmd.Key);
                    break;
            }
        }
    }

    private ulong Resolve(long entityRef)
    {
        if (entityRef >= 0)
            return (ulong)entityRef;
        var tempId = (uint)(-entityRef - 1);
        return _tempTable.TryGetValue(tempId, out var id) ? id : 0UL;
    }

    // Reused scratch — the returned span is consumed synchronously by the Impl call.
    private ReadOnlySpan<(string, string)> BuildBundle(IList<CompValue>? comps)
    {
        _bundleScratch.Clear();
        if (comps != null)
            foreach (var cv in comps)
            {
                if (!_state.IdToEntry.TryGetValue(cv.TypeId, out var e))
                    continue;
                if (!IsApplicable(cv.Encoding, e.Path))
                    continue;
                _bundleScratch.Add((e.Path, Utf8(cv.Data)));
            }
        return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_bundleScratch);
    }

    private ReadOnlySpan<string> BuildPaths(IList<ushort>? typeIds)
    {
        _pathScratch.Clear();
        if (typeIds != null)
            foreach (var id in typeIds)
                if (_state.IdToEntry.TryGetValue(id, out var e))
                    _pathScratch.Add(e.Path);
        return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_pathScratch);
    }

    private static string Utf8(Memory<byte>? data)
        => data is { Length: > 0 } d ? System.Text.Encoding.UTF8.GetString(d.Span) : "{}";

    // Encoding.Typed (the phase-2 registry SetFlat path) is not implemented yet. Wire
    // input is a boundary: SKIP the one payload and log it rather than throwing —
    // a throw here escapes ApplyCommandBuffer and silently drops every remaining
    // command in the guest's buffer (half-built UI, no diagnostic beyond one line).
    private static bool IsApplicable(ModAbi.Encoding encoding, string path)
    {
        if (encoding == ModAbi.Encoding.Json)
            return true;
        Console.WriteLine("[ecs-mod] skipped {0}: CompValue encoding {1} is not implemented", path, encoding);
        return false;
    }
}
