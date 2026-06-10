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
            if (sampler != null)
                return BuildTileFromPrecompute(parent, tileX, tileZ, tileSize, biome, Precompute(sampler, tileX, tileZ, tileSize));

            var data = new TerrainData
            {
                heightmapResolution = HeightmapRes,
                alphamapResolution = AlphamapRes,
                size = new Vector3(tileSize, LegacyHeight, tileSize),
            };
            data.SetHeights(0, 0, BuildLegacyHeights(tileX, tileZ, tileSize, seed));
            var assets = GetBiomeAssets(biome);
            data.terrainLayers = new[] { assets.Layer };
            data.SetAlphamaps(0, 0, FullCoverAlpha());

            var go = Terrain.CreateTerrainGameObject(data); // also adds a TerrainCollider
            go.name = $"TerrainTile_{tileX}_{tileZ}";
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = new Vector3(tileX * tileSize, 0f, tileZ * tileSize);
            if (assets.Material != null)
                go.GetComponent<Terrain>().materialTemplate = assets.Material;
            return go;
        }

        /// <summary>Pure-C# half of a geo tile (heights + splats + water level): NO Unity scene APIs, so the
        /// streamer can run it on a background thread and walking never pays the 129x129 sampling spike in a
        /// single frame ("oyun her tickte kasıyor" part 2).</summary>
        public sealed class TilePrecompute
        {
            public float[,] Heights;
            public float[,,] Alpha;
            public float WaterY;
        }

        public static TilePrecompute Precompute(WorldGeoSampler sampler, int tileX, int tileZ, float tileSize)
        {
            return new TilePrecompute
            {
                Heights = BuildGeoHeights(sampler, tileX, tileZ, tileSize, out float waterY),
                Alpha = BuildGeoAlpha(sampler, tileX, tileZ, tileSize),
                WaterY = waterY,
            };
        }

        /// <summary>Main-thread half: turns a precompute into the live Terrain GameObject.</summary>
        public static GameObject BuildTileFromPrecompute(
            Transform parent, int tileX, int tileZ, float tileSize, BiomeKind biome, TilePrecompute pre)
        {
            var data = new TerrainData
            {
                heightmapResolution = HeightmapRes,
                alphamapResolution = AlphamapRes,
                size = new Vector3(tileSize, GeoYMax - GeoYMin, tileSize),
            };
            data.SetHeights(0, 0, pre.Heights);

            var assets = GetBiomeAssets(biome);
            data.terrainLayers = new[]
            {
                assets.Layer,
                GetBiomeAssets(BiomeKind.Coast).Layer,    // beach sand
                GetBiomeAssets(BiomeKind.Mountain).Layer, // steep rock
            };
            data.SetAlphamaps(0, 0, pre.Alpha);

            var go = Terrain.CreateTerrainGameObject(data); // also adds a TerrainCollider
            go.name = $"TerrainTile_{tileX}_{tileZ}";
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = new Vector3(tileX * tileSize, GeoYMin, tileZ * tileSize);

            if (assets.Material != null)
                go.GetComponent<Terrain>().materialTemplate = assets.Material;

            // No water above the town's head: IDW blending near big elevation steps can claim a "lake level"
            // far above the home settlement with no basin to hold it — that sheet renders as a giant teal
            // triangle IN THE SKY ("gökyüzünde deniz efekti"). Local realization keeps water at or below the
            // settlement (sea −2.5m, lakeside shores likewise); anything higher is a blend artifact — skip it.
            const float MaxLocalWaterAboveHome = 8f;
            if (!float.IsNaN(pre.WaterY))
            {
                if (pre.WaterY - GeoYMin < 2f)
                {
                    // Below the vertical window: a plateau town's distant sea sits under the clipped terrain
                    // floor — the sheet would render underground (invisible) and spam misleading logs.
                }
                else if (pre.WaterY <= MaxLocalWaterAboveHome)
                {
                    AddWaterSurface(go.transform, tileSize, pre.WaterY - GeoYMin); // sea OR lake level, per tile
                    RuntimeWaterIndex.Register(tileX, tileZ, pre.WaterY, tileSize); // F3/swim: SwimView probes this
                }
                else
                    Debug.Log($"[Terrain] skipped sky-water sheet under 'TerrainTile_{tileX}_{tileZ}' (claimed level +{pre.WaterY:0.#}m above home — no basin)");
            }
            return go;
        }

        // Heights from the world geography function, normalized into the [GeoYMin, GeoYMax] window (the
        // terrain object sits at y = GeoYMin so heightmap 0..1 maps back to true relative metres). Also
        // reports the highest LOCAL water surface (sea or lake) under any wet sample, so the tile carries
        // ONE water plane at the right level instead of assuming global sea.
        private static float[,] BuildGeoHeights(WorldGeoSampler sampler, int tileX, int tileZ, float tileSize, out float waterSurfaceY)
        {
            var heights = new float[HeightmapRes, HeightmapRes];
            float step = tileSize / (HeightmapRes - 1);
            waterSurfaceY = float.NaN;
            for (int y = 0; y < HeightmapRes; y++)
            {
                for (int x = 0; x < HeightmapRes; x++)
                {
                    var s = sampler.Sample((tileX * tileSize) + (x * step), (tileZ * tileSize) + (y * step));
                    if (s.IsWater)
                    {
                        float w = (float)s.WaterSurfaceMeters;
                        if (float.IsNaN(waterSurfaceY) || w > waterSurfaceY) waterSurfaceY = w;
                    }
                    heights[y, x] = Mathf.Clamp01(((float)s.ElevationMeters - GeoYMin) / (GeoYMax - GeoYMin));
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
            if (renderer != null)
            {
                renderer.sharedMaterial = RuntimeMaterialPalette.Water();
                // One log per water sheet: lets a playtest log answer "was that black thing OUR water, and
                // with which shader?" instead of guessing from screenshots.
                Debug.Log($"[Terrain] water sheet under '{tile.name}' at local y={localY:0.#} shader='{renderer.sharedMaterial.shader?.name}'");
                // No shadows on the water sheet: at low sun angles the shore-bowl rim shadowed the ENTIRE
                // plane, which is exactly the "black area outside town" the playtest reported.
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
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
