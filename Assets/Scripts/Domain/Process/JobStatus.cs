using System;

// Design note:
// JobStatus is a stable-string lifecycle value object for jobs on the JobBoard.
// Lifecycle codes are stable across save/load, tests, and visual snapshots so
// new lifecycle states (or refined block reasons) ship as data, not enum branches.
// Closes CO-05 in DOCS/sprint-faz-4-atom-map.md Debt ledger.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Stable-string lifecycle status for a job. The code is a normalized string
    /// so new states can ship without enum branching. Equality is by code only.
    /// </summary>
    public readonly struct JobStatus : IEquatable<JobStatus>
    {
        private const string BlockedPrefix = "blocked";
        private const string BlockedSeparator = ":";

        private readonly string _code;

        private JobStatus(string code)
        {
            _code = code;
        }

        /// <summary>Job exists but no actor has claimed it.</summary>
        public static JobStatus Pending { get; } = new JobStatus("pending");

        /// <summary>Job has an actor assignment but pathing has not started yet.</summary>
        public static JobStatus Assigned { get; } = new JobStatus("assigned");

        /// <summary>Assigned actor is moving toward the target worksite.</summary>
        public static JobStatus Traveling { get; } = new JobStatus("traveling");

        /// <summary>Actor reached the worksite queue but cannot start yet.</summary>
        public static JobStatus Queued { get; } = new JobStatus("queued");

        /// <summary>Recipe work is currently progressing at the worksite.</summary>
        public static JobStatus Active { get; } = new JobStatus("active");

        /// <summary>Job completed all requested units. Terminal.</summary>
        public static JobStatus Completed { get; } = new JobStatus("completed");

        /// <summary>Job was intentionally removed before completion. Terminal.</summary>
        public static JobStatus Canceled { get; } = new JobStatus("canceled");

        /// <summary>Stable normalized code, e.g. "pending", "active", "blocked:too_hungry".</summary>
        public string Code => _code ?? Pending.Code;

        /// <summary>True when this status should leave the active board lifecycle.</summary>
        public bool IsTerminal => Code == Completed.Code || Code == Canceled.Code;

        /// <summary>
        /// Creates a blocked status with a stable, normalized reason suffix
        /// (trimmed + lower-case). An empty or whitespace reason returns the
        /// bare "blocked" sentinel so log search stays stable.
        /// </summary>
        public static JobStatus Blocked(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return new JobStatus(BlockedPrefix);

            var normalized = reason.Trim().ToLowerInvariant();
            return new JobStatus(BlockedPrefix + BlockedSeparator + normalized);
        }

        /// <summary>Returns true when both statuses carry the same stable code.</summary>
        public bool Equals(JobStatus other) => Code == other.Code;

        public override bool Equals(object obj) => obj is JobStatus other && Equals(other);

        public override int GetHashCode() => Code.GetHashCode();

        public override string ToString() => Code;

        public static bool operator ==(JobStatus left, JobStatus right) => left.Equals(right);

        public static bool operator !=(JobStatus left, JobStatus right) => !left.Equals(right);
    }
}
