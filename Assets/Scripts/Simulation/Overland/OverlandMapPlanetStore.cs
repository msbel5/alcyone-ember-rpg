using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Worldgen.Planet;

namespace EmberCrpg.Simulation.Overland
{
    /// <summary>
    /// Sidecar store attaching the full-resolution <see cref="PlanetField"/> to an overland map (same
    /// pattern as OverlandMapGeographyStore): the map stays pure domain data, while renderers/samplers can
    /// reach the rich planet source it was projected from instead of the flattened 128x64 raster.
    /// </summary>
    internal static class OverlandMapPlanetStore
    {
        private static readonly ConditionalWeakTable<OverlandMap, PlanetField> Fields =
            new ConditionalWeakTable<OverlandMap, PlanetField>();

        public static void Register(OverlandMap map, PlanetField field)
        {
            if (map == null || field == null) return; // legacy non-planet worlds simply have no sidecar
            Fields.Remove(map);
            Fields.Add(map, field);
        }

        public static bool TryGet(OverlandMap map, out PlanetField field)
        {
            field = null;
            return map != null && Fields.TryGetValue(map, out field);
        }
    }

    /// <summary>
    /// Renders the rich planet-sourced atlas for an overland map — the organic icosphere look the old planet
    /// maps had, instead of the blocky overland raster. Same equirect frame as the tile projection (row 0 =
    /// north), so it is drop-in compatible with the marker math. Per-map, per-size cached; returns false for
    /// legacy worlds with no planet sidecar (callers fall back to OverlandMapImageSampler).
    /// </summary>
    public static class PlanetAtlas
    {
        private sealed class SizeCache
        {
            public readonly Dictionary<long, PlanetImage> BySize = new Dictionary<long, PlanetImage>();
        }

        private static readonly ConditionalWeakTable<OverlandMap, SizeCache> Caches =
            new ConditionalWeakTable<OverlandMap, SizeCache>();

        public static bool TryRender(OverlandMap map, int width, int height, out PlanetImage image)
        {
            image = null;
            if (width <= 0 || height <= 0) return false;
            if (!OverlandMapPlanetStore.TryGet(map, out var field)) return false;

            var cache = Caches.GetValue(map, _ => new SizeCache());
            long key = ((long)width << 32) | (uint)height;
            if (cache.BySize.TryGetValue(key, out image)) return true;

            // No baked settlement dots: the UI pins are the ONLY marker layer (Daggerfall rule — the map
            // image carries terrain truth, markers are drawn from data on top, so they can never disagree).
            var sampled = PlanetImageSampler.Sample(field, width, height, stampSettlements: false);
            // PlanetImageSampler emits row 0 = NORTH, but Unity's LoadRawTextureData puts the FIRST byte row
            // at the BOTTOM of the texture (the overland raster already obeys that convention, which is why
            // its markers aligned). Flip to south-first here so the planet atlas is a drop-in replacement —
            // without this the displayed map is vertically mirrored and every pin reads "in the sea".
            var flipped = new byte[sampled.Rgba.Length];
            int stride = width * 4;
            for (int row = 0; row < height; row++)
                System.Array.Copy(sampled.Rgba, row * stride, flipped, (height - 1 - row) * stride, stride);
            image = new PlanetImage(width, height, flipped);
            cache.BySize[key] = image;
            return true;
        }

        /// <summary>
        /// Ore richness (0..1) of the icosphere tile under an overland cell — drives local mine realization
        /// (content phase: the planet's resource layer reaching the ground). False for legacy worlds.
        /// </summary>
        public static bool TryGetTileOre(OverlandMap map, int tileX, int tileY, out double ironOre, out double coal)
        {
            ironOre = 0d;
            coal = 0d;
            if (!OverlandMapPlanetStore.TryGet(map, out var field)) return false;

            double lat = (System.Math.PI / 2d) - (((tileY + 0.5d) / map.Height) * System.Math.PI);
            double lon = (((tileX + 0.5d) / map.Width) * 2d * System.Math.PI) - System.Math.PI;
            double cosLat = System.Math.Cos(lat);
            double dx = cosLat * System.Math.Cos(lon), dy = System.Math.Sin(lat), dz = cosLat * System.Math.Sin(lon);

            var grid = field.Grid;
            int best = 0;
            double bestDot = double.NegativeInfinity;
            for (int i = 0; i < grid.Count; i++)
            {
                var p = grid.TileAt(i).Position;
                double dot = (p.X * dx) + (p.Y * dy) + (p.Z * dz);
                if (dot > bestDot) { bestDot = dot; best = i; }
            }

            var data = field.TileAt(best);
            ironOre = data.IronOre;
            coal = data.Coal;
            return true;
        }

        private static readonly ConditionalWeakTable<OverlandMap, Dictionary<int, (float x, float y)>> AnchorCache =
            new ConditionalWeakTable<OverlandMap, Dictionary<int, (float x, float y)>>();

        /// <summary>
        /// ONE map truth (the Daggerfall lesson: every layer keys off the same grid). Anchors a tile's map
        /// marker to the lat/lon of the ICOSPHERE TILE its overland cell centre maps to — the same nearest-tile
        /// rule the atlas pixels use — so the dot is guaranteed to sit on its own rendered land instead of a
        /// coarse cell centre whose pixel can belong to a neighbouring ocean tile. Percent is in atlas space
        /// (x: 0=west..100=east, y: 0=north..100=south). False for legacy worlds without a planet sidecar.
        /// </summary>
        public static bool TryGetTileAnchorPercent(OverlandMap map, int tileX, int tileY, out float xPercent, out float yPercent)
        {
            xPercent = 0f;
            yPercent = 0f;
            if (!OverlandMapPlanetStore.TryGet(map, out var field)) return false;

            var cache = AnchorCache.GetValue(map, _ => new Dictionary<int, (float x, float y)>());
            int cacheKey = (tileY * map.Width) + tileX;
            if (cache.TryGetValue(cacheKey, out var cached))
            {
                xPercent = cached.x;
                yPercent = cached.y;
                return true;
            }

            // Same equirect cell-centre formula the atlas/geography projection uses, then the same
            // nearest-icosphere-tile rule (max dot product over the unit sphere).
            double lat = (System.Math.PI / 2d) - (((tileY + 0.5d) / map.Height) * System.Math.PI);
            double lon = (((tileX + 0.5d) / map.Width) * 2d * System.Math.PI) - System.Math.PI;
            double cosLat = System.Math.Cos(lat);
            double dx = cosLat * System.Math.Cos(lon), dy = System.Math.Sin(lat), dz = cosLat * System.Math.Sin(lon);

            var grid = field.Grid;
            int best = 0;
            double bestDot = double.NegativeInfinity;
            for (int i = 0; i < grid.Count; i++)
            {
                var p = grid.TileAt(i).Position;
                double dot = (p.X * dx) + (p.Y * dy) + (p.Z * dz);
                if (dot > bestDot) { bestDot = dot; best = i; }
            }

            // A coastal cell's centre can resolve to an OCEAN tile (the baked dots used to mask this); the
            // settlement is on land by construction, so anchor to the nearest LAND tile in the 2-ring
            // neighbourhood instead. The predicate MUST be the painter's actual ocean rule —
            // PlanetImageSampler.Color paints ocean iff !IsLand (lake/river tiles are land-side inland
            // water and are fine to pin on). An elevation-threshold predicate here left pins stranded on
            // ocean-painted tiles whose elevation sat above the threshold.
            if (!field.TileAt(best).IsLand)
            {
                double bestLandDot = double.NegativeInfinity;
                int land = -1;
                var ring1 = grid.TileAt(best).Neighbors;
                for (int i = 0; i < ring1.Count; i++)
                {
                    var ring2 = grid.TileAt(ring1[i]).Neighbors;
                    for (int j = -1; j < ring2.Count; j++)
                    {
                        int cand = j < 0 ? ring1[i] : ring2[j];
                        if (!field.TileAt(cand).IsLand) continue;
                        var q = grid.TileAt(cand).Position;
                        double d = (q.X * dx) + (q.Y * dy) + (q.Z * dz);
                        if (d > bestLandDot) { bestLandDot = d; land = cand; }
                    }
                }
                if (land >= 0) best = land;
            }

            var pos = grid.TileAt(best).Position;
            double tileLat = System.Math.Asin(System.Math.Max(-1d, System.Math.Min(1d, pos.Y)));
            double tileLon = System.Math.Atan2(pos.Z, pos.X);
            xPercent = (float)(((tileLon + System.Math.PI) / (2d * System.Math.PI)) * 100d);
            yPercent = (float)((((System.Math.PI / 2d) - tileLat) / System.Math.PI) * 100d);
            cache[cacheKey] = (xPercent, yPercent);
            return true;
        }
    }
}
