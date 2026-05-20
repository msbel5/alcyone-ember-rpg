using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Time;
using EmberCrpg.Presentation.VisualLayer;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation.VisualLayer
{
    /// <summary>Pins season + day-of-year HUD snapshot.</summary>
    public sealed class SeasonClockSnapshotTests
    {
        private static SeasonCalendar FullYearCalendar()
        {
            return new SeasonCalendar(new[]
            {
                new SeasonDefinition(Season.Spring, 1, 90),
                new SeasonDefinition(Season.Summer, 91, 180),
                new SeasonDefinition(Season.Autumn, 181, 270),
                new SeasonDefinition(Season.Winter, 271, 360),
            });
        }

        [Test]
        public void NullCalendar_ProducesEmptySeasonCode()
        {
            var now = new GameTime(10 * GameTime.MinutesPerDay);
            var snapshot = SeasonClockSnapshot.From(now, null);
            Assert.That(snapshot.SeasonCode, Is.EqualTo(string.Empty));
            Assert.That(snapshot.DayOfYear, Is.EqualTo(now.DayOfYear));
        }

        [Test]
        public void Spring_DayOne_HasSpringCode()
        {
            var calendar = FullYearCalendar();
            var now = new GameTime(0); // Day 1.

            var snapshot = SeasonClockSnapshot.From(now, calendar);

            Assert.That(snapshot.SeasonCode, Is.EqualTo("spring"));
            Assert.That(snapshot.DayOfYear, Is.EqualTo(1));
        }

        [Test]
        public void Winter_HasWinterCode()
        {
            var calendar = FullYearCalendar();
            var now = new GameTime(280L * GameTime.MinutesPerDay); // day 281 of year

            var snapshot = SeasonClockSnapshot.From(now, calendar);

            Assert.That(snapshot.SeasonCode, Is.EqualTo("winter"));
        }
    }
}
