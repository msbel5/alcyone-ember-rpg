using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>
    /// SOUL-01/02 acceptance gate: the deterministic world must visibly ADVANCE over game-time when
    /// ticked through the real <see cref="WorldTickComposer"/> — crops grow, a pending job is claimed
    /// and its actor leaves idle, and a price drifts with stock. This is the headless proof that the
    /// production-economy systems are wired to the world root and ticked (not a screenshot). Same seed
    /// -> same asserts.
    /// </summary>
    public sealed class WorldLivesOverNTicksTests
    {
        // 1 tick == 1 game-minute; 240 ticks == 1 game-day. Two game-days = 480 ticks.
        private const int TwoGameDaysTicks = 2 * WorldTickComposer.TicksPerGameDay;

        private static readonly SiteId Site = new SiteId(77UL);
        private static readonly GridPosition ForgeCell = new GridPosition(4, 5);
        private static readonly GridPosition FarmCell = new GridPosition(2, 2);
        private static readonly JobId Job = new JobId(701UL);
        private static readonly WorldComponentId PlantId = new WorldComponentId(90UL);
        private static readonly WorldComponentId SoilId = new WorldComponentId(10UL);
        private static readonly ActorId Worker = new ActorId(1UL);

        [Test]
        public void Advance_OverTwoGameDays_GrowsCrops_ClaimsJob_AndDriftsPrice()
        {
            var world = BuildSeededWorld();
            var startStage = world.Plants.Get(PlantId).StageId;
            var startIronPrice = world.Prices.GetPrice(Site, "iron");
            Assert.That(startStage, Is.EqualTo(new PlantStageId("seed")), "precondition: crop starts at seed");
            Assert.That(startIronPrice, Is.EqualTo(10), "precondition: iron has a listed price to drift");
            Assert.That(world.Actors.Get(Worker).ScheduleState.IsIdle, Is.True, "precondition: worker starts idle");

            var composer = new WorldTickComposer();
            composer.Advance(world, 0); // anchor at world creation
            for (var tick = 1; tick <= TwoGameDaysTicks; tick++)
                composer.Advance(world, tick);

            // (a) a PlantComponent growth stage advanced (seed -> sprout -> ...).
            var grownStage = world.Plants.Get(PlantId).StageId;
            Assert.That(grownStage, Is.Not.EqualTo(startStage),
                "a crop should have advanced at least one growth stage over two game-days");

            // (b) the pending job was claimed AND some actor is no longer idle.
            Assert.That(world.Jobs.GetStatus(Job), Is.EqualTo(JobStatus.Assigned),
                "the seeded pending job should have been claimed");
            Assert.That(world.Jobs.GetClaimedBy(Job), Is.EqualTo(Worker),
                "the willing idle worker should hold the claim");
            Assert.That(world.Actors.Records.Any(a => !a.ScheduleState.IsIdle), Is.True,
                "at least one actor should have a non-idle schedule after assignment");
            Assert.That(world.Actors.Get(Worker).ScheduleState.CurrentJobId, Is.EqualTo(Job),
                "the worker's schedule should reference the claimed job");

            // (c) at least one PriceLedger entry changed.
            var endIronPrice = world.Prices.GetPrice(Site, "iron");
            Assert.That(endIronPrice, Is.GreaterThan(startIronPrice),
                "a below-threshold stockpile should have driven its site price up");
        }

        [Test]
        public void Advance_IsDeterministic_SameSeedSameOutcome()
        {
            var a = RunAndSnapshot();
            var b = RunAndSnapshot();
            Assert.That(a, Is.EqualTo(b), "the same seeded world ticked twice must produce identical results");
        }

        private static string RunAndSnapshot()
        {
            var world = BuildSeededWorld();
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            for (var tick = 1; tick <= TwoGameDaysTicks; tick++)
                composer.Advance(world, tick);

            return string.Join(
                "|",
                world.Plants.Get(PlantId).StageId.Value,
                world.Plants.Get(PlantId).DaysInStage.ToString(),
                world.Jobs.GetStatus(Job).Code,
                world.Jobs.GetClaimedBy(Job).Value.ToString(),
                world.Actors.Get(Worker).ScheduleState.IsIdle.ToString(),
                world.Prices.GetPrice(Site, "iron").ToString());
        }

        // Builds a fixed, minimal living world: one idle willing worker, an active forge worksite with
        // a matching pending smelting job, a farm plot (soil + seeded wheat), and an understocked iron
        // stockpile with a listed price. All ids/positions are constants so the run is deterministic.
        private static WorldState BuildSeededWorld()
        {
            var world = new WorldState();
            // Start at 08:00 day 1 (480 min) — a multiple of both the hour and day periods, matching
            // the playable world's clock so cadence crossings land cleanly.
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
            stockpile.Add("iron", 2); // below the composer's LowStock threshold so the price rises
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
