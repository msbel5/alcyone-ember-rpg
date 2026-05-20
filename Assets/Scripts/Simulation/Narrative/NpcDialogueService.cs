using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Memory;

namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>
    /// Builds a deterministic typed DialogueResponse for an ask-about call:
    /// pulls from memory + faction relation + mood, applies optional gating
    /// predicate, falls back to the topic's default answer template.
    /// Faz 9 Atom 8.
    /// </summary>
    public sealed class NpcDialogueService
    {
        private readonly MemoryRecallService _recall;
        private readonly DialogueTemplateRegistry _templates;

        public NpcDialogueService(MemoryRecallService recall, DialogueTemplateRegistry templates)
        {
            _recall = recall ?? throw new ArgumentNullException(nameof(recall));
            _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        }

        public DialogueResponse Ask(
            ActorRecord asker,
            ActorRecord askee,
            MemoryComponent askeeMemory,
            TopicDef topic,
            FactionId askerFaction,
            FactionId askeeFaction,
            FactionStore factions,
            GameTime now,
            GameTime memoryHorizon)
        {
            if (asker == null) throw new ArgumentNullException(nameof(asker));
            if (askee == null) throw new ArgumentNullException(nameof(askee));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            if (factions != null && !askerFaction.IsEmpty && !askeeFaction.IsEmpty)
            {
                var reputation = factions.GetReputation(askerFaction, askeeFaction);
                if (reputation.ToRelationKind().Equals(FactionRelationKind.War))
                    return DialogueResponse.Refused("hostility");
            }

            if (askee.Mood.Value <= 20)
                return DialogueResponse.Refused("mood_too_low");

            var substitutions = new Dictionary<string, string>
            {
                { "topic", topic.Id.Code },
                { "asker", asker.Name ?? string.Empty },
                { "askee", askee.Name ?? string.Empty },
            };

            if (askeeMemory != null && _recall.HasRecentFact(askeeMemory, topic.Id, memoryHorizon))
            {
                var template = _templates.Get(topic.DefaultAnswerTemplateId + "_remembered")
                    ?? _templates.Get(topic.DefaultAnswerTemplateId);
                return DialogueResponse.Spoken(template?.Render(substitutions) ?? topic.PromptPhrasing);
            }

            var fallback = _templates.Get(topic.DefaultAnswerTemplateId);
            return DialogueResponse.Spoken(fallback?.Render(substitutions) ?? topic.PromptPhrasing);
        }
    }

    /// <summary>Immutable response from NpcDialogueService.</summary>
    public sealed class DialogueResponse
    {
        private DialogueResponse(string text, string refusalReason)
        {
            Text = text ?? string.Empty;
            RefusalReason = refusalReason ?? string.Empty;
        }

        public string Text { get; }
        public string RefusalReason { get; }
        public bool Refused => !string.IsNullOrEmpty(RefusalReason);

        public static DialogueResponse Spoken(string text) => new DialogueResponse(text, null);
        public static DialogueResponse Refused(string reason) => new DialogueResponse(null, reason);
    }
}
