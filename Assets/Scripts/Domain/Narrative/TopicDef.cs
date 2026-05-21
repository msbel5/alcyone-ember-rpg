using System;

namespace EmberCrpg.Domain.Narrative
{
    /// <summary>
    /// Immutable data row for one dialogue topic. Carries the stable id, the
    /// player-facing prompt phrasing, a gating predicate id (resolved by the
    /// dialogue service), and a default answer template id used when no
    /// memory-derived response applies. Faz 9 Atom 3 (refactor target of the
    /// Sprint 1 AskAboutTopic class per docs/kickoff-faz-9.md).
    /// </summary>
    public sealed class TopicDef : IEquatable<TopicDef>
    {
        public TopicDef(TopicId id, string promptPhrasing, string gatingPredicateId, string defaultAnswerTemplateId)
        {
            if (id.IsEmpty)
                throw new ArgumentException("TopicDef.Id must be non-empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(promptPhrasing))
                throw new ArgumentException("TopicDef.PromptPhrasing must be non-blank.", nameof(promptPhrasing));
            if (string.IsNullOrWhiteSpace(defaultAnswerTemplateId))
                throw new ArgumentException("TopicDef.DefaultAnswerTemplateId must be non-blank.", nameof(defaultAnswerTemplateId));

            Id = id;
            PromptPhrasing = promptPhrasing;
            GatingPredicateId = string.IsNullOrWhiteSpace(gatingPredicateId) ? string.Empty : gatingPredicateId.Trim();
            DefaultAnswerTemplateId = defaultAnswerTemplateId.Trim();
        }

        public TopicId Id { get; }
        public string PromptPhrasing { get; }

        /// <summary>
        /// Optional gating predicate id. Empty string means the topic is always
        /// available; otherwise the dialogue service resolves the predicate
        /// before surfacing this topic to the player.
        /// </summary>
        public string GatingPredicateId { get; }

        /// <summary>Template id used when no memory-derived answer applies.</summary>
        public string DefaultAnswerTemplateId { get; }

        public bool HasGate => !string.IsNullOrEmpty(GatingPredicateId);

        public bool Equals(TopicDef other)
        {
            if (other == null) return false;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj) => Equals(obj as TopicDef);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => $"TopicDef({Id})";
    }
}
