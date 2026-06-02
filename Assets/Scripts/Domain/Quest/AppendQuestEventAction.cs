using System;
using EmberCrpg.Domain.World;

// Design note:
// AppendQuestEventAction is the Command that records deterministic quest-related world events.
// Pattern: Command.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Command that appends a quest-related world event with a deterministic reason label.</summary>
    public sealed class AppendQuestEventAction : IQuestAction
    {
        public AppendQuestEventAction(WorldEventKind kind, string reason)
        {
            if (kind == WorldEventKind.None)
                throw new ArgumentException("Quest event action cannot append the empty event sentinel.", nameof(kind));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Quest event reason is required.", nameof(reason));

            Kind = kind;
            Reason = reason.Trim();
        }

        /// <summary>Deterministic world event kind appended by this command.</summary>
        public WorldEventKind Kind { get; }
        /// <summary>Deterministic reason label appended with this world event.</summary>
        public string Reason { get; }

        /// <summary>Appends the configured quest world event through the mutation context.</summary>
        public void Apply(QuestMutationContext context)
        {
            context.AppendEvent(Kind, Reason);
        }
    }
}
