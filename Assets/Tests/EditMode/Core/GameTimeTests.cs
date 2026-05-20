using System;
using EmberCrpg.Domain.Core;
using NUnit.Framework;

// Design note:
// These tests pin GameTime as a deterministic total-minute calendar primitive.
// They do not cover real-time advancement, Unity clocks, weather, schedules, or logging.
namespace EmberCrpg.Tests.EditMode.Core
{
    /// <summary>Verifies Ember's deterministic total-minute game calendar.</summary>
    public sealed class GameTimeTests
    {
        /// <summary>A constructed timestamp exposes the supplied total game minutes.</summary>
        [Test]
        public void Constructor_StoresTotalMinutes() => Assert.That(new GameTime(42).TotalMinutes, Is.EqualTo(42));

        /// <summary>Negative total minutes are rejected before the game epoch.</summary>
        [Test]
        public void Constructor_NegativeTotalMinutes_ThrowsArgumentOutOfRange() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameTime(-1));

        /// <summary>The minute component wraps within the hour.</summary>
        [Test]
        public void Minute_WrapsWithinHour() => Assert.That(new GameTime(61).Minute, Is.EqualTo(1));

        /// <summary>The hour component wraps within the day.</summary>
        [Test]
        public void Hour_WrapsWithinDay() => Assert.That(new GameTime(25 * 60).Hour, Is.EqualTo(1));

        /// <summary>The day of month is one-based inside a thirty-day month.</summary>
        [Test]
        public void DayOfMonth_IsOneBasedWithinThirtyDayMonth() =>
            Assert.That(new GameTime(30 * GameTime.MinutesPerDay).DayOfMonth, Is.EqualTo(1));

        /// <summary>The month is one-based inside a twelve-month year.</summary>
        [Test]
        public void Month_IsOneBasedWithinYear() =>
            Assert.That(new GameTime(GameTime.MinutesPerMonth).Month, Is.EqualTo(2));

        /// <summary>The year is one-based and advances after each full year.</summary>
        [Test]
        public void Year_IsOneBasedAndAdvancesEveryYear() =>
            Assert.That(new GameTime(GameTime.MinutesPerYear).Year, Is.EqualTo(2));

        /// <summary>The day of year is one-based inside a 360-day year.</summary>
        [Test]
        public void DayOfYear_IsOneBasedWithinYear() =>
            Assert.That(new GameTime(41 * GameTime.MinutesPerDay).DayOfYear, Is.EqualTo(42));

        /// <summary>AddMinutes returns a new timestamp advanced by raw minutes.</summary>
        [Test]
        public void AddMinutes_ReturnsAdvancedTime() =>
            Assert.That(new GameTime(10).AddMinutes(5).TotalMinutes, Is.EqualTo(15));

        /// <summary>AddHours uses sixty-minute hours.</summary>
        [Test]
        public void AddHours_UsesSixtyMinuteHours() =>
            Assert.That(new GameTime(0).AddHours(2).TotalMinutes, Is.EqualTo(120));

        /// <summary>AddDays uses 1440-minute days.</summary>
        [Test]
        public void AddDays_Uses1440MinuteDays() =>
            Assert.That(new GameTime(0).AddDays(2).TotalMinutes, Is.EqualTo(2880));

        /// <summary>AddMonths uses thirty-day game months.</summary>
        [Test]
        public void AddMonths_UsesThirtyDayMonths() =>
            Assert.That(new GameTime(0).AddMonths(2).TotalMinutes, Is.EqualTo(86400));

        /// <summary>AddYears uses 518400-minute years.</summary>
        [Test]
        public void AddYears_Uses518400MinuteYears() =>
            Assert.That(new GameTime(0).AddYears(2).TotalMinutes, Is.EqualTo(1036800));

        /// <summary>The plus operator advances time by minutes.</summary>
        [Test]
        public void Operator_Plus_AddsMinutes() => Assert.That((new GameTime(10) + 5).TotalMinutes, Is.EqualTo(15));

        /// <summary>The minus operator returns the signed delta in minutes.</summary>
        [Test]
        public void Operator_Minus_ReturnsDeltaMinutes() =>
            Assert.That(new GameTime(30) - new GameTime(10), Is.EqualTo(20));

        /// <summary>The less-than operator compares total minutes.</summary>
        [Test]
        public void Operator_LessThan_ComparesByTotalMinutes() =>
            Assert.That(new GameTime(10) < new GameTime(20), Is.True);

        /// <summary>Equality operators match the Equals method.</summary>
        [Test]
        public void Operator_EqualsAndNotEquals_MatchEqualsMethod()
        {
            var left = new GameTime(10);
            var equal = new GameTime(10);
            var different = new GameTime(20);

            Assert.That(new[] { left == equal, left != different }, Is.EqualTo(new[] { left.Equals(equal), !left.Equals(different) }));
        }

        /// <summary>ToString includes the year, day of year, and zero-padded time.</summary>
        [Test]
        public void ToString_IncludesYearDayAndTime() =>
            Assert.That(new GameTime(41 * 1440 + 8 * 60 + 30).ToString(), Is.EqualTo("Year 1 Day 42 08:30"));
    }
}
