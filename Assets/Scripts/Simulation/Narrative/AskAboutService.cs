using System.Linq;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Actors;

// Design note:
// AskAboutService provides Sprint 1's deterministic talk-NPC topic replies.
// Inputs: world state and selected topic id.
// Outputs: grounded topic text plus remembered topic state on the talker actor.
// Bible reference: ARCHITECTURE.md DM query surface, PRD FR-04/FR-07.
namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>Rules-based Ask About shell for the vertical slice.</summary>
    public sealed class AskAboutService
    {
        private readonly NpcMemoryQueryService _memoryQueries = new NpcMemoryQueryService();

        public string Ask(SliceWorldState world, string topicId)
        {
            var topic = world.Topics.FirstOrDefault(candidate => candidate.Id == topicId);
            if (topic == null)
                return "Sage Nera tilts her head. That topic is still a blank.";

            var context = _memoryQueries.GetDialogueContext(world.NpcMemory, world.Actors.FirstByRole(ActorRole.Talker).Id, topicId);
            var memory = world.NpcMemory.GetOrCreate(world.Actors.FirstByRole(ActorRole.Talker).Id);
            world.Actors.FirstByRole(ActorRole.Talker).RecordTopic(topicId);
            memory.MarkDialogueSeen(topicId);
            memory.RecordEvent(new InteractionEvent(
                world.Time,
                ActorMemoryEventTypes.DialogueTopic,
                world.Actors.FirstByRole(ActorRole.Player).Id,
                topicId,
                string.Empty,
                0,
                world.Actors.FirstByRole(ActorRole.Talker).Position));

            switch (context.State)
            {
                case DialogueMemoryState.NewTopic:
                    return $"Sage Nera says: {topic.Answer}";
                case DialogueMemoryState.RememberedTopic:
                    return $"Sage Nera repeats: {topic.Answer}";
                default:
                    return $"Sage Nera traces the familiar answer again ({context.TopicAskCount + 1} tellings): {topic.Answer}";
            }
        }
    }
}
