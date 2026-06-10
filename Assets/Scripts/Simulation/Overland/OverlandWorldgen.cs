using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.Worldgen;
// Domain.Worldgen also declares a (climate) BiomeKind; the overland map uses the PRD 8-biome set.
using BiomeKind = EmberCrpg.Domain.Overland.BiomeKind;

namespace EmberCrpg.Simulation.Overland
{
    /// <summary>Pure deterministic entry point that adds a traversable overland grid on top of GeneratedWorld ids.</summary>
    public static partial class OverlandWorldgen
    {
        private const uint FallbackSeed = 42u;

        public static OverlandMap Generate(uint seed, OverlandParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            uint normalizedSeed = seed == 0u ? FallbackSeed : seed;
            var world = WorldgenService.Generate(normalizedSeed, WorldgenParameters.Default);
            return Generate(world, parameters);
        }

        public static OverlandMap Generate(GeneratedWorld world, OverlandParameters parameters)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            uint normalizedSeed = world.Seed == 0u ? FallbackSeed : world.Seed;
            var geography = world.Geography ?? WorldGeographyProvider.Generate(normalizedSeed, world.Regions.Count, world.Regions);
            EnsureMatchingParameters(geography, parameters);

            var regionIds = geography.CopyRegionIds();
            var biomes = geography.CopyOverlandBiomes();
            var settlements = ProjectSettlements(world.Settlements, geography);
            var tileSeeds = RollTileSeeds(normalizedSeed, geography.TileCount);
            var tiles = BuildTiles(geography.Width, geography.Height, regionIds, biomes, tileSeeds, settlements);
            var map = new OverlandMap(geography.Width, geography.Height, tiles, settlements);
            OverlandMapGeographyStore.Register(map, geography);
            return map;
        }

        private static void EnsureMatchingParameters(WorldGeography geography, OverlandParameters parameters)
        {
            if (parameters.Width != geography.Width || parameters.Height != geography.Height)
            {
                throw new ArgumentException(
                    "Overland parameters must match the GeneratedWorld geography dimensions for direct world projection.",
                    nameof(parameters));
            }
        }

        private static RegionTile[] BuildTiles(
            int width,
            int height,
            RegionId[] regionIds,
            BiomeKind[] biomes,
            uint[] tileSeeds,
            IReadOnlyList<OverlandSettlement> settlements)
        {
            var settlementIdsByTile = new List<SettlementId>[width * height];
            for (int i = 0; i < settlementIdsByTile.Length; i++)
                settlementIdsByTile[i] = new List<SettlementId>();

            for (int i = 0; i < settlements.Count; i++)
            {
                var settlement = settlements[i];
                int tileIndex = ToIndex(settlement.TilePosition.X, settlement.TilePosition.Y, width);
                settlementIdsByTile[tileIndex].Add(settlement.Id);
            }

            var tiles = new RegionTile[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = ToIndex(x, y, width);
                    tiles[index] = new RegionTile(
                        x,
                        y,
                        regionIds[index],
                        biomes[index],
                        settlementIdsByTile[index],
                        tileSeeds[index],
                        DetermineClimate(biomes[index]));
                }
            }

            return tiles;
        }

        private static uint[] RollTileSeeds(uint seed, int count)
        {
            var rng = new XorShiftRng(seed ^ 0xA511E9B3u);
            var result = new uint[count];
            for (int i = 0; i < count; i++)
                result[i] = (uint)rng.NextInt(int.MaxValue);
            return result;
        }

        private static ClimateKind DetermineClimate(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.Coast: return ClimateKind.Maritime;
                case BiomeKind.Swamp: return ClimateKind.Wetland;
                case BiomeKind.Mountain: return ClimateKind.Highland;
                case BiomeKind.Desert: return ClimateKind.Arid;
                case BiomeKind.Tundra: return ClimateKind.Polar;
                case BiomeKind.Ash: return ClimateKind.Ashen;
                default: return ClimateKind.Temperate;
            }
        }

        private static int ToIndex(int x, int y, int width)
        {
            return (y * width) + x;
        }
    }
}
