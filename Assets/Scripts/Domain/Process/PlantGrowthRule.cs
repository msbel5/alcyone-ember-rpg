using EmberCrpg.Domain.Time;

namespace EmberCrpg.Domain.Process
{
    /// <summary>Data row describing whether a plant can grow under one season/weather condition.</summary>
    public sealed class PlantGrowthRule
    {
        public PlantGrowthRule(Season season, bool allowsGrowth, bool blockedBySnow)
        {
            Season = season;
            AllowsGrowth = allowsGrowth;
            BlockedBySnow = blockedBySnow;
        }

        public Season Season { get; }
        public bool AllowsGrowth { get; }
        public bool BlockedBySnow { get; }

        public bool Matches(Season season)
        {
            return Season == Season.None || Season == season;
        }

        public bool CanGrow(bool isSnowing)
        {
            return AllowsGrowth && (!isSnowing || !BlockedBySnow);
        }
    }
}
