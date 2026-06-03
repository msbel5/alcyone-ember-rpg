using System;
using System.Collections.Generic;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Derives raw-material ledgers from tectonics, hydrology, climate, and seeded impact events.</summary>
    public sealed class ResourceStage : IPlanetStage
    {
        private const int OreClusterRadius = 1;
        private const double OreClusterDecay = 0.32d;
        private const double PreciousClusterDecay = 0.28d;

        public string Name => "Resources";

        public void Run(PlanetGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Field = Apply(context.RequireField(), context.Fork(PlanetGenerationContext.ResourceStageSeed));
        }

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
            OreDeposit[] deposits = ChooseOreDeposits(field, impacts, PlanetRng.Fork(rng, 0x4F524544u));

            // Ore forms as localized districts: impacts add late-veneer metals, while hydrothermal, volcanic,
            // and ancient-marine chemistry precipitate metals into clustered deposits instead of smearing whole
            // mountain belts. Radius-1 brushes keep most land ore-free and make rich cores easy to recognize.
            for (int i = 0; i < deposits.Length; i++)
            {
                OreDeposit deposit = deposits[i];
                brush.Add(iron, deposit.TileId, deposit.Radius, deposit.IronAbundance, OreClusterDecay);
                brush.Add(precious, deposit.TileId, deposit.Radius, deposit.PreciousMetalAbundance, PreciousClusterDecay);
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
                    ironOre: source.IsLand ? iron[tileId] : 0d,
                    preciousMetal: source.IsLand ? precious[tileId] : 0d,
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

        private static OreDeposit[] ChooseOreDeposits(PlanetField field, PlanetImpactSite[] impacts, XorShiftRng rng)
        {
            int targetCount = OreDepositTargetCount(field);
            if (targetCount == 0)
                return Array.Empty<OreDeposit>();

            var candidates = new List<OreCandidate>();
            AddImpactCandidates(field, impacts, rng, candidates);
            AddConvergentCandidates(field, rng, candidates);
            AddVolcanicCandidates(field, rng, candidates);
            AddAncientSeabedCandidates(field, rng, candidates);
            candidates.Sort(CompareOreCandidates);

            var selected = new List<OreDeposit>(targetCount);
            var selectedCenters = new bool[field.TileCount];
            SelectOreDeposits(field, rng, candidates, targetCount, DepositSpacingFor(field), selected, selectedCenters);
            if (selected.Count < targetCount)
                SelectOreDeposits(field, rng, candidates, targetCount, 1, selected, selectedCenters);

            return selected.ToArray();
        }

        private static int OreDepositTargetCount(PlanetField field)
        {
            int landTiles = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (field.TileAt(tileId).IsLand)
                    landTiles++;
            }

            if (landTiles == 0)
                return 0;
            return Math.Min(landTiles, Math.Max(3, (int)Math.Round(Math.Sqrt(landTiles) * 0.30d, MidpointRounding.AwayFromZero)));
        }

        private static int DepositSpacingFor(PlanetField field)
        {
            return Math.Max(2, Math.Min(5, field.Grid.SubdivisionLevel));
        }

        private static void AddImpactCandidates(PlanetField field, PlanetImpactSite[] impacts, XorShiftRng rng, List<OreCandidate> candidates)
        {
            var iron = new double[field.TileCount];
            var precious = new double[field.TileCount];
            for (int i = 0; i < impacts.Length; i++)
            {
                PlanetImpactSite impact = impacts[i];
                iron[impact.TileId] = Math.Max(iron[impact.TileId], impact.IronAbundance);
                precious[impact.TileId] = Math.Max(precious[impact.TileId], impact.PreciousMetalAbundance);
            }

            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (iron[tileId] <= 0d || !field.TileAt(tileId).IsLand)
                    continue;

                double score = 2.20d + (iron[tileId] * 0.46d) + (precious[tileId] * 0.32d);
                AddCandidate(candidates, rng, tileId, score, 0.72d + (iron[tileId] * 0.24d), 0.18d + (precious[tileId] * 0.60d), 0);
            }
        }

        private static void AddConvergentCandidates(PlanetField field, XorShiftRng rng, List<OreCandidate> candidates)
        {
            var convergence = new double[field.TileCount];
            for (int i = 0; i < field.Boundaries.Edges.Count; i++)
            {
                PlateBoundaryEdge edge = field.Boundaries.Edges[i];
                if (edge.Kind != PlateBoundaryKind.Convergent)
                    continue;

                double signal = Math.Min(1d, edge.Magnitude / field.Parameters.DriftScale);
                convergence[edge.TileA] = Math.Max(convergence[edge.TileA], signal);
                convergence[edge.TileB] = Math.Max(convergence[edge.TileB], signal);
            }

            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                double signal = convergence[tileId];
                if (signal <= 0d || !field.TileAt(tileId).IsLand)
                    continue;

                double height = LandHeightFor(field, tileId);
                double score = 1.58d + (signal * 0.72d) + (height * 0.10d);
                AddCandidate(candidates, rng, tileId, score, 0.62d + (signal * 0.23d), 0.16d + (signal * 0.30d), 1);
            }
        }

        private static void AddVolcanicCandidates(PlanetField field, XorShiftRng rng, List<OreCandidate> candidates)
        {
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                if (!tile.IsLand)
                    continue;

                bool oceanicIsland = field.Plates.Plates[tile.PlateId].Kind == PlateKind.Oceanic;
                if (!oceanicIsland && tile.Biome != PlanetBiome.Mountain)
                    continue;

                double height = LandHeightFor(field, tileId);
                double islandBonus = oceanicIsland ? 0.18d : 0d;
                double score = 1.08d + (height * 0.42d) + islandBonus;
                AddCandidate(candidates, rng, tileId, score, 0.54d + (height * 0.16d), 0.10d + (height * 0.20d) + islandBonus, 2);
            }
        }

        private static void AddAncientSeabedCandidates(PlanetField field, XorShiftRng rng, List<OreCandidate> candidates)
        {
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                if (!tile.IsLand)
                    continue;

                double low = 1d - Clamp01((tile.Elevation - field.Parameters.SeaLevelThreshold) / 0.34d);
                double marine = HasOceanNeighbor(field, tileId) ? 1d : field.Plates.Plates[tile.PlateId].Kind == PlateKind.Oceanic ? 0.64d : 0d;
                double basin = low * marine * FlatnessFor(field, tileId);
                if (basin <= 0d)
                    continue;

                double score = 0.96d + (basin * 0.36d);
                AddCandidate(candidates, rng, tileId, score, 0.58d + (basin * 0.16d), 0.03d + (basin * 0.05d), 3);
            }
        }

        private static void AddCandidate(List<OreCandidate> candidates, XorShiftRng rng, int tileId, double score, double iron, double precious, int sourceRank)
        {
            candidates.Add(new OreCandidate(
                tileId,
                score,
                PlanetRng.NextUnit(rng),
                Clamp01(iron),
                Clamp01(precious),
                sourceRank));
        }

        private static void SelectOreDeposits(
            PlanetField field,
            XorShiftRng rng,
            List<OreCandidate> candidates,
            int targetCount,
            int spacing,
            List<OreDeposit> selected,
            bool[] selectedCenters)
        {
            var spacingSearch = new DepositSpacingSearch(field.Grid);
            for (int i = 0; i < candidates.Count && selected.Count < targetCount; i++)
            {
                OreCandidate candidate = candidates[i];
                if (selectedCenters[candidate.TileId])
                    continue;
                if (spacingSearch.HasMarkedWithin(selectedCenters, candidate.TileId, spacing))
                    continue;

                double iron = Clamp01(candidate.IronAbundance * (0.94d + (PlanetRng.NextUnit(rng) * 0.06d)));
                double precious = Clamp01(candidate.PreciousMetalAbundance * (0.90d + (PlanetRng.NextUnit(rng) * 0.10d)));
                selectedCenters[candidate.TileId] = true;
                selected.Add(new OreDeposit(candidate.TileId, OreClusterRadius, iron, precious));
            }
        }

        private static int CompareOreCandidates(OreCandidate left, OreCandidate right)
        {
            int score = right.Score.CompareTo(left.Score);
            if (score != 0)
                return score;
            int tie = right.TieBreak.CompareTo(left.TieBreak);
            if (tie != 0)
                return tie;
            int source = left.SourceRank.CompareTo(right.SourceRank);
            return source != 0 ? source : left.TileId.CompareTo(right.TileId);
        }

        private static double LandHeightFor(PlanetField field, int tileId)
        {
            return Clamp01((field.TileAt(tileId).Elevation - field.Parameters.SeaLevelThreshold) / 0.92d);
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

        private sealed class DepositSpacingSearch
        {
            private readonly IcosphereGrid _grid;
            private readonly int[] _distance;
            private readonly int[] _queue;
            private readonly int[] _seen;
            private int _stamp;

            public DepositSpacingSearch(IcosphereGrid grid)
            {
                _grid = grid;
                _distance = new int[grid.Count];
                _queue = new int[grid.Count];
                _seen = new int[grid.Count];
            }

            public bool HasMarkedWithin(bool[] marked, int start, int maxDistance)
            {
                _stamp++;
                if (_stamp == int.MaxValue)
                {
                    Array.Clear(_seen, 0, _seen.Length);
                    _stamp = 1;
                }

                int head = 0;
                int tail = 0;
                _queue[tail++] = start;
                _distance[start] = 0;
                _seen[start] = _stamp;

                while (head < tail)
                {
                    int tileId = _queue[head++];
                    int distance = _distance[tileId];
                    if (marked[tileId])
                        return true;
                    if (distance >= maxDistance)
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

                return false;
            }
        }

        private struct OreCandidate
        {
            public OreCandidate(int tileId, double score, double tieBreak, double ironAbundance, double preciousMetalAbundance, int sourceRank)
            {
                TileId = tileId;
                Score = score;
                TieBreak = tieBreak;
                IronAbundance = ironAbundance;
                PreciousMetalAbundance = preciousMetalAbundance;
                SourceRank = sourceRank;
            }

            public int TileId { get; }
            public double Score { get; }
            public double TieBreak { get; }
            public double IronAbundance { get; }
            public double PreciousMetalAbundance { get; }
            public int SourceRank { get; }
        }

        private struct OreDeposit
        {
            public OreDeposit(int tileId, int radius, double ironAbundance, double preciousMetalAbundance)
            {
                TileId = tileId;
                Radius = radius;
                IronAbundance = ironAbundance;
                PreciousMetalAbundance = preciousMetalAbundance;
            }

            public int TileId { get; }
            public int Radius { get; }
            public double IronAbundance { get; }
            public double PreciousMetalAbundance { get; }
        }
    }
}
