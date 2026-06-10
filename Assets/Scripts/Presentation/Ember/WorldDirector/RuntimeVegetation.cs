using EmberCrpg.Domain.Overland;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F5/vegetation (DFU recipe): deterministic tree scatter per terrain tile. Cross-quad trees with a
    /// procedurally painted canopy texture (no asset files); spawn chance follows the DFU numbers — high on
    /// grass, low on dirt, near-zero on rock — gated by slope ≤50° and a clearance ring around the
    /// settlement so the town plaza stays open.
    /// </summary>
    public static class RuntimeVegetation
    {
        private const float CellSize = 9f;            // one spawn roll per 9m cell
        private const float GrassChance = 0.16f;      // DFU grass 0.9 is per-2m-tile; rescaled to our 9m cells
        private const float DirtChance = 0.04f;
        private const float MaxSlopeDeg = 50f;        // DFU steepness rejection
        private const float SettlementClearance = 70f; // keep the town readable (DFU natureClearance analogue)

        private static Material _trunkMat, _canopyMat;

        public static int Scatter(Transform tileTransform, Terrain terrain, int tileX, int tileZ, float tileSize, BiomeKind biome)
        {
            if (terrain == null || biome == BiomeKind.Desert || biome == BiomeKind.Ash) return 0; // bare biomes (DFU desert ×0.25 → v1: none)
            float biomeBoost = biome == BiomeKind.Forest ? 2.2f : 1f; // forests read as forests

            EnsureMaterials();
            var root = new GameObject("Vegetation");
            root.transform.SetParent(tileTransform, worldPositionStays: false);

            int planted = 0;
            int cells = Mathf.FloorToInt(tileSize / CellSize);
            for (int cz = 0; cz < cells; cz++)
            {
                for (int cx = 0; cx < cells; cx++)
                {
                    float lx = (cx + 0.5f) * CellSize, lz = (cz + 0.5f) * CellSize;
                    float wxF = (tileX * tileSize) + lx, wzF = (tileZ * tileSize) + lz;

                    // settlement clearance is measured from the WORLD origin = the home plaza
                    if ((wxF * wxF) + (wzF * wzF) < SettlementClearance * SettlementClearance) continue;

                    uint h = TreeHash(tileX, tileZ, cx, cz);
                    float roll = (h & 0xFFFF) / 65536f;
                    bool dirtPatch = ((h >> 16) & 1) == 1; // crude alignment with the dirt-noise band
                    float chance = (dirtPatch ? DirtChance : GrassChance) * biomeBoost;
                    if (roll > chance) continue;

                    // jitter inside the cell, then ground-snap + slope gate via the live terrain
                    float jx = lx + (((h >> 17) & 0x7F) / 127f - 0.5f) * CellSize;
                    float jz = lz + (((h >> 24) & 0x7F) / 127f - 0.5f) * CellSize;
                    var world = tileTransform.position + new Vector3(jx, 0f, jz);
                    float groundY = terrain.SampleHeight(world) + tileTransform.position.y;
                    float steep = terrain.terrainData.GetSteepness(jx / tileSize, jz / tileSize);
                    if (steep > MaxSlopeDeg) continue;
                    if (RuntimeWaterIndex.TryGetAt(new Vector3(world.x, 0f, world.z), out float waterY) && groundY < waterY + 0.5f)
                        continue; // no trees in the surf

                    PlantTree(root.transform, new Vector3(jx, groundY - tileTransform.position.y, jz), h);
                    planted++;
                }
            }
            return planted;
        }

        // GEOMETRY trees (lookaround27+28 finding): runtime alpha-cutout quads rendered as black boxes —
        // the build STRIPS the URP Lit alpha-test variant no asset references. Solid-material geometry is
        // the proven bulletproof path (same as buildings) and fits the blocky art style.
        private static void PlantTree(Transform parent, Vector3 localPos, uint seed)
        {
            EnsureMaterials();
            float scale = 0.8f + ((seed >> 8) & 0x3F) / 63f * 0.9f; // 0.8..1.7
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent, worldPositionStays: false);
            tree.transform.localPosition = localPos;
            tree.transform.localRotation = Quaternion.Euler(0f, (seed % 360u), 0f);
            tree.transform.localScale = Vector3.one * scale;

            Box(tree.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.32f, 2.6f, 0.32f), 0f, _trunkMat);
            var canopy = ((seed >> 14) & 1) == 0 ? _canopyMat : _canopyMat2; // two greens break uniformity
            Box(tree.transform, new Vector3(0f, 3.2f, 0f), new Vector3(2.7f, 2.2f, 2.7f), 0f, canopy);
            Box(tree.transform, new Vector3(0f, 4.3f, 0f), new Vector3(1.9f, 1.7f, 1.9f), 35f, canopy);
        }

        private static void Box(Transform parent, Vector3 pos, Vector3 size, float yaw, Material mat)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "TreePart";
            var col = b.GetComponent<Collider>();
            if (col != null) Object.Destroy(col); // walk-through canopy; trunk thin enough to ignore
            b.transform.SetParent(parent, worldPositionStays: false);
            b.transform.localPosition = pos;
            b.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            b.transform.localScale = size;
            b.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static Material _canopyMat2;
        private static void EnsureMaterials()
        {
            if (_canopyMat != null) return;
            _trunkMat = RuntimeMaterialPalette.Solid(new Color(0.30f, 0.21f, 0.12f));
            _canopyMat = RuntimeMaterialPalette.Solid(new Color(0.13f, 0.36f, 0.14f));
            _canopyMat2 = RuntimeMaterialPalette.Solid(new Color(0.18f, 0.44f, 0.16f));
        }

        private static uint TreeHash(int tileX, int tileZ, int cx, int cz)
        {
            unchecked
            {
                uint h = ((uint)tileX * 374761393u) ^ ((uint)tileZ * 668265263u) ^ ((uint)cx * 2246822519u) ^ ((uint)cz * 3266489917u);
                h = (h ^ (h >> 15)) * 2654435761u;
                return h ^ (h >> 13);
            }
        }
    }
}
