using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Computes latitude temperature, wind-borne moisture, and Whittaker-style biomes.</summary>
    public sealed class ClimateStage : IPlanetStage
    {
        private const double SeaMoisture = 1d;

        public string Name => "Climate";

        public void Run(PlanetGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Field = Apply(context.RequireField(), context.Fork(PlanetGenerationContext.ClimateStageSeed));
        }

        public PlanetField Apply(PlanetField field, XorShiftRng rng)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            var temperature = new double[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
                temperature[tileId] = TemperatureAt(field.Grid.TileAt(tileId).Position, field.TileAt(tileId).Elevation, field.Parameters.SeaLevelThreshold);

            double[] moisture = ComputeMoisture(field);
            var tiles = new PlanetTileField[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField source = field.TileAt(tileId);
                double tileMoisture = source.IsLand ? moisture[tileId] : SeaMoisture;
                PlanetBiome biome = BiomeFor(source.IsLand, source.Elevation, field.Parameters.SeaLevelThreshold, temperature[tileId], tileMoisture);
                tiles[tileId] = source.CopyWith(
                    temperature: temperature[tileId],
                    moisture: tileMoisture,
                    biome: biome);
            }

            return new PlanetField(field.Seed, field.Parameters, field.Grid, field.Plates, field.Boundaries, tiles);
        }

        public static double TemperatureAt(PlanetVector position, double elevation, double seaLevel)
        {
            double latitude = Math.Asin(Clamp(position.Y, -1d, 1d));
            double poleDistance = Math.Abs(latitude) / (Math.PI * 0.5d);
            double insolation = 1d - Math.Pow(poleDistance, 1.35d);
            double lapse = Math.Max(0d, elevation - seaLevel) * 0.32d;
            return Clamp01((insolation * 0.94d) + 0.04d - lapse);
        }

        public static PlanetVector PrevailingWind(PlanetVector position)
        {
            var north = new PlanetVector(0d, 1d, 0d);
            PlanetVector east = PlanetVector.Cross(north, position);
            if (east.Length <= 0.000000001d)
                east = new PlanetVector(1d, 0d, 0d);
            else
                east = east.Normalize();

            double latitudeDegrees = Math.Abs(Math.Asin(Clamp(position.Y, -1d, 1d)) * 180d / Math.PI);
            bool westerly = latitudeDegrees >= 30d && latitudeDegrees < 60d;
            return westerly ? east : east.Scale(-1d);
        }

        private static double[] ComputeMoisture(PlanetField field)
        {
            int steps = 10 + (field.Grid.SubdivisionLevel * 7);
            var airborne = new double[field.TileCount];
            var precipitation = new double[field.TileCount];

            for (int tileId = 0; tileId < field.TileCount; tileId++)
                airborne[tileId] = field.TileAt(tileId).IsLand ? 0d : SeaMoisture;

            for (int step = 0; step < steps; step++)
            {
                var next = new double[field.TileCount];
                for (int tileId = 0; tileId < field.TileCount; tileId++)
                {
                    PlanetTileField source = field.TileAt(tileId);
                    double carried = source.IsLand ? airborne[tileId] : Math.Max(airborne[tileId], SeaMoisture);
                    if (carried <= 0.000000001d)
                        continue;

                    int target = DownwindNeighbor(field.Grid, tileId);
                    double climb = Math.Max(0d, field.TileAt(target).Elevation - source.Elevation);
                    double depositRate = Clamp(0.055d + (climb * 0.90d), 0.055d, 0.62d);
                    double deposited = carried * depositRate;
                    precipitation[tileId] += deposited * 0.40d;
                    precipitation[target] += deposited * 0.60d;

                    double remaining = (carried - deposited) * 0.78d;
                    next[target] += remaining;
                    if (!source.IsLand)
                        next[tileId] = Math.Max(next[tileId], SeaMoisture);
                }

                for (int tileId = 0; tileId < next.Length; tileId++)
                {
                    if (next[tileId] > SeaMoisture)
                        next[tileId] = SeaMoisture;
                }

                airborne = next;
            }

            var moisture = new double[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!field.TileAt(tileId).IsLand)
                {
                    moisture[tileId] = SeaMoisture;
                    continue;
                }

                PlanetVector position = field.Grid.TileAt(tileId).Position;
                double equatorBias = 1d - (Math.Abs(Math.Asin(Clamp(position.Y, -1d, 1d))) / (Math.PI * 0.5d));
                // Baseline humidity so deep interiors aren't bone-dry (real continents still rain inland):
                // a small floor + stronger latitude (equator) term, and divide the orographic precipitation by
                // less so coasts/uplands read wetter. Rain-shadow ordering (windward > leeward) is preserved
                // because the precipitation term still carries the orographic signal.
                double wetness = (precipitation[tileId] / Math.Max(1d, steps * 0.05d)) + (airborne[tileId] * 0.22d) + (equatorBias * 0.16d) + 0.08d;
                moisture[tileId] = Clamp01(wetness);
            }

            return moisture;
        }

        private static int DownwindNeighbor(IcosphereGrid grid, int tileId)
        {
            PlanetVector position = grid.TileAt(tileId).Position;
            PlanetVector wind = PrevailingWind(position);
            int best = grid.TileAt(tileId).Neighbors[0];
            double bestDot = double.NegativeInfinity;

            var neighbors = grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighborId = neighbors[i];
                PlanetVector direction = grid.TileAt(neighborId).Position.Subtract(position);
                double length = direction.Length;
                double dot = length <= 0.000000001d ? double.NegativeInfinity : PlanetVector.Dot(direction.Scale(1d / length), wind);
                if (dot > bestDot || (Math.Abs(dot - bestDot) <= 0.000000001d && neighborId < best))
                {
                    bestDot = dot;
                    best = neighborId;
                }
            }

            return best;
        }

        private static PlanetBiome BiomeFor(bool isLand, double elevation, double seaLevel, double temperature, double moisture)
        {
            if (!isLand)
                return PlanetBiome.Ocean;
            if (elevation >= seaLevel + 0.92d)
                return PlanetBiome.Mountain;
            if (temperature <= 0.10d)
                return PlanetBiome.Ice;
            if (temperature <= 0.22d)
                return PlanetBiome.Tundra;
            if (temperature <= 0.38d && moisture >= 0.38d)
                return PlanetBiome.Taiga;
            if (moisture <= 0.12d)
                return PlanetBiome.Desert;
            if (temperature >= 0.72d && moisture >= 0.68d)
                return PlanetBiome.TropicalRainforest;
            if (temperature >= 0.62d && moisture >= 0.30d)
                return PlanetBiome.Savanna;
            if (moisture >= 0.52d)
                return PlanetBiome.TemperateForest;
            return moisture >= 0.18d ? PlanetBiome.Grassland : PlanetBiome.Desert;
        }

        private static double Clamp01(double value)
        {
            return Clamp(value, 0d, 1d);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
                return minimum;
            if (value > maximum)
                return maximum;
            return value;
        }
    }
}
