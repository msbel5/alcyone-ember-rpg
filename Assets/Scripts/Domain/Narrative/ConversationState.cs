using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Domain.Narrative
{
    /// <summary>
    /// EMB-020: the single model for an in-progress conversation. Before this, dialogue state was
    /// scattered across the adapter's loose <c>_activeDialogActor</c> / <c>_currentPortrait</c> fields
    /// and three half-built service shells (AskAbout/AskDm/NpcDialogue). ConversationState binds the
    /// current speaker, their portrait, and the per-actor topic set (from <see cref="NpcTopicCatalog"/>)
    /// into one immutable snapshot the dialog UI reads. The live utterance line stays dynamic on the
    /// adapter (it changes as the LLM streams); this type owns the *who* and the *what can I ask*.
    /// Pure data — no Unity, deterministic.
    /// </summary>
    public sealed class ConversationState
    {
        public static readonly ConversationState None =
            new ConversationState(string.Empty, string.Empty, new List<AskAboutTopic>());

        public ConversationState(string actorName, string portrait, IReadOnlyList<AskAboutTopic> topics)
            : this(default, default, actorName, portrait, topics)
        {
        }

        public ConversationState(
            ActorId actorId,
            NpcId npcId,
            string actorName,
            string portrait,
            IReadOnlyList<AskAboutTopic> topics)
        {
            ActorId = actorId;
            NpcId = npcId;
            ActorName = actorName ?? string.Empty;
            Portrait = portrait ?? string.Empty;
            Topics = topics ?? new List<AskAboutTopic>();
        }

        public ActorId ActorId { get; }
        public NpcId NpcId { get; }
        public string ActorName { get; }
        public string Portrait { get; }
        public IReadOnlyList<AskAboutTopic> Topics { get; }

        public bool IsActive => !ActorId.IsEmpty || !NpcId.IsEmpty || !string.IsNullOrEmpty(ActorName);

        /// <summary>Look up a topic this actor actually offers, by stable id. Returns null when the id
        /// is not part of this conversation — callers must not answer for topics the actor never had.</summary>
        public AskAboutTopic FindTopic(string topicId)
        {
            if (string.IsNullOrEmpty(topicId)) return null;
            foreach (var t in Topics)
                if (t != null && string.Equals(t.Id, topicId, System.StringComparison.Ordinal))
                    return t;
            return null;
        }
    }
}
