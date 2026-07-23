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
                DirtLayer(),                              // F5: noise-broken meadow patches (DFU recipe)
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

            // F5/vegetation: deterministic tree scatter (grass-weighted, slope-gated, town-cleared).
            int trees = RuntimeVegetation.Scatter(go.transform, go.GetComponent<Terrain>(), tileX, tileZ, tileSize, biome);
            if (trees > 0)
                Debug.Log($"[Terrain] vegetation: {trees} trees on 'TerrainTile_{tileX}_{tileZ}'.");
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
            var alpha = new float[AlphamapRes, AlphamapRes, 4];
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

                    // F5/ground variety (DFU recipe): flat ground was 100% base layer — break it with
                    // fBm-driven dirt patches (~50m features, 3 octaves). Own hash-lattice noise: pure C#,
                    // deterministic, and safe on the precompute worker thread.
                    float n = Fbm((float)wx * 0.02f, (float)wz * 0.02f);
                    float dirt = Mathf.Clamp01((n - 0.52f) / 0.18f) * 0.85f * Mathf.Max(0f, 1f - sand - rock);

                    alpha[y, x, 1] = sand;
                    alpha[y, x, 2] = rock;
                    alpha[y, x, 3] = dirt;
                    alpha[y, x, 0] = Mathf.Max(0f, 1f - sand - rock - dirt);
                }
            }
            return alpha;
        }

        // Deterministic 3-octave value-noise fBm (hash lattice + smoothstep) — thread-safe, no UnityEngine.
        private static float Fbm(float x, float z)
        {
            float sum = 0f, amp = 0.5f;
            for (int o = 0; o < 3; o++)
            {
                sum += amp * ValueNoise(x, z);
                x *= 2f; z *= 2f; amp *= 0.4f; // DFU persistence 0.4
            }
            return sum / 0.78f; // normalize (0.5+0.2+0.08)
        }

        private static float ValueNoise(float x, float z)
        {
            int x0 = Mathf.FloorToInt(x), z0 = Mathf.FloorToInt(z);
            float fx = x - x0, fz = z - z0;
            fx = fx * fx * (3f - 2f * fx);
            fz = fz * fz * (3f - 2f * fz);
            float a = Hash01(x0, z0), b = Hash01(x0 + 1, z0), c = Hash01(x0, z0 + 1), d = Hash01(x0 + 1, z0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
        }

        private static float Hash01(int x, int z)
        {
            unchecked
            {
                uint h = ((uint)x * 374761393u) ^ ((uint)z * 668265263u);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static TerrainLayer _dirtLayer;
        // Deterministic 2-octave value-noise ground with per-biome palette + sparse speckle
        // highlights (blades/flowers/frost). 256px, tiled at 9m — nature at a glance, zero assets.
        private static readonly Dictionary<BiomeKind, Texture2D> NatureCache = new Dictionary<BiomeKind, Texture2D>();

        public static Texture2D NatureTexture(BiomeKind biome) // public: the settlement ground plane shares it
        {
            if (NatureCache.TryGetValue(biome, out var cached)) return cached;
            (Color dark, Color light, Color speckle) = biome switch
            {
                BiomeKind.Forest => (new Color(0.06f, 0.15f, 0.06f), new Color(0.13f, 0.24f, 0.09f), new Color(0.22f, 0.34f, 0.13f)),
                BiomeKind.Coast  => (new Color(0.55f, 0.48f, 0.33f), new Color(0.72f, 0.64f, 0.45f), new Color(0.30f, 0.42f, 0.20f)),
                BiomeKind.Desert => (new Color(0.62f, 0.50f, 0.30f), new Color(0.78f, 0.66f, 0.42f), new Color(0.54f, 0.42f, 0.26f)),
                BiomeKind.Tundra => (new Color(0.52f, 0.56f, 0.55f), new Color(0.72f, 0.76f, 0.74f), new Color(0.86f, 0.90f, 0.92f)),
                BiomeKind.Swamp  => (new Color(0.14f, 0.20f, 0.12f), new Color(0.24f, 0.30f, 0.16f), new Color(0.34f, 0.38f, 0.22f)),
                _                => (new Color(0.10f, 0.20f, 0.07f), new Color(0.20f, 0.33f, 0.12f), new Color(0.33f, 0.46f, 0.16f)), // plains grass
            };

            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true)
            { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            uint seedBase = (uint)(1000 + (int)biome);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Three octaves: MACRO (64px ≈ 2.3m) survives mip-averaging at distance —
                // without it the ground collapsed to a flat pale wash past ~15m (R1 finding).
                float n = 0.45f * LatticeNoise(x, y, 64, seedBase ^ 0x51ED270Bu)
                        + 0.40f * LatticeNoise(x, y, 16, seedBase)
                        + 0.15f * LatticeNoise(x, y, 4, seedBase ^ 0x9E3779B9u);
                n = Mathf.Clamp01((n - 0.5f) * 1.6f + 0.5f); // contrast push — mips flatten, we pre-sharpen
                var color = Color.Lerp(dark, light, n);
                uint h = Hash((uint)x * 374761393u ^ (uint)y * 668265263u ^ seedBase);
                if ((h & 63u) == 0u) color = speckle; // sparse blades/flowers/frost
                pixels[y * size + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            NatureCache[biome] = texture;
            return texture;
        }

        private static float LatticeNoise(int x, int y, int cell, uint seed)
        {
            int gx = x / cell, gy = y / cell;
            float fx = (x % cell) / (float)cell, fy = (y % cell) / (float)cell;
            int wrap = 256 / cell;
            float V(int cx, int cy) => (Hash((uint)(((cx % wrap + wrap) % wrap) * 2246822519L
                ^ ((cy % wrap + wrap) % wrap) * 3266489917L) ^ seed) & 0xFFFF) / 65536f;
            float top = Mathf.Lerp(V(gx, gy), V(gx + 1, gy), fx);
            float bottom = Mathf.Lerp(V(gx, gy + 1), V(gx + 1, gy + 1), fx);
            return Mathf.Lerp(top, bottom, fy);
        }

        private static uint Hash(uint value)
        {
            unchecked { value = (value ^ (value >> 15)) * 2654435761u; return value ^ (value >> 13); }
        }

        private static TerrainLayer DirtLayer()
        {
            if (_dirtLayer != null) return _dirtLayer;
            var texture = NatureTexture(BiomeKind.Desert); // earthy noise doubles as the meadow-break dirt
            _dirtLayer = new TerrainLayer { diffuseTexture = texture, tileSize = new Vector2(9f, 9f) };
            return _dirtLayer;
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

            // 10/10 R1: outdoor biomes wear PROCEDURAL NATURE, not recycled interior floor
            // plates — the env_* cobbles read as bathroom tile across whole landscapes (the
            // buyer-score's single loudest complaint). Rock biomes keep their stony plates.
            bool rocky = biome == BiomeKind.Mountain || biome == BiomeKind.Ash;
            var texture = rocky
                ? (RuntimeMaterialPalette.LoadGeneratedTexture(RuntimeMaterialPalette.GroundTextureId(biome))
                   ?? SolidTexture(RuntimeMaterialPalette.GroundColor(biome)))
                : NatureTexture(biome);
            var tint = RuntimeMaterialPalette.GroundColor(biome);
            var layer = new TerrainLayer
            {
                diffuseTexture = texture,
                tileSize = new Vector2(rocky ? 14f : 9f, rocky ? 14f : 9f),
                diffuseRemapMin = Vector4.zero,
                // Nature textures carry their own palette — remap only the rocky plates.
                // CALIBRATION (colour-probe verdict): this terrain pipeline renders albedo
                // ~2.5-3x brighter than authored — pure blue probed as pastel lavender. The
                // remap pre-divides inside the shader so nature reads at its painted darkness.
                diffuseRemapMax = rocky
                    ? new Vector4(
                        Mathf.Lerp(1f, tint.r, 0.55f), Mathf.Lerp(1f, tint.g, 0.55f), Mathf.Lerp(1f, tint.b, 0.55f), 1f)
                    : new Vector4(0.38f, 0.38f, 0.38f, 1f),
            };

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
