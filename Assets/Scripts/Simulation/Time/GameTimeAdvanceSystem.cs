using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;

// Design note:
// GameTimeAdvanceSystem is Faz 5's first Simulation TIME atom. It advances
// only deterministic GameTime and optional EventLog rows. It does not model
// weather, plant growth, schedules, or real-time Unity clocks.
namespace EmberCrpg.Simulation.Time
{
    /// <summary>Advances deterministic game time and emits day/season transition events.</summary>
    public sealed class GameTimeAdvanceSystem
    {
        private readonly SeasonCalendar _seasonCalendar;

        public GameTimeAdvanceSystem(SeasonCalendar seasonCalendar)
        {
            _seasonCalendar = seasonCalendar ?? throw new ArgumentNullException(nameof(seasonCalendar));
        }

        public GameTime Advance(GameTime current, long minutes)
        {
            if (minutes < 0)
                throw new ArgumentOutOfRangeException(nameof(minutes), "Game time cannot advance by negative minutes.");

            return current.AddMinutes(minutes);
        }

        public GameTime Advance(
            GameTime current,
            long minutes,
            WorldEventLog eventLog,
            SiteId siteId)
        {
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));
            if (siteId.IsEmpty)
                throw new ArgumentException("Time transition events require a site locus.", nameof(siteId));

            var next = Advance(current, minutes);
            AppendTransitionEvents(current, next, eventLog, siteId);
            return next;
        }

        private void AppendTransitionEvents(
            GameTime previous,
            GameTime current,
            WorldEventLog eventLog,
            SiteId siteId)
        {
            if (current.DayOfYear != previous.DayOfYear || current.Year != previous.Year)
            {
                eventLog.Append(new WorldEvent(
                    current,
                    WorldEventKind.DayAdvanced,
                    default,
                    siteId,
                    $"day_advanced:{siteId.Value}",
                    new ReasonTrace(new[]
                    {
                        "time_advance",
                        $"site:{siteId.Value}",
                        $"from:{previous.TotalMinutes}",
                        $"to:{current.TotalMinutes}",
                        $"day:{previous.DayOfYear}->{current.DayOfYear}",
                        $"year:{previous.Year}->{current.Year}",
                    })));
            }

            var previousSeason = _seasonCalendar.GetSeason(previous);
            var currentSeason = _seasonCalendar.GetSeason(current);
            if (currentSeason != previousSeason)
            {
                eventLog.Append(new WorldEvent(
                    current,
                    WorldEventKind.SeasonChanged,
                    default,
                    siteId,
                    $"season_changed:{siteId.Value}:{currentSeason}",
                    new ReasonTrace(new[]
                    {
                        "time_advance",
                        $"site:{siteId.Value}",
                        $"from_season:{previousSeason}",
                        $"to_season:{currentSeason}",
                        $"time:{current.TotalMinutes}",
                    })));
            }
        }
    }
}
