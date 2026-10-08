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
    internal const uint AbiVersion = 4;

    private readonly IModWasmExecutor _executor;
    private readonly int _handle;
    private readonly CoreModState _state;
    private readonly ModHostContext _ctx;

    // guest system id (SystemDecl.id) keyed by the neutral spec the runner passes back.
    private readonly Dictionary<ModSystemSpec, uint> _sysToId = new();
    // observer token (ModObserverSpec.Name = obs id string) -> (obs id, value type id, spec).
    private readonly Dictionary<string, (uint ObsId, ushort TypeId, ModObserverSpec Spec)> _obsByName = new();
    // Per-observer param scratch (observers with params), keyed by token.
    private readonly Dictionary<string, ParamsScratch> _obsScratch = new();
    // Placeholder-id resolver handed to the command applier (cached delegate).
    private readonly ModEntityResolver _resolver;
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
    private readonly Dictionary<ModSystemSpec, string> _profLabels = new();
    // The guest keeps each Res param's last value (SetupReply.res_unchanged), so an
    // unchanged one crosses as a flag instead of its bytes.
    private bool _resUnchanged;
    // Reused ObserverInput graph + its payload buffer (one fire at a time).
    private readonly ModJsonBuffer _obsJson = new();
    private readonly CompValue _obsValue = new() { Encoding = ModAbi.Encoding.Json };
    private readonly ObserverInput _obsInput;
    // Packet observer scratch: the message (FlatSharp serializes from Memory, the chain
    // hands a span) and the verdict's replacement (valid until the next call).
    private byte[] _packetInput = new byte[512];
    private byte[] _packetReplacement = new byte[512];
    private int _packetReplacementLength;
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
        _resolver = ResolvePlaceholder;
    }

    public void Setup()
    {
        _state.PathToId.Clear();
        _state.IdToEntry.Clear();
        _sysToId.Clear();
        _obsByName.Clear();
        _obsScratch.Clear();
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
        _resUnchanged = reply.ResUnchanged;
        TranslateSetup(reply);
    }

    // SetupReply.systems -> ctx.Systems/SystemsByStage (via AppImpl.AddSystems, exactly
    // as the component path does) + observers -> ctx.Observers / PacketObservers. Declaration order is the
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
                        AddParam(si, pd, sd.Name ?? "?");
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
                var spec = new ModObserverSpec { Name = token, Kind = kind, TypePath = typePath };
                if (kind == ModObserverKind.Packet)
                    spec.SetPacketFilter((ModPacketDirection)(byte)od.PacketDirection,
                        od.PacketIds is { } ids ? ids.Span : default);
                if (od.Params != null && od.Params.Count > 0)
                {
                    var holder = new SystemImpl(token);
                    foreach (var pd in od.Params)
                        AddParam(holder, pd, "observer " + token);
                    spec.Params.AddRange(holder.Spec.Params);
                }
                _ctx.AddObserver(spec);
                var valueType = kind is ModObserverKind.Insert or ModObserverKind.Remove ? od.TypeId
                    : kind == ModObserverKind.Custom && od.EventName != null && _state.PathToId.TryGetValue(od.EventName, out var evId) ? evId
                    : (ushort)0xFFFF;
                _obsByName[token] = (od.Id, valueType, spec);
            }
    }

    // One declared parameter. A Res/ResMut/Events param whose type id names no
    // registered resource/event is a setup failure (named), like a bad query term.
    private void AddParam(SystemImpl si, ParamDecl pd, string ownerName)
    {
        switch (pd.Kind)
        {
            case ParamKind.Commands:
                si.AddCommands();
                break;
            case ParamKind.Query:
                si.AddQuery(BuildTerms(pd.Query, ownerName));
                break;
            case ParamKind.Res:
            case ParamKind.ResMut:
                if (!_state.IdToEntry.TryGetValue(pd.TypeId, out var re) || re.Kind != ModRegistryKind.Resource)
                    throw new InvalidOperationException($"'{ownerName}' declares a resource param with unregistered type id {pd.TypeId}");
                si.AddRes(re.Path, pd.Kind == ParamKind.ResMut);
                break;
            case ParamKind.Events:
                if (!_state.IdToEntry.TryGetValue(pd.TypeId, out var ee) || ee.Kind != ModRegistryKind.Event)
                    throw new InvalidOperationException($"'{ownerName}' declares an events param with unregistered type id {pd.TypeId}");
                si.AddEvents(_ctx, ee.Path);
                break;
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
            if (!_state.IdToEntry.TryGetValue(t.TypeId, out var e) || !_ctx.Registry.TryGet(e.Path, out var comp))
                throw new InvalidOperationException(
                    $"system '{systemName}' query term #{i} names unregistered component type id {t.TypeId}");
            if (t.Kind == QueryTermKind.Mut)
                ModQueryTerm.RejectReadOnlyMut(_ctx,
                    systemName.StartsWith("observer ", StringComparison.Ordinal) ? systemName : $"system '{systemName}'", comp, e.Path);
            terms[i] = new ModQueryTerm((ModQueryTermKind)(byte)t.Kind, e.Path);
        }
        return terms;
    }

    public void RunSystem(ModSystemSpec sys)
    {
        // ModProfiler census (TINYECS_MOD_PROFILE=1): timestamps only when on.
        var prof = ModProfiler.Enabled;
        ModProfiler.Stat? stat = null;
        long t0 = 0;
        if (prof)
        {
            ModProfiler.Frame(_ctx.App != null && _ctx.App.HasResource<TinyEcs.Bevy.Time>() ? _ctx.App.GetResource<TinyEcs.Bevy.Time>().Total : 0);
            if (!_profLabels.TryGetValue(sys, out var label))
                _profLabels[sys] = label = ModProfiler.Label(sys);
            stat = ModProfiler.Get(_ctx.Name, label);
            t0 = ModProfiler.Now();
        }

        _snapshotScratch.Clear();
        var scratch = ScratchFor(sys);
        var gated = BuildInputs(sys.Params, sys.LastRunWorldTick, sys.Name, scratch.Params, out var anyRows);

        // The queries were evaluated above, so the Changed/Added window closes HERE -
        // even when the guest call is idle-skipped below (the rows were still consumed).
        sys.LastRunWorldTick = TinyEcs.Bevy.SystemTicks.Current;

        try
        {
            // Idle-skip (same policy as every backend): every query empty this tick AND
            // last - skip the guest call. A resource param opts out (no signal to gate on).
            if (ModdingPlugin.ShouldSkipIdle(sys, gated, anyRows))
            {
                if (stat != null)
                {
                    stat.Skipped++;
                    stat.BuildTicks += ModProfiler.Now() - t0;
                }
                return;
            }

            var sysId = scratch.SysId;
            var input = scratch.Input;
            input.SysId = sysId;
            input.Tick = CurrentTick();
            // null, not an empty vector: a Commands-only system's wire shape must not
            // change (the guest distinguishes "no queries" from "an empty one").
            input.Queries = scratch.Params.LiveQueries.Count > 0 ? scratch.Params.LiveQueries : null;
            input.Resources = scratch.Params.LiveResources.Count > 0 ? scratch.Params.LiveResources : null;
            input.Events = scratch.Params.LiveEvents.Count > 0 ? scratch.Params.LiveEvents : null;
            var len = Serialize(SystemInput.Serializer, input);
            var t1 = prof ? ModProfiler.Now() : 0;
            Memory<byte> reply;
            try
            {
                reply = _executor.CallRun(_handle, sysId, _writeScratch.AsSpan(0, len));
            }
            catch
            {
                // The guest may not have taken this run's resource values in: the next
                // run sends them in full.
                scratch.Params.ForgetDelivered();
                throw;
            }
            var t2 = prof ? ModProfiler.Now() : 0;
            if (!reply.IsEmpty)
                ApplyCommandBuffer(ParseCommands(reply));
            NotifySpawned();
            if (stat != null)
            {
                foreach (var q in scratch.Params.LiveQueries)
                    stat.Rows += q.Rows?.Count ?? 0;
                stat.Calls++;
                stat.BytesIn += len;
                stat.BytesOut += reply.Length;
                stat.BuildTicks += t1 - t0;
                stat.GuestTicks += t2 - t1;
                stat.ApplyTicks += ModProfiler.Now() - t2;
            }
        }
        finally
        {
            ReturnSnapshots();
        }
    }

    private void ReturnSnapshots()
    {
        foreach (var arr in _snapshotScratch)
            ArrayPool<ulong>.Shared.Return(arr);
        _snapshotScratch.Clear();
    }

    // Evaluates a system's / observer's params into scratch.Live*: query rows, resource
    // values, the events since the last run. Returns whether the idle-skip may gate on
    // it (has a query and no resource param); anyRows = some query matched or some
    // events arrived.
    private bool BuildInputs(List<ModParam> ps, uint sinceTick, string ownerName, ParamsScratch scratch, out bool anyRows)
    {
        scratch.LiveQueries.Clear();
        scratch.LiveResources.Clear();
        scratch.LiveEvents.Clear();
        anyRows = false;
        var hasQuery = false;
        var hasRes = false;
        var queryIndex = 0;
        var resIndex = 0;
        var eventsIndex = 0;

        for (var pi = 0; pi < ps.Count; pi++)
        {
            var p = ps[pi];
            switch (p.Kind)
            {
                case ModParamKind.Query:
                {
                    hasQuery = true;
                    var q = p.Query!;
                    var snapshot = ModdingPlugin.BuildSnapshot(_ctx, q, sinceTick, out var matched);
                    _snapshotScratch.Add(snapshot);
                    anyRows |= matched > 0;

                    // Resolved once per spec, not per row: mapper + interned wire type id.
                    var mappers = q.RowMappers(_ctx.Registry, _state.PathToId, ownerName);
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
                    scratch.LiveQueries.Add(param.Out);
                    break;
                }
                case ModParamKind.Res:
                case ModParamKind.ResMut:
                {
                    hasRes = true;
                    var slot = scratch.Res(resIndex++);
                    slot.Out.ParamIndex = (uint)pi;
                    slot.Out.Value = null;
                    slot.Out.Unchanged = false;
                    var present = false;
                    if (_ctx.App != null && _ctx.Registry.TryGetResource(p.TypePath!, out var res))
                    {
                        // The calling mod's slice: a per-mod resource serves its own state.
                        slot.Json.Reset();
                        res.GetJsonUtf8For(_ctx.App, _ctx.Name, slot.Json);
                        if (!ModJson.IsAbsent(slot.Json.WrittenSpan))
                        {
                            present = true;
                            slot.Value.TypeId = _state.PathToId.TryGetValue(p.TypePath!, out var tid) ? tid : (ushort)0xFFFF;
                            slot.Value.Data = slot.Json.Written;
                            slot.Out.Value = slot.Value;
                        }
                    }
                    // A guest that keeps each Res param's last value (SetupReply.res_unchanged)
                    // is told "unchanged" instead of being re-sent the same bytes. Resources
                    // carry no change ticks, so the signal is a byte compare against what
                    // this param last delivered.
                    if (_resUnchanged)
                    {
                        if (present && slot.Delivered && slot.WrittenEqualsLast())
                        {
                            slot.Out.Value = null;
                            slot.Out.Unchanged = true;
                        }
                        else
                        {
                            slot.RememberDelivered(present);
                        }
                    }
                    scratch.LiveResources.Add(slot.Out);
                    break;
                }
                case ModParamKind.Events:
                {
                    var buffer = p.Events!;
                    buffer.Swap();
                    anyRows |= buffer.Current.Count > 0;
                    var slot = scratch.Events(eventsIndex++);
                    slot.Out.ParamIndex = (uint)pi;
                    var typeId = _state.PathToId.TryGetValue(p.TypePath!, out var etid) ? etid : (ushort)0xFFFF;
                    slot.Values.Clear();
                    foreach (var json in buffer.Current)
                        slot.Values.Add(slot.Rent(slot.Values.Count, typeId, json));
                    scratch.LiveEvents.Add(slot.Out);
                    break;
                }
            }
        }
        return hasQuery && !hasRes;
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
        public readonly ParamsScratch Params = new();
    }

    private sealed class ParamsScratch
    {
        // Diagnostic owner name (an observer's, built once instead of per fire).
        public string Label = "";
        public readonly List<QueryRows> LiveQueries = new();
        public readonly List<ResValue> LiveResources = new();
        public readonly List<EventValues> LiveEvents = new();
        private readonly List<ParamScratch> _params = new();
        private readonly List<ResScratch> _res = new();
        private readonly List<EventsScratch> _events = new();

        public ParamScratch Param(int index, uint paramIndex, int compCount)
        {
            while (_params.Count <= index)
                _params.Add(new ParamScratch(compCount));
            var p = _params[index];
            p.Out.ParamIndex = paramIndex;
            return p;
        }

        public ResScratch Res(int index)
        {
            while (_res.Count <= index)
                _res.Add(new ResScratch());
            return _res[index];
        }

        /// Forget what every Res param delivered: the next run sends full values.
        public void ForgetDelivered()
        {
            foreach (var r in _res)
                r.Delivered = false;
        }

        public EventsScratch Events(int index)
        {
            while (_events.Count <= index)
                _events.Add(new EventsScratch());
            return _events[index];
        }
    }

    private sealed class ResScratch
    {
        public readonly ResValue Out = new();
        public readonly CompValue Value = new() { Encoding = ModAbi.Encoding.Json };
        public readonly ModJsonBuffer Json = new();
        // The bytes this param last delivered (valid while Delivered).
        public bool Delivered;
        private byte[] _last = Array.Empty<byte>();
        private int _lastLen;

        public bool WrittenEqualsLast() => Json.WrittenSpan.SequenceEqual(_last.AsSpan(0, _lastLen));

        public void RememberDelivered(bool present)
        {
            Delivered = present;
            if (!present)
                return;
            var src = Json.WrittenSpan;
            if (_last.Length < src.Length)
                _last = new byte[Math.Max(src.Length, _last.Length * 2)];
            src.CopyTo(_last);
            _lastLen = src.Length;
        }
    }

    private sealed class EventsScratch
    {
        public readonly EventValues Out;
        public readonly List<CompValue> Values = new();
        // Grow-only per-index CompValue + its own UTF8 buffer (see ModJsonBuffer: a
        // shared arena would relocate under the Memory slices already handed out).
        private readonly List<(CompValue Value, ModJsonBuffer Json)> _pool = new();
        public EventsScratch() => Out = new EventValues { Values = Values };

        public CompValue Rent(int index, ushort typeId, string json)
        {
            while (_pool.Count <= index)
                _pool.Add((new CompValue { Encoding = ModAbi.Encoding.Json }, new ModJsonBuffer()));
            var (cv, buf) = _pool[index];
            buf.Reset();
            IModComponent.WriteUtf8(buf, json);
            cv.TypeId = typeId;
            cv.Data = buf.Written;
            return cv;
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
        CallObserverBuffered(export, entity, obs);
    }

    public void CallObserver(string export, ulong entity, ReadOnlySpan<byte> json)
    {
        if (!_obsByName.TryGetValue(export, out var obs))
            return; // unknown observer token — no-op
        _obsJson.Reset();
        if (!json.IsEmpty)
        {
            json.CopyTo(_obsJson.GetSpan(json.Length));
            _obsJson.Advance(json.Length);
        }
        CallObserverBuffered(export, entity, obs);
    }

    // The payload is already in _obsJson (copied out of the caller's span BEFORE the
    // guest call, which may enqueue further fires into the arena it points at).
    private void CallObserverBuffered(string export, ulong entity, (uint ObsId, ushort TypeId, ModObserverSpec Spec) obs)
    {
        _obsInput.ObsId = obs.ObsId;
        _obsInput.Entity = entity;
        _obsValue.TypeId = obs.TypeId;
        _obsValue.Data = _obsJson.Written;
        _obsInput.Queries = null;
        _obsInput.Resources = null;
        _obsInput.Events = null;
        _obsInput.Packet = null;
        _obsInput.PacketDirection = default;
        CallObserverInput(export, entity, obs);
    }

    // Serializes _obsInput (the caller filled the trigger half) plus the observer's own
    // params, calls the guest and applies the returned commands. A packet observer's
    // verdict + replacement are taken out of the reply before anything else can call
    // into this instance.
    private ModPacketVerdict CallObserverInput(string export, ulong entity, (uint ObsId, ushort TypeId, ModObserverSpec Spec) obs)
    {
        var verdict = ModPacketVerdict.Pass;
        _snapshotScratch.Clear();
        try
        {
            // An observer's own params (queries / res / events) are evaluated per fire,
            // exactly like a system's.
            var spec = obs.Spec;
            if (spec.Params.Count > 0)
            {
                if (!_obsScratch.TryGetValue(export, out var scratch))
                    _obsScratch[export] = scratch = new ParamsScratch { Label = "observer " + export };
                BuildInputs(spec.Params, spec.LastRunWorldTick, scratch.Label, scratch, out _);
                spec.LastRunWorldTick = TinyEcs.Bevy.SystemTicks.Current;
                _obsInput.Queries = scratch.LiveQueries.Count > 0 ? scratch.LiveQueries : null;
                _obsInput.Resources = scratch.LiveResources.Count > 0 ? scratch.LiveResources : null;
                _obsInput.Events = scratch.LiveEvents.Count > 0 ? scratch.LiveEvents : null;
            }

            var len = Serialize(ObserverInput.Serializer, _obsInput);
            var t1 = ModProfiler.Enabled ? ModProfiler.Now() : 0;
            Memory<byte> reply;
            try
            {
                reply = _executor.CallObserver(_handle, obs.ObsId, entity, _writeScratch.AsSpan(0, len));
            }
            catch
            {
                if (_obsScratch.TryGetValue(export, out var failed))
                    failed.ForgetDelivered();
                throw;
            }
            var t2 = ModProfiler.Enabled ? ModProfiler.Now() : 0;
            if (!reply.IsEmpty)
            {
                var cb = ParseCommands(reply);
                if (obs.Spec.Kind == ModObserverKind.Packet)
                    verdict = TakeVerdict(cb);
                ApplyCommandBuffer(cb);
            }
            NotifySpawned();
            if (ModProfiler.Enabled && obs.Spec.Kind != ModObserverKind.Packet)
            {
                var stat = ModProfiler.Get(_ctx.Name, "obs:" + obs.Spec.Kind + ":" + (obs.Spec.TypePath ?? export));
                stat.Calls++;
                stat.BytesIn += len;
                stat.BytesOut += reply.Length;
                stat.GuestTicks += t2 - t1;
                stat.ApplyTicks += ModProfiler.Now() - t2;
            }
        }
        finally
        {
            ReturnSnapshots();
        }
        return verdict;
    }

    // Copies the replacement out of the parsed reply: it aliases _replyCopy, which the
    // commands applied next (or NotifySpawned) may re-enter and overwrite.
    private ModPacketVerdict TakeVerdict(CommandBuffer cb)
    {
        var verdict = (ModPacketVerdict)(byte)cb.Verdict;
        if (verdict != ModPacketVerdict.Replace)
            return verdict == ModPacketVerdict.Block ? verdict : ModPacketVerdict.Pass;
        var bytes = cb.Replacement is { } r ? r.Span : default;
        if (bytes.IsEmpty)
            return ModPacketVerdict.Pass;
        if (_packetReplacement.Length < bytes.Length)
            _packetReplacement = new byte[Math.Max(bytes.Length, _packetReplacement.Length * 2)];
        bytes.CopyTo(_packetReplacement);
        _packetReplacementLength = bytes.Length;
        return ModPacketVerdict.Replace;
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

    // mod_observer with the message in ObserverInput.packet (entity 0, no value); the
    // verdict rides the returned CommandBuffer, 0 / no reply = Pass.
    public ModPacketVerdict CallPacketObserver(string export, ModPacketDirection dir, ReadOnlySpan<byte> packet, out ReadOnlySpan<byte> replacement)
    {
        replacement = default;
        if (!_obsByName.TryGetValue(export, out var obs))
            return ModPacketVerdict.Pass;
        if (_packetInput.Length < packet.Length)
            _packetInput = new byte[Math.Max(packet.Length, _packetInput.Length * 2)];
        packet.CopyTo(_packetInput);
        _obsInput.ObsId = obs.ObsId;
        _obsInput.Entity = 0;
        _obsInput.Value = null;
        _obsInput.Queries = null;
        _obsInput.Resources = null;
        _obsInput.Events = null;
        _obsInput.PacketDirection = (PacketDirection)(byte)dir;
        _obsInput.Packet = _packetInput.AsMemory(0, packet.Length);
        ModPacketVerdict verdict;
        try
        {
            verdict = CallObserverInput(export, 0, obs);
        }
        finally
        {
            _obsInput.Value = _obsValue;
            _obsInput.Packet = null;
        }
        if (verdict == ModPacketVerdict.Replace)
            replacement = _packetReplacement.AsSpan(0, _packetReplacementLength);
        return verdict;
    }

    // Re-instantiate from fresh bytes (the executor reuses whatever host-import
    // wiring it built at Load), then re-run setup — ModdingPlugin.ReloadMod has
    // already cleared ctx.Systems/Observers.
    public void Reload(in ModSource source)
    {
        _executor.Reload(_handle, in source);
        Setup();
    }

    public void Dispose() => _executor.DisposeInstance(_handle);

    // The guest-facing `SystemInput.tick` is HOST MILLISECONDS (Time.Total), not a
    // world change tick and not a frame counter: guests use it as a monotonic clock
    // (mod SDK `Wait.Ms(ms)` / `Wait.Until(.., timeoutMs)` / per-tick memo
    // invalidation). Deliberately unaffected by the change-tick redesign — exporting
    // World.CurrentTick here would hand guests a counter that moves once per system
    // run, and World.FrameCount would silently reinterpret every guest-side
    // millisecond deadline as frames.
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

    // The reply is parsed LAZILY (payloads stay slices of the buffer; greedy parsing
    // copied every component payload into a fresh byte[]) out of a runner-owned copy:
    // the executor's reply buffer is only valid until its next call on this handle, and
    // applying a command can re-enter it (an emitted action sends a packet -> the
    // packet chain -> this mod's packet observer) while the walk is still reading.
    private byte[] _replyCopy = new byte[1024];

    private CommandBuffer ParseCommands(Memory<byte> reply)
    {
        if (_replyCopy.Length < reply.Length)
            _replyCopy = new byte[Math.Max(_replyCopy.Length * 2, reply.Length)];
        reply.Span.CopyTo(_replyCopy);
        return CommandBuffer.Serializer.Parse(_replyCopy.AsMemory(0, reply.Length), FlatBufferDeserializationOption.Lazy);
    }
    // Walks the cmds in order through the neutral GuestBridge Impl structs. Entity
    // refs are int64: >=0 real ecs id, <0 temp ref (index = -(v)-1 into the temp-id
    // table established by SpawnCmd.temp_id entries of THIS buffer).
    private void ApplyCommandBuffer(CommandBuffer cb)
    {
        var cmds = cb.Cmds;
        if (cmds == null || cmds.Count == 0)
            return;

        var commands = new CommandsImpl(_ctx, _resolver);
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
                commands.EmitEvent(ee.EventName ?? string.Empty, ee.Entity, Utf8(ee.Data).Span);
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

    // A placeholder (1<<63 | temp_id) in a component's entity field -> the entity this
    // buffer spawned under that temp id (0 if none). Real ids pass through.
    private ulong ResolvePlaceholder(ulong id)
        => !ModEntityRef.IsPlaceholder(id) ? id
            : _tempTable.TryGetValue(ModEntityRef.TempId(id), out var real) ? real : 0UL;

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
        // Index loops: foreach over the IList boxes an enumerator per command.
        var count = comps?.Count ?? 0;
        for (var i = 0; i < count; i++)
        {
            var cv = comps![i];
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
        var count = typeIds?.Count ?? 0;
        for (var i = 0; i < count; i++)
            if (_state.IdToEntry.TryGetValue(typeIds![i], out var e))
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
