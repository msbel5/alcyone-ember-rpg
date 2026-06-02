using EmberCrpg.Domain.Overland;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Builds ONE seamless terrain TILE for the streaming world (Phase C). Each tile's heightmap is sampled
    /// from a single CONTINUOUS world-space height function, so adjacent tiles match exactly at their shared
    /// edge (no seams). The function is flat within <see cref="FlatRadius"/> of the world origin (where the
    /// settlement + player sit) and ramps into gentle, seed-deterministic Perlin hills outward. Textured with
    /// the biome's generated floor via a URP terrain material; the tile carries an automatic TerrainCollider.
    /// Same seed -> identical terrain everywhere (roguelike-deterministic). The TerrainStreamer manages the
    /// bubble of live tiles around the player, so the world renders as you walk and has no hard edge.
    /// </summary>
    public static class RuntimeTerrainBuilder
    {
        private const int HeightmapRes = 129;   // must be 2^n + 1; 129 keeps per-tile gen cheap for streaming
        private const int AlphamapRes = 64;
        private const float TerrainHeight = 26f;
        private const float FlatRadius = 55f;   // flat central area (the settlement) in metres from world origin
        private const float HillRampMetres = 220f;

        public static GameObject BuildTile(Transform parent, int tileX, int tileZ, float tileSize, BiomeKind biome, uint seed)
        {
            var data = new TerrainData
            {
                heightmapResolution = HeightmapRes,
                alphamapResolution = AlphamapRes,
                size = new Vector3(tileSize, TerrainHeight, tileSize),
            };

            data.SetHeights(0, 0, BuildHeights(tileX, tileZ, tileSize, seed));
            data.terrainLayers = new[] { BiomeLayer(biome) };
            data.SetAlphamaps(0, 0, FullCoverAlpha());

            var go = Terrain.CreateTerrainGameObject(data); // also adds a TerrainCollider
            go.name = $"TerrainTile_{tileX}_{tileZ}";
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = new Vector3(tileX * tileSize, 0f, tileZ * tileSize);

            var terrain = go.GetComponent<Terrain>();
            var urpTerrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (urpTerrainShader != null) terrain.materialTemplate = new Material(urpTerrainShader);
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

        private static TerrainLayer BiomeLayer(BiomeKind biome)
        {
            var texture = RuntimeMaterialPalette.LoadGeneratedTexture(RuntimeMaterialPalette.GroundTextureId(biome))
                          ?? SolidTexture(RuntimeMaterialPalette.GroundColor(biome));
            return new TerrainLayer { diffuseTexture = texture, tileSize = new Vector2(14f, 14f) };
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
    }
}
