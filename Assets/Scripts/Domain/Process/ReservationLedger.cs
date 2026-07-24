using System.Collections.Generic;

// Design note:
// W32 EAT slice (docs/ruh/w32/02-decision-reservation.md §4): the deterministic reservation
// ledger living on WorldState. Rows + NextId are the saved truth (insertion order = save order);
// the (site,tag) count and per-actor indexes are DERIVED, never saved, rebuilt after load.
// CONSTRAINT (determinism): Try-pattern, no exceptions, no RNG; NextId is persisted so ids
// never collide across a save/load boundary.
namespace EmberCrpg.Domain.Process
{
    /// <summary>Deterministic count-based reservation ledger for stockpile units.</summary>
    public sealed class ReservationLedger
    {
        public List<ReservationRecord> Rows = new List<ReservationRecord>();
        public ulong NextId = 1;

        private readonly Dictionary<(ulong SiteId, string Tag), int> _countBySiteTag = new Dictionary<(ulong, string), int>();
        private readonly Dictionary<ulong, ReservationRecord> _rowByActor = new Dictionary<ulong, ReservationRecord>();

        /// <summary>Active reserved units of a tag at a site. O(1).</summary>
        public int ReservedCount(ulong siteId, string tag)
        {
            return tag != null && _countBySiteTag.TryGetValue((siteId, tag), out var count) ? count : 0;
        }

        /// <summary>
        /// Claims 1 unit. False when the effective stock is exhausted (the LAST unit is never
        /// handed out twice — core invariant) or the actor already holds a row (max 1 per actor).
        /// </summary>
        public bool TryReserve(ulong siteId, string tag, ulong actorId, long untilMinutes, int pileCount, out ulong id)
        {
            id = 0UL;
            if (string.IsNullOrEmpty(tag))
                return false;
            if (pileCount - ReservedCount(siteId, tag) <= 0)
                return false;
            if (_rowByActor.ContainsKey(actorId))
                return false;

            var row = new ReservationRecord
            {
                Id = NextId++,
                SiteId = siteId,
                ItemTag = tag,
                ActorId = actorId,
                UntilMinutes = untilMinutes,
            };
            Rows.Add(row);
            Index(row);
            id = row.Id;
            return true;
        }

        /// <summary>Removes a row by id. Missing id is an idempotent no-op (double release), returns false.</summary>
        public bool Release(ulong id)
        {
            for (var i = 0; i < Rows.Count; i++)
            {
                if (Rows[i] != null && Rows[i].Id == id)
                {
                    Unindex(Rows[i]);
                    Rows.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Finds the actor's active row (arrival validation path). O(1).</summary>
        public bool TryGetByActor(ulong actorId, out ReservationRecord row)
        {
            return _rowByActor.TryGetValue(actorId, out row);
        }

        /// <summary>Removes rows with UntilMinutes &lt; now in Rows order (deterministic); returns the count.</summary>
        public int SweepExpired(long nowMinutes, ICollection<ReservationRecord> removed)
        {
            var count = 0;
            for (var i = Rows.Count - 1; i >= 0; i--)
            {
                var row = Rows[i];
                if (row == null || row.UntilMinutes >= nowMinutes)
                    continue;
                Unindex(row);
                Rows.RemoveAt(i);
                removed?.Add(row);
                count++;
            }

            return count;
        }

        /// <summary>Rebuilds the derived indexes from Rows — load / EnsureInvariants path.</summary>
        public void RebuildIndexes()
        {
            Rows ??= new List<ReservationRecord>();
            if (NextId == 0UL)
                NextId = 1UL;
            _countBySiteTag.Clear();
            _rowByActor.Clear();
            foreach (var row in Rows)
            {
                if (row != null)
                    Index(row);
            }
        }

        private void Index(ReservationRecord row)
        {
            var key = (row.SiteId, row.ItemTag ?? string.Empty);
            _countBySiteTag.TryGetValue(key, out var count);
            _countBySiteTag[key] = count + 1;
            _rowByActor[row.ActorId] = row;
        }

        private void Unindex(ReservationRecord row)
        {
            var key = (row.SiteId, row.ItemTag ?? string.Empty);
            if (_countBySiteTag.TryGetValue(key, out var count))
            {
                if (count <= 1)
                    _countBySiteTag.Remove(key);
                else
                    _countBySiteTag[key] = count - 1;
            }

            if (_rowByActor.TryGetValue(row.ActorId, out var current) && ReferenceEquals(current, row))
                _rowByActor.Remove(row.ActorId);
        }
    }
}
