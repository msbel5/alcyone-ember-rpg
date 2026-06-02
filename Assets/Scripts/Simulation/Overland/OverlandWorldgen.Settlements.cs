using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.Worldgen;
// Domain.Worldgen also declares a (climate) BiomeKind; the overland map uses the PRD 8-biome set.
using BiomeKind = EmberCrpg.Domain.Overland.BiomeKind;

namespace EmberCrpg.Simulation.Overland
{
    public static partial class OverlandWorldgen
    {
        private static IReadOnlyList<OverlandSettlement> PlaceSettlements(
            uint seed,
            OverlandParameters parameters,
            GeneratedWorld world,
            RegionId[] regionIds,
            BiomeKind[] biomes)
        {
            int targetCount = EstimateSettlementCapacity(parameters, biomes);
            if (targetCount > world.Settlements.Count)
                targetCount = world.Settlements.Count;

            var selected = SelectSettlements(seed, world.Settlements, targetCount);
            var tileGroups = GroupTilesByRegion(parameters.Width, parameters.Height, regionIds);
            var occupancy = new int[parameters.Width * parameters.Height];
            var placements = new List<OverlandSettlement>(selected.Count);
            var kindRng = new XorShiftRng(seed ^ 0xB5297A4Du);

            for (int i = 0; i < selected.Count; i++)
            {
                var record = selected[i];
                int tileIndex = ChooseSettlementTile(parameters, record, tileGroups, biomes, occupancy, placements);
                occupancy[tileIndex]++;

                int x = tileIndex % parameters.Width;
                int y = tileIndex / parameters.Width;
                var biome = biomes[tileIndex];
                var kind = ClassifySettlementKind(record.Size, biome, kindRng.NextInt(100));
                placements.Add(new OverlandSettlement(record.Id, kind, new GridPosition(x, y), record.Name, TemplatePackTag(kind)));
            }

            return placements;
        }

        private static int EstimateSettlementCapacity(OverlandParameters parameters, BiomeKind[] biomes)
        {
            double capacity = 0d;
            for (int i = 0; i < biomes.Length; i++)
                capacity += BiomeDensityWeight(biomes[i]) * parameters.SettlementDensity;

            int rounded = (int)System.Math.Round(capacity, System.MidpointRounding.AwayFromZero);
            return rounded < 12 ? 12 : rounded;
        }

        private static List<SettlementRecord> SelectSettlements(uint seed, IReadOnlyList<SettlementRecord> settlements, int targetCount)
        {
            var rng = new XorShiftRng(seed ^ 0x314D159Du);
            var ranked = new List<ScoredSettlement>(settlements.Count);
            for (int i = 0; i < settlements.Count; i++)
            {
                var settlement = settlements[i];
                uint score = ((uint)SettlementPriority(settlement.Size) * 100000u) + (uint)rng.NextInt(100000);
                ranked.Add(new ScoredSettlement(settlement, score));
            }

            ranked.Sort((left, right) =>
            {
                int scoreCompare = right.Score.CompareTo(left.Score);
                return scoreCompare != 0 ? scoreCompare : left.Record.Id.Value.CompareTo(right.Record.Id.Value);
            });

            var selected = new List<SettlementRecord>(targetCount);
            for (int i = 0; i < targetCount; i++)
                selected.Add(ranked[i].Record);
            return selected;
        }

        private static Dictionary<ulong, List<int>> GroupTilesByRegion(int width, int height, RegionId[] regionIds)
        {
            var result = new Dictionary<ulong, List<int>>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = ToIndex(x, y, width);
                    ulong key = regionIds[index].Value;
                    if (!result.TryGetValue(key, out var tiles))
                    {
                        tiles = new List<int>();
                        result.Add(key, tiles);
                    }

                    tiles.Add(index);
                }
            }

            return result;
        }

        private static int ChooseSettlementTile(
            OverlandParameters parameters,
            SettlementRecord record,
            Dictionary<ulong, List<int>> tileGroups,
            BiomeKind[] biomes,
            int[] occupancy,
            List<OverlandSettlement> placements)
        {
            var candidates = tileGroups[record.Region.Value];
            int bestTile = candidates[0];
            int bestScore = int.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                int tileIndex = candidates[i];
                int x = tileIndex % parameters.Width;
                int y = tileIndex / parameters.Width;
                int score = ScoreTile(parameters, record.Size, biomes[tileIndex], x, y, occupancy[tileIndex], placements);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTile = tileIndex;
                }
            }

            return bestTile;
        }

        private static int ScoreTile(
            OverlandParameters parameters,
            SettlementSize size,
            BiomeKind biome,
            int x,
            int y,
            int occupancy,
            List<OverlandSettlement> placements)
        {
            int score = (int)(BiomeSettlementWeight(biome, size) * 1000d);
            score -= occupancy * 160;

            int centerX = parameters.Width / 2;
            int centerY = parameters.Height / 2;
            int centerDistance = System.Math.Abs(centerX - x) + System.Math.Abs(centerY - y);

            if (placements.Count == 0)
                score += 90 - (centerDistance * 8);
            else
                score += 110 - (NearestPlacedDistance(x, y, placements) * 12);

            if (size == SettlementSize.Capital || size == SettlementSize.City)
                score += 40 - (centerDistance * 2);

            return score;
        }

        private static int NearestPlacedDistance(int x, int y, List<OverlandSettlement> placements)
        {
            int best = int.MaxValue;
            var here = new GridPosition(x, y);
            for (int i = 0; i < placements.Count; i++)
            {
                int distance = OverlandMap.ChebyshevDistance(here, placements[i].TilePosition);
                if (distance < best)
                    best = distance;
            }

            return best;
        }

        private static double BiomeDensityWeight(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.Plains: return 1.20d;
                case BiomeKind.Forest: return 0.95d;
                case BiomeKind.Coast: return 1.05d;
                case BiomeKind.Mountain: return 0.55d;
                case BiomeKind.Swamp: return 0.45d;
                case BiomeKind.Desert: return 0.35d;
                case BiomeKind.Tundra: return 0.40d;
                default: return 0.30d;
            }
        }

        private static double BiomeSettlementWeight(BiomeKind biome, SettlementSize size)
        {
            double weight = BiomeDensityWeight(biome);
            if (size == SettlementSize.Capital || size == SettlementSize.City)
            {
                if (biome == BiomeKind.Plains || biome == BiomeKind.Coast || biome == BiomeKind.Forest) weight += 0.30d;
                if (biome == BiomeKind.Ash || biome == BiomeKind.Desert || biome == BiomeKind.Tundra) weight -= 0.25d;
            }
            else if (size == SettlementSize.Hamlet || size == SettlementSize.Village)
            {
                if (biome == BiomeKind.Mountain || biome == BiomeKind.Ash || biome == BiomeKind.Swamp) weight += 0.10d;
            }

            return weight;
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

        private static int SettlementPriority(SettlementSize size)
        {
            switch (size)
            {
                case SettlementSize.Capital: return 5;
                case SettlementSize.City: return 4;
                case SettlementSize.Town: return 3;
                case SettlementSize.Village: return 2;
                default: return 1;
            }
        }

        private readonly struct ScoredSettlement
        {
            public ScoredSettlement(SettlementRecord record, uint score)
            {
                Record = record;
                Score = score;
            }

            public SettlementRecord Record { get; }
            public uint Score { get; }
        }
    }
}
