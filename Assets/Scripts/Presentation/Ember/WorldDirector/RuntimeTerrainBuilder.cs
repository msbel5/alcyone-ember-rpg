using System.Collections.Generic;
using EmberCrpg.Domain.Overland;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Builds ONE seamless terrain TILE for the streaming world (Phase C). Each tile's heightmap is sampled
    /// from a single CONTINUOUS world-space height function, so adjacent tiles match exactly at their shared
    /// edge (no seams). The function is flat within <see cref="FlatRadius"/> of the world origin (where the
    /// settlement + player sit) and ramps into gentle, seed-deterministic Perlin hills outward.
    ///
    /// MEMORY: the biome's TerrainLayer + URP material are created ONCE and cached per biome (a tiny bounded
    /// set), so streaming hundreds of tiles does not allocate a fresh material/texture per tile. The only
    /// per-tile allocation is the heightmap <see cref="TerrainData"/>, which the streamer explicitly frees
    /// when it unloads a tile — together these stop the "RAM grows as you walk" stutter.
    /// </summary>
    public static class RuntimeTerrainBuilder
    {
        private const int HeightmapRes = 129;   // must be 2^n + 1; 129 keeps per-tile gen cheap for streaming
        private const int AlphamapRes = 64;
        private const float TerrainHeight = 26f;
        private const float FlatRadius = 55f;   // flat central area (the settlement) in metres from world origin
        private const float HillRampMetres = 220f;

        private static readonly Dictionary<BiomeKind, BiomeAssets> BiomeCache = new Dictionary<BiomeKind, BiomeAssets>();

        public static GameObject BuildTile(Transform parent, int tileX, int tileZ, float tileSize, BiomeKind biome, uint seed)
        {
            var data = new TerrainData
            {
                heightmapResolution = HeightmapRes,
                alphamapResolution = AlphamapRes,
                size = new Vector3(tileSize, TerrainHeight, tileSize),
            };

            data.SetHeights(0, 0, BuildHeights(tileX, tileZ, tileSize, seed));

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

        // One continuous world-space height function, sampled at this tile's world coords -> seamless edges.
        private static float[,] BuildHeights(int tileX, int tileZ, float tileSize, uint seed)
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
                    float dist = Mathf.Sqrt((wx * wx) + (wz * wz)); // distance to world origin (settlement)
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
            // but fall back robustly so the ground is NEVER the magenta "missing shader" colour: terrain shader ->
            // URP/Lit (kept by the buildings) -> Standard. On a non-terrain fallback, paint the biome texture as
            // the base map (tiled) so the ground still reads as textured ground rather than a flat colour.
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
