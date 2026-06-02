using EmberCrpg.Simulation.WorldDirector;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Builds one building from a deterministic <see cref="BuildingPlacement"/> as a four-wall box shell of
    /// runtime primitive cubes (no AssetDatabase). Walls are thin so the interior stays an open, enterable
    /// volume for the Phase-2 interiors work; Phase 1 just needs a readable silhouette + collision.
    /// </summary>
    public static class RuntimeBuildingBuilder
    {
        private const float WallThickness = 0.25f;

        public static GameObject Build(Transform parent, BuildingPlacement placement)
        {
            var root = new GameObject("Building");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = new Vector3(placement.OriginX, 0f, placement.OriginZ);

            var material = RuntimeMaterialPalette.Opaque(RuntimeMaterialPalette.WallColor(placement.MaterialIndex));
            float halfX = placement.SizeX / 2f;
            float halfZ = placement.SizeZ / 2f;
            float wallY = placement.Height / 2f;

            // North + south walls span X; east + west walls span Z. Interior left open.
            AddWall(root.transform, new Vector3(0f, wallY, halfZ), new Vector3(placement.SizeX, placement.Height, WallThickness), material);
            AddWall(root.transform, new Vector3(0f, wallY, -halfZ), new Vector3(placement.SizeX, placement.Height, WallThickness), material);
            AddWall(root.transform, new Vector3(halfX, wallY, 0f), new Vector3(WallThickness, placement.Height, placement.SizeZ), material);
            AddWall(root.transform, new Vector3(-halfX, wallY, 0f), new Vector3(WallThickness, placement.Height, placement.SizeZ), material);
            return root;
        }

        private static void AddWall(Transform parent, Vector3 localPosition, Vector3 size, Material material)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent, worldPositionStays: false);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = size;
            var renderer = wall.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }
    }
}
