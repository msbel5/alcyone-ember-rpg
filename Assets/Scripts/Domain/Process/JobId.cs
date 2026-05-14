using System;

// Design note:
// JobId is the smallest stable PROCESS-box job handle for Faz 3. It is a pure
// Domain value with no allocation, lookup, ticking, logging, serialization, or
// Unity dependency. Zero is reserved as the empty sentinel so JobBoard and
// JobAssignmentSystem can reject missing jobs deterministically in later atoms.
// Atom-map ref: DOCS/sprint-faz-3-atom-map.md Pure job definition rail.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Stable handle to a job request. Value type; default value means no job.
    /// </summary>
    public readonly struct JobId : IEquatable<JobId>
    {
        private readonly ulong _value;

        /// <summary>
        /// Creates a job handle from its raw stable identifier.
        /// </summary>
        public JobId(ulong value)
        {
            _value = value;
        }

        /// <summary>
        /// Raw stable identifier carried by this job handle.
        /// </summary>
        public ulong Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True when this handle is the empty no-job sentinel.
        /// </summary>
        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        /// <summary>
        /// Returns true when both job handles carry the same raw identifier.
        /// </summary>
        public bool Equals(JobId other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Returns true when the object is a job handle with the same raw identifier.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is JobId other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code derived only from the raw stable identifier.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a compact debug label for this job handle.
        /// </summary>
        public override string ToString()
        {
            return IsEmpty ? "JobId.Empty" : $"JobId({_value})";
        }

        /// <summary>
        /// Returns true when both job handles carry the same raw identifier.
        /// </summary>
        public static bool operator ==(JobId left, JobId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true when job handles carry different raw identifiers.
        /// </summary>
        public static bool operator !=(JobId left, JobId right)
        {
            return !left.Equals(right);
        }
    }
}
