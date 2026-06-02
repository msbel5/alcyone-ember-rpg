using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// SOUL-03: ScheduleSystem must step a job-assigned actor one tile per game-hour toward its
    /// worksite during work hours, route idle actors to day anchors, and route all actors home
    /// outside work hours. Pure Domain/Simulation: deterministic, no Unity.
    /// </summary>
    public sealed class ScheduleSystemTests
    {
        private static readonly JobId Job = new JobId(701UL);
        private static readonly SiteId Site = new SiteId(77UL);
        private static readonly GridPosition Worksite = new GridPosition(5, 3);

        private static GameTime WorkHour => new GameTime(9 * GameTime.MinutesPerHour); // 09:00
        private static GameTime NightHour => new GameTime(2 * GameTime.MinutesPerHour); // 02:00

        [Test]
        public void Advance_DuringWorkHours_StepsAssignedActorOneTileTowardWorksite()
        {
            var actors = new ActorStore();
            actors.Add(AssignedActor(new GridPosition(0, 0)));
            var system = new ScheduleSystem();

            system.Advance(actors, WorkHour);

            // One Chebyshev step: each axis advances by exactly one toward (5,3).
            Assert.That(actors.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void Advance_RepeatedDuringWorkHours_ConvergesToWorksiteWithoutOvershoot()
        {
            var actors = new ActorStore();
            actors.Add(AssignedActor(new GridPosition(0, 0)));
            var system = new ScheduleSystem();

            // Chebyshev distance to (5,3) is 5; five work-hours must land exactly on the worksite.
            for (var hour = 0; hour < 5; hour++)
                system.Advance(actors, WorkHour);
            Assert.That(actors.Get(new ActorId(1)).Position, Is.EqualTo(Worksite));

            // Further advances do not overshoot — the actor stays put once it has arrived.
            system.Advance(actors, WorkHour);
            Assert.That(actors.Get(new ActorId(1)).Position, Is.EqualTo(Worksite));
        }

        [Test]
        public void Advance_OutsideWorkHours_DoesNotMoveActor()
        {
            var actors = new ActorStore();
            actors.Add(AssignedActor(new GridPosition(0, 0)));
            var system = new ScheduleSystem();

            system.Advance(actors, NightHour);

            Assert.That(actors.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void Advance_IdleActorWithoutAnchor_DoesNotMove()
        {
            var actors = new ActorStore();
            actors.Add(IdleActor(new GridPosition(0, 0)));
            var system = new ScheduleSystem();

            system.Advance(actors, WorkHour);

            Assert.That(actors.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void Advance_DuringWorkHours_StepsIdleActorTowardDayAnchor()
        {
            var actors = new ActorStore();
            actors.Add(Record(new GridPosition(0, 0)).WithHomeAndAnchor(new GridPosition(0, 0), new GridPosition(3, 2)));
            var system = new ScheduleSystem();

            system.Advance(actors, WorkHour);

            Assert.That(actors.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void Advance_OutsideWorkHours_StepsAssignedActorTowardHome()
        {
            var actors = new ActorStore();
            var actor = Record(new GridPosition(5, 3)).WithHomeAndAnchor(new GridPosition(2, 1), new GridPosition(9, 9));
            actor.ApplyScheduleState(ActorScheduleState.Assigned(Job, Site, Worksite));
            actors.Add(actor);
            var system = new ScheduleSystem();

            system.Advance(actors, NightHour);

            Assert.That(actors.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(4, 2)));
        }

        private static ActorRecord AssignedActor(GridPosition position)
        {
            var actor = Record(position);
            actor.ApplyScheduleState(ActorScheduleState.Assigned(Job, Site, Worksite));
            return actor;
        }

        private static ActorRecord IdleActor(GridPosition position)
        {
            return Record(position);
        }

        private static ActorRecord Record(GridPosition position)
        {
            return new ActorRecord(
                new ActorId(1),
                "Mover",
                ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(10, 10),
                    new VitalStat(10, 10),
                    new VitalStat(10, 10)),
                position,
                accuracy: 50,
                dodge: 10,
                armor: 0,
                baseDamage: 1);
        }
    }
}
