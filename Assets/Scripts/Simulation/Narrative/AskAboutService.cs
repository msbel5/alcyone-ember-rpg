using System.Linq;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;

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
        public string Ask(SliceWorldState world, string topicId)
        {
            var topic = world.Topics.FirstOrDefault(candidate => candidate.Id == topicId);
            if (topic == null)
                return "Sage Nera tilts her head. That topic is still a blank.";

            var memory = world.NpcMemory.GetOrCreate(world.Talker.Id);
            var firstTime = !memory.HasDialogueSeen(topicId) && !world.Talker.AskedTopicIds.Contains(topicId);
            world.Talker.RecordTopic(topicId);
            memory.MarkDialogueSeen(topicId);
            memory.RecordEvent(new InteractionEvent(
                world.Time,
                ActorMemoryEventTypes.DialogueTopic,
                world.Player.Id,
                topicId,
                string.Empty,
                0,
                world.Talker.Position));

            return firstTime
                ? $"Sage Nera says: {topic.Answer}"
                : $"Sage Nera repeats: {topic.Answer}";
        }
    }
}
