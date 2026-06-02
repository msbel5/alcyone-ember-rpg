using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    public sealed class WorldTickDigestGoldenTests
    {
        // Re-baselined on 2026-06-03 for coherent time: 60/1440 tick cadence and per-tick schedule movement.
        private const string BaselineHash = "3c63d493c1ceda5d58e140ebbb199b616e788d54c943f16285dee33094bfa2b8";
        private static int OneGameDayTicks => WorldTickComposer.TicksPerGameDay;
        private static int TwoGameDaysTicks => 2 * WorldTickComposer.TicksPerGameDay;

        private static readonly SiteId Site = new SiteId(77UL);
        private static readonly GridPosition ForgeCell = new GridPosition(4, 5);
        private static readonly GridPosition FarmCell = new GridPosition(2, 2);
        private static readonly JobId Job = new JobId(701UL);
        private static readonly WorldComponentId PlantId = new WorldComponentId(90UL);
        private static readonly WorldComponentId SoilId = new WorldComponentId(10UL);
        private static readonly ActorId Worker = new ActorId(1UL);

        [Test]
        public void Advance_OverTwoGameDays_MatchesCommittedBaselineDigest()
        {
            var digest = DigestAfterTicks(TwoGameDaysTicks);
            var secondDigest = DigestAfterTicks(TwoGameDaysTicks);
            Assert.That(
                secondDigest,
                Is.EqualTo(digest),
                "World digest baseline must be byte-identical across repeated same-seed advances.");
            Assert.That(
                digest,
                Is.EqualTo(BaselineHash),
                "World digest baseline drifted. Captured digest: " + digest + " second digest: " + secondDigest);
        }

        [Test]
        public void Advance_SameSeedSameTicks_ProducesSameDigest()
        {
            var first = DigestAfterTicks(TwoGameDaysTicks);
            var second = DigestAfterTicks(TwoGameDaysTicks);
            Assert.That(second, Is.EqualTo(first), "same seed + same ticks must be deterministic");
        }

        [Test]
        public void Advance_DifferentTickHorizons_ProduceDifferentDigests()
        {
            var oneDay = DigestAfterTicks(OneGameDayTicks);
            var twoDays = DigestAfterTicks(TwoGameDaysTicks);
            Assert.That(twoDays, Is.Not.EqualTo(oneDay), "digest should move when simulation horizon changes");
        }

        private static string DigestAfterTicks(int ticks)
        {
            var world = BuildSeededWorld();
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            for (var tick = 1; tick <= ticks; tick++)
                composer.Advance(world, tick);

            return WorldStateDigest.Compute(world);
        }

        // Keep this seed aligned with WorldLivesOverNTicksTests.BuildSeededWorld().
        private static WorldState BuildSeededWorld()
        {
            var world = new WorldState();
            world.Time = new GameTime(8 * GameTime.MinutesPerHour);

            world.Actors = new ActorStore();
            world.Actors.Add(Worker0());

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
