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

            // INTERIORS v1 (content phase): roof + wood floor + deterministic furniture + a warm hearth
            // light, so stepping through the door enters a ROOM instead of a roofless pen. Everything is
            // seeded from the placement coordinates — the same town always furnishes identically.
            AddSlab(root.transform, "Roof", new Vector3(0f, placement.Height + 0.06f, 0f),
                new Vector3(placement.SizeX + 0.5f, 0.12f, placement.SizeZ + 0.5f), RuntimeMaterialPalette.Solid(new Color(0.23f, 0.19f, 0.16f)));
            AddSlab(root.transform, "Floor", new Vector3(0f, 0.03f, 0f),
                new Vector3(placement.SizeX - WallThickness, 0.06f, placement.SizeZ - WallThickness), RuntimeMaterialPalette.Solid(new Color(0.42f, 0.30f, 0.18f)));
            Furnish(root.transform, placement, entrance);
            AddHearthLight(root.transform, placement);
            return root;
        }

        // Deterministic interior dressing: 2-4 pieces against the wall OPPOSITE the door (never blocking the
        // entrance), seeded from the building's position so re-realizing the town reproduces every room.
        private static void Furnish(Transform root, BuildingPlacement placement, DoorSide entrance)
        {
            uint seed = unchecked((uint)((placement.OriginX * 73856093f) + (placement.OriginZ * 19349663f)) | 1u);
            var wood = RuntimeMaterialPalette.Solid(new Color(0.36f, 0.25f, 0.15f));
            var cloth = RuntimeMaterialPalette.Solid(new Color(0.45f, 0.32f, 0.30f));

            // Back wall direction = opposite the entrance; the lateral axis spreads the pieces.
            Vector3 back;
            switch (entrance)
            {
                case DoorSide.North: back = new Vector3(0f, 0f, -1f); break;
                case DoorSide.South: back = new Vector3(0f, 0f, 1f); break;
                case DoorSide.East: back = new Vector3(-1f, 0f, 0f); break;
                default: back = new Vector3(1f, 0f, 0f); break;
            }
            var side = new Vector3(back.z, 0f, -back.x);
            float backDepth = ((Mathf.Abs(back.x) > 0.5f ? placement.SizeX : placement.SizeZ) / 2f) - 0.9f;
            float lateralHalf = ((Mathf.Abs(back.x) > 0.5f ? placement.SizeZ : placement.SizeX) / 2f) - 0.9f;
            if (backDepth <= 0.4f || lateralHalf <= 0.4f) return; // tiny shed: leave it bare

            int pieces = 2 + (int)(Hash01(ref seed) * 3f); // 2..4
            for (int i = 0; i < pieces; i++)
            {
                float lateral = ((Hash01(ref seed) * 2f) - 1f) * lateralHalf;
                float depth = backDepth - (Hash01(ref seed) * 0.8f);
                var pos = (back * depth) + (side * lateral);
                int kind = (int)(Hash01(ref seed) * 3f);
                switch (kind)
                {
                    case 0: AddSlab(root, "Bed", pos + new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 1.8f), cloth); break;
                    case 1: AddSlab(root, "Table", pos + new Vector3(0f, 0.4f, 0f), new Vector3(1.2f, 0.8f, 0.8f), wood); break;
                    default: AddSlab(root, "Crate", pos + new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 0.7f), wood); break;
                }
            }
        }

        private static void AddHearthLight(Transform root, BuildingPlacement placement)
        {
            var go = new GameObject("HearthLight");
            go.transform.SetParent(root, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0f, placement.Height * 0.55f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.55f);
            light.intensity = 1.3f;
            light.range = Mathf.Max(placement.SizeX, placement.SizeZ) * 1.2f;
            light.shadows = LightShadows.None; // many small rooms: keep them cheap
        }

        private static float Hash01(ref uint state)
        {
            unchecked
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                return (state & 0xFFFFFF) / 16777216f;
            }
        }

        private static void AddSlab(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            slab.transform.SetParent(parent, worldPositionStays: false);
            slab.transform.localPosition = localPosition;
            slab.transform.localScale = size;
            var renderer = slab.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;
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
