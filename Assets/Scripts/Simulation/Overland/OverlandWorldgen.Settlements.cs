using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Worldgen;
// Domain.Worldgen also declares a (climate) BiomeKind; the overland map uses the PRD 8-biome set.
using BiomeKind = EmberCrpg.Domain.Overland.BiomeKind;

namespace EmberCrpg.Simulation.Overland
{
    public static partial class OverlandWorldgen
    {
        private static IReadOnlyList<OverlandSettlement> ProjectSettlements(
            IReadOnlyList<SettlementRecord> settlements,
            WorldGeography geography)
        {
            if (settlements == null) throw new ArgumentNullException(nameof(settlements));
            if (geography == null) throw new ArgumentNullException(nameof(geography));

            var placements = new List<OverlandSettlement>(settlements.Count);
            for (int i = 0; i < settlements.Count; i++)
            {
                var record = settlements[i];
                if (!record.HasTilePosition)
                    throw new InvalidOperationException("Generated settlement " + record.Name + " has no authoritative geography tile.");
                if (record.TileX < 0 || record.TileY < 0 || record.TileX >= geography.Width || record.TileY >= geography.Height)
                    throw new InvalidOperationException("Generated settlement " + record.Name + " has an out-of-bounds geography tile.");
                if (!geography.IsLandAt(record.TileX, record.TileY))
                    throw new InvalidOperationException("Generated settlement " + record.Name + " is not on a land tile.");

                var biome = geography.OverlandBiomeAt(record.TileX, record.TileY);
                var kind = ClassifySettlementKind(record.Size, biome, StableKindRoll(record.Id));
                placements.Add(new OverlandSettlement(record.Id, kind, new GridPosition(record.TileX, record.TileY), record.Name, TemplatePackTag(kind)));
            }

            return placements;
        }

        private static int StableKindRoll(SettlementId id)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)id.Value) * 16777619u;
                hash = (hash ^ (uint)(id.Value >> 32)) * 16777619u;
                return (int)(hash % 100u);
            }
        }

        private static SettlementKind ClassifySettlementKind(SettlementSize size, BiomeKind biome, int roll)
        {
            if (size == SettlementSize.Capital || size == SettlementSize.City)
                return SettlementKind.City;
            if (size == SettlementSize.Town)
                return roll < 70 ? SettlementKind.Town : SettlementKind.Village;

            switch (biome)
            {
                case BiomeKind.Mountain:
                case BiomeKind.Ash:
                    if (roll < 35) return SettlementKind.Hamlet;
                    if (roll < 68) return SettlementKind.Shrine;
                    return SettlementKind.Dungeon;
                case BiomeKind.Desert:
                case BiomeKind.Tundra:
                    if (roll < 40) return SettlementKind.Hamlet;
                    if (roll < 72) return SettlementKind.Inn;
                    return SettlementKind.Shrine;
                case BiomeKind.Swamp:
                    if (roll < 45) return SettlementKind.Hamlet;
                    if (roll < 75) return SettlementKind.Shrine;
                    return SettlementKind.Dungeon;
                default:
                    if (roll < 50) return SettlementKind.Village;
                    if (roll < 72) return SettlementKind.Hamlet;
                    if (roll < 88) return SettlementKind.Inn;
                    return SettlementKind.Shrine;
            }
        }

        private static string TemplatePackTag(SettlementKind kind)
        {
            switch (kind)
            {
                case SettlementKind.City: return "city";
                case SettlementKind.Town: return "town";
                case SettlementKind.Village:
                case SettlementKind.Hamlet: return "village";
                case SettlementKind.Inn: return "inn";
                case SettlementKind.Shrine: return "shrine";
                default: return "dungeon";
            }
        }
    }
}
