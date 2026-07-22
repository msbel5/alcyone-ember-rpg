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

        // CAN SUYU H2: the lunch WINDOW is dead. Hunger itself sends people to the food spot —
        // a hungry civilian walks there at ANY hour, a fed one never does (even at 12:30), and
        // the watch holds its post regardless. The midday crowd is an EMERGENT consequence of
        // the morning hunger ramp, not a routing rule.
        [Test]
        public void Advance_HungerNotTheClock_SendsCiviliansToTheFoodSpot()
        {
            var foodSpot = new GridPosition(5, 0);
            var midMorning = new GameTime(9 * GameTime.MinutesPerHour); // 09:00 — NOT lunch time

            var hungry = new ActorStore();
            var hungryActor = Record(new GridPosition(0, 0)).WithHomeAndAnchor(new GridPosition(0, 0), new GridPosition(9, 9));
            hungryActor.ApplyNeeds(hungryActor.Needs.WithHunger(new NeedValue(70)));
            hungry.Add(hungryActor);
            var system = new ScheduleSystem();

            system.Advance(hungry, midMorning, foodSpot);
            Assert.That(hungry.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(1, 0)),
                "a HUNGRY civilian heads for the food at 09:00 — no window needed");

            var fed = new ActorStore();
            var fedActor = Record(new GridPosition(0, 0)).WithHomeAndAnchor(new GridPosition(0, 0), new GridPosition(9, 9));
            fedActor.ApplyNeeds(fedActor.Needs.WithHunger(new NeedValue(10)));
            fed.Add(fedActor);
            system.Advance(fed, new GameTime((12 * GameTime.MinutesPerHour) + 30), foodSpot);
            Assert.That(fed.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(1, 1)),
                "a FED civilian ignores the tavern even at 12:30 — the clock does not route meals");

            var guards = new ActorStore();
            var guard = Record(new GridPosition(0, 0), ActorRole.Guard)
                .WithHomeAndAnchor(new GridPosition(0, 0), new GridPosition(0, 9));
            guard.ApplyNeeds(guard.Needs.WithHunger(new NeedValue(90)));
            guards.Add(guard);
            system.Advance(guards, midMorning, foodSpot);
            Assert.That(guards.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(0, 1)),
                "the watch keeps its post route even starving — guards eat off-shift");
        }

        // CAN SUYU H2: the decision table itself — needs pick the behavior deterministically.
        [Test]
        public void ChooseTarget_UtilityTable_NeedsDriveTheChoice()
        {
            var foodSpot = new GridPosition(5, 0);
            var home = new GridPosition(0, 0);
            var anchor = new GridPosition(9, 9);
            var day = new GameTime(9 * GameTime.MinutesPerHour);
            var night = new GameTime(23 * GameTime.MinutesPerHour);

            var actor = Record(new GridPosition(3, 3)).WithHomeAndAnchor(home, anchor);

            actor.ApplyNeeds(ActorNeeds.Comfortable.WithHunger(new NeedValue(80)));
            Assert.That(ScheduleSystem.ChooseTarget(actor, day, foodSpot), Is.EqualTo(foodSpot), "hunger wins the day");

            actor.ApplyNeeds(ActorNeeds.Comfortable.WithFatigue(new NeedValue(90)));
            Assert.That(ScheduleSystem.ChooseTarget(actor, night, foodSpot), Is.EqualTo(home), "exhaustion sends you home at night");

            actor.ApplyNeeds(ActorNeeds.Comfortable);
            Assert.That(ScheduleSystem.ChooseTarget(actor, day, foodSpot), Is.EqualTo(anchor), "a comfortable idle actor minds its day anchor");
        }

        [Test]
        public void Advance_PinnedEnemyLairGuard_HoldsPositionEvenWhenDisplaced()
        {
            var actors = new ActorStore();
            var lair = new GridPosition(8, 8);
            actors.Add(Record(new GridPosition(3, 3), ActorRole.Enemy).WithHomeAndAnchor(lair, lair));
            var system = new ScheduleSystem();

            system.Advance(actors, WorkHour);   // would step toward the day anchor...
            system.Advance(actors, NightHour);  // ...or home — a lair guard does neither.

            Assert.That(actors.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(3, 3)),
                "F18: a pinned Enemy (home == dayAnchor) is chase-driven only; the daily rhythm must not rubber-band it");
        }

        [Test]
        public void Advance_CommutingEnemy_StillWalksHomeAtNight()
        {
            var actors = new ActorStore();
            actors.Add(Record(new GridPosition(5, 5), ActorRole.Enemy)
                .WithHomeAndAnchor(new GridPosition(0, 0), new GridPosition(9, 9)));
            var system = new ScheduleSystem();

            system.Advance(actors, NightHour);

            Assert.That(actors.Get(new ActorId(1)).Position, Is.EqualTo(new GridPosition(4, 4)),
                "street outlaws (home != dayAnchor) keep the F6 curfew commute");
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

        private static ActorRecord Record(GridPosition position, ActorRole role = ActorRole.Talker)
        {
            return new ActorRecord(
                new ActorId(1),
                "Mover",
                role,
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
