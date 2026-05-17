using System;

// Design note:
// GameTime is Ember's deterministic calendar timestamp, stored as total game minutes.
// It does not advance real time, read Unity clocks, handle schedules, log events, or model weather.
namespace EmberCrpg.Domain.Core
{
    /// <summary>Deterministic game timestamp backed by total minutes since the Ember epoch.</summary>
    public readonly struct GameTime : IEquatable<GameTime>, IComparable<GameTime>
    {
        /// <summary>Number of game minutes in one game hour.</summary>
        public const int MinutesPerHour = 60;
        /// <summary>Number of game minutes in one 24-hour game day.</summary>
        public const int MinutesPerDay = 1440;
        /// <summary>Number of game minutes in one 30-day game month.</summary>
        public const int MinutesPerMonth = 43200;
        /// <summary>Number of game minutes in one 360-day game year.</summary>
        public const int MinutesPerYear = 518400;
        /// <summary>Number of game days in one deterministic game month.</summary>
        public const int DaysPerMonth = 30;
        /// <summary>Number of game months in one deterministic game year.</summary>
        public const int MonthsPerYear = 12;
        /// <summary>Number of game days in one deterministic game year.</summary>
        public const int DaysPerYear = DaysPerMonth * MonthsPerYear;

        private readonly long _totalMinutes;

        /// <summary>Creates a game timestamp from total minutes since the Ember epoch.</summary>
        public GameTime(long totalMinutes)
        {
            if (totalMinutes < 0)
                throw new ArgumentOutOfRangeException(nameof(totalMinutes), "Game time cannot be before the epoch.");
            _totalMinutes = totalMinutes;
        }

        /// <summary>Total game minutes since the Ember epoch.</summary>
        public long TotalMinutes { get { return _totalMinutes; } }
        /// <summary>Minute component within the current game hour.</summary>
        public int Minute { get { return (int)(_totalMinutes % MinutesPerHour); } }
        /// <summary>Hour component within the current game day.</summary>
        public int Hour { get { return (int)((_totalMinutes / MinutesPerHour) % 24); } }
        /// <summary>One-based day component within the current 30-day game month.</summary>
        public int DayOfMonth { get { return (int)((_totalMinutes / MinutesPerDay) % 30) + 1; } }
        /// <summary>One-based month component within the current 12-month game year.</summary>
        public int Month { get { return (int)((_totalMinutes / MinutesPerMonth) % 12) + 1; } }
        /// <summary>One-based game year.</summary>
        public long Year { get { return (_totalMinutes / MinutesPerYear) + 1; } }
        /// <summary>One-based day component within the current 360-day game year.</summary>
        public int DayOfYear { get { return (int)((_totalMinutes / MinutesPerDay) % 360) + 1; } }

        /// <summary>Returns a new timestamp advanced by game minutes.</summary>
        public GameTime AddMinutes(long minutes) { return new GameTime(_totalMinutes + minutes); }
        /// <summary>Returns a new timestamp advanced by game hours.</summary>
        public GameTime AddHours(long hours) { return AddMinutes(hours * MinutesPerHour); }
        /// <summary>Returns a new timestamp advanced by game days.</summary>
        public GameTime AddDays(long days) { return AddMinutes(days * MinutesPerDay); }
        /// <summary>Returns a new timestamp advanced by 30-day game months.</summary>
        public GameTime AddMonths(long months) { return AddMinutes(months * MinutesPerMonth); }
        /// <summary>Returns a new timestamp advanced by game years.</summary>
        public GameTime AddYears(long years) { return AddMinutes(years * MinutesPerYear); }

        /// <summary>Compares timestamps by total game minutes.</summary>
        public int CompareTo(GameTime other) { return _totalMinutes.CompareTo(other._totalMinutes); }
        /// <summary>Returns true when both timestamps carry the same total game minutes.</summary>
        public bool Equals(GameTime other) { return _totalMinutes == other._totalMinutes; }
        /// <summary>Returns true when the object is a timestamp with the same total game minutes.</summary>
        public override bool Equals(object obj) { return obj is GameTime other && Equals(other); }
        /// <summary>Returns a hash code derived only from total game minutes.</summary>
        public override int GetHashCode() { return _totalMinutes.GetHashCode(); }
        /// <summary>Returns a compact year/day/time debug label.</summary>
        public override string ToString() { return $"Year {Year} Day {DayOfYear} {Hour:00}:{Minute:00}"; }

        /// <summary>Returns a new timestamp advanced by game minutes.</summary>
        public static GameTime operator +(GameTime time, long minutes) { return time.AddMinutes(minutes); }
        /// <summary>Returns the signed delta between two timestamps in game minutes.</summary>
        public static long operator -(GameTime left, GameTime right) { return left._totalMinutes - right._totalMinutes; }
        /// <summary>Returns true when both timestamps carry the same total game minutes.</summary>
        public static bool operator ==(GameTime left, GameTime right) { return left.Equals(right); }
        /// <summary>Returns true when timestamps carry different total game minutes.</summary>
        public static bool operator !=(GameTime left, GameTime right) { return !left.Equals(right); }
        /// <summary>Returns true when the left timestamp is earlier than the right timestamp.</summary>
        public static bool operator <(GameTime left, GameTime right) { return left.CompareTo(right) < 0; }
        /// <summary>Returns true when the left timestamp is later than the right timestamp.</summary>
        public static bool operator >(GameTime left, GameTime right) { return left.CompareTo(right) > 0; }
        /// <summary>Returns true when the left timestamp is not later than the right timestamp.</summary>
        public static bool operator <=(GameTime left, GameTime right) { return left.CompareTo(right) <= 0; }
        /// <summary>Returns true when the left timestamp is not earlier than the right timestamp.</summary>
        public static bool operator >=(GameTime left, GameTime right) { return left.CompareTo(right) >= 0; }
    }
}
