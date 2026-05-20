using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Domain.Time
{
    /// <summary>Pure calendar service that resolves GameTime into data-defined seasons.</summary>
    public sealed class SeasonCalendar
    {
        private readonly ReadOnlyCollection<SeasonDefinition> _seasons;

        /// <summary>Creates a calendar from deterministic season rows.</summary>
        public SeasonCalendar(IEnumerable<SeasonDefinition> seasons)
        {
            if (seasons == null)
                throw new ArgumentNullException(nameof(seasons));

            var ordered = new List<SeasonDefinition>(seasons);
            if (ordered.Count == 0)
                throw new ArgumentException("Season calendar requires at least one season definition.", nameof(seasons));

            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] == null)
                    throw new ArgumentException("Season calendar cannot contain null definitions.", nameof(seasons));
            }

            ordered.Sort((left, right) => left.StartDayOfYear.CompareTo(right.StartDayOfYear));
            for (var i = 0; i < ordered.Count; i++)
            {
                if (i > 0 && ordered[i].StartDayOfYear <= ordered[i - 1].EndDayOfYear)
                    throw new ArgumentException("Season calendar definitions cannot overlap.", nameof(seasons));
            }

            _seasons = new ReadOnlyCollection<SeasonDefinition>(ordered);
        }

        /// <summary>Ordered season definition rows.</summary>
        public IReadOnlyList<SeasonDefinition> Seasons { get { return _seasons; } }

        /// <summary>Returns the season containing the supplied timestamp.</summary>
        public Season GetSeason(GameTime time)
        {
            Season season;
            if (TryGetSeason(time, out season))
                return season;

            throw new InvalidOperationException("No season definition covers the supplied game time.");
        }

        /// <summary>Tries to resolve the season for the supplied timestamp without throwing.</summary>
        public bool TryGetSeason(GameTime time, out Season season)
        {
            var dayOfYear = time.DayOfYear;
            for (var i = 0; i < _seasons.Count; i++)
            {
                if (_seasons[i].ContainsDay(dayOfYear))
                {
                    season = _seasons[i].Season;
                    return true;
                }
            }

            season = Season.None;
            return false;
        }

        /// <summary>Returns true when advancing from previous to current crosses a season boundary.</summary>
        public bool IsSeasonBoundary(GameTime previous, GameTime current)
        {
            return GetSeason(previous) != GetSeason(current);
        }
    }
}
