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
            var rng = new XorShiftRng(normalizedSeed);

            var regionSeeds = BuildRegionSeeds(rng, world.Regions, parameters.Width, parameters.Height);
            var biomeSeeds = BuildBiomeSeeds(rng, parameters.Width, parameters.Height, parameters.BiomeSeedCount);
            var regionIds = AssignRegions(parameters.Width, parameters.Height, regionSeeds);
            var biomes = AssignBiomes(parameters.Width, parameters.Height, biomeSeeds);
            SmoothSingleTileIslands(parameters.Width, parameters.Height, biomes);

            var settlements = PlaceSettlements(normalizedSeed, parameters, world, regionIds, biomes);
            var tileSeeds = RollTileSeeds(normalizedSeed, parameters.Width * parameters.Height);
            var tiles = BuildTiles(parameters, regionIds, biomes, tileSeeds, settlements);

            return new OverlandMap(parameters.Width, parameters.Height, tiles, settlements);
        }

        private static RegionTile[] BuildTiles(
            OverlandParameters parameters,
            RegionId[] regionIds,
            BiomeKind[] biomes,
            uint[] tileSeeds,
            IReadOnlyList<OverlandSettlement> settlements)
        {
            var settlementIdsByTile = new List<SettlementId>[parameters.Width * parameters.Height];
            for (int i = 0; i < settlementIdsByTile.Length; i++)
                settlementIdsByTile[i] = new List<SettlementId>();

            for (int i = 0; i < settlements.Count; i++)
            {
                var settlement = settlements[i];
                int tileIndex = ToIndex(settlement.TilePosition.X, settlement.TilePosition.Y, parameters.Width);
                settlementIdsByTile[tileIndex].Add(settlement.Id);
            }

            var tiles = new RegionTile[parameters.Width * parameters.Height];
            for (int y = 0; y < parameters.Height; y++)
            {
                for (int x = 0; x < parameters.Width; x++)
                {
                    int index = ToIndex(x, y, parameters.Width);
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

        private readonly struct SeedPoint<TValue>
        {
            public SeedPoint(int x, int y, TValue value, int order)
            {
                X = x;
                Y = y;
                Value = value;
                Order = order;
            }

            public int X { get; }
            public int Y { get; }
            public TValue Value { get; }
            public int Order { get; }
        }
    }
}
