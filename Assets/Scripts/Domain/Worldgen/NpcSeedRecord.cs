using System;
using EmberCrpg.Domain.Core;

// Design note:
// NpcSeedRecord is the worldgen FOUNDATION's per-NPC payload — one of the
// ~500-1000 named NPCs distributed across settlements. "Seed" because this
// record does not carry the full runtime ActorRecord surface (vitals,
// inventory, schedules); it carries just enough for a future hydration step
// to spawn a real ActorRecord when the player walks into a settlement.
// Defensive constructor pattern: id non-empty, home non-empty (every NPC
// lives in a settlement), faction non-empty (factionless NPCs would need a
// dedicated NoFaction sentinel and that is out of scope here), name non-blank,
// role non-None, birthYear has no bound check because history can extend
// back arbitrarily.
namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>Pure record describing a procedurally seeded NPC by id, home settlement, faction, name, birth year, and role.</summary>
    public sealed class NpcSeedRecord
    {
        public NpcSeedRecord(NpcId id, SettlementId home, FactionId faction, string name, int birthYear, NpcRole role)
            : this(id, home, faction, name, birthYear, role, string.Empty)
        {
        }

        public NpcSeedRecord(NpcId id, SettlementId home, FactionId faction, string name, int birthYear, NpcRole role, string portraitAssetPath)
        {
            if (id.IsEmpty)
                throw new ArgumentException("NpcId.Empty cannot back an NpcSeedRecord.", nameof(id));
            if (home.IsEmpty)
                throw new ArgumentException("SettlementId.Empty cannot back an NpcSeedRecord — every NPC lives in a settlement.", nameof(home));
            if (faction.IsEmpty)
                throw new ArgumentException("FactionId.Empty cannot back an NpcSeedRecord — factionless NPCs require a dedicated sentinel.", nameof(faction));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("NPC name is required.", nameof(name));
            if (role == NpcRole.None)
                throw new ArgumentException("NpcRole.None is reserved as the empty sentinel.", nameof(role));

            Id = id;
            Home = home;
            Faction = faction;
            Name = name;
            BirthYear = birthYear;
            Role = role;
            PortraitAssetPath = portraitAssetPath ?? string.Empty;
        }

        public NpcId Id { get; }
        public SettlementId Home { get; }
        public FactionId Faction { get; }
        public string Name { get; }
        public int BirthYear { get; }
        public NpcRole Role { get; }
        public string PortraitAssetPath { get; }
    }
}
