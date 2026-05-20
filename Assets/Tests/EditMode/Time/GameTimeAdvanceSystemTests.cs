using System;
using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Time;
using NUnit.Framework;

// Design note:
// These tests pin Faz 5 Atom 2: deterministic time advancement plus product-
// visible transition rows in WorldEventLog. Plant growth consumes these events
// in later PROCESS atoms, so this file deliberately stops at TIME.
namespace EmberCrpg.Tests.EditMode.Time
{
    /// <summary>Verifies deterministic GameTime advancement and transition event rows.</summary>
    public sealed class GameTimeAdvanceSystemTests
    {
        [Test]
        public void Advance_ReturnsTimeAdvancedByMinutesWithoutSideEffects()
        {
            var system = new GameTimeAdvanceSystem(CreateCalendar());
            var current = new GameTime(5);

            var next = system.Advance(current, GameTime.MinutesPerDay + 10);

            Assert.That(next.TotalMinutes, Is.EqualTo(GameTime.MinutesPerDay + 15));
            Assert.That(next.DayOfYear, Is.EqualTo(2));
            Assert.That(current.TotalMinutes, Is.EqualTo(5));
        }

        [Test]
        public void Advance_AppendsDayAdvancedEventWhenDayChanges()
        {
            var system = new GameTimeAdvanceSystem(CreateCalendar());
            var log = new WorldEventLog();
            var site = new SiteId(44);

            var next = system.Advance(new GameTime(0), GameTime.MinutesPerDay, log, site);

            Assert.That(next.DayOfYear, Is.EqualTo(2));
            Assert.That(log.Count, Is.EqualTo(1));

            var evt = log.Events.Single();
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.DayAdvanced));
            Assert.That(evt.SiteId, Is.EqualTo(site));
            Assert.That(evt.ActorId.IsEmpty, Is.True);
            Assert.That(evt.Tick, Is.EqualTo(next));
            Assert.That(evt.Reason, Is.EqualTo("day_advanced:44"));
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[]
            {
                "time_advance",
                "site:44",
                "from:0",
                $"to:{GameTime.MinutesPerDay}",
                "day:1->2",
                "year:1->1",
            }));
        }

        [Test]
        public void Advance_AppendsSeasonChangedAfterDayEventWhenBoundaryCrosses()
        {
            var system = new GameTimeAdvanceSystem(CreateCalendar());
            var log = new WorldEventLog();
            var site = new SiteId(8);
            var day90 = new GameTime(89L * GameTime.MinutesPerDay);

            var next = system.Advance(day90, GameTime.MinutesPerDay, log, site);

            Assert.That(next.DayOfYear, Is.EqualTo(91));
            Assert.That(log.Count, Is.EqualTo(2));
            Assert.That(log.Events[0].Kind, Is.EqualTo(WorldEventKind.DayAdvanced));
            Assert.That(log.Events[1].Kind, Is.EqualTo(WorldEventKind.SeasonChanged));
            Assert.That(log.Events[1].Reason, Is.EqualTo("season_changed:8:Summer"));
            Assert.That(log.Events[1].ReasonTrace.Causes, Is.EqualTo(new[]
            {
                "time_advance",
                "site:8",
                "from_season:Spring",
                "to_season:Summer",
                $"time:{90L * GameTime.MinutesPerDay}",
            }));
        }

        [Test]
        public void Advance_DoesNotAppendEventsWhenStillSameDayAndSeason()
        {
            var system = new GameTimeAdvanceSystem(CreateCalendar());
            var log = new WorldEventLog();

            system.Advance(new GameTime(0), 30, log, new SiteId(3));

            Assert.That(log.IsEmpty, Is.True);
        }

        [Test]
        public void Advance_RejectsInvalidInputs()
        {
            var system = new GameTimeAdvanceSystem(CreateCalendar());

            Assert.Throws<ArgumentOutOfRangeException>(() => system.Advance(new GameTime(0), -1));
            Assert.Throws<ArgumentNullException>(() => new GameTimeAdvanceSystem(null));
            Assert.Throws<ArgumentNullException>(() => system.Advance(new GameTime(0), 1, null, new SiteId(1)));
            Assert.Throws<ArgumentException>(() => system.Advance(new GameTime(0), 1, new WorldEventLog(), default));
        }

        private static SeasonCalendar CreateCalendar()
        {
            return new SeasonCalendar(new[]
            {
                new SeasonDefinition(Season.Spring, 1, 90),
                new SeasonDefinition(Season.Summer, 91, 180),
                new SeasonDefinition(Season.Autumn, 181, 270),
                new SeasonDefinition(Season.Winter, 271, GameTime.DaysPerYear),
            });
        }
    }
}
