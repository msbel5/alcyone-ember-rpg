using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Overland
{
    public static partial class OverlandWorldgen
    {
        private static readonly BiomeKind[] AllBiomes =
        {
            BiomeKind.Plains,
            BiomeKind.Forest,
            BiomeKind.Mountain,
            BiomeKind.Coast,
            BiomeKind.Swamp,
            BiomeKind.Desert,
            BiomeKind.Tundra,
            BiomeKind.Ash,
        };

        private static SeedPoint<BiomeKind>[] BuildBiomeSeeds(XorShiftRng rng, int width, int height, int count)
        {
            var indices = BuildShuffledTileIndices(rng, width, height);
            var biomeOrder = (BiomeKind[])AllBiomes.Clone();
            ShuffleBiomes(rng, biomeOrder);

            var result = new SeedPoint<BiomeKind>[count];
            for (int i = 0; i < count; i++)
            {
                int tileIndex = indices[i];
                int x = tileIndex % width;
                int y = tileIndex / width;
                var biome = i < biomeOrder.Length ? biomeOrder[i] : RollBiome(rng);
                result[i] = new SeedPoint<BiomeKind>(x, y, biome, i);
            }

            return result;
        }

        private static BiomeKind[] AssignBiomes(int width, int height, SeedPoint<BiomeKind>[] seeds)
        {
            var result = new BiomeKind[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    result[ToIndex(x, y, width)] = NearestSeedValue(x, y, seeds);
            }

            return result;
        }

        private static void SmoothSingleTileIslands(int width, int height, BiomeKind[] biomes)
        {
            var scratch = new BiomeKind[biomes.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < biomes.Length; i++)
                    scratch[i] = biomes[i];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = ToIndex(x, y, width);
                        BiomeKind current = biomes[index];
                        int currentCount = CountMatchingNeighbors(width, height, biomes, x, y, current);
                        if (currentCount > 0)
                            continue;

                        var dominant = current;
                        int bestCount = -1;
                        for (int ny = y - 1; ny <= y + 1; ny++)
                        {
                            for (int nx = x - 1; nx <= x + 1; nx++)
                            {
                                if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                                    continue;

                                var neighbor = biomes[ToIndex(nx, ny, width)];
                                int neighborCount = CountMatchingNeighbors(width, height, biomes, x, y, neighbor);
                                if (neighborCount > bestCount)
                                {
                                    dominant = neighbor;
                                    bestCount = neighborCount;
                                }
                            }
                        }

                        scratch[index] = dominant;
                    }
                }

                for (int i = 0; i < biomes.Length; i++)
                    biomes[i] = scratch[i];
            }
        }

        private static int CountMatchingNeighbors(int width, int height, BiomeKind[] biomes, int x, int y, BiomeKind biome)
        {
            int count = 0;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    if (biomes[ToIndex(nx, ny, width)] == biome)
                        count++;
                }
            }

            return count;
        }

        private static BiomeKind RollBiome(XorShiftRng rng)
        {
            int roll = rng.NextInt(100);
            if (roll < 24) return BiomeKind.Plains;
            if (roll < 42) return BiomeKind.Forest;
            if (roll < 55) return BiomeKind.Coast;
            if (roll < 67) return BiomeKind.Mountain;
            if (roll < 76) return BiomeKind.Desert;
            if (roll < 84) return BiomeKind.Swamp;
            if (roll < 92) return BiomeKind.Tundra;
            return BiomeKind.Ash;
        }

        private static void ShuffleBiomes(XorShiftRng rng, BiomeKind[] values)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int swapIndex = rng.NextInt(i + 1);
                var temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }

        private static BiomeKind NearestSeedValue(int x, int y, SeedPoint<BiomeKind>[] seeds)
        {
            int bestDistance = int.MaxValue;
            var bestBiome = seeds[0].Value;
            int bestOrder = seeds[0].Order;
            for (int i = 0; i < seeds.Length; i++)
            {
                int dx = x - seeds[i].X;
                int dy = y - seeds[i].Y;
                int distance = (dx * dx) + (dy * dy);
                if (distance < bestDistance || (distance == bestDistance && seeds[i].Order < bestOrder))
                {
                    bestDistance = distance;
                    bestBiome = seeds[i].Value;
                    bestOrder = seeds[i].Order;
                }
            }

            return bestBiome;
        }
    }
}
