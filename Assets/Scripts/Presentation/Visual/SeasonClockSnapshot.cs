using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Time;

namespace EmberCrpg.Presentation.Visual
{
    /// <summary>
    /// Read-only snapshot of the current game time and resolved season for Unity HUD.
    /// Pure C#: no UnityEngine, no mutation. Faz 11 Atom 3.
    /// </summary>
    public readonly struct SeasonClockSnapshot
    {
        public SeasonClockSnapshot(GameTime now, int dayOfYear, string seasonCode)
        {
            Now = now;
            DayOfYear = dayOfYear;
            SeasonCode = seasonCode ?? string.Empty;
        }

        public GameTime Now { get; }
        public int DayOfYear { get; }
        public string SeasonCode { get; }

        public static SeasonClockSnapshot From(GameTime now, SeasonCalendar calendar)
        {
            if (calendar == null)
                return new SeasonClockSnapshot(now, now.DayOfYear, string.Empty);

            var season = calendar.GetSeason(now);
            return new SeasonClockSnapshot(now, now.DayOfYear, season.ToString().ToLowerInvariant());
        }
    }
}
