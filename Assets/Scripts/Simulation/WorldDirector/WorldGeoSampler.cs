using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Overland;

namespace EmberCrpg.Simulation.WorldDirector
{
    public readonly struct GeoSample
    {
        public GeoSample(double elevationMeters, bool isWater, double sandBlend01, double waterSurfaceMeters)
        {
            ElevationMeters = elevationMeters;
            IsWater = isWater;
            SandBlend01 = sandBlend01;
            WaterSurfaceMeters = waterSurfaceMeters;
        }

        public double ElevationMeters { get; }
        public bool IsWater { get; }
        public double SandBlend01 { get; }

        /// <summary>Local water level (sea, or the lake surface when standing in a lake tile), relative metres.</summary>
        public double WaterSurfaceMeters { get; }
    }

    /// <summary>
    /// Continuous world-space geography function: (x,z metres) → elevation / water / beach. With a planet
    /// sidecar (OverlandMapPlanetStore) it samples the SAME icosphere the map renders — true coastlines and
    /// LAKES at ~28km feature scale — falling back to bicubic over the 128x64 WorldGeography raster for
    /// legacy worlds. Elevation is RELATIVE to the home settlement's ground (origin = 0) so the player rig /
    /// building y≈0 contract holds; the local water surface is exposed per sample. Engine-free, deterministic.
    /// </summary>
    public sealed class WorldGeoSampler
    {
        private const double HeightScaleMeters = 600d;  // full 0..1 geography elevation range, in metres
        private const double DetailAmpMeters = 12d;     // local rolling-hill noise on top of the coarse field
        private const double FlatRadiusMeters = 60d;    // settlement pad stays exactly at home ground level
        private const double FlattenRampMeters = 220d;
        private const double BeachBandMeters = 4d;

        private readonly OverlandMapGeographySnapshot _geo;
        private readonly OverlandMap _map;
        private readonly PlanetSurfaceSampler _surface; // null on the legacy non-planet path
        private readonly int _homeX;
        private readonly int _homeY;
        private readonly double _homeElev;
        private readonly uint _seed;

        /// <summary>Sea surface height relative to the home settlement ground (usually negative inland).</summary>
        public double SeaLevelMeters { get; }

        public static bool TryCreate(OverlandMap map, GridPosition homeTile, uint seed, out WorldGeoSampler sampler)
        {
            sampler = null;
            if (map == null || !OverlandMapGeographyStore.TryGet(map, out var geo)) return false;
            OverlandMapPlanetStore.TryGet(map, out var planet); // optional rich source
            sampler = new WorldGeoSampler(map, geo, planet == null ? null : new PlanetSurfaceSampler(planet), homeTile, seed);
            return true;
        }

        private WorldGeoSampler(OverlandMap map, OverlandMapGeographySnapshot geo, PlanetSurfaceSampler surface, GridPosition home, uint seed)
        {
            _map = map;
            _geo = geo;
            _surface = surface;
            _homeX = home.X;
            _homeY = home.Y;
            _seed = seed == 0u ? 1u : seed;

            if (_surface != null)
            {
                SurfaceAt(home.X + 0.5d, home.Y + 0.5d, out _homeElev, out _);
                SeaLevelMeters = (_surface.SeaLevel - _homeElev) * HeightScaleMeters;
            }
            else
            {
                _homeElev = Bicubic(home.X + 0.5d, home.Y + 0.5d);
                SeaLevelMeters = (ComputeSeaReference(geo) - _homeElev) * HeightScaleMeters;
            }
        }

        public GeoSample Sample(double worldXMeters, double worldZMeters)
        {
            double tx = WorldSpaceProjection.TileFracX(_homeX, worldXMeters);
            double ty = WorldSpaceProjection.TileFracY(_homeY, worldZMeters);

            double meters, waterY;
            if (_surface != null)
            {
                SurfaceAt(tx, ty, out double elev, out double water);
                meters = (elev - _homeElev) * HeightScaleMeters;
                waterY = (water - _homeElev) * HeightScaleMeters;
            }
            else
            {
                meters = (Bicubic(tx, ty) - _homeElev) * HeightScaleMeters;
                waterY = SeaLevelMeters;
            }

            // Detail noise fades on the settlement pad and damps near the water line so beaches stay
            // readable; the whole field then blends to home ground (0) inside the pad.
            double dist = Math.Sqrt((worldXMeters * worldXMeters) + (worldZMeters * worldZMeters));
            double settleBlend = SmoothRamp(dist);
            double coastDamp = Math.Max(0.25d, Clamp01(Math.Abs(meters - waterY) / 10d));
            meters += DetailNoise(worldXMeters, worldZMeters) * DetailAmpMeters * settleBlend * coastDamp;
            meters *= settleBlend;

            double aboveWater = meters - waterY;
            double sand = aboveWater <= 0d ? 1d : Clamp01(1d - (aboveWater / BeachBandMeters));
            return new GeoSample(meters, aboveWater < 0d, sand, waterY);
        }

        /// <summary>Dominant biome for the overland tile under the given world position (water → Coast).</summary>
        public BiomeKind BiomeAt(double worldXMeters, double worldZMeters)
        {
            int tx = (int)Math.Floor(WorldSpaceProjection.TileFracX(_homeX, worldXMeters));
            int ty = (int)Math.Floor(WorldSpaceProjection.TileFracY(_homeY, worldZMeters));
            if (!_geo.IsLand(tx, ty)) return BiomeKind.Coast;
            return _map.TryGetTile(WrapX(tx), ClampY(ty), out var tile) ? tile.Biome : BiomeKind.Plains;
        }

        // Tile-fraction → equirect lat/lon, IDENTICAL to the atlas/projection convention (row 0 = north,
        // lon -π at x=0), then the planet surface blend. One projection, one truth.
        private void SurfaceAt(double tileFracX, double tileFracY, out double elevation, out double waterLevel)
        {
            double lat = (Math.PI / 2d) - ((tileFracY / _map.Height) * Math.PI);
            double lon = ((tileFracX / _map.Width) * 2d * Math.PI) - Math.PI;
            _surface.Sample(lat, lon, out elevation, out waterLevel);
        }

        // Sea reference for LEGACY worlds is data-driven (midpoint between the highest water tile and the
        // lowest land tile); planet worlds use the planet's own SeaLevelThreshold instead.
        private static double ComputeSeaReference(OverlandMapGeographySnapshot geo)
        {
            double maxWater = double.NegativeInfinity, minLand = double.PositiveInfinity;
            for (int y = 0; y < geo.Height; y++)
            {
                for (int x = 0; x < geo.Width; x++)
                {
                    double e = geo.Elevation(x, y);
                    if (geo.IsLand(x, y)) { if (e < minLand) minLand = e; }
                    else if (e > maxWater) maxWater = e;
                }
            }

            if (double.IsNegativeInfinity(maxWater)) return minLand - 0.02d; // all-land world: sea below everything
            if (double.IsPositiveInfinity(minLand)) return maxWater + 0.02d;
            return (maxWater + minLand) * 0.5d;
        }

        // Catmull-Rom bicubic over the geography grid; samples live at tile centres, snapshot wraps X / clamps Y.
        private double Bicubic(double tileFracX, double tileFracY)
        {
            double u = tileFracX - 0.5d, v = tileFracY - 0.5d;
            int x0 = (int)Math.Floor(u), y0 = (int)Math.Floor(v);
            double fx = u - x0, fy = v - y0;

            double r0 = Row(x0, y0 - 1, fx);
            double r1 = Row(x0, y0, fx);
            double r2 = Row(x0, y0 + 1, fx);
            double r3 = Row(x0, y0 + 2, fx);
            return CatmullRom(r0, r1, r2, r3, fy);
        }

        private double Row(int x0, int y, double fx)
            => CatmullRom(_geo.Elevation(x0 - 1, y), _geo.Elevation(x0, y), _geo.Elevation(x0 + 1, y), _geo.Elevation(x0 + 2, y), fx);

        private double DetailNoise(double x, double z)
            => (ValueNoise(x / 190d, z / 190d, _seed) * 0.7d) + (ValueNoise(x / 47d, z / 47d, _seed ^ 0x9E37u) * 0.3d);

        private static double ValueNoise(double x, double z, uint seed)
        {
            int xi = (int)Math.Floor(x), zi = (int)Math.Floor(z);
            double fx = SmoothStep(x - xi), fz = SmoothStep(z - zi);
            double a = Hash01(xi, zi, seed), b = Hash01(xi + 1, zi, seed);
            double c = Hash01(xi, zi + 1, seed), d = Hash01(xi + 1, zi + 1, seed);
            return (Lerp(Lerp(a, b, fx), Lerp(c, d, fx), fz) * 2d) - 1d;
        }

        private static double Hash01(int x, int z, uint seed)
        {
            unchecked
            {
                uint h = seed;
                h ^= (uint)x * 0x9E3779B1u; h = (h ^ (h >> 15)) * 0x85EBCA6Bu;
                h ^= (uint)z * 0xC2B2AE35u; h = (h ^ (h >> 13)) * 0x27D4EB2Fu;
                h ^= h >> 16;
                return h / 4294967296d;
            }
        }

        private static double SmoothRamp(double dist)
        {
            double t = Clamp01((dist - FlatRadiusMeters) / FlattenRampMeters);
            return t * t * (3d - (2d * t));
        }

        private static double CatmullRom(double p0, double p1, double p2, double p3, double t)
            => p1 + (0.5d * t * (p2 - p0 + (t * ((2d * p0) - (5d * p1) + (4d * p2) - p3 + (t * ((3d * (p1 - p2)) + p3 - p0))))));

        private static double SmoothStep(double t) => t * t * (3d - (2d * t));
        private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
        private static double Clamp01(double v) => v < 0d ? 0d : (v > 1d ? 1d : v);

        private int WrapX(int x)
        {
            int w = _map.Width;
            int r = x % w;
            return r < 0 ? r + w : r;
        }

        private int ClampY(int y) => y < 0 ? 0 : (y >= _map.Height ? _map.Height - 1 : y);
    }
}
