using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>
    /// F4-DoD: save → load → SAME WORLD, byte-identical digest. The seeded world advances a full game day
    /// (jobs, growth, prices, needs all moved), round-trips through the save mapper, and must digest
    /// identically — any store the mapper drops or distorts fails loudly here.
    /// </summary>
    public sealed class SaveLoadDigestRoundtripTests
    {
        private static readonly SiteId Site = new SiteId(77UL);
        private static readonly GridPosition ForgeCell = new GridPosition(4, 5);
        private static readonly GridPosition FarmCell = new GridPosition(2, 2);
        private static readonly JobId Job = new JobId(701UL);
        private static readonly WorldComponentId PlantId = new WorldComponentId(90UL);
        private static readonly WorldComponentId SoilId = new WorldComponentId(10UL);
        private static readonly ActorId Worker = new ActorId(1UL);

        [Test]
        public void SaveThenLoad_PreservesWorldDigest()
        {
            var world = BuildSeededWorld();
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            for (var tick = 1; tick <= WorldTickComposer.TicksPerGameDay; tick++)
                composer.Advance(world, tick);

            var before = WorldStateDigest.Compute(world);

            var data = WorldSaveMapper.ToData(world);
            var loaded = WorldSaveMapper.ToWorld(data, BuildSeededWorld());
            var after = WorldStateDigest.Compute(loaded);

            Assert.That(after, Is.EqualTo(before),
                "save→load must reproduce the world byte-identically (a dropped/distorted store fails here)");
        }

        private static WorldState BuildSeededWorld()
        {
            var world = new WorldState();
            world.Time = new GameTime(8 * GameTime.MinutesPerHour);

            world.Actors = new ActorStore();
            world.Actors.Add(Worker0());
            world.Actors.Add(PlayerActor());   // the save mapper anchors on the Player- and
            world.Actors.Add(MerchantActor()); // Merchant-role records (authored slice always has both)
            world.Actors.Add(SimpleActor(4UL, "Watch Bren", ActorRole.Guard));
            world.Actors.Add(SimpleActor(5UL, "Gnasher", ActorRole.Enemy)); // the mapper expects the full fixed cast

            world.Worksites.Add(new WorksiteRecord(Site, ForgeCell, WorksiteKind.Furnace, isActive: true));
            world.Jobs.Add(new JobRequest(
                Job,
                new RecipeId(1001UL),
                Site,
                ForgeCell,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Worker));

            world.Plants.Add(PlantId, new PlantComponent(PlantId, Site, FarmCell, "wheat", new PlantStageId("seed"), 0));
            world.Soils.Add(SoilId, new SoilComponent(SoilId, Site, FarmCell, fertility: 70, moisture: 60, plantId: PlantId));

            var stockpile = new StockpileComponent(Site);
            stockpile.Add("iron", 2);
            world.Stockpiles.Add(stockpile);
            world.Prices.SetPrice(Site, "iron", 10);

            return world;
        }

        private static ActorRecord PlayerActor()
        {
            return new ActorRecord(
                new ActorId(2UL),
                "Vael",
                ActorRole.Player,
                new EmberStatBlock(50, 50, 50, 50, 50, 50),
                new ActorVitals(
                    new VitalStat(40, 40),
                    new VitalStat(40, 40),
                    new VitalStat(25, 25)),
                new GridPosition(1, 1),
                accuracy: 50,
                dodge: 30,
                armor: 4,
                baseDamage: 6);
        }

        private static ActorRecord SimpleActor(ulong id, string name, ActorRole role)
        {
            return new ActorRecord(
                new ActorId(id),
                name,
                role,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(30, 30),
                    new VitalStat(15, 15)),
                new GridPosition(3, (int)id),
                accuracy: 40,
                dodge: 30,
                armor: 3,
                baseDamage: 4);
        }

        private static ActorRecord MerchantActor()
        {
            return new ActorRecord(
                new ActorId(3UL),
                "Trader Mira",
                ActorRole.Merchant,
                new EmberStatBlock(45, 45, 45, 45, 45, 45),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(30, 30),
                    new VitalStat(20, 20)),
                new GridPosition(2, 1),
                accuracy: 35,
                dodge: 30,
                armor: 2,
                baseDamage: 3);
        }

        private static ActorRecord Worker0()
        {
            var actor = new ActorRecord(
                Worker,
                "Smith Ada",
                ActorRole.Talker,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(30, 30),
                    new VitalStat(20, 20)),
                new GridPosition(0, 0),
                accuracy: 40,
                dodge: 30,
                armor: 4,
                baseDamage: 4);
            actor.ApplyJobPreferences(new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });
            return actor;
        }
    }
}
