using System;
using System.Collections.Generic;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Places population centers from local resource, water, terrain, and trade affordances.</summary>
    public sealed class SettlementStage : IPlanetStage
    {
        public const double MinimumFreshWater = 0.22d;
        private const double MinimumSuitability = 0.38d;
        private const double RichOreDepositThreshold = 0.68d;

        public string Name => "Settlements";

        public void Run(PlanetGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Field = Apply(context.RequireField(), context.Fork(PlanetGenerationContext.SettlementStageSeed));
        }

        public PlanetField Apply(PlanetField field, XorShiftRng rng)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            var suitability = new double[field.TileCount];
            var capacity = new double[field.TileCount];
            int bestFallback = -1;
            double bestFallbackScore = double.NegativeInfinity;

            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!IsHabitable(field, tileId))
                    continue;

                suitability[tileId] = SuitabilityFor(field, tileId);
                capacity[tileId] = CarryingCapacityScoreFor(field, tileId);
                if (suitability[tileId] > bestFallbackScore ||
                    (Math.Abs(suitability[tileId] - bestFallbackScore) <= 0.000000001d && tileId < bestFallback))
                {
                    bestFallback = tileId;
                    bestFallbackScore = suitability[tileId];
                }
            }

            var candidates = new List<Candidate>();
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (suitability[tileId] >= MinimumSuitability && IsLocalMaximum(field, suitability, tileId))
                    candidates.Add(new Candidate(tileId, suitability[tileId], capacity[tileId]));
            }

            if (candidates.Count == 0 && bestFallback >= 0)
                candidates.Add(new Candidate(bestFallback, suitability[bestFallback], capacity[bestFallback]));

            candidates.Sort(CompareCandidates);
            int spacing = MinimumSpacingFor(field);
            var occupied = new bool[field.TileCount];
            var spacingSearch = new SpacingSearch(field.Grid);
            var accepted = new List<Candidate>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                if (spacingSearch.HasMarkedWithin(occupied, candidate.TileId, spacing - 1))
                    continue;

                occupied[candidate.TileId] = true;
                accepted.Add(candidate);
            }

            // F5/density-B (user-approved "dengeli"): a SECOND, relaxed-spacing pass grows the pool toward
            // ~150 sites using NON-FOREST candidates only — farm-side counts grow, so the farm>forest mix
            // invariant holds BY CONSTRUCTION (the naive global spacing cut broke it; see MinimumSpacingFor).
            // Primary-band leftovers gave only +7 (the ≥0.38 maxima pool is small): a SECONDARY suitability
            // band (0.30–0.38 local maxima, still non-forest, relaxed pass only) supplies the rest.
            const int TargetSites = 150;
            const double SecondaryBandFloor = 0.30d;
            int relaxedSpacing = Math.Max(2, spacing - 1);

            var relaxedPool = new List<Candidate>(candidates);
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (suitability[tileId] >= SecondaryBandFloor && suitability[tileId] < MinimumSuitability
                    && IsLocalMaximum(field, suitability, tileId))
                    relaxedPool.Add(new Candidate(tileId, suitability[tileId], capacity[tileId]));
            }
            relaxedPool.Sort(CompareCandidates);

            for (int i = 0; i < relaxedPool.Count && accepted.Count < TargetSites; i++)
            {
                Candidate extra = relaxedPool[i];
                if (occupied[extra.TileId]) continue;
                var extraBiome = field.TileAt(extra.TileId).Biome;
                if (extraBiome == PlanetBiome.TemperateForest || extraBiome == PlanetBiome.Taiga
                    || extraBiome == PlanetBiome.TropicalRainforest) continue; // forest hamlets stay capped
                if (spacingSearch.HasMarkedWithin(occupied, extra.TileId, relaxedSpacing - 1)) continue;
                occupied[extra.TileId] = true;
                accepted.Add(extra);
            }

            int capitalIndex = CapitalIndex(accepted);
            var settlements = new PlanetSettlement[accepted.Count];
            for (int i = 0; i < accepted.Count; i++)
            {
                Candidate candidate = accepted[i];
                PlanetSettlementType type = i == capitalIndex ? PlanetSettlementType.Capital : TypeFor(field, candidate.TileId, candidate.Capacity);
                settlements[i] = new PlanetSettlement(
                    candidate.TileId,
                    type,
                    PopulationFor(candidate.Capacity),
                    DominantResourcesFor(field, candidate.TileId));
            }

            return new PlanetField(
                field.Seed,
                field.Parameters,
                field.Grid,
                field.Plates,
                field.Boundaries,
                field.CopyTiles(),
                settlements,
                field.CopyResourceImpacts());
        }

        public static int MinimumSpacingFor(PlanetField field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            // F1/density note: spacing 4 was TRIED to grow the site pool past ~88 — it broke the world's
            // farm>forest settlement-mix invariant (denser pools over-admit forest maxima) and the planet
            // golden digest. The pool stays spacing-5; density rides the late-frontier founding wave, which
            // founds EVERY viable site (~88 on the shipped seed). Growing past that is a real design
            // trade-off (flavor vs density) — escalated to the user with the A/B/C density question.
            return Math.Max(2, Math.Min(6, field.Grid.SubdivisionLevel + 1));
        }

        public static double SuitabilityFor(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            if (!tile.IsLand)
                return 0d;

            double flatness = FlatnessFor(field, tileId);
            double resource = ResourceAccessFor(field, tileId);
            double coast = HasOceanNeighbor(field, tileId) ? 1d : 0d;
            double river = RiverAccessFor(field, tileId);
            double crossroads = CrossroadsScoreFor(field, tileId);
            double comfort = 1d - Clamp01(Math.Abs(tile.Temperature - 0.55d) / 0.55d);

            return Clamp01(
                (tile.FreshWater * 0.27d) +
                (tile.SoilFertility * 0.29d) +
                (resource * 0.12d) +
                (flatness * 0.13d) +
                (river * 0.10d) +
                (coast * 0.06d) +
                (crossroads * 0.05d) +
                (comfort * 0.04d));
        }

        public static double CarryingCapacityScoreFor(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            if (!tile.IsLand)
                return 0d;

            double localSoil = NeighborhoodMean(field, tileId, PlanetResourceKind.SoilFertility);
            double localWater = NeighborhoodMean(field, tileId, PlanetResourceKind.FreshWater);
            double tradeAccess = Math.Max(Math.Max(HasOceanNeighbor(field, tileId) ? 1d : 0d, RiverAccessFor(field, tileId)), CrossroadsScoreFor(field, tileId));
            double resourceIndustry = ResourceAccessFor(field, tileId);

            return Clamp01(
                (localSoil * 0.44d) +
                (localWater * 0.37d) +
                (tradeAccess * 0.12d) +
                (resourceIndustry * 0.08d));
        }

        public static double CrossroadsScoreFor(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            int traversable = 0;
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                PlanetTileField neighbor = field.TileAt(neighbors[i]);
                if (neighbor.IsLand && neighbor.Biome != PlanetBiome.Ice && Math.Abs(neighbor.Elevation - tile.Elevation) <= 0.42d)
                    traversable++;
            }

            return traversable / 6d;
        }

        private static bool IsHabitable(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            return tile.IsLand &&
                tile.Biome != PlanetBiome.Ocean &&
                tile.Biome != PlanetBiome.Ice &&
                tile.FreshWater >= MinimumFreshWater &&
                FlatnessFor(field, tileId) >= 0.12d;
        }

        private static bool IsLocalMaximum(PlanetField field, double[] suitability, int tileId)
        {
            double score = suitability[tileId];
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighbor = neighbors[i];
                double neighborScore = suitability[neighbor];
                if (neighborScore > score + 0.000000001d)
                    return false;
                if (Math.Abs(neighborScore - score) <= 0.000000001d && neighbor < tileId)
                    return false;
            }

            return true;
        }

        private static PlanetSettlementType TypeFor(PlanetField field, int tileId, double capacity)
        {
            PlanetTileField tile = field.TileAt(tileId);
            double ore = MaxAdjacentOre(field, tileId);
            double wood = MaxNearbyResource(field, tileId, PlanetResourceKind.Wood);
            double crossroads = CrossroadsScoreFor(field, tileId);
            bool coast = HasOceanNeighbor(field, tileId);
            bool fertileWatered = tile.SoilFertility >= 0.44d && tile.FreshWater >= 0.30d;
            bool richOre = ore >= RichOreDepositThreshold;

            // Genesis settlements are farming-first: agriculture predates Iron-Age mining, so ore can found a
            // mining town only when a directly present rich deposit clearly dominates ordinary farm value.
            if (richOre && (!fertileWatered || ore >= tile.SoilFertility + 0.18d))
                return PlanetSettlementType.MiningTown;
            if (coast && capacity >= 0.55d && tile.FreshWater >= 0.38d)
                return PlanetSettlementType.Port;
            if (crossroads >= 0.88d && capacity >= 0.50d && tile.SoilFertility < 0.72d)
                return PlanetSettlementType.MarketTown;
            if (wood >= 0.62d && wood >= tile.SoilFertility * 0.80d)
                return PlanetSettlementType.ForestHamlet;
            if (fertileWatered)
                return PlanetSettlementType.FarmVillage;
            if (tile.SoilFertility >= 0.36d || tile.FreshWater >= 0.50d)
                return PlanetSettlementType.FarmVillage;
            if (coast)
                return PlanetSettlementType.Port;
            if (crossroads >= 0.78d)
                return PlanetSettlementType.MarketTown;
            if (wood >= 0.44d)
                return PlanetSettlementType.ForestHamlet;
            return PlanetSettlementType.FarmVillage;
        }

        private static PlanetResourceKind[] DominantResourcesFor(PlanetField field, int tileId)
        {
            var scores = new ResourceScore[(int)PlanetResourceKind.FreshWater + 1];
            for (int i = 0; i < scores.Length; i++)
            {
                var kind = (PlanetResourceKind)i;
                scores[i] = new ResourceScore(kind, MaxNearbyResource(field, tileId, kind));
            }

            Array.Sort(scores, CompareResourceScores);
            return new[] { scores[0].Kind, scores[1].Kind, scores[2].Kind };
        }

        private static int PopulationFor(double capacity)
        {
            return 45 + (int)Math.Round(Math.Pow(Clamp01(capacity), 1.55d) * 18000d, MidpointRounding.AwayFromZero);
        }

        private static int CapitalIndex(List<Candidate> accepted)
        {
            int best = -1;
            double bestCapacity = double.NegativeInfinity;
            for (int i = 0; i < accepted.Count; i++)
            {
                Candidate candidate = accepted[i];
                if (candidate.Capacity > bestCapacity ||
                    (Math.Abs(candidate.Capacity - bestCapacity) <= 0.000000001d && (best < 0 || candidate.TileId < accepted[best].TileId)))
                {
                    best = i;
                    bestCapacity = candidate.Capacity;
                }
            }

            return best;
        }

        private static double ResourceAccessFor(PlanetField field, int tileId)
        {
            double ore = Math.Max(MaxNearbyResource(field, tileId, PlanetResourceKind.IronOre), MaxNearbyResource(field, tileId, PlanetResourceKind.PreciousMetal));
            double industrial = Math.Max(Math.Max(ore, MaxNearbyResource(field, tileId, PlanetResourceKind.Coal)), MaxNearbyResource(field, tileId, PlanetResourceKind.OilGas));
            double wood = MaxNearbyResource(field, tileId, PlanetResourceKind.Wood);
            double stone = MaxNearbyResource(field, tileId, PlanetResourceKind.Stone);
            double clay = MaxNearbyResource(field, tileId, PlanetResourceKind.Clay);
            return Clamp01((industrial * 0.32d) + (wood * 0.22d) + (stone * 0.16d) + (clay * 0.08d));
        }

        private static double MaxAdjacentOre(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            double best = Math.Max(tile.IronOre, tile.PreciousMetal);
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                PlanetTileField neighbor = field.TileAt(neighbors[i]);
                best = Math.Max(best, Math.Max(neighbor.IronOre, neighbor.PreciousMetal));
            }

            return best;
        }

        private static double MaxNearbyResource(PlanetField field, int tileId, PlanetResourceKind kind)
        {
            double best = ResourceValue(field.TileAt(tileId), kind);
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighbor = neighbors[i];
                best = Math.Max(best, ResourceValue(field.TileAt(neighbor), kind));

                var secondRing = field.Grid.TileAt(neighbor).Neighbors;
                for (int j = 0; j < secondRing.Count; j++)
                    best = Math.Max(best, ResourceValue(field.TileAt(secondRing[j]), kind));
            }

            return best;
        }

        private static double NeighborhoodMean(PlanetField field, int tileId, PlanetResourceKind kind)
        {
            double sum = ResourceValue(field.TileAt(tileId), kind);
            int count = 1;
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                sum += ResourceValue(field.TileAt(neighbors[i]), kind);
                count++;
            }

            return sum / count;
        }

        private static double ResourceValue(PlanetTileField tile, PlanetResourceKind kind)
        {
            switch (kind)
            {
                case PlanetResourceKind.IronOre:
                    return tile.IronOre;
                case PlanetResourceKind.PreciousMetal:
                    return tile.PreciousMetal;
                case PlanetResourceKind.Coal:
                    return tile.Coal;
                case PlanetResourceKind.OilGas:
                    return tile.OilGas;
                case PlanetResourceKind.Stone:
                    return tile.Stone;
                case PlanetResourceKind.Clay:
                    return tile.Clay;
                case PlanetResourceKind.Wood:
                    return tile.Wood;
                case PlanetResourceKind.SoilFertility:
                    return tile.SoilFertility;
                case PlanetResourceKind.FreshWater:
                    return tile.FreshWater;
                default:
                    return 0d;
            }
        }

        private static double RiverAccessFor(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            if (tile.IsRiver || tile.IsLake)
                return 1d;

            double best = 0d;
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                PlanetTileField neighbor = field.TileAt(neighbors[i]);
                if (neighbor.IsRiver || neighbor.IsLake)
                    best = Math.Max(best, 0.72d);
            }

            return best;
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

        private static int CompareCandidates(Candidate left, Candidate right)
        {
            int score = right.Score.CompareTo(left.Score);
            if (score != 0)
                return score;
            int capacity = right.Capacity.CompareTo(left.Capacity);
            return capacity != 0 ? capacity : left.TileId.CompareTo(right.TileId);
        }

        private static int CompareResourceScores(ResourceScore left, ResourceScore right)
        {
            int score = right.Score.CompareTo(left.Score);
            return score != 0 ? score : left.Kind.CompareTo(right.Kind);
        }

        private static double Clamp01(double value)
        {
            if (value < 0d)
                return 0d;
            if (value > 1d)
                return 1d;
            return value;
        }

        private struct Candidate
        {
            public Candidate(int tileId, double score, double capacity)
            {
                TileId = tileId;
                Score = score;
                Capacity = capacity;
            }

            public int TileId { get; }
            public double Score { get; }
            public double Capacity { get; }
        }

        private struct ResourceScore
        {
            public ResourceScore(PlanetResourceKind kind, double score)
            {
                Kind = kind;
                Score = score;
            }

            public PlanetResourceKind Kind { get; }
            public double Score { get; }
        }

        private sealed class SpacingSearch
        {
            private readonly IcosphereGrid _grid;
            private readonly int[] _distance;
            private readonly int[] _queue;
            private readonly int[] _seen;
            private int _stamp;

            public SpacingSearch(IcosphereGrid grid)
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
    }
}
