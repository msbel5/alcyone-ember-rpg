using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Adds seeded 3D FBM relief without replacing plate-scale structure.</summary>
    public sealed class ElevationNoise
    {
        private const int Octaves = 5;
        // Lower base frequency = continent-scale lobes; amplitude raised to ~0.42 so it is COMPARABLE to the
        // plate step (oceanic -0.34 -> continental +0.20 = 0.54). Below that, noise could never push a
        // continental tile under sea level or an oceanic tile above it, so coastlines were forced onto the plate
        // polygons. At this amplitude bays cut into land and islands rise offshore -> organic, noise-driven
        // coastlines, while the tectonic mountains (elevation up to ~1.2) still dominate the high ground.
        private const double BaseFrequency = 1.7d;
        private const double Lacunarity = 2.05d;
        private const double Gain = 0.52d;
        private const double Amplitude = 0.42d;

        public PlanetField Apply(PlanetField field, XorShiftRng rng)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            uint noiseSeed = (uint)rng.NextInt(int.MaxValue);
            double offsetX = PlanetRng.NextSignedUnit(rng) * 97d;
            double offsetY = PlanetRng.NextSignedUnit(rng) * 97d;
            double offsetZ = PlanetRng.NextSignedUnit(rng) * 97d;
            var tiles = new PlanetTileField[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField source = field.TileAt(tileId);
                PlanetVector position = field.Grid.TileAt(tileId).Position;
                double relief = Fbm(position, noiseSeed, offsetX, offsetY, offsetZ) * Amplitude;
                double elevation = source.Elevation + relief;
                tiles[tileId] = source.CopyWith(
                    elevation: elevation,
                    isLand: elevation >= field.Parameters.SeaLevelThreshold);
            }

            return new PlanetField(field.Seed, field.Parameters, field.Grid, field.Plates, field.Boundaries, tiles);
        }

        private static double Fbm(PlanetVector position, uint seed, double offsetX, double offsetY, double offsetZ)
        {
            double frequency = BaseFrequency;
            double amplitude = 1d;
            double sum = 0d;
            double amplitudeSum = 0d;

            for (int octave = 0; octave < Octaves; octave++)
            {
                double x = (position.X * frequency) + offsetX;
                double y = (position.Y * frequency) + offsetY;
                double z = (position.Z * frequency) + offsetZ;
                sum += ValueNoise(x, y, z, seed + (uint)(octave * 0x9E37u)) * amplitude;
                amplitudeSum += amplitude;
                frequency *= Lacunarity;
                amplitude *= Gain;
            }

            return amplitudeSum <= 0d ? 0d : sum / amplitudeSum;
        }

        private static double ValueNoise(double x, double y, double z, uint seed)
        {
            int x0 = FloorToInt(x);
            int y0 = FloorToInt(y);
            int z0 = FloorToInt(z);
            int x1 = x0 + 1;
            int y1 = y0 + 1;
            int z1 = z0 + 1;

            double sx = Fade(x - x0);
            double sy = Fade(y - y0);
            double sz = Fade(z - z0);

            double ix00 = Lerp(HashToSignedUnit(seed, x0, y0, z0), HashToSignedUnit(seed, x1, y0, z0), sx);
            double ix10 = Lerp(HashToSignedUnit(seed, x0, y1, z0), HashToSignedUnit(seed, x1, y1, z0), sx);
            double ix01 = Lerp(HashToSignedUnit(seed, x0, y0, z1), HashToSignedUnit(seed, x1, y0, z1), sx);
            double ix11 = Lerp(HashToSignedUnit(seed, x0, y1, z1), HashToSignedUnit(seed, x1, y1, z1), sx);

            double iy0 = Lerp(ix00, ix10, sy);
            double iy1 = Lerp(ix01, ix11, sy);
            return Lerp(iy0, iy1, sz);
        }

        private static int FloorToInt(double value)
        {
            int truncated = (int)value;
            return value < truncated ? truncated - 1 : truncated;
        }

        private static double Fade(double t)
        {
            return t * t * t * (t * ((t * 6d) - 15d) + 10d);
        }

        private static double Lerp(double from, double to, double t)
        {
            return from + ((to - from) * t);
        }

        private static double HashToSignedUnit(uint seed, int x, int y, int z)
        {
            uint bits = Hash(seed, x, y, z);
            return (bits / 2147483648d) - 1d;
        }

        private static uint Hash(uint seed, int x, int y, int z)
        {
            unchecked
            {
                uint h = seed ^ 0xA341316Cu;
                h ^= (uint)x * 0x9E3779B9u;
                h = RotateLeft(h, 13);
                h ^= (uint)y * 0x85EBCA6Bu;
                h = RotateLeft(h, 17);
                h ^= (uint)z * 0xC2B2AE35u;
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return h;
            }
        }

        private static uint RotateLeft(uint value, int bits)
        {
            return (value << bits) | (value >> (32 - bits));
        }
    }
}
