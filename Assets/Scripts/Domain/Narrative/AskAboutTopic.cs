using System;

// Design note:
// AskAboutTopic stores one deterministic talk topic and reply used by Sprint 1's dialogue shell.
// Inputs: stable topic id, player-facing label, and grounded answer text.
// Outputs: pure topic records for tests, UI, and save/load.
// Bible reference: ARCHITECTURE.md DM query surface, PRD FR-04/FR-07.
namespace EmberCrpg.Domain.Narrative
{
    /// <summary>Deterministic Ask About topic for the talker NPC.</summary>
    public sealed class AskAboutTopic
    {
        public AskAboutTopic(string id, string label, string answer)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Topic id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Topic label is required.", nameof(label));
            if (string.IsNullOrWhiteSpace(answer))
                throw new ArgumentException("Topic answer is required.", nameof(answer));

            Id = id;
            Label = label;
            Answer = answer;
        }

        public string Id { get; }
        public string Label { get; }
        public string Answer { get; }
    }
}
