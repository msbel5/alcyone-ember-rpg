using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>M6 pin: harvest requires HANDS - the field waits when nobody is near.</summary>
    public sealed class HarvestHandsServiceTests
    {
        private static ActorRecord Actor(ulong id, ActorRole role, GridPosition position)
        {
            return new ActorRecord(
                new ActorId(id), "A" + id, role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                position, accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
        }

        private static PlantComponent Wheat(GridPosition at)
            => new PlantComponent(new WorldComponentId(1), new SiteId(1), at, "wheat", new PlantStageId("ripe"), 0);

        [Test]
        public void FindHarvester_AdjacentVillager_IsTheHands()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Actors.Add(Actor(11, ActorRole.Talker, new GridPosition(8, 5)));
            Assert.That(HarvestHandsService.FindHarvester(world, Wheat(new GridPosition(8, 4)))?.Id.Value,
                Is.EqualTo(11UL));
        }

        [Test]
        public void FindHarvester_NobodyWithinReach_ReturnsNull_ThePlotWaits()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Actors.Add(Actor(11, ActorRole.Talker, new GridPosition(12, 4)));
            Assert.That(HarvestHandsService.FindHarvester(world, Wheat(new GridPosition(8, 4))), Is.Null,
                "three cells away is out of reach - the field stays ripe");
        }

        [Test]
        public void FindHarvester_EnemiesNeverHarvest_AndNearestWins()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Actors.Add(Actor(1, ActorRole.Enemy, new GridPosition(8, 4)));
            world.Actors.Add(Actor(2, ActorRole.Talker, new GridPosition(10, 4)));
            world.Actors.Add(Actor(3, ActorRole.Talker, new GridPosition(8, 3)));
            Assert.That(HarvestHandsService.FindHarvester(world, Wheat(new GridPosition(8, 4)))?.Id.Value,
                Is.EqualTo(3UL), "a wolf on the plot does not harvest; the closest villager does");
        }
    }
}
