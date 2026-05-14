using System;

// Design note:
// JobPriority is actor-local LIVING/PROCESS preference data for Faz 3. Lower
// numeric values win deterministically; the disabled sentinel is explicit so an
// actor can opt out of a JobKind without inventing hidden magic numbers. This
// type does not select jobs, mutate actors, or read JobBoard state.
// Atom-map ref: DOCS/sprint-faz-3-atom-map.md Pure job definition rail.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Validated actor job priority. Active priorities are positive and lower values win.
    /// </summary>
    public readonly struct JobPriority : IEquatable<JobPriority>, IComparable<JobPriority>
    {
        private readonly int _value;

        private JobPriority(int value)
        {
            _value = value;
        }

        /// <summary>
        /// Explicit disabled sentinel: the actor does not want this job kind.
        /// </summary>
        public static JobPriority Disabled
        {
            get { return default; }
        }

        /// <summary>
        /// Creates an active priority. Lower positive numbers win.
        /// </summary>
        public static JobPriority Active(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Active job priority must be positive.");

            return new JobPriority(value);
        }

        /// <summary>
        /// Raw priority value. Zero means disabled; positive values are active.
        /// </summary>
        public int Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True when this priority participates in job assignment.
        /// </summary>
        public bool IsActive
        {
            get { return _value > 0; }
        }

        /// <summary>
        /// Returns true when both priorities carry the same raw value.
        /// </summary>
        public bool Equals(JobPriority other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Returns true when the object is a job priority with the same raw value.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is JobPriority other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code derived only from the raw priority value.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Compares two priorities for deterministic assignment ordering. Active priorities
        /// sort before disabled priorities; among active priorities, lower number wins.
        /// </summary>
        public int CompareTo(JobPriority other)
        {
            if (IsActive && !other.IsActive)
                return -1;
            if (!IsActive && other.IsActive)
                return 1;
            if (!IsActive && !other.IsActive)
                return 0;

            return _value.CompareTo(other._value);
        }

        /// <summary>
        /// Returns a compact debug label for this priority.
        /// </summary>
        public override string ToString()
        {
            return IsActive ? $"JobPriority({_value})" : "JobPriority.Disabled";
        }

        /// <summary>Returns true when priorities carry the same raw value.</summary>
        public static bool operator ==(JobPriority left, JobPriority right)
        {
            return left.Equals(right);
        }

        /// <summary>Returns true when priorities carry different raw values.</summary>
        public static bool operator !=(JobPriority left, JobPriority right)
        {
            return !left.Equals(right);
        }
    }
}
