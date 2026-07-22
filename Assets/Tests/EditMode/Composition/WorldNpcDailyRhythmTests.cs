using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    public sealed class WorldNpcDailyRhythmTests
    {
        private const int ExpectedActors = 8;
        private const int ExpectedClaimedJobs = 4;
        private const int StartMinutes = 5 * GameTime.MinutesPerHour + 50;
        private const int ProofDayOffset = 2;
        private static readonly SiteId Site = new SiteId(910UL);
        private static readonly ActorId Requester = new ActorId(999UL);

        // CAN SUYU H2: the workday sample moves to MID-MORNING (10:00). At noon a living town
        // is (correctly) drifting to the table; post-breakfast/pre-crossover is when a FED town
        // is provably at work. The rhythm contract is unchanged — the sampling hour respects needs.
        private static int MiddayTick => TickToDayHour(ProofDayOffset, 10);
        private static int NightTick => TickToDayHour(ProofDayOffset, 22);

        [Test]
        public void Advance_OverMoreThanTwoGameDays_MovesNpcPopulationByDailyRhythm()
        {
            var world = BuildSeededWorld();
            var start = PositionsById(world);
            var composer = new WorldTickComposer();

            composer.Advance(world, 0);
            AdvanceThrough(composer, world, 1, MiddayTick);

            var midday = PositionsById(world);
            var movedByMidday = CountChanged(start, midday);
            var middayAtDayTargets = CountAt(world, actor => actor.DayAnchor);
            var claimedJobs = world.Jobs.Requests.Count(request => world.Jobs.IsClaimed(request.Id));

            AdvanceThrough(composer, world, MiddayTick + 1, NightTick);

            var night = PositionsById(world);
            var nightAtHomes = CountAt(world, actor => actor.Home);
            var divergedByHour = CountChanged(midday, night);
            var assignmentEvents = world.Events.Events.Count(evt => evt.Kind == WorldEventKind.JobAssigned);

            Assert.That(MiddayTick, Is.GreaterThan(2 * WorldTickComposer.TicksPerGameDay),
                "the midday proof sample must exceed two configured game-days");
            Assert.That(NightTick, Is.GreaterThan(2 * WorldTickComposer.TicksPerGameDay),
                "the proof horizon must exceed two configured game-days");
            Assert.That(movedByMidday, Is.EqualTo(ExpectedActors), "many actors should leave spawn for day targets");
            Assert.That(middayAtDayTargets, Is.EqualTo(ExpectedActors), "midday actors should converge on worksites/day anchors");
            Assert.That(nightAtHomes, Is.EqualTo(ExpectedActors), "night actors should converge back home");
            Assert.That(divergedByHour, Is.EqualTo(ExpectedActors), "midday and night positions should visibly diverge");
            Assert.That(claimedJobs, Is.EqualTo(ExpectedClaimedJobs), "multiple role-mapped jobs should be claimed");
            Assert.That(assignmentEvents, Is.EqualTo(ExpectedClaimedJobs), "claims should be emitted through real composition");
        }

        private static WorldState BuildSeededWorld()
        {
            var world = new WorldState
            {
                Time = new GameTime(StartMinutes),
                Actors = new ActorStore(),
            };

            AddNpc(world, 1UL, "Smith Ada", NpcRole.Blacksmith, new GridPosition(0, 0), new GridPosition(5, 2));
            AddNpc(world, 2UL, "Smith Bram", NpcRole.Blacksmith, new GridPosition(0, 2), new GridPosition(6, 3));
            AddNpc(world, 3UL, "Farmer Cora", NpcRole.Farmer, new GridPosition(1, 4), new GridPosition(5, 5));
            AddNpc(world, 4UL, "Farmer Dain", NpcRole.Farmer, new GridPosition(2, 5), new GridPosition(6, 6));
            AddNpc(world, 5UL, "Merchant Edda", NpcRole.Merchant, new GridPosition(9, 0), new GridPosition(6, 1));
            AddNpc(world, 6UL, "Guard Fenn", NpcRole.Guard, new GridPosition(9, 2), new GridPosition(6, 2));
            AddNpc(world, 7UL, "Scholar Gari", NpcRole.Scholar, new GridPosition(9, 4), new GridPosition(6, 4));
            AddNpc(world, 8UL, "Priest Hale", NpcRole.Priest, new GridPosition(9, 6), new GridPosition(6, 5));

            // CAN SUYU H2: a LIVING town needs a larder — without one, need-driven actors
            // correctly stay home starving and the rhythm premise dissolves. No site record
            // → the reach check stays permissive; the meal is the point here, not geometry.
            var larder = new StockpileComponent(Site);
            larder.Add("wheat", 400);
            world.Stockpiles.Add(larder);

            return world;
        }

        private static void AddNpc(WorldState world, ulong id, string name, NpcRole role, GridPosition home, GridPosition dayAnchor)
        {
            var actorId = new ActorId(id);
            var jobKind = NpcRoleJobMapper.ToJobKind(role);
            var preferences = jobKind.HasValue
                ? new[] { new ActorJobPreference(jobKind.Value, JobPriority.Active(1)) }
                : null;

            world.Actors.Add(new ActorRecord(
                actorId,
                name,
                ActorRole.Talker,
                new EmberStatBlock(30, 30, 30, 30, 30, 30),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(30, 30),
                    new VitalStat(10, 10)),
                home,
                accuracy: 30,
                dodge: 20,
                armor: 2,
                baseDamage: 3,
                jobPreferences: preferences,
                home: home,
                dayAnchor: dayAnchor));

            if (jobKind.HasValue)
                AddMatchingJob(world, actorId, jobKind.Value, dayAnchor);
        }

        private static void AddMatchingJob(WorldState world, ActorId actorId, JobKind kind, GridPosition position)
        {
            world.Worksites.Add(new WorksiteRecord(Site, position, WorksiteFor(kind), isActive: true));
            world.Jobs.Add(kind == JobKind.Farmer
                ? FarmingJobRequestFactory.CreatePlantingJob(JobFor(actorId), Site, position, Requester, JobPriority.Active(1))
                : new JobRequest(
                    JobFor(actorId),
                    new RecipeId(1001UL),
                    Site,
                    position,
                    WorksiteKind.Furnace,
                    JobKind.Smith,
                    JobPriority.Active(1),
                    quantity: 1,
                    requesterId: Requester));
        }

        private static WorksiteKind WorksiteFor(JobKind kind)
        {
            return kind == JobKind.Farmer ? WorksiteKind.Field : WorksiteKind.Furnace;
        }

        private static JobId JobFor(ActorId actorId)
        {
            return new JobId(1000UL + actorId.Value);
        }

        private static int TickToDayHour(int dayOffset, int hour)
        {
            return dayOffset * WorldTickComposer.TicksPerGameDay + hour * GameTime.MinutesPerHour - StartMinutes;
        }

        private static void AdvanceThrough(WorldTickComposer composer, WorldState world, int firstTick, int lastTick)
        {
            for (var tick = firstTick; tick <= lastTick; tick++)
                composer.Advance(world, tick);
        }

        private static Dictionary<ActorId, GridPosition> PositionsById(WorldState world)
        {
            return world.Actors.Records.ToDictionary(actor => actor.Id, actor => actor.Position);
        }

        private static int CountChanged(
            IReadOnlyDictionary<ActorId, GridPosition> before,
            IReadOnlyDictionary<ActorId, GridPosition> after)
        {
            return before.Count(row => after.TryGetValue(row.Key, out var position) && !position.Equals(row.Value));
        }

        private static int CountAt(WorldState world, System.Func<ActorRecord, GridPosition> target)
        {
            return world.Actors.Records.Count(actor => actor.Position.ManhattanDistanceTo(target(actor)) <= 1);
        }
    }
}
