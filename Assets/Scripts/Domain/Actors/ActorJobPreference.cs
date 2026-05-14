using System;
using EmberCrpg.Domain.Process;

// Design note:
// ActorJobPreference is Faz 3's first actor-local LIVING row for job assignment.
// It records a concrete JobKind plus an explicit priority, but it does not read
// JobBoard state, select jobs, move actors, start recipes, or emit EventLog
// lines. Those behaviours belong to the later JobAssignmentSystem atoms.
// Atom-map ref: DOCS/sprint-faz-3-atom-map.md Actor job preference and schedule rail.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>
    /// Immutable actor-local preference row used to match actors to job kinds.
    /// </summary>
    public readonly struct ActorJobPreference : IEquatable<ActorJobPreference>
    {
        public ActorJobPreference(JobKind kind, JobPriority priority)
        {
            if (kind == JobKind.None)
                throw new ArgumentException("ActorJobPreference requires a concrete job kind.", nameof(kind));

            Kind = kind;
            Priority = priority;
        }

        /// <summary>Job category this actor can be considered for.</summary>
        public JobKind Kind { get; }

        /// <summary>Actor-local priority; disabled means the actor opts out.</summary>
        public JobPriority Priority { get; }

        /// <summary>True when this row participates in assignment.</summary>
        public bool IsEnabled => Priority.IsActive;

        /// <summary>Creates an explicit opt-out row for a concrete job kind.</summary>
        public static ActorJobPreference Disabled(JobKind kind)
        {
            return new ActorJobPreference(kind, JobPriority.Disabled);
        }

        /// <summary>Returns true when both preference rows carry the same kind and priority.</summary>
        public bool Equals(ActorJobPreference other)
        {
            return Kind == other.Kind && Priority == other.Priority;
        }

        /// <summary>Returns true when the object is a preference row with the same kind and priority.</summary>
        public override bool Equals(object obj)
        {
            return obj is ActorJobPreference other && Equals(other);
        }

        /// <summary>Returns a hash code derived from the job kind and priority only.</summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Priority);
        }

        /// <summary>Returns a compact debug label for this preference row.</summary>
        public override string ToString()
        {
            return IsEnabled ? $"ActorJobPreference({Kind}, {Priority.Value})" : $"ActorJobPreference({Kind}, disabled)";
        }

        /// <summary>Returns true when preference rows carry the same kind and priority.</summary>
        public static bool operator ==(ActorJobPreference left, ActorJobPreference right)
        {
            return left.Equals(right);
        }

        /// <summary>Returns true when preference rows carry different kind or priority values.</summary>
        public static bool operator !=(ActorJobPreference left, ActorJobPreference right)
        {
            return !left.Equals(right);
        }
    }
}
