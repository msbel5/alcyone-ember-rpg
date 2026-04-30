using System.Linq;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin Sprint 3's deterministic persistent NPC memory surface.
// They verify mechanical memory facts only; narrative flavor remains outside the memory module.
namespace EmberCrpg.Tests.EditMode.Narrative
{
    /// <summary>Verifies NPC memory records and changes deterministic interaction output.</summary>
    public sealed class PersistentNpcMemoryTests
    {
        [Test]
        public void AskAbout_FirstInteraction_RecordsDialogueMemory()
        {
            var world = new SliceWorldFactory().Create(1337);

            new AskAboutService().Ask(world, "embers");

            Assert.That(world.NpcMemory.TryGet(world.Talker.Id, out var memory), Is.True);
            Assert.That(memory.HasDialogueSeen("embers"), Is.True);
            Assert.That(memory.CountEvents(ActorMemoryEventTypes.DialogueTopic), Is.EqualTo(1));
            var interactionEvent = memory.Events.Single();
            Assert.That(interactionEvent.ActorSeen, Is.EqualTo(world.Player.Id));
            Assert.That(interactionEvent.SubjectId, Is.EqualTo("embers"));
            Assert.That(interactionEvent.Location, Is.EqualTo(world.Talker.Position));
        }

        [Test]
        public void AskAbout_RepeatedInteraction_ChangesOutputFromPersistentMemory()
        {
            var world = new SliceWorldFactory().Create(1337);
            var service = new AskAboutService();

            var firstReply = service.Ask(world, "embers");
            var secondReply = service.Ask(world, "embers");

            Assert.That(firstReply, Does.Contain("says"));
            Assert.That(secondReply, Does.Contain("repeats"));
            Assert.That(secondReply, Is.Not.EqualTo(firstReply));
            Assert.That(world.NpcMemory.TryGet(world.Talker.Id, out var memory), Is.True);
            Assert.That(memory.CountEvents(ActorMemoryEventTypes.DialogueTopic), Is.EqualTo(2));
        }
    }
}
