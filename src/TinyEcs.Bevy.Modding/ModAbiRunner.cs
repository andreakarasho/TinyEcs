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
    internal const uint AbiVersion = 2;

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
    // Same pairs in SpawnCmd order — the dictionary answers Resolve()'s lookups, this
    // list preserves the order SpawnedInput promises the guest.
    private readonly List<(uint TempId, ulong Entity)> _tempOrder = new();

    // Reused per RunSystem: ArrayPool query snapshots in flight, returned after the call.
    private readonly List<ulong[]> _snapshotScratch = new();
    // Reused FlatSharp write buffer (grown as needed); handed to the executor as a
    // span, so there is no exact-length copy.
    private byte[] _writeScratch = new byte[1024];
    // Reused command-apply scratch (bundles/paths are tiny and consumed synchronously).
    private readonly List<(string, ReadOnlyMemory<byte>)> _bundleScratch = new();
    private readonly List<string> _pathScratch = new();
    // Per-system reusable SystemInput object graph — see SysScratch.
    private readonly Dictionary<ModSystemSpec, SysScratch> _sysScratch = new();
    // Reused ObserverInput graph + its payload buffer (one fire at a time).
    private readonly ModJsonBuffer _obsJson = new();
    private readonly CompValue _obsValue = new() { Encoding = ModAbi.Encoding.Json };
    private readonly ObserverInput _obsInput;
    // Reused SpawnedInput graph; the pair list is grown, cleared and refilled.
    private readonly List<SpawnResolved> _spawnedLive = new();
    private readonly List<SpawnResolved> _spawnedPool = new();
    private readonly SpawnedInput _spawnedInput;

    public ModAbiRunner(IModWasmExecutor executor, int handle, CoreModState state, ModHostContext ctx)
    {
        _executor = executor;
        _handle = handle;
        _state = state;
        _ctx = ctx;
        _obsInput = new ObserverInput { Value = _obsValue };
        _spawnedInput = new SpawnedInput { Spawned = _spawnedLive };
    }

    public void Setup()
    {
        _state.PathToId.Clear();
        _state.IdToEntry.Clear();
        _sysToId.Clear();
        _obsByName.Clear();
        // Reload replaces every ModSystemSpec (ReloadMod clears ctx.Systems), so the
        // per-spec scratch keyed on the old ones is dead weight.
        _sysScratch.Clear();

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

        var len = Serialize(Handshake.Serializer, handshake);
        var replyBytes = _executor.CallSetup(_handle, _writeScratch.AsSpan(0, len));
        if (replyBytes.IsEmpty)
            throw new InvalidOperationException("mod_setup returned no SetupReply");
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
                            si.AddQuery(BuildTerms(pd.Query, sd.Name ?? "?"));
                    }
                if (sd.After != null)
                    foreach (var aid in sd.After)
                        if (idToName.TryGetValue(aid, out var n)) si.Spec.After.Add(n);
                if (sd.Before != null)
                    foreach (var bid in sd.Before)
                        if (idToName.TryGetValue(bid, out var n)) si.Spec.Before.Add(n);

                si.Spec.IntervalMs = sd.IntervalMs;

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

    // A term whose type id names nothing registered (or names a resource/event
    // instead of a component) is a HARD setup failure: the row builder emits one
    // CompValue per read term in declaration order, so silently dropping a term
    // shifts every later positional accessor in the guest. Throwing here makes the
    // loader skip the mod with a named error instead.
    private ModQueryTerm[] BuildTerms(QueryDecl? q, string systemName)
    {
        if (q?.Terms == null || q.Terms.Count == 0)
            return Array.Empty<ModQueryTerm>();
        var terms = new ModQueryTerm[q.Terms.Count];
        for (var i = 0; i < q.Terms.Count; i++)
        {
            var t = q.Terms[i];
            if (!_state.IdToEntry.TryGetValue(t.TypeId, out var e) || !_ctx.Registry.TryGet(e.Path, out _))
                throw new InvalidOperationException(
                    $"system '{systemName}' query term #{i} names unregistered component type id {t.TypeId}");
            terms[i] = new ModQueryTerm((ModQueryTermKind)(byte)t.Kind, e.Path);
        }
        return terms;
    }

    public void RunSystem(ModSystemSpec sys)
    {
        _snapshotScratch.Clear();
        var scratch = ScratchFor(sys);
        var liveQueries = scratch.LiveQueries;
        liveQueries.Clear();
        var hasQuery = false;
        var anyRows = false;
        var queryIndex = 0;

        for (var pi = 0; pi < sys.Params.Count; pi++)
        {
            var p = sys.Params[pi];
            if (p.Kind != ModParamKind.Query)
                continue;
            hasQuery = true;
            var q = p.Query!;
            var snapshot = ModdingPlugin.BuildSnapshot(_ctx, q, sys.LastRunWorldTick, out var matched);
            _snapshotScratch.Add(snapshot);
            anyRows |= matched > 0;

            // Resolved once per spec, not per row: mapper + interned wire type id.
            var mappers = q.RowMappers(_ctx.Registry, _state.PathToId, sys.Name);
            var param = scratch.Param(queryIndex++, (uint)pi, mappers.Length);
            var rows = param.LiveRows;
            rows.Clear();

            for (var r = 0; r < matched; r++)
            {
                var entId = snapshot[r];
                if (!_ctx.World.Exists(entId))
                    continue;
                var slot = param.Rent(rows.Count);
                slot.Row.Entity = entId;
                for (var ci = 0; ci < mappers.Length; ci++)
                {
                    var (comp, typeId) = mappers[ci];
                    var json = slot.Json[ci];
                    json.Reset();
                    comp.GetJsonUtf8(_ctx.World, entId, json);
                    var cv = slot.Comps[ci];
                    cv.TypeId = typeId;
                    cv.Data = json.Written;
                }
                rows.Add(slot.Row);
            }
            liveQueries.Add(param.Out);
        }

        // The queries were evaluated above, so the Changed window closes HERE — even
        // when the guest call is idle-skipped below (the rows were still consumed).
        sys.LastRunWorldTick = _ctx.World.CurrentTick;

        try
        {
            // Idle-skip (same policy as every backend): every query empty this
            // tick AND last — skip the guest call. The finally still returns snapshots.
            if (ModdingPlugin.ShouldSkipIdle(sys, hasQuery, anyRows))
                return;

            var sysId = scratch.SysId;
            var input = scratch.Input;
            input.SysId = sysId;
            input.Tick = CurrentTick();
            // null, not an empty vector: a Commands-only system's wire shape must not
            // change (the guest distinguishes "no queries" from "an empty one").
            input.Queries = liveQueries.Count > 0 ? liveQueries : null;
            var len = Serialize(SystemInput.Serializer, input);
            var reply = _executor.CallRun(_handle, sysId, _writeScratch.AsSpan(0, len));
            if (!reply.IsEmpty)
                ApplyCommandBuffer(CommandBuffer.Serializer.Parse(reply));
            NotifySpawned();
        }
        finally
        {
            foreach (var arr in _snapshotScratch)
                ArrayPool<ulong>.Shared.Return(arr);
            _snapshotScratch.Clear();
        }
    }

    private SysScratch ScratchFor(ModSystemSpec sys)
    {
        if (_sysScratch.TryGetValue(sys, out var s))
            return s;
        return _sysScratch[sys] = new SysScratch(_sysToId.TryGetValue(sys, out var sid) ? sid : 0u);
    }

    // FlatSharp serializes FROM the object model, so reusing the SystemInput graph
    // (QueryRows / Row / CompValue and their backing lists + JSON buffers) is what
    // removes the per-row garbage: at 200 rows x 3 components that was 800 objects
    // and 600 byte[] per tick. Grow-only, refilled in place; one graph per system,
    // safe because a mod's systems run one at a time on the scheduler thread.
    private sealed class SysScratch(uint sysId)
    {
        public readonly uint SysId = sysId;
        public readonly SystemInput Input = new();
        public readonly List<QueryRows> LiveQueries = new();
        private readonly List<ParamScratch> _params = new();

        public ParamScratch Param(int index, uint paramIndex, int compCount)
        {
            while (_params.Count <= index)
                _params.Add(new ParamScratch(compCount));
            var p = _params[index];
            p.Out.ParamIndex = paramIndex;
            return p;
        }
    }

    private sealed class ParamScratch
    {
        public readonly QueryRows Out;
        public readonly List<Row> LiveRows = new();
        private readonly List<RowScratch> _pool = new();
        private readonly int _compCount;

        public ParamScratch(int compCount)
        {
            _compCount = compCount;
            Out = new QueryRows { Rows = LiveRows };
        }

        public RowScratch Rent(int index)
        {
            while (_pool.Count <= index)
                _pool.Add(new RowScratch(_compCount));
            return _pool[index];
        }
    }

    private sealed class RowScratch
    {
        public readonly Row Row;
        public readonly CompValue[] Comps;
        public readonly ModJsonBuffer[] Json;

        public RowScratch(int compCount)
        {
            Comps = new CompValue[compCount];
            Json = new ModJsonBuffer[compCount];
            var comps = new List<CompValue>(compCount);
            for (var i = 0; i < compCount; i++)
            {
                Comps[i] = new CompValue { Encoding = ModAbi.Encoding.Json };
                Json[i] = new ModJsonBuffer();
                comps.Add(Comps[i]);
            }
            Row = new Row { Comps = comps };
        }
    }

    public void CallObserver(string export, ulong entity, string json)
    {
        if (!_obsByName.TryGetValue(export, out var obs))
            return; // unknown observer token — no-op

        _obsJson.Reset();
        if (!string.IsNullOrEmpty(json))
            IModComponent.WriteUtf8(_obsJson, json);
        _obsInput.ObsId = obs.ObsId;
        _obsInput.Entity = entity;
        _obsValue.TypeId = obs.TypeId;
        _obsValue.Data = _obsJson.Written;

        var len = Serialize(ObserverInput.Serializer, _obsInput);
        var reply = _executor.CallObserver(_handle, obs.ObsId, entity, _writeScratch.AsSpan(0, len));
        if (!reply.IsEmpty)
            ApplyCommandBuffer(CommandBuffer.Serializer.Parse(reply));
        NotifySpawned();
    }

    // Push the temp-id -> real-ecs-id pairs from the buffer just applied back into the
    // guest (optional mod_spawned export). Called on BOTH apply paths (RunSystem and
    // CallObserver); no-ops when the last buffer spawned nothing. The table is cleared
    // afterwards so a later apply that spawns nothing never re-sends stale pairs.
    private void NotifySpawned()
    {
        if (_tempOrder.Count == 0)
            return;

        _spawnedLive.Clear();
        for (var i = 0; i < _tempOrder.Count; i++)
        {
            while (_spawnedPool.Count <= i)
                _spawnedPool.Add(new SpawnResolved());
            var sr = _spawnedPool[i];
            (sr.TempId, sr.Entity) = _tempOrder[i];
            _spawnedLive.Add(sr);
        }
        _tempTable.Clear();
        _tempOrder.Clear();

        var len = Serialize(SpawnedInput.Serializer, _spawnedInput);
        _executor.CallSpawned(_handle, _writeScratch.AsSpan(0, len));
    }

    // The host picks the logical export name; the core backend maps any bool export
    // onto the single mod_filter guest export. Absent export (or the mod didn't ask
    // to filter via wants_filter) = no call, returns false.
    public bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data)
        => _wantsFilter && _executor.CallFilter(_handle, arg, data);

    public bool WantsFilter => _wantsFilter;

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

    // Serialize into the reused scratch buffer and return the written length; the
    // caller hands the executor `_writeScratch.AsSpan(0, len)` (the executor seam is
    // span-in, so FlatSharp's max-size-vs-actual-size distinction costs no copy).
    private int Serialize<T>(ISerializer<T> serializer, T value) where T : class
    {
        var max = serializer.GetMaxSize(value);
        if (_writeScratch.Length < max)
            _writeScratch = new byte[max];
        return serializer.Write(_writeScratch, value);
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
        _tempOrder.Clear();

        // FlatSharp parses lazily, so a malformed payload (or a JSON body the registry
        // refuses) throws WHERE IT IS TOUCHED — mid-iteration. Without a per-command
        // guard that abandons every command after it: half-built UI, one log line.
        try
        {
            for (var i = 0; i < cmds.Count; i++)
            {
                var kind = default(Cmd.ItemKind);
                try
                {
                    var cmd = cmds[i];
                    kind = cmd.Kind;
                    ApplyOne(commands, cmd);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ecs-mod] {0} cmd #{1} ({2}) failed: {3}", _ctx.Name, i, kind, e.Message);
                }
            }
        }
        catch (Exception e)
        {
            // The vector itself is unreadable — nothing more to salvage.
            Console.WriteLine("[ecs-mod] {0}: command buffer is malformed: {1}", _ctx.Name, e.Message);
        }
    }

    private void ApplyOne(CommandsImpl commands, Cmd cmd)
    {
        switch (cmd.Kind)
        {
            case Cmd.ItemKind.SpawnCmd:
            {
                var sc = cmd.SpawnCmd;
                var ec = commands.Spawn(BuildBundle(sc.Comps));
                var newId = ec.Id().EcsId;
                _tempTable[sc.TempId] = newId;
                _tempOrder.Add((sc.TempId, newId));
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
                        commands.ResourceSet(e.Path, Utf8(v.Data).Span);
                }
                break;
            }
            case Cmd.ItemKind.EmitEventCmd:
            {
                var ee = cmd.EmitEventCmd;
                commands.EmitEvent(ee.EventName ?? string.Empty, ee.Entity,
                    System.Text.Encoding.UTF8.GetString(Utf8(ee.Data).Span));
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

    private ulong Resolve(long entityRef)
    {
        if (entityRef >= 0)
            return (ulong)entityRef;
        var tempId = (uint)(-entityRef - 1);
        return _tempTable.TryGetValue(tempId, out var id) ? id : 0UL;
    }

    // Reused scratch — the returned span is consumed synchronously by the Impl call.
    // Payloads stay as UTF8 slices of the reply buffer: the registry deserializes
    // straight off the span (SetJsonUtf8), so no string per component per command.
    private ReadOnlySpan<(string, ReadOnlyMemory<byte>)> BuildBundle(IList<CompValue>? comps)
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

    // An absent/empty payload IS "{}" (a tag carries no data both ways).
    private static readonly ReadOnlyMemory<byte> EmptyObject = new byte[] { (byte)'{', (byte)'}' };

    private static ReadOnlyMemory<byte> Utf8(Memory<byte>? data)
        => data is { Length: > 0 } d ? d : EmptyObject;

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
