using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Applies bounded stream-power and talus smoothing passes.</summary>
    public sealed class ErosionStage
    {
        private const int StreamPasses = 3;
        private const int ThermalPasses = 3;
        private const int FinalStreamPasses = 2;
        private const double TalusLimit = 0.16d;
        private const double TalusTransferRate = 0.025d;

        public PlanetField Apply(PlanetField field, XorShiftRng rng)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            var elevation = new double[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
                elevation[tileId] = field.TileAt(tileId).Elevation;

            for (int pass = 0; pass < StreamPasses; pass++)
                ApplyStreamPower(field, elevation);
            for (int pass = 0; pass < ThermalPasses; pass++)
                ApplyThermalTalus(field, elevation);
            for (int pass = 0; pass < FinalStreamPasses; pass++)
                ApplyStreamPower(field, elevation);

            var tiles = new PlanetTileField[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField source = field.TileAt(tileId);
                bool isLand = elevation[tileId] >= field.Parameters.SeaLevelThreshold;
                tiles[tileId] = source.CopyWith(
                    elevation: elevation[tileId],
                    isLand: isLand,
                    biome: isLand ? source.Biome : PlanetBiome.Ocean,
                    flow: isLand ? source.Flow : 0d,
                    isRiver: isLand && source.IsRiver,
                    isLake: isLand && source.IsLake);
            }

            return new PlanetField(field.Seed, field.Parameters, field.Grid, field.Plates, field.Boundaries, tiles);
        }

        private static void ApplyStreamPower(PlanetField field, double[] elevation)
        {
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                if (!tile.IsLand || tile.Flow <= 0d)
                    continue;

                int downstream = LowestNeighbor(field, elevation, tileId);
                if (downstream < 0)
                    continue;

                double slope = elevation[tileId] - elevation[downstream];
                if (slope <= 0d)
                    continue;

                double flowScale = Math.Min(1d, Math.Sqrt(tile.Flow) * 0.050d);
                double cut = Math.Min(slope * 0.38d, slope * flowScale);
                elevation[tileId] -= cut;
            }
        }

        private static void ApplyThermalTalus(PlanetField field, double[] elevation)
        {
            var delta = new double[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!field.TileAt(tileId).IsLand)
                    continue;

                var neighbors = field.Grid.TileAt(tileId).Neighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int neighbor = neighbors[i];
                    if (neighbor <= tileId || !field.TileAt(neighbor).IsLand)
                        continue;

                    double slope = elevation[tileId] - elevation[neighbor];
                    double excess = Math.Abs(slope) - TalusLimit;
                    if (excess <= 0d)
                        continue;

                    double transfer = excess * TalusTransferRate;
                    if (slope > 0d)
                    {
                        delta[tileId] -= transfer;
                        delta[neighbor] += transfer;
                    }
                    else
                    {
                        delta[tileId] += transfer;
                        delta[neighbor] -= transfer;
                    }
                }
            }

            for (int tileId = 0; tileId < elevation.Length; tileId++)
                elevation[tileId] += delta[tileId];
        }

        private static int LowestNeighbor(PlanetField field, double[] elevation, int tileId)
        {
            int best = -1;
            double bestElevation = elevation[tileId];
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighbor = neighbors[i];
                double candidate = elevation[neighbor];
                if (candidate < bestElevation || (Math.Abs(candidate - bestElevation) <= 0.000000001d && best >= 0 && neighbor < best))
                {
                    best = neighbor;
                    bestElevation = candidate;
                }
            }

            return best;
        }
    }
}
