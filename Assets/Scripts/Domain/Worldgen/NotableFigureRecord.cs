using System;
using EmberCrpg.Domain.Core;

// Design note:
// NotableFigureRecord is the public worldgen projection of the internal
// multi-century history figure state. It stays deliberately small: stable
// id, display name, title, birth/death years, home settlement, and faction.
// The full dynasty/court simulation remains private to Simulation.Worldgen.
namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>Pure record describing a notable figure surfaced from the history simulator.</summary>
    public sealed class NotableFigureRecord
    {
        public NotableFigureRecord(
            int id,
            string name,
            string title,
            int birthYear,
            int? deathYear,
            SettlementId homeSettlement,
            FactionId faction)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Notable figure id must be positive.");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Notable figure name is required.", nameof(name));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Notable figure title is required.", nameof(title));
            if (deathYear.HasValue && deathYear.Value < birthYear)
                throw new ArgumentOutOfRangeException(nameof(deathYear), deathYear.Value, "Death year cannot precede birth year.");
            if (homeSettlement.IsEmpty)
                throw new ArgumentException("Notable figures require a home settlement.", nameof(homeSettlement));
            if (faction.IsEmpty)
                throw new ArgumentException("Notable figures require a faction.", nameof(faction));

            Id = id;
            Name = name;
            Title = title;
            BirthYear = birthYear;
            DeathYear = deathYear;
            HomeSettlement = homeSettlement;
            Faction = faction;
        }

        public int Id { get; }
        public string Name { get; }
        public string Title { get; }
        public int BirthYear { get; }
        public int? DeathYear { get; }
        public SettlementId HomeSettlement { get; }
        public FactionId Faction { get; }
    }
}
