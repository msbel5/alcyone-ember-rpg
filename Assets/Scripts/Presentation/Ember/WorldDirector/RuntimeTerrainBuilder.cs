using EmberCrpg.Domain.Overland;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Builds a real Unity Terrain for the settlement at runtime (research-backed: TerrainData.SetHeights +
    /// SetAlphamaps splatmap, per the GameDev Academy / Unity terrain-API approach). A flat central plateau
    /// (where the settlement + player sit at world y=0) blends into gentle, seed-deterministic Perlin hills
    /// toward the edges, textured with the biome's generated floor. Replaces the flat box-plane: real ground
    /// with biome texture, an automatic TerrainCollider, and a rim boundary so the player cannot fall off.
    /// Same seed -> identical hills (roguelike-deterministic).
    /// </summary>
    public static class RuntimeTerrainBuilder
    {
        private const int HeightmapRes = 257;   // must be 2^n + 1
        private const int AlphamapRes = 256;
        private const float TerrainSize = 400f; // metres per side
        private const float TerrainHeight = 26f;
        private const float FlatRadius = 55f;   // flat central area (the settlement) in metres

        public static Terrain Build(Transform parent, BiomeKind biome, uint seed)
        {
            var data = new TerrainData
            {
                heightmapResolution = HeightmapRes,
                alphamapResolution = AlphamapRes,
                size = new Vector3(TerrainSize, TerrainHeight, TerrainSize),
            };

            data.SetHeights(0, 0, BuildHeights(seed));
            data.terrainLayers = new[] { BiomeLayer(biome) };
            data.SetAlphamaps(0, 0, FullCoverAlpha());

            var go = Terrain.CreateTerrainGameObject(data); // also adds a TerrainCollider
            go.name = "GeneratedTerrain";
            go.transform.SetParent(parent, worldPositionStays: false);
            // Terrain origin is its corner; offset so the flat plateau centre sits on the world origin where
            // the settlement (built at 0,0) + player spawn.
            go.transform.localPosition = new Vector3(-TerrainSize / 2f, 0f, -TerrainSize / 2f);

            var terrain = go.GetComponent<Terrain>();
            var urpTerrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (urpTerrainShader != null) terrain.materialTemplate = new Material(urpTerrainShader);

            BuildRimBoundary(parent);
            Debug.Log($"[WorldDirector] terrain built ({TerrainSize:0}x{TerrainSize:0}m, biome {biome})");
            return terrain;
        }

        // Flat (height 0) within FlatRadius of centre; gentle Perlin hills beyond, deterministic from the seed.
        private static float[,] BuildHeights(uint seed)
        {
            var heights = new float[HeightmapRes, HeightmapRes];
            float half = (HeightmapRes - 1) / 2f;
            float metresPerSample = TerrainSize / (HeightmapRes - 1);
            float ox = seed % 619, oz = seed % 397; // deterministic noise offset from the world seed
            for (int y = 0; y < HeightmapRes; y++)
            {
                for (int x = 0; x < HeightmapRes; x++)
                {
                    float dx = (x - half) * metresPerSample;
                    float dz = (y - half) * metresPerSample;
                    float dist = Mathf.Sqrt((dx * dx) + (dz * dz));
                    if (dist <= FlatRadius) { heights[y, x] = 0f; continue; }

                    float ramp = Mathf.Clamp01((dist - FlatRadius) / ((TerrainSize * 0.5f) - FlatRadius));
                    float n = Mathf.PerlinNoise((x * 0.05f) + ox, (y * 0.05f) + oz);
                    heights[y, x] = ramp * ramp * (0.12f + (0.55f * n)); // rise toward the edges
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

        // Low invisible collider ring just inside the terrain edge so the player cannot leave the world.
        private static void BuildRimBoundary(Transform parent)
        {
            float r = (TerrainSize / 2f) - 3f;
            const float h = 8f, t = 2f;
            AddWall(parent, new Vector3(0f, h / 2f, r), new Vector3(TerrainSize, h, t));
            AddWall(parent, new Vector3(0f, h / 2f, -r), new Vector3(TerrainSize, h, t));
            AddWall(parent, new Vector3(r, h / 2f, 0f), new Vector3(t, h, TerrainSize));
            AddWall(parent, new Vector3(-r, h / 2f, 0f), new Vector3(t, h, TerrainSize));
        }

        private static void AddWall(Transform parent, Vector3 localPosition, Vector3 size)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "WorldBoundary";
            wall.transform.SetParent(parent, worldPositionStays: false);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = size;
            var r = wall.GetComponent<MeshRenderer>();
            if (r != null) r.enabled = false;
        }
    }
}
