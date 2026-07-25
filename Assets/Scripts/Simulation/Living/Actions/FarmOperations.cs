using System.Collections.Generic;
using System.Globalization;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// W33-01 §4/§7.1: FoodOperations' sibling — shared validated lookups for the FARM phase
// machine, and the ONE home of the plot/carry reservation key encoding. The ledger never
// parses tags; only this class does. CONSTRAINT (namespace disjointness): the "plot:" and
// "carry:" prefixes must NEVER leak into a StockpileComponent tag or FoodPileCache.FoodTags —
// a prefixed tag reaching a pile would corrupt effective-stock math.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Shared world lookups + reservation-key codec for the FARM phase machine.</summary>
    internal static class FarmOperations
    {
        /// <summary>Chebyshev reach for harvesting a plot — verbatim retired HarvestHandsService.ReachCells.</summary>
        public const int HarvestReachCells = 2;

        /// <summary>Deterministic plant identity: 1 soil ↔ ≤1 plant, so a pure function suffices —
        /// no counter, no save field, no Add collision (harvest Removes first). W33-01 §7.2.</summary>
        public const ulong PlantIdBase = 500_000UL;

        private const string PlotPrefix = "plot:";
        private const string CarryPrefix = "carry:";

        public static string PlotKey(WorldComponentId soilId)
            => PlotPrefix + soilId.Value.ToString(CultureInfo.InvariantCulture);

        public static string CarryKey(string cropTag) => CarryPrefix + cropTag;

        /// <summary>Failed parse = corrupt row = ReservationLost at the caller.</summary>
        public static bool TryParsePlotKey(string itemTag, out WorldComponentId soilId)
        {
            soilId = default;
            if (itemTag == null || !itemTag.StartsWith(PlotPrefix, System.StringComparison.Ordinal))
                return false;
            if (!ulong.TryParse(itemTag.Substring(PlotPrefix.Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var raw) || raw == 0UL)
                return false;
            soilId = new WorldComponentId(raw);
            return true;
        }

        public static bool TryParseCarryKey(string itemTag, out string cropTag)
        {
            cropTag = null;
            if (itemTag == null || !itemTag.StartsWith(CarryPrefix, System.StringComparison.Ordinal)
                || itemTag.Length <= CarryPrefix.Length)
                return false;
            cropTag = itemTag.Substring(CarryPrefix.Length);
            return true;
        }

        public static WorldComponentId PlantIdFor(WorldComponentId soilId)
            => new WorldComponentId(PlantIdBase + soilId.Value);

        /// <summary>The soil whose PlantId links the given plant; null = orphan (pre-heal saves).</summary>
        public static SoilComponent FindSoilForPlant(WorldState world, WorldComponentId plantId)
        {
            if (world.Soils == null) return null;
            foreach (var row in world.Soils.Rows)
                if (row.Value != null && row.Value.PlantId.Equals(plantId))
                    return row.Value;
            return null;
        }

        /// <summary>First free (plantless) soil of a site, preferring the advisory position;
        /// Rows order breaks ties deterministically. Null = site fully planted or soilless.</summary>
        public static SoilComponent FindFreeSoil(WorldState world, SiteId siteId, GridPosition preferred)
        {
            if (world.Soils == null) return null;
            SoilComponent first = null;
            foreach (var row in world.Soils.Rows)
            {
                var soil = row.Value;
                if (soil == null || !soil.SiteId.Equals(siteId) || soil.HasPlant) continue;
                if (soil.Position.Equals(preferred)) return soil;
                first ??= soil;
            }
            return first;
        }

        /// <summary>Catalog row for a species id; null when uncatalogued.</summary>
        public static PlantSpeciesDef SpeciesFor(IReadOnlyList<PlantSpeciesDef> catalog, string speciesId)
        {
            if (catalog == null) return null;
            for (var i = 0; i < catalog.Count; i++)
                if (catalog[i] != null && catalog[i].SpeciesId == speciesId)
                    return catalog[i];
            return null;
        }

        /// <summary>Catalog-driven harvestability; an unknown species is NOT harvestable (loud
        /// absence beats a silent guess — the slice catalog is single-species by design).</summary>
        public static bool IsHarvestable(IReadOnlyList<PlantSpeciesDef> catalog, PlantComponent plant)
        {
            if (catalog == null || plant == null) return false;
            for (var i = 0; i < catalog.Count; i++)
            {
                var species = catalog[i];
                if (species == null || species.SpeciesId != plant.SpeciesId) continue;
                return species.TryGetStage(plant.StageId, out var stage) && stage.IsHarvestable;
            }
            return false;
        }

        /// <summary>Find-or-create mirror of the retired HarvestStep block so a pileless site
        /// still receives its first stock. Null only for an empty SiteId (bare test worlds).</summary>
        public static StockpileComponent FindOrCreatePile(WorldState world, SiteId siteId)
        {
            if (siteId.IsEmpty || world.Stockpiles == null) return null;
            var pile = FoodOperations.FindPile(world, siteId.Value);
            if (pile == null)
            {
                pile = new StockpileComponent(siteId);
                world.Stockpiles.Add(pile);
            }
            return pile;
        }

        public static long Chebyshev(GridPosition a, GridPosition b)
            => System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y));
    }
}
