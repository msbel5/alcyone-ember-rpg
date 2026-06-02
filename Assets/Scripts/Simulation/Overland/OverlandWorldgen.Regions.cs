using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Overland
{
    public static partial class OverlandWorldgen
    {
        private static SeedPoint<RegionId>[] BuildRegionSeeds(XorShiftRng rng, System.Collections.Generic.IReadOnlyList<RegionRecord> regions, int width, int height)
        {
            var indices = BuildShuffledTileIndices(rng, width, height);
            var seeds = new SeedPoint<RegionId>[regions.Count];
            for (int i = 0; i < regions.Count; i++)
            {
                int tileIndex = indices[i];
                int x = tileIndex % width;
                int y = tileIndex / width;
                seeds[i] = new SeedPoint<RegionId>(x, y, regions[i].Id, i);
            }

            return seeds;
        }

        private static RegionId[] AssignRegions(int width, int height, SeedPoint<RegionId>[] seeds)
        {
            var result = new RegionId[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    result[ToIndex(x, y, width)] = NearestRegion(x, y, seeds);
            }

            return result;
        }

        private static RegionId NearestRegion(int x, int y, SeedPoint<RegionId>[] seeds)
        {
            int bestDistance = int.MaxValue;
            var bestRegion = seeds[0].Value;
            int bestOrder = seeds[0].Order;
            for (int i = 0; i < seeds.Length; i++)
            {
                int dx = x - seeds[i].X;
                int dy = y - seeds[i].Y;
                int distance = (dx * dx) + (dy * dy);
                if (distance < bestDistance || (distance == bestDistance && seeds[i].Order < bestOrder))
                {
                    bestDistance = distance;
                    bestRegion = seeds[i].Value;
                    bestOrder = seeds[i].Order;
                }
            }

            return bestRegion;
        }

        private static int[] BuildShuffledTileIndices(XorShiftRng rng, int width, int height)
        {
            int count = width * height;
            var indices = new int[count];
            for (int i = 0; i < count; i++)
                indices[i] = i;

            for (int i = count - 1; i > 0; i--)
            {
                int swapIndex = rng.NextInt(i + 1);
                int temp = indices[i];
                indices[i] = indices[swapIndex];
                indices[swapIndex] = temp;
            }

            return indices;
        }
    }
}
