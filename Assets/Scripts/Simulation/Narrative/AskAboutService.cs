using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;

// Design note:
// AskAboutService provides Sprint 1's deterministic talk-NPC topic replies.
// Inputs: world state and selected topic id.
// Outputs: grounded topic text plus remembered topic state on the talker actor and NPC memory.
// Bible reference: ARCHITECTURE.md DM query surface + ActorMemory, PRD FR-04/FR-07, Sprint 3 memory slice.
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

            var firstTime = !world.Talker.AskedTopicIds.Contains(topicId);
            world.Talker.RecordTopic(topicId);
            var memory = world.NpcMemories == null ? null : world.NpcMemories.GetOrCreate(world.Talker.Id);
            if (memory != null)
            {
                memory.Remember(new ActorMemoryEvent(
                    world.Time,
                    ActorMemoryEventType.DialogueTopic,
                    world.Player.Id,
                    ItemId.Empty,
                    0,
                    topicId,
                    "Sage Nera discussed topic '" + topicId + "'."));
            }

            return firstTime
                ? $"Sage Nera says: {topic.Answer}"
                : $"Sage Nera repeats: {topic.Answer}";
        }
    }
}
