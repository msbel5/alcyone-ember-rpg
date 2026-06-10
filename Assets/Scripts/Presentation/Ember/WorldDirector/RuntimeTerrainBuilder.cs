using System.Collections.Generic;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.WorldDirector;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Builds ONE seamless terrain TILE for the streaming world. With a <see cref="WorldGeoSampler"/> the
    /// heightmap is the SAME continuous geography function the atlas map renders (bicubic world elevation +
    /// deterministic detail, sea/beach aware) — the walkable ground IS the map. Splat layers: biome ground,
    /// beach sand near/below the sea, rock on steep slopes; tiles that dip under sea level get a translucent
    /// water plane at the exact sea height. Without a sampler (no geography registered, e.g. standalone
    /// scenes) it falls back to the legacy origin-flat Perlin hills so older scenes keep working.
    ///
    /// MEMORY: per-biome layer/material assets are cached; the per-tile TerrainData is freed by the streamer
    /// on unload (that explicit free is what stopped the "RAM grows as you walk" stutter).
    /// </summary>
    public static class RuntimeTerrainBuilder
    {
        private const int HeightmapRes = 129;   // must be 2^n + 1; keeps per-tile gen cheap for streaming
        private const int AlphamapRes = 65;
        private const float LegacyHeight = 26f; // vertical range of the old Perlin fallback
        private const float FlatRadius = 55f;
        private const float HillRampMetres = 220f;
        private const float GeoYMin = -150f;    // vertical window (relative to home ground) for geo terrain
        private const float GeoYMax = 550f;

        private static readonly Dictionary<BiomeKind, BiomeAssets> BiomeCache = new Dictionary<BiomeKind, BiomeAssets>();

        public static GameObject BuildTile(
            Transform parent, int tileX, int tileZ, float tileSize, BiomeKind biome, uint seed, WorldGeoSampler sampler = null)
        {
            bool geo = sampler != null;
            var data = new TerrainData
            {
                heightmapResolution = HeightmapRes,
                alphamapResolution = AlphamapRes,
                size = new Vector3(tileSize, geo ? GeoYMax - GeoYMin : LegacyHeight, tileSize),
            };

            float minElev = 0f;
            data.SetHeights(0, 0, geo
                ? BuildGeoHeights(sampler, tileX, tileZ, tileSize, out minElev)
                : BuildLegacyHeights(tileX, tileZ, tileSize, seed));

            var assets = GetBiomeAssets(biome);
            if (geo)
            {
                data.terrainLayers = new[]
                {
                    assets.Layer,
                    GetBiomeAssets(BiomeKind.Coast).Layer,    // beach sand
                    GetBiomeAssets(BiomeKind.Mountain).Layer, // steep rock
                };
                data.SetAlphamaps(0, 0, BuildGeoAlpha(sampler, tileX, tileZ, tileSize));
            }
            else
            {
                data.terrainLayers = new[] { assets.Layer };
                data.SetAlphamaps(0, 0, FullCoverAlpha());
            }

            var go = Terrain.CreateTerrainGameObject(data); // also adds a TerrainCollider
            go.name = $"TerrainTile_{tileX}_{tileZ}";
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = new Vector3(tileX * tileSize, geo ? GeoYMin : 0f, tileZ * tileSize);

            if (assets.Material != null)
                go.GetComponent<Terrain>().materialTemplate = assets.Material;

            if (geo && minElev < (float)sampler.SeaLevelMeters + 0.05f)
                AddWaterSurface(go.transform, tileSize, (float)sampler.SeaLevelMeters - GeoYMin);
            return go;
        }

        // Heights from the world geography function, normalized into the [GeoYMin, GeoYMax] window. The
        // terrain object sits at y = GeoYMin so heightmap 0..1 maps back to true relative metres.
        private static float[,] BuildGeoHeights(WorldGeoSampler sampler, int tileX, int tileZ, float tileSize, out float minElev)
        {
            var heights = new float[HeightmapRes, HeightmapRes];
            float step = tileSize / (HeightmapRes - 1);
            minElev = float.MaxValue;
            for (int y = 0; y < HeightmapRes; y++)
            {
                for (int x = 0; x < HeightmapRes; x++)
                {
                    float e = (float)sampler.Sample((tileX * tileSize) + (x * step), (tileZ * tileSize) + (y * step)).ElevationMeters;
                    if (e < minElev) minElev = e;
                    heights[y, x] = Mathf.Clamp01((e - GeoYMin) / (GeoYMax - GeoYMin));
                }
            }
            return heights;
        }

        // Splat weights: sand from the sampler's beach band, rock from local slope, biome ground otherwise.
        private static float[,,] BuildGeoAlpha(WorldGeoSampler sampler, int tileX, int tileZ, float tileSize)
        {
            var alpha = new float[AlphamapRes, AlphamapRes, 3];
            float step = tileSize / (AlphamapRes - 1);
            for (int y = 0; y < AlphamapRes; y++)
            {
                for (int x = 0; x < AlphamapRes; x++)
                {
                    double wx = (tileX * tileSize) + (x * step), wz = (tileZ * tileSize) + (y * step);
                    var s = sampler.Sample(wx, wz);
                    float dx = (float)(sampler.Sample(wx + step, wz).ElevationMeters - s.ElevationMeters);
                    float dz = (float)(sampler.Sample(wx, wz + step).ElevationMeters - s.ElevationMeters);
                    float slope = Mathf.Sqrt((dx * dx) + (dz * dz)) / step;

                    float sand = (float)s.SandBlend01;
                    float rock = Mathf.Clamp01((slope - 0.45f) / 0.30f) * (1f - sand);
                    alpha[y, x, 1] = sand;
                    alpha[y, x, 2] = rock;
                    alpha[y, x, 0] = Mathf.Max(0f, 1f - sand - rock);
                }
            }
            return alpha;
        }

        // Translucent sea/lake plane at the exact sampler sea height (OpenMW recipe: flat quad, no collider).
        private static void AddWaterSurface(Transform tile, float tileSize, float localY)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane); // Unity Plane = 10m across at scale 1
            water.name = "Water";
            var collider = water.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider); // visual surface only; swimming is future work
            water.transform.SetParent(tile, worldPositionStays: false);
            water.transform.localPosition = new Vector3(tileSize * 0.5f, localY, tileSize * 0.5f);
            water.transform.localScale = new Vector3(tileSize / 10f, 1f, tileSize / 10f);
            var renderer = water.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = RuntimeMaterialPalette.Water();
        }

        // Legacy continuous height function (origin-flat + Perlin hills) for scenes without world geography.
        private static float[,] BuildLegacyHeights(int tileX, int tileZ, float tileSize, uint seed)
        {
            var heights = new float[HeightmapRes, HeightmapRes];
            float metresPerSample = tileSize / (HeightmapRes - 1);
            float ox = seed % 619, oz = seed % 397;
            for (int y = 0; y < HeightmapRes; y++)
            {
                for (int x = 0; x < HeightmapRes; x++)
                {
                    float wx = (tileX * tileSize) + (x * metresPerSample);
                    float wz = (tileZ * tileSize) + (y * metresPerSample);
                    float dist = Mathf.Sqrt((wx * wx) + (wz * wz));
                    if (dist <= FlatRadius) { heights[y, x] = 0f; continue; }

                    float ramp = Mathf.Clamp01((dist - FlatRadius) / HillRampMetres);
                    float n = Mathf.PerlinNoise((wx * 0.012f) + ox, (wz * 0.012f) + oz);
                    heights[y, x] = ramp * ramp * (0.10f + (0.55f * n));
                }
            }
            return heights;
        }

        // Create (once) and cache the biome's terrain layer + URP material, so streamed tiles share them.
        private static BiomeAssets GetBiomeAssets(BiomeKind biome)
        {
            if (BiomeCache.TryGetValue(biome, out var cached)) return cached;

            var texture = RuntimeMaterialPalette.LoadGeneratedTexture(RuntimeMaterialPalette.GroundTextureId(biome))
                          ?? SolidTexture(RuntimeMaterialPalette.GroundColor(biome));
            var layer = new TerrainLayer { diffuseTexture = texture, tileSize = new Vector2(14f, 14f) };

            // The terrain shader is force-included at build time (Windows64BuildMenu.EnsureRuntimeShadersIncluded),
            // but fall back robustly so the ground is NEVER the magenta "missing shader" colour.
            var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            Material material;
            if (shader != null)
            {
                material = new Material(shader);
            }
            else
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = lit != null ? new Material(lit) : null;
                if (material != null && texture != null)
                {
                    material.mainTexture = texture;
                    if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                    if (material.HasProperty("_BaseMap_ST")) material.SetTextureScale("_BaseMap", new Vector2(18f, 18f));
                }
            }

            var assets = new BiomeAssets(layer, material);
            BiomeCache[biome] = assets;
            return assets;
        }

        private static float[,,] FullCoverAlpha()
        {
            var alpha = new float[AlphamapRes, AlphamapRes, 1];
            for (int y = 0; y < AlphamapRes; y++)
                for (int x = 0; x < AlphamapRes; x++)
                    alpha[y, x, 0] = 1f;
            return alpha;
        }

        private static Texture2D SolidTexture(Color color)
        {
            var t = new Texture2D(2, 2) { wrapMode = TextureWrapMode.Repeat };
            t.SetPixels(new[] { color, color, color, color });
            t.Apply();
            return t;
        }

        private readonly struct BiomeAssets
        {
            public BiomeAssets(TerrainLayer layer, Material material) { Layer = layer; Material = material; }
            public TerrainLayer Layer { get; }
            public Material Material { get; }
        }
    }
}
