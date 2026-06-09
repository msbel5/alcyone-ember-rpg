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
        private const float DoorWidth = 1.6f;

        public static GameObject Build(Transform parent, BuildingPlacement placement)
        {
            var root = new GameObject("Building");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = new Vector3(placement.OriginX, 0f, placement.OriginZ);
            root.AddComponent<BuildingAccessibilityVolume>().Configure(
                placement.SizeX,
                placement.SizeZ,
                margin: 0.9f);

            // Use the generated wall texture when it exists; fall back to a flat wall colour otherwise.
            var material = RuntimeMaterialPalette.Textured(
                RuntimeMaterialPalette.WallTextureId(placement.MaterialIndex),
                RuntimeMaterialPalette.WallColor(placement.MaterialIndex),
                tiling: 2f);
            float halfX = placement.SizeX / 2f;
            float halfZ = placement.SizeZ / 2f;
            float wallY = placement.Height / 2f;

            var entrance = ChooseEntranceSide(placement);

            // North + south walls span X; east + west walls span Z. The entrance is a full-height gap facing
            // the settlement center so NPCs and the player can reach spawned actors before room interiors exist.
            AddWallX(root.transform, halfZ, placement.SizeX, placement.Height, material, entrance == DoorSide.North);
            AddWallX(root.transform, -halfZ, placement.SizeX, placement.Height, material, entrance == DoorSide.South);
            AddWallZ(root.transform, halfX, placement.SizeZ, placement.Height, material, entrance == DoorSide.East);
            AddWallZ(root.transform, -halfX, placement.SizeZ, placement.Height, material, entrance == DoorSide.West);
            return root;
        }

        private static DoorSide ChooseEntranceSide(BuildingPlacement placement)
        {
            if (Mathf.Abs(placement.OriginX) > Mathf.Abs(placement.OriginZ))
                return placement.OriginX >= 0f ? DoorSide.West : DoorSide.East;
            return placement.OriginZ >= 0f ? DoorSide.South : DoorSide.North;
        }

        private static void AddWallX(
            Transform parent,
            float z,
            float width,
            float height,
            Material material,
            bool withDoor)
        {
            if (!withDoor || width <= DoorWidth + WallThickness)
            {
                AddWall(parent, new Vector3(0f, height / 2f, z), new Vector3(width, height, WallThickness), material);
                return;
            }

            float segmentWidth = (width - DoorWidth) / 2f;
            float offset = (DoorWidth + segmentWidth) / 2f;
            AddWall(parent, new Vector3(-offset, height / 2f, z), new Vector3(segmentWidth, height, WallThickness), material);
            AddWall(parent, new Vector3(offset, height / 2f, z), new Vector3(segmentWidth, height, WallThickness), material);
        }

        private static void AddWallZ(
            Transform parent,
            float x,
            float depth,
            float height,
            Material material,
            bool withDoor)
        {
            if (!withDoor || depth <= DoorWidth + WallThickness)
            {
                AddWall(parent, new Vector3(x, height / 2f, 0f), new Vector3(WallThickness, height, depth), material);
                return;
            }

            float segmentDepth = (depth - DoorWidth) / 2f;
            float offset = (DoorWidth + segmentDepth) / 2f;
            AddWall(parent, new Vector3(x, height / 2f, -offset), new Vector3(WallThickness, height, segmentDepth), material);
            AddWall(parent, new Vector3(x, height / 2f, offset), new Vector3(WallThickness, height, segmentDepth), material);
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

        private enum DoorSide
        {
            North,
            South,
            East,
            West
        }
    }
}
