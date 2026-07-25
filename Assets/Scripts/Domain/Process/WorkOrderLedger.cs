using System.Collections.Generic;

// Design note:
// W34 WORK slice (docs/ruh/w34/02-work-actions.md §5.2): the work-in-progress piece ON the
// bench, homed on WorldState so it outlives both the action chain and the claimant (the
// "pause" semantics) and so the save mapper has a world store to write — the old
// JobAssignmentSystem._activeOrders private dictionary could never be rehydrated, which was
// the double-consumption save wound. Mirror of ReservationLedger: Rows are the saved truth
// (insertion order = save order), the jobId index is DERIVED, never saved, rebuilt after load.
// CONSTRAINT (determinism): pure data + Try-pattern, no exceptions on the query path, no RNG.
// CONSTRAINT (funding invariant, §5.2): ProgressTicks == 0 <=> the current execution's inputs
// are NOT yet consumed; > 0 <=> consumed. Writers must keep input consumption and the first
// counter hit inside the same PerformWork step so a row is never ambiguous about its inputs.
namespace EmberCrpg.Domain.Process
{
    /// <summary>One in-flight recipe work order row — pure Domain data (RecipeWorkOrder is Simulation-side).</summary>
    public sealed class WorkOrderRecord
    {
        public ulong JobId;              // rebind key to the JobBoard claim; saved (the old DTO's missing field)
        public ulong RecipeId;
        public ulong SiteId;
        public int PositionX;            // bench cell
        public int PositionY;
        public ulong StartedByActorId;   // attribution only; a takeover does not rewrite it (§13.4)
        public int ProgressTicks;        // the counter — single writer is living.action_advance@PerTick:22 (W34 WORK slice)
        public int CompletedExecutions;  // batch progress (_completedExecutionCounts' new home)
    }

    /// <summary>Deterministic world-root store for in-flight work orders, keyed by job id.</summary>
    public sealed class WorkOrderLedger
    {
        public List<WorkOrderRecord> Rows = new List<WorkOrderRecord>();

        private readonly Dictionary<ulong, WorkOrderRecord> _rowByJob = new Dictionary<ulong, WorkOrderRecord>();

        /// <summary>Finds the order row bound to a job (decision/advance resume path). O(1).</summary>
        public bool TryGetByJob(ulong jobId, out WorkOrderRecord row)
        {
            return _rowByJob.TryGetValue(jobId, out row);
        }

        /// <summary>Adds a row. False (no mutation) on null, jobId 0, or a duplicate job binding — max 1 row per job.</summary>
        public bool Add(WorkOrderRecord row)
        {
            if (row == null || row.JobId == 0UL || _rowByJob.ContainsKey(row.JobId))
                return false;
            Rows.Add(row);
            _rowByJob[row.JobId] = row;
            return true;
        }

        /// <summary>Removes the row bound to a job. Missing job is an idempotent no-op, returns false.</summary>
        public bool Remove(ulong jobId)
        {
            if (!_rowByJob.Remove(jobId))
                return false;
            for (var i = 0; i < Rows.Count; i++)
            {
                if (Rows[i] != null && Rows[i].JobId == jobId)
                {
                    Rows.RemoveAt(i);
                    break;
                }
            }

            return true;
        }

        /// <summary>Rebuilds the derived job index from Rows — load / EnsureInvariants path.</summary>
        public void RebuildIndexes()
        {
            Rows ??= new List<WorkOrderRecord>();
            _rowByJob.Clear();
            foreach (var row in Rows)
            {
                if (row != null && row.JobId != 0UL)
                    _rowByJob[row.JobId] = row;
            }
        }
    }
}
