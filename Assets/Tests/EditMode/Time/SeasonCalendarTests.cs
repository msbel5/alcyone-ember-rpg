using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Time;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Time
{
    /// <summary>Verifies the Phase 5 data-driven season calendar primitive.</summary>
    public sealed class SeasonCalendarTests
    {
        [Test]
        public void GetSeason_ResolvesDayOfYearFromDataRows()
        {
            var calendar = CreateFourSeasonCalendar();

            Assert.That(calendar.GetSeason(new GameTime(0)), Is.EqualTo(Season.Spring));
            Assert.That(calendar.GetSeason(new GameTime(90 * GameTime.MinutesPerDay)), Is.EqualTo(Season.Summer));
            Assert.That(calendar.GetSeason(new GameTime(180 * GameTime.MinutesPerDay)), Is.EqualTo(Season.Autumn));
            Assert.That(calendar.GetSeason(new GameTime(270 * GameTime.MinutesPerDay)), Is.EqualTo(Season.Winter));
        }

        [Test]
        public void IsSeasonBoundary_ReturnsTrueOnlyWhenSeasonChanges()
        {
            var calendar = CreateFourSeasonCalendar();

            Assert.That(calendar.IsSeasonBoundary(new GameTime(88 * GameTime.MinutesPerDay), new GameTime(89 * GameTime.MinutesPerDay)), Is.False);
            Assert.That(calendar.IsSeasonBoundary(new GameTime(89 * GameTime.MinutesPerDay), new GameTime(90 * GameTime.MinutesPerDay)), Is.True);
        }

        [Test]
        public void Constructor_RejectsOverlappingRows()
        {
            Assert.Throws<ArgumentException>(() => new SeasonCalendar(new[]
            {
                new SeasonDefinition(Season.Spring, 1, 100),
                new SeasonDefinition(Season.Summer, 100, 180),
            }));
        }

        [Test]
        public void TryGetSeason_ReturnsFalseForCalendarGap()
        {
            var calendar = new SeasonCalendar(new[]
            {
                new SeasonDefinition(Season.Spring, 1, 30),
            });

            Season season;
            Assert.That(calendar.TryGetSeason(new GameTime(31 * GameTime.MinutesPerDay), out season), Is.False);
            Assert.That(season, Is.EqualTo(Season.None));
        }

        private static SeasonCalendar CreateFourSeasonCalendar()
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
