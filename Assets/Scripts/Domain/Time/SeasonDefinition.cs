using System;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Domain.Time
{
    /// <summary>Data row mapping an inclusive day-of-year range to a season label.</summary>
    public sealed class SeasonDefinition
    {
        /// <summary>Creates one deterministic calendar range for a season.</summary>
        public SeasonDefinition(Season season, int startDayOfYear, int endDayOfYear)
        {
            if (season == Season.None)
                throw new ArgumentException("Season definition must carry a concrete season.", nameof(season));
            if (startDayOfYear < 1 || startDayOfYear > GameTime.DaysPerYear)
                throw new ArgumentOutOfRangeException(nameof(startDayOfYear), "Season start day must be inside the game year.");
            if (endDayOfYear < 1 || endDayOfYear > GameTime.DaysPerYear)
                throw new ArgumentOutOfRangeException(nameof(endDayOfYear), "Season end day must be inside the game year.");
            if (endDayOfYear < startDayOfYear)
                throw new ArgumentException("Season end day cannot be before the start day.", nameof(endDayOfYear));

            Season = season;
            StartDayOfYear = startDayOfYear;
            EndDayOfYear = endDayOfYear;
        }

        /// <summary>Season label carried by this row.</summary>
        public Season Season { get; }

        /// <summary>Inclusive one-based day-of-year where this season starts.</summary>
        public int StartDayOfYear { get; }

        /// <summary>Inclusive one-based day-of-year where this season ends.</summary>
        public int EndDayOfYear { get; }

        /// <summary>Returns true when the supplied one-based day-of-year falls inside this row.</summary>
        public bool ContainsDay(int dayOfYear)
        {
            return dayOfYear >= StartDayOfYear && dayOfYear <= EndDayOfYear;
        }
    }
}
