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
        // Re-baselined on 2026-06-10: NeedsStep now emits ONE summary event per hourly crossing instead of
        // one per actor (the per-actor spam grew the unbounded event log to ~1GB by day 90 and the Gen2 GC
        // pauses were the felt per-tick stutter). Determinism itself is unchanged — the same-seed double
        // advance still produced byte-identical digests in the run that captured this value.
        // Re-baselined 2026-06-11 for v0.2 F7 HarvestStep (daily grow→harvest→price chain — ripe crops now
        // yield into stockpiles and replant; the shipcheck economy-chain FLAT finding drove it). Determinism
        // held: the same-seed double advance produced identical digests in the capturing run.
        private const string BaselineHash = "88be0d13a986d56dfc7c8e259e20e0cf63e2a412cf1da6405434e3106afb9b56";
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
