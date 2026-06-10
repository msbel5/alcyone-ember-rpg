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

            image = PlanetImageSampler.Sample(field, width, height);
            cache.BySize[key] = image;
            return true;
        }
    }
}
