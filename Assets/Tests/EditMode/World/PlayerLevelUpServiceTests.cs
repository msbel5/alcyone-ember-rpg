using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    public sealed class PlayerLevelUpServiceTests
    {
        [Test]
        public void TryApply_RaisesLevel_UpdatesStats_AndLearnsSpell()
        {
            var world = new WorldFactory().Create(77);
            var player = world.Actors.FirstByRole(ActorRole.Player);

            var success = new PlayerLevelUpService().TryApply(
                world,
                new PlayerLevelUpChoice(2, 1, 1, 0, 1, 0, WorldSpellCatalog.EmberWardTemplateId),
                out var message);

            Assert.That(success, Is.True);
            Assert.That(message, Does.Contain("Level 2"));
            Assert.That(world.PlayerLevel, Is.EqualTo(2));
            Assert.That(world.PlayerKnownSpellIds, Does.Contain(WorldSpellCatalog.EmberWardTemplateId));
            var updated = world.Actors.FirstByRole(ActorRole.Player);
            Assert.That(updated.Stats.Mig, Is.EqualTo(player.Stats.Mig + 2));
            Assert.That(updated.Stats.Agi, Is.EqualTo(player.Stats.Agi + 1));
            Assert.That(updated.Stats.End, Is.EqualTo(player.Stats.End + 1));
            Assert.That(updated.Stats.Ins, Is.EqualTo(player.Stats.Ins + 1));
            Assert.That(world.Events.Events.Last().Kind, Is.EqualTo(EmberCrpg.Domain.World.WorldEventKind.StorytellerCheckpoint));
        }

        [Test]
        public void TryApply_RejectsWrongPointBudget()
        {
            var world = new WorldFactory().Create(78);

            var success = new PlayerLevelUpService().TryApply(
                world,
                new PlayerLevelUpChoice(1, 1, 0, 0, 0, 0, WorldSpellCatalog.EmberWardTemplateId),
                out var message);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("exactly 5 points"));
            Assert.That(world.PlayerLevel, Is.EqualTo(1));
            Assert.That(world.PlayerKnownSpellIds, Does.Not.Contain(WorldSpellCatalog.EmberWardTemplateId));
        }
    }
}
