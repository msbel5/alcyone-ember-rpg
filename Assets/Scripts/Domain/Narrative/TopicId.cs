using System;

namespace EmberCrpg.Domain.Narrative
{
    /// <summary>
    /// Stable string identifier for a dialogue topic. New topics ship as data
    /// rows; equality is by normalized code only. Phase 9 Atom 2 (refactor of the
    /// Sprint 1 AskAboutTopic id field (Sprint 1 narrative).
    /// </summary>
    public readonly struct TopicId : IEquatable<TopicId>
    {
        private readonly string _code;

        public TopicId(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("TopicId code must be non-blank.", nameof(code));
            _code = code.Trim().ToLowerInvariant();
        }

        public static TopicId Empty { get; } = default;

        public string Code => _code ?? string.Empty;

        public bool IsEmpty => string.IsNullOrEmpty(_code);

        public bool Equals(TopicId other) => Code == other.Code;
        public override bool Equals(object obj) => obj is TopicId other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(TopicId a, TopicId b) => a.Equals(b);
        public static bool operator !=(TopicId a, TopicId b) => !a.Equals(b);
    }
}
