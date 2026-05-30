using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// Design note:
// ReasonTrace is the Phase 1 PROCESS-box causal-chain record attached to a
// WorldEvent: an ordered, root-first, immutable sequence of cause labels that
// records why a downstream event happened.
// Inputs: enumerable of cause labels (defensive copy at construction); each
// label must be non-blank.
// Outputs: immutable record consumed by WorldEvent / WorldEventLog in follow-up
// Phase 1 PRs; no Unity, no I/O, no serialization concerns. Mirrors the
// FactionRecord / SiteRecord defensive-constructor pattern so invariants are
// pinned at construction.
// Atom-map ref: docs/sprint-phase-1-atom-map.md WorldEvent log + ReasonTrace sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>Pure record describing a causal chain as an ordered sequence of cause labels.</summary>
    public sealed class ReasonTrace
    {
        private readonly string[] _causes;
        // ReadOnlyCollection wrapper prevents callers from casting Causes back to
        // string[] and mutating the internal cause chain — IReadOnlyList<string>
        // alone does not.
        private readonly ReadOnlyCollection<string> _causesView;

        public ReasonTrace(IEnumerable<string> causes)
        {
            if (causes == null)
                throw new ArgumentNullException(nameof(causes));

            var buffer = new List<string>();
            foreach (var cause in causes)
            {
                if (string.IsNullOrWhiteSpace(cause))
                    throw new ArgumentException("ReasonTrace causes cannot be blank or whitespace.", nameof(causes));
                buffer.Add(cause);
            }

            if (buffer.Count == 0)
                throw new ArgumentException("ReasonTrace requires at least one cause.", nameof(causes));

            _causes = buffer.ToArray();
            _causesView = new ReadOnlyCollection<string>(_causes);
        }

        /// <summary>Root-first snapshot of the causes supplied at construction.</summary>
        public IReadOnlyList<string> Causes
        {
            get { return _causesView; }
        }

        /// <summary>Number of causes in the chain (always at least one).</summary>
        public int Depth
        {
            get { return _causes.Length; }
        }

        /// <summary>The root cause — the first entry in the chain.</summary>
        public string RootCause
        {
            get { return _causes[0]; }
        }

        /// <summary>The most recent cause — the last entry in the chain.</summary>
        public string LeafCause
        {
            get { return _causes[_causes.Length - 1]; }
        }

        /// <summary>True when the supplied cause label appears in the chain (case-sensitive match).</summary>
        public bool HasCause(string cause)
        {
            if (string.IsNullOrWhiteSpace(cause))
                return false;

            for (int i = 0; i < _causes.Length; i++)
            {
                if (string.Equals(_causes[i], cause, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
