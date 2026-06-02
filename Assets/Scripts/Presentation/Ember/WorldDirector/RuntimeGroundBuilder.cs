using EmberCrpg.Domain.Overland;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Builds the biome-tinted ground plane the realized settlement stands on — the runtime twin of the
    /// editor EmberTerrainBuilder's ground plane, using a primitive Plane + a runtime material (no
    /// AssetDatabase). The plane carries a MeshCollider so the player's CharacterController has a floor.
    /// </summary>
    public static class RuntimeGroundBuilder
    {
        public static GameObject Build(Transform parent, float radius, BiomeKind biome)
        {
            // Generous walkable margin around the settlement so a first-person player cannot reach the edge
            // and fall into the void before the boundary even matters.
            float groundRadius = Mathf.Max(radius + 60f, 90f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); // Unity Plane = 10m across at scale 1
            ground.name = "GeneratedGround";
            ground.transform.SetParent(parent, worldPositionStays: false);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localScale = new Vector3((groundRadius * 2f) / 10f, 1f, (groundRadius * 2f) / 10f);

            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                float tiling = Mathf.Max(6f, groundRadius / 5f); // repeat the floor texture across the plane
                renderer.sharedMaterial = RuntimeMaterialPalette.Textured(
                    RuntimeMaterialPalette.GroundTextureId(biome), RuntimeMaterialPalette.GroundColor(biome), tiling);
            }

            BuildBoundary(parent, groundRadius);
            return ground;
        }

        // Four tall invisible collider walls at the plane edge so the player cannot walk off into the void.
        private static void BuildBoundary(Transform parent, float radius)
        {
            const float height = 6f, thickness = 1f;
            AddWall(parent, new Vector3(0f, height / 2f, radius), new Vector3(radius * 2f, height, thickness));
            AddWall(parent, new Vector3(0f, height / 2f, -radius), new Vector3(radius * 2f, height, thickness));
            AddWall(parent, new Vector3(radius, height / 2f, 0f), new Vector3(thickness, height, radius * 2f));
            AddWall(parent, new Vector3(-radius, height / 2f, 0f), new Vector3(thickness, height, radius * 2f));
        }

        private static void AddWall(Transform parent, Vector3 localPosition, Vector3 size)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); // keeps its BoxCollider as the barrier
            wall.name = "WorldBoundary";
            wall.transform.SetParent(parent, worldPositionStays: false);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = size;
            var r = wall.GetComponent<MeshRenderer>();
            if (r != null) r.enabled = false; // invisible — collider only
        }
    }
}
