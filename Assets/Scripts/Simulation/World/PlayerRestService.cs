using EmberCrpg.Domain.Actors;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// PLAYTEST ("rest tusu olmali"): resting maths, pure and test-pinned. Fatigue and mana
    /// refill with sleep; health knits at max/12 per slept hour (a full night is ~2/3 of max) -
    /// full recovery still wants food, a healer, or more days.
    /// </summary>
    public static class PlayerRestService
    {
        public const int DawnHour = 6;

        /// <summary>Whole hours from now until the NEXT 06:00 (1..24; exactly at dawn = 24).</summary>
        public static int HoursUntilDawn(long totalMinutes)
        {
            long minuteOfDay = totalMinutes % 1440L;
            long delta = ((DawnHour * 60L) - minuteOfDay + 1440L) % 1440L;
            if (delta == 0) delta = 1440L;
            return (int)((delta + 59L) / 60L);
        }

        public static ActorVitals RestedVitals(ActorVitals vitals, int hoursSlept)
        {
            int heal = (vitals.Health.Max * hoursSlept) / 12;
            var health = new VitalStat(
                System.Math.Min(vitals.Health.Max, vitals.Health.Current + heal), vitals.Health.Max);
            return new ActorVitals(health, vitals.Fatigue.Refill(), vitals.Mana.Refill());
        }
    }
}
