using System;
using System.Collections.Generic;
using EmberCrpg.Simulation.Worldgen.Planet;

namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// Continuous surface sampler over the planet icosphere: latitude-band-accelerated nearest-tile lookup
    /// (same recipe as PlanetImageSampler) + inverse-distance blend over the tile and its neighbours, so
    /// elevation and water vary smoothly between ~28km planet tiles. This is what gives the 3D terrain TRUE
    /// coastlines and lakes from the same planet data the map renders, instead of the flattened 128x64
    /// raster. Engine-free and deterministic.
    /// </summary>
    internal sealed class PlanetSurfaceSampler
    {
        private readonly PlanetField _field;
        private readonly List<int>[] _bands;
        private readonly double _bandSize;

        public PlanetSurfaceSampler(PlanetField field)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            int bandCount = Math.Max(8, (int)Math.Round(Math.Sqrt(field.Grid.Count)));
            _bandSize = Math.PI / bandCount;
            _bands = new List<int>[bandCount];
            for (int i = 0; i < bandCount; i++) _bands[i] = new List<int>();
            for (int t = 0; t < field.Grid.Count; t++)
            {
                var p = field.Grid.TileAt(t).Position;
                _bands[BandOf(Math.Asin(Clamp1(p.Y)))].Add(t);
            }
        }

        /// <summary>The planet's own sea level (normalized elevation units) — the single water truth.</summary>
        public double SeaLevel => _field.Parameters.SeaLevelThreshold;

        /// <summary>
        /// Blended surface at lat/lon: ground elevation plus the LOCAL water level (sea everywhere; raised to
        /// the lake surface when the nearest tile is a lake). All values in planet-normalized elevation units.
        /// </summary>
        public void Sample(double lat, double lon, out double elevation, out double waterLevel)
        {
            double cosLat = Math.Cos(lat);
            double x = cosLat * Math.Cos(lon), y = Math.Sin(lat), z = cosLat * Math.Sin(lon);

            int nearest = NearestTile(x, y, z, lat);
            var centerTile = _field.Grid.TileAt(nearest);

            double wSum = 0d, eSum = 0d, lakeLevel = double.NaN;
            BlendTile(nearest, x, y, z, ref wSum, ref eSum, ref lakeLevel);
            var neighbors = centerTile.Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
                BlendTile(neighbors[i], x, y, z, ref wSum, ref eSum, ref lakeLevel);

            elevation = wSum > 0d ? eSum / wSum : _field.TileAt(nearest).Elevation;
            waterLevel = SeaLevel;
            if (_field.TileAt(nearest).IsLake && !double.IsNaN(lakeLevel))
                waterLevel = Math.Max(waterLevel, lakeLevel);
        }

        private void BlendTile(int tileId, double x, double y, double z, ref double wSum, ref double eSum, ref double lakeLevel)
        {
            var p = _field.Grid.TileAt(tileId).Position;
            double dx = p.X - x, dy = p.Y - y, dz = p.Z - z;
            double w = 1d / (((dx * dx) + (dy * dy) + (dz * dz)) + 1e-12d);
            var data = _field.TileAt(tileId);
            wSum += w;
            eSum += w * data.Elevation;
            if (data.IsLake)
                lakeLevel = double.IsNaN(lakeLevel) ? data.Elevation : Math.Max(lakeLevel, data.Elevation);
        }

        private int _lastNearest = -1;

        // Greedy walk over the icosphere adjacency graph: from the previous result (terrain rasters scan
        // coherently, so it is usually 0-2 hops away) climb to the neighbour with the highest dot product
        // until no neighbour improves. On a convex sphere this converges to the TRUE nearest tile, so the
        // result is deterministic and independent of the start point; the band index only seeds cold starts.
        private int NearestTile(double x, double y, double z, double lat)
        {
            int current = _lastNearest >= 0 ? _lastNearest : BandSeed(lat);
            double currentDot = Dot(current, x, y, z);
            while (true)
            {
                var neighbors = _field.Grid.TileAt(current).Neighbors;
                int best = current;
                double bestDot = currentDot;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    double d = Dot(neighbors[i], x, y, z);
                    if (d > bestDot) { bestDot = d; best = neighbors[i]; }
                }
                if (best == current) break;
                current = best;
                currentDot = bestDot;
            }
            _lastNearest = current;
            return current;
        }

        /// <summary>
        /// Finds the nearest WATER tile (ocean !IsLand, or lake) within 3 rings of the tile under lat/lon and
        /// returns the unit bearing toward it (local east/north) plus its water level — what a port or
        /// lakeside settlement needs to realize its OWN shore locally (the Daggerfall location-includes-shore
        /// pattern). False when the location is genuinely inland.
        /// </summary>
        public bool TryFindShore(double lat, double lon, out double bearingEast, out double bearingNorth, out double waterLevel)
        {
            bearingEast = 0d;
            bearingNorth = 0d;
            waterLevel = SeaLevel;
            double cosLat = Math.Cos(lat);
            double x = cosLat * Math.Cos(lon), y = Math.Sin(lat), z = cosLat * Math.Sin(lon);
            int home = NearestTile(x, y, z, lat);

            var visited = new HashSet<int> { home };
            var frontier = new List<int> { home };
            int bestWater = -1;
            double bestDot = double.NegativeInfinity;
            for (int ring = 0; ring < 3; ring++)
            {
                var next = new List<int>();
                for (int f = 0; f < frontier.Count; f++)
                {
                    var nbs = _field.Grid.TileAt(frontier[f]).Neighbors;
                    for (int i = 0; i < nbs.Count; i++)
                    {
                        int t = nbs[i];
                        if (!visited.Add(t)) continue;
                        next.Add(t);
                        var data = _field.TileAt(t);
                        if (data.IsLand && !data.IsLake) continue;
                        double d = Dot(t, x, y, z);
                        if (d > bestDot) { bestDot = d; bestWater = t; }
                    }
                }
                if (bestWater >= 0) break; // the closest ring with water wins
                frontier = next;
            }

            if (bestWater < 0) return false;

            var p = _field.Grid.TileAt(bestWater).Position;
            double wLat = Math.Asin(Clamp1(p.Y));
            double wLon = Math.Atan2(p.Z, p.X);
            double dLon = wLon - lon;
            if (dLon > Math.PI) dLon -= 2d * Math.PI;
            if (dLon < -Math.PI) dLon += 2d * Math.PI;
            double east = dLon * cosLat, north = wLat - lat;
            double len = Math.Sqrt((east * east) + (north * north));
            if (len < 1e-12d) return false;
            bearingEast = east / len;
            bearingNorth = north / len;

            var waterData = _field.TileAt(bestWater);
            waterLevel = waterData.IsLake ? Math.Max(SeaLevel, waterData.Elevation) : SeaLevel;
            return true;
        }

        private double Dot(int tileId, double x, double y, double z)
        {
            var p = _field.Grid.TileAt(tileId).Position;
            return (p.X * x) + (p.Y * y) + (p.Z * z);
        }

        private int BandSeed(double lat)
        {
            var band = _bands[BandOf(lat)];
            return band.Count > 0 ? band[0] : 0;
        }

        private int BandOf(double lat)
        {
            int band = (int)(((Math.PI / 2d) - lat) / _bandSize);
            return band < 0 ? 0 : (band >= _bands.Length ? _bands.Length - 1 : band);
        }

        private static double Clamp1(double v) => v < -1d ? -1d : (v > 1d ? 1d : v);
    }
}
