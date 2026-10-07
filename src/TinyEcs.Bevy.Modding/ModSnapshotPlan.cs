// The pushed snapshot as a real TinyEcs query: every term whose mapper is a plain
// column (ModComponent / ModPresence) becomes a With / Without of one composed query,
// so only matching archetypes are visited and a Changed / Added term reads its column's
// tick array directly. Terms over projections (a mapper that is not one column) are
// evaluated per surviving entity through their Has / ChangedSince / AddedSince hooks.

using System.Buffers;
using TinyEcs.Collections;

namespace TinyEcs.Bevy.Modding;

internal sealed class ModSnapshotPlan
{
    private readonly Query? _query;          // null = a required term names nothing registered
    private readonly int[] _tickTerms;       // plain Changed / Added terms
    private readonly int[] _slowTerms;       // projection terms, tick-filtered ones first

    private ModSnapshotPlan(Query? query, int[] tickTerms, int[] slowTerms)
    {
        _query = query;
        _tickTerms = tickTerms;
        _slowTerms = slowTerms;
    }

    /// Null when no plain required term exists to drive the archetype query.
    public static ModSnapshotPlan? Build(World world, (IModComponent? Comp, ModQueryTermKind Kind)[] mappers)
    {
        var qb = world.QueryBuilder();
        var plainRequired = 0;
        var impossible = false;
        var ticks = new List<int>();
        var slowFirst = new List<int>();
        var slowRest = new List<int>();
        for (var i = 0; i < mappers.Length; i++)
        {
            var (comp, kind) = mappers[i];
            var without = kind == ModQueryTermKind.Without;
            var tickTerm = kind is ModQueryTermKind.Changed or ModQueryTermKind.Added;
            if (comp == null)
            {
                // Unregistered: matches only as a Without (BuildSnapshot's rule).
                if (!without)
                    impossible = true;
                continue;
            }
            if (comp.AddQueryTerm(qb, without))
            {
                if (!without)
                    plainRequired++;
                if (tickTerm)
                    ticks.Add(i);
                continue;
            }
            (tickTerm ? slowFirst : slowRest).Add(i);
        }
        if (impossible)
            return new ModSnapshotPlan(null, Array.Empty<int>(), Array.Empty<int>());
        if (plainRequired == 0)
            return null;
        slowFirst.AddRange(slowRest);
        return new ModSnapshotPlan(qb.Build(), ticks.ToArray(), slowFirst.ToArray());
    }

    public ulong[] Run(World world, (IModComponent? Comp, ModQueryTermKind Kind)[] mappers, uint sinceTick, out int matched)
    {
        matched = 0;
        if (_query == null)
            return ArrayPool<ulong>.Shared.Rent(1);

        var now = world.CurrentTick;
        var found = new PooledList<ulong>(16);
        bool[]? mask = null;
        try
        {
            var it = _query.Iter();
            while (it.Next())
            {
                var entities = it.Entities();
                var n = entities.Length;
                if (mask == null || mask.Length < n)
                {
                    if (mask != null)
                        ArrayPool<bool>.Shared.Return(mask);
                    mask = ArrayPool<bool>.Shared.Rent(n);
                }
                mask.AsSpan(0, n).Fill(true);

                foreach (var ti in _tickTerms)
                {
                    var (comp, kind) = mappers[ti];
                    var ticks = comp!.ColumnTicks(ref it, kind == ModQueryTermKind.Added);
                    if (ticks.IsEmpty)
                        continue;
                    for (var r = 0; r < n; r++)
                        if (mask[r] && !ChangeTick.IsNewerThan(ticks[r], sinceTick, now))
                            mask[r] = false;
                }

                for (var r = 0; r < n; r++)
                {
                    if (!mask[r])
                        continue;
                    var id = entities[r].ID;
                    var ok = true;
                    foreach (var si in _slowTerms)
                    {
                        var (comp, kind) = mappers[si];
                        var has = kind switch
                        {
                            ModQueryTermKind.Changed => comp!.ChangedSince(world, id, sinceTick),
                            ModQueryTermKind.Added => comp!.AddedSince(world, id, sinceTick),
                            _ => comp!.Has(world, id),
                        };
                        if (kind == ModQueryTermKind.Without ? has : !has)
                        {
                            ok = false;
                            break;
                        }
                    }
                    if (ok)
                        found.Add(id);
                }
            }

            var result = ArrayPool<ulong>.Shared.Rent(found.Count == 0 ? 1 : found.Count);
            for (var i = 0; i < found.Count; i++)
                result[i] = found[i];
            matched = found.Count;
            return result;
        }
        finally
        {
            if (mask != null)
                ArrayPool<bool>.Shared.Return(mask);
            found.Dispose();
        }
    }
}
