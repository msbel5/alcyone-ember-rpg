using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;

// Design note:
// JobBoard is Faz 3's deterministic PROCESS queue for pending JobRequest rows.
// It only stores, orders, claims, and removes jobs. Actor preference matching,
// RecipeSystem starts, save/load mapping, and EventLog output remain later atom
// rows so this bundle stays pure and easily testable.
// Atom-map ref: DOCS/sprint-faz-3-atom-map.md Job board state.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Pure-Domain queue of pending jobs. Requests enumerate in insertion order,
    /// while selection chooses the lowest active priority and then insertion order.
    /// </summary>
    public sealed class JobBoard
    {
        private readonly Dictionary<JobId, Entry> _byId = new Dictionary<JobId, Entry>();
        private readonly List<JobId> _order = new List<JobId>();
        private int _nextClaimSequence;

        /// <summary>Number of pending jobs held by the board.</summary>
        public int Count => _byId.Count;

        /// <summary>Adds a pending request in deterministic insertion order.</summary>
        public void Add(JobRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (_byId.ContainsKey(request.Id))
                throw new InvalidOperationException($"JobBoard already contains {request.Id}.");

            _byId.Add(request.Id, new Entry(request));
            _order.Add(request.Id);
        }

        /// <summary>Returns true when the board contains the given pending job id.</summary>
        public bool Contains(JobId id)
        {
            return !id.IsEmpty && _byId.ContainsKey(id);
        }

        /// <summary>Tries to fetch a request by id.</summary>
        public bool TryGet(JobId id, out JobRequest request)
        {
            if (!id.IsEmpty && _byId.TryGetValue(id, out var entry))
            {
                request = entry.Request;
                return true;
            }

            request = null;
            return false;
        }

        /// <summary>
        /// Finds the next unclaimed job by request priority, then by insertion order.
        /// </summary>
        public bool TryPeekNext(out JobRequest request)
        {
            request = null;
            Entry best = null;
            var bestOrder = -1;

            for (var i = 0; i < _order.Count; i++)
            {
                var entry = _byId[_order[i]];
                if (entry.IsClaimed)
                    continue;

                if (best == null || entry.Request.Priority.CompareTo(best.Request.Priority) < 0)
                {
                    best = entry;
                    bestOrder = i;
                }
                else if (best != null && entry.Request.Priority.CompareTo(best.Request.Priority) == 0 && i < bestOrder)
                {
                    best = entry;
                    bestOrder = i;
                }
            }

            if (best == null)
                return false;

            request = best.Request;
            return true;
        }

        /// <summary>
        /// Claims one existing unclaimed job for one actor. Returns false for empty
        /// actors, missing jobs, already claimed jobs, or actors that already hold
        /// another pending claim.
        /// </summary>
        public bool TryClaim(JobId id, ActorId actorId, out JobRequest request)
        {
            request = null;
            if (id.IsEmpty || actorId.IsEmpty)
                return false;
            if (!_byId.TryGetValue(id, out var entry) || entry.IsClaimed)
                return false;
            if (_byId.Values.Any(candidate => candidate.ClaimedBy == actorId))
                return false;

            entry.ClaimedBy = actorId;
            entry.ClaimSequence = _nextClaimSequence++;
            request = entry.Request;
            return true;
        }

        /// <summary>
        /// Returns the deterministic queue index for a claimed job among all claimed
        /// jobs sharing the same worksite (SiteId + WorksitePosition). Ordering follows
        /// claim sequence: the first actor to claim at that worksite gets index 0.
        /// Returns -1 for empty ids, unknown ids, or unclaimed jobs.
        /// Closes CO-04 in DOCS/sprint-faz-4-atom-map.md Debt ledger.
        /// </summary>
        public int GetQueueIndex(JobId id)
        {
            if (id.IsEmpty || !_byId.TryGetValue(id, out var entry) || !entry.IsClaimed)
                return -1;

            var queueIndex = 0;
            foreach (var candidate in _byId.Values)
            {
                if (!candidate.IsClaimed)
                    continue;
                if (candidate.Request.Id.Equals(entry.Request.Id))
                    continue;
                if (!candidate.Request.SiteId.Equals(entry.Request.SiteId))
                    continue;
                if (!candidate.Request.WorksitePosition.Equals(entry.Request.WorksitePosition))
                    continue;
                if (candidate.ClaimSequence < entry.ClaimSequence)
                    queueIndex++;
            }
            return queueIndex;
        }

        /// <summary>Returns true when a pending job has been claimed.</summary>
        public bool IsClaimed(JobId id)
        {
            return !id.IsEmpty && _byId.TryGetValue(id, out var entry) && entry.IsClaimed;
        }

        /// <summary>
        /// Returns the deterministic lifecycle status for a tracked job.
        /// Pending when present but unclaimed, Assigned when an actor holds the claim.
        /// Terminal states (Completed/Canceled) are removed from the board, so an
        /// unknown id resolves to Pending as the safe non-terminal default for callers.
        /// Closes CO-05 in DOCS/sprint-faz-4-atom-map.md Debt ledger.
        /// </summary>
        public JobStatus GetStatus(JobId id)
        {
            if (id.IsEmpty || !_byId.TryGetValue(id, out var entry))
                return JobStatus.Pending;
            return entry.IsClaimed ? JobStatus.Assigned : JobStatus.Pending;
        }

        /// <summary>Returns the actor holding a pending job claim, or ActorId.Empty.</summary>
        public ActorId GetClaimedBy(JobId id)
        {
            return !id.IsEmpty && _byId.TryGetValue(id, out var entry) ? entry.ClaimedBy : default;
        }

        /// <summary>
        /// Returns the deterministic claim sequence for a claimed job, or 0 when unclaimed
        /// or unknown. Persisted by SliceSaveMapper so the load path can preserve original
        /// queue order across save/load (PR#138 bot review fix).
        /// </summary>
        public int GetClaimSequence(JobId id)
        {
            return !id.IsEmpty && _byId.TryGetValue(id, out var entry) ? entry.ClaimSequence : 0;
        }

        /// <summary>
        /// Restores a claim with the exact sequence number recorded by a prior save.
        /// Behaves like <see cref="TryClaim"/> but does not increment _nextClaimSequence
        /// off the wire — instead it bumps the high-water mark so subsequent live
        /// claims keep producing monotonically-growing sequence values.
        /// </summary>
        public bool TryRestoreClaim(JobId id, ActorId actorId, int claimSequence)
        {
            if (id.IsEmpty || actorId.IsEmpty)
                return false;
            if (!_byId.TryGetValue(id, out var entry) || entry.IsClaimed)
                return false;
            entry.ClaimedBy = actorId;
            entry.ClaimSequence = claimSequence;
            if (claimSequence >= _nextClaimSequence)
                _nextClaimSequence = claimSequence + 1;
            return true;
        }

        /// <summary>Removes a completed pending job.</summary>
        public bool Complete(JobId id)
        {
            return Remove(id);
        }

        /// <summary>Removes a canceled pending job.</summary>
        public bool Cancel(JobId id)
        {
            return Remove(id);
        }

        /// <summary>Drops every pending job and claim.</summary>
        public void Clear()
        {
            _byId.Clear();
            _order.Clear();
        }

        /// <summary>Requests in deterministic insertion order, including claimed jobs.</summary>
        public IEnumerable<JobRequest> Requests
        {
            get
            {
                foreach (var id in _order)
                    yield return _byId[id].Request;
            }
        }

        private bool Remove(JobId id)
        {
            if (id.IsEmpty || !_byId.Remove(id))
                return false;

            _order.Remove(id);
            return true;
        }

        private sealed class Entry
        {
            public Entry(JobRequest request)
            {
                Request = request;
            }

            public JobRequest Request { get; }

            public ActorId ClaimedBy { get; set; }

            public int ClaimSequence { get; set; }

            public bool IsClaimed => !ClaimedBy.IsEmpty;
        }
    }
}
