using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Derives raw-material ledgers from tectonics, hydrology, climate, and seeded impact events.</summary>
    public sealed class ResourceStage
    {
        private const int BoundaryRadius = 2;
        private const double BoundaryDecay = 0.58d;

        public PlanetField Apply(PlanetField field, XorShiftRng rng)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            var iron = new double[field.TileCount];
            var precious = new double[field.TileCount];
            var brush = new RadialBrush(field.Grid);
            PlanetImpactSite[] impacts = ChooseImpactSites(field, rng);

            // Late-veneer bombardment is modeled as seeded impact sites: iron/nickel-rich meteorites are common,
            // while gold/platinum-group delivery is rarer, so precious metal abundance is drawn from a steep curve.
            for (int i = 0; i < impacts.Length; i++)
            {
                PlanetImpactSite impact = impacts[i];
                brush.Add(iron, impact.TileId, impact.Radius, impact.IronAbundance, 0.56d);
                brush.Add(precious, impact.TileId, Math.Max(1, impact.Radius - 1), impact.PreciousMetalAbundance, 0.50d);
            }

            // Convergent margins drive arc volcanism and hydrothermal circulation; porphyry and volcanogenic
            // systems concentrate ore along those plate boundaries rather than uniformly across continents.
            for (int i = 0; i < field.Boundaries.Edges.Count; i++)
            {
                PlateBoundaryEdge edge = field.Boundaries.Edges[i];
                if (edge.Kind != PlateBoundaryKind.Convergent)
                    continue;

                double activity = 0.24d + (Math.Min(1d, edge.Magnitude / field.Parameters.DriftScale) * 0.28d);
                brush.Add(iron, edge.TileA, BoundaryRadius, activity, BoundaryDecay);
                brush.Add(iron, edge.TileB, BoundaryRadius, activity, BoundaryDecay);
                brush.Add(precious, edge.TileA, BoundaryRadius, activity * 0.34d, BoundaryDecay);
                brush.Add(precious, edge.TileB, BoundaryRadius, activity * 0.34d, BoundaryDecay);
            }

            var tiles = new PlanetTileField[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField source = field.TileAt(tileId);
                double flatness = FlatnessFor(field, tileId);
                double freshWater = FreshWaterFor(field, tileId);
                double stone = StoneFor(field, tileId, flatness);
                double clay = ClayFor(field, tileId, flatness);
                double wood = WoodFor(source);
                double coal = CoalFor(field, tileId);
                double oilGas = OilGasFor(field, tileId, flatness);
                double soilFertility = SoilFertilityFor(source, flatness, freshWater);

                tiles[tileId] = source.CopyWith(
                    ironOre: iron[tileId],
                    preciousMetal: precious[tileId],
                    coal: coal,
                    oilGas: oilGas,
                    stone: stone,
                    clay: clay,
                    wood: wood,
                    soilFertility: soilFertility,
                    freshWater: freshWater);
            }

            return new PlanetField(
                field.Seed,
                field.Parameters,
                field.Grid,
                field.Plates,
                field.Boundaries,
                tiles,
                field.CopySettlements(),
                impacts);
        }

        public static PlanetImpactSite[] ChooseImpactSites(PlanetField field, XorShiftRng rng)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            int count = Math.Min(field.TileCount, Math.Max(3, 3 + (field.Grid.SubdivisionLevel * 3)));
            int radius = 2 + Math.Min(4, field.Grid.SubdivisionLevel);
            var used = new bool[field.TileCount];
            var impacts = new PlanetImpactSite[count];

            for (int i = 0; i < count; i++)
            {
                int nth = rng.NextInt(field.TileCount - i);
                int tileId = SelectNthUnused(used, nth);
                used[tileId] = true;

                double ironAbundance = 0.58d + (PlanetRng.NextUnit(rng) * 0.37d);
                double rareRoll = PlanetRng.NextUnit(rng);
                double preciousAbundance = 0.02d + (Math.Pow(rareRoll, 4d) * 0.42d);
                if (rng.NextInt(17) == 0)
                    preciousAbundance += 0.18d;

                impacts[i] = new PlanetImpactSite(tileId, radius, ironAbundance, preciousAbundance);
            }

            return impacts;
        }

        public static bool IsForestBiome(PlanetBiome biome)
        {
            return biome == PlanetBiome.Taiga ||
                biome == PlanetBiome.TemperateForest ||
                biome == PlanetBiome.TropicalRainforest;
        }

        private static int SelectNthUnused(bool[] used, int nth)
        {
            int seen = 0;
            for (int tileId = 0; tileId < used.Length; tileId++)
            {
                if (used[tileId])
                    continue;
                if (seen == nth)
                    return tileId;
                seen++;
            }

            throw new InvalidOperationException("Unable to select an unused impact tile.");
        }

        private static double StoneFor(PlanetField field, int tileId, double flatness)
        {
            PlanetTileField tile = field.TileAt(tileId);
            if (!tile.IsLand)
                return 0d;

            double height = Clamp01((tile.Elevation - field.Parameters.SeaLevelThreshold) / 0.92d);
            double exposed = tile.Biome == PlanetBiome.Mountain ? 0.34d : 0d;
            return Clamp01(0.10d + (height * 0.56d) + ((1d - flatness) * 0.32d) + exposed);
        }

        private static double ClayFor(PlanetField field, int tileId, double flatness)
        {
            PlanetTileField tile = field.TileAt(tileId);
            if (!tile.IsLand)
                return 0d;

            double water = FreshWaterProximity(field, tileId);
            double low = 1d - Clamp01((tile.Elevation - field.Parameters.SeaLevelThreshold) / 0.55d);
            return Clamp01(water * (0.38d + (flatness * 0.36d) + (low * 0.24d)));
        }

        private static double WoodFor(PlanetTileField tile)
        {
            if (!tile.IsLand || !IsForestBiome(tile.Biome))
                return 0d;

            double biomeDensity = tile.Biome == PlanetBiome.TropicalRainforest ? 1d : tile.Biome == PlanetBiome.TemperateForest ? 0.86d : 0.64d;
            return Clamp01(biomeDensity * (0.42d + (tile.Moisture * 0.58d)));
        }

        private static double CoalFor(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            if (!tile.IsLand)
                return 0d;

            // Coal is a paleo-swamp proxy: humid forest/wet biomass is strongest on low-to-mid, subsiding terrain.
            double lowMid = LowMidElevation(tile.Elevation, field.Parameters.SeaLevelThreshold);
            double forestWetness = IsForestBiome(tile.Biome) ? 0.70d + (tile.Moisture * 0.30d) : Math.Max(0d, tile.Moisture - 0.58d) * 0.62d;
            return Clamp01(lowMid * forestWetness * tile.Moisture);
        }

        private static double OilGasFor(PlanetField field, int tileId, double flatness)
        {
            PlanetTileField tile = field.TileAt(tileId);
            if (!tile.IsLand)
                return 0d;

            // Marine source rocks accumulate in low coastal/former-seafloor basins, then become useful where uplifted
            // or exposed on land; oceanic-plate islands get a small former-seafloor bias.
            double low = 1d - Clamp01((tile.Elevation - field.Parameters.SeaLevelThreshold) / 0.38d);
            double coast = HasOceanNeighbor(field, tileId) ? 1d : 0d;
            double formerSeafloor = field.Plates.Plates[tile.PlateId].Kind == PlateKind.Oceanic ? 0.26d : 0d;
            double basin = low * flatness;
            return Clamp01(low * ((coast * 0.58d) + (basin * 0.34d) + formerSeafloor));
        }

        private static double SoilFertilityFor(PlanetTileField tile, double flatness, double freshWater)
        {
            if (!tile.IsLand)
                return 0d;

            double fertility = (BiomeFertilityBase(tile.Biome) * 0.44d) +
                (tile.Moisture * 0.20d) +
                (flatness * 0.20d) +
                (freshWater * 0.30d);
            if (tile.Biome == PlanetBiome.Ice || tile.Biome == PlanetBiome.Mountain)
                fertility *= 0.55d;
            return Clamp01(fertility);
        }

        private static double FreshWaterFor(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            if (!tile.IsLand)
                return 0d;

            double water = FreshWaterProximity(field, tileId);
            double rainfall = tile.Moisture * 0.34d;
            double flow = Math.Min(0.26d, tile.Flow / Math.Max(1d, HydrologyStage.RiverFlowThreshold(field.TileCount) * 3.8d));
            return Clamp01(water + rainfall + flow);
        }

        private static double FreshWaterProximity(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            if (tile.IsRiver || tile.IsLake)
                return 1d;

            double best = 0d;
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighbor = neighbors[i];
                PlanetTileField neighborTile = field.TileAt(neighbor);
                if (neighborTile.IsRiver || neighborTile.IsLake)
                    best = Math.Max(best, 0.70d);

                var secondRing = field.Grid.TileAt(neighbor).Neighbors;
                for (int j = 0; j < secondRing.Count; j++)
                {
                    PlanetTileField secondTile = field.TileAt(secondRing[j]);
                    if (secondTile.IsRiver || secondTile.IsLake)
                        best = Math.Max(best, 0.38d);
                }
            }

            return best;
        }

        private static double LowMidElevation(double elevation, double seaLevel)
        {
            double height = elevation - seaLevel;
            if (height > 0.88d)
                return 0d;
            return Clamp01(1d - (Math.Abs(height - 0.24d) / 0.56d));
        }

        private static double FlatnessFor(PlanetField field, int tileId)
        {
            double elevation = field.TileAt(tileId).Elevation;
            double steepest = 0d;
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
                steepest = Math.Max(steepest, Math.Abs(elevation - field.TileAt(neighbors[i]).Elevation));

            return 1d - Clamp01(steepest / 0.58d);
        }

        private static bool HasOceanNeighbor(PlanetField field, int tileId)
        {
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (!field.TileAt(neighbors[i]).IsLand)
                    return true;
            }

            return false;
        }

        private static double BiomeFertilityBase(PlanetBiome biome)
        {
            switch (biome)
            {
                case PlanetBiome.TropicalRainforest:
                    return 0.54d;
                case PlanetBiome.TemperateForest:
                    return 0.66d;
                case PlanetBiome.Grassland:
                    return 0.60d;
                case PlanetBiome.Savanna:
                    return 0.46d;
                case PlanetBiome.Taiga:
                    return 0.34d;
                case PlanetBiome.Tundra:
                    return 0.16d;
                case PlanetBiome.Desert:
                    return 0.04d;
                case PlanetBiome.Mountain:
                    return 0.08d;
                case PlanetBiome.Ice:
                    return 0.02d;
                default:
                    return 0d;
            }
        }

        private static double Clamp01(double value)
        {
            if (value < 0d)
                return 0d;
            if (value > 1d)
                return 1d;
            return value;
        }

        private sealed class RadialBrush
        {
            private readonly IcosphereGrid _grid;
            private readonly int[] _distance;
            private readonly int[] _queue;
            private readonly int[] _seen;
            private int _stamp;

            public RadialBrush(IcosphereGrid grid)
            {
                _grid = grid;
                _distance = new int[grid.Count];
                _queue = new int[grid.Count];
                _seen = new int[grid.Count];
            }

            public void Add(double[] target, int center, int radius, double amount, double decay)
            {
                if (amount <= 0d || radius < 0)
                    return;

                _stamp++;
                if (_stamp == int.MaxValue)
                {
                    Array.Clear(_seen, 0, _seen.Length);
                    _stamp = 1;
                }

                int head = 0;
                int tail = 0;
                _queue[tail++] = center;
                _distance[center] = 0;
                _seen[center] = _stamp;

                while (head < tail)
                {
                    int tileId = _queue[head++];
                    int distance = _distance[tileId];
                    target[tileId] = Clamp01(target[tileId] + (amount * Math.Pow(decay, distance)));
                    if (distance >= radius)
                        continue;

                    var neighbors = _grid.TileAt(tileId).Neighbors;
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        int neighbor = neighbors[i];
                        if (_seen[neighbor] == _stamp)
                            continue;

                        _seen[neighbor] = _stamp;
                        _distance[neighbor] = distance + 1;
                        _queue[tail++] = neighbor;
                    }
                }
            }
        }
    }
}
