using System;

namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>Immutable world-generation profile persisted on the world root.</summary>
    public sealed class WorldProfile : IEquatable<WorldProfile>
    {
        public WorldProfile(
            WorldStyle style,
            WorldGenre genre,
            uint seed,
            int targetPopulation,
            int regionCount,
            int factionCount,
            int historyYears,
            string moodKeyword,
            string playerCallingKeyword,
            string startLocationKeyword)
        {
            if (targetPopulation <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetPopulation), targetPopulation, "Target population must be positive.");
            if (regionCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(regionCount), regionCount, "Region count must be positive.");
            if (factionCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(factionCount), factionCount, "Faction count must be positive.");
            if (historyYears <= 0)
                throw new ArgumentOutOfRangeException(nameof(historyYears), historyYears, "History years must be positive.");

            Style = style;
            Genre = genre;
            Seed = seed == 0u ? 2463534242u : seed;
            TargetPopulation = targetPopulation;
            RegionCount = regionCount;
            FactionCount = factionCount;
            HistoryYears = historyYears;
            MoodKeyword = moodKeyword ?? string.Empty;
            PlayerCallingKeyword = playerCallingKeyword ?? string.Empty;
            StartLocationKeyword = startLocationKeyword ?? string.Empty;
        }

        public WorldStyle Style { get; }
        public WorldGenre Genre { get; }
        public uint Seed { get; }
        public int TargetPopulation { get; }
        public int RegionCount { get; }
        public int FactionCount { get; }
        public int HistoryYears { get; }
        public string MoodKeyword { get; }
        public string PlayerCallingKeyword { get; }
        public string StartLocationKeyword { get; }

        public bool Equals(WorldProfile other)
        {
            return other != null
                && Style == other.Style
                && Genre == other.Genre
                && Seed == other.Seed
                && TargetPopulation == other.TargetPopulation
                && RegionCount == other.RegionCount
                && FactionCount == other.FactionCount
                && HistoryYears == other.HistoryYears
                && string.Equals(MoodKeyword, other.MoodKeyword, StringComparison.Ordinal)
                && string.Equals(PlayerCallingKeyword, other.PlayerCallingKeyword, StringComparison.Ordinal)
                && string.Equals(StartLocationKeyword, other.StartLocationKeyword, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is WorldProfile other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Style;
                hash = (hash * 397) ^ (int)Genre;
                hash = (hash * 397) ^ Seed.GetHashCode();
                hash = (hash * 397) ^ TargetPopulation;
                hash = (hash * 397) ^ RegionCount;
                hash = (hash * 397) ^ FactionCount;
                hash = (hash * 397) ^ HistoryYears;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(MoodKeyword);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(PlayerCallingKeyword);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(StartLocationKeyword);
                return hash;
            }
        }
    }
}
