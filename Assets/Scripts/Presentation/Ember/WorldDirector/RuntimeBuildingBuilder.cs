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
        private const float DoorHeight = 2.2f;

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
                new Vector3(placement.SizeX + 0.5f, 0.12f, placement.SizeZ + 0.5f),
                RuntimeMaterialPalette.Textured(RuntimeMaterialPalette.RoofTextureId(placement.MaterialIndex),
                    new Color(0.55f, 0.48f, 0.40f), tiling: 1.5f));
            // Ridge step: a second, smaller slab breaks the flat-box silhouette into a stepped
            // roof — the cheapest read of "someone built this" the skyline can buy.
            AddSlab(root.transform, "RoofRidge", new Vector3(0f, placement.Height + 0.42f, 0f),
                new Vector3(placement.SizeX * 0.55f, 0.6f, placement.SizeZ * 0.55f),
                RuntimeMaterialPalette.Textured(RuntimeMaterialPalette.RoofTextureId(placement.MaterialIndex),
                    new Color(0.50f, 0.44f, 0.37f), tiling: 1.0f));
            // Chimney: a deterministic rooftop stack (seeded from the placement, same trick as
            // the furnishings) — half the houses smoke-ready, and the skyline stops repeating.
            uint chimneyRoll = unchecked(((uint)(placement.OriginX * 8f) * 73856093u)
                ^ ((uint)(placement.OriginZ * 8f) * 19349663u)) | 1u;
            if ((chimneyRoll & 3u) != 0u) // ~3 in 4 buildings
            {
                float cx = (placement.SizeX * 0.28f) * (((chimneyRoll >> 3) & 1u) == 0u ? 1f : -1f);
                AddSlab(root.transform, "Chimney",
                    new Vector3(cx, placement.Height + 0.95f, placement.SizeZ * 0.18f),
                    new Vector3(0.45f, 1.3f, 0.45f),
                    RuntimeMaterialPalette.Solid(new Color(0.34f, 0.30f, 0.28f)));
            }
            AddSlab(root.transform, "Floor", new Vector3(0f, 0.03f, 0f),
                new Vector3(placement.SizeX - WallThickness, 0.06f, placement.SizeZ - WallThickness), RuntimeMaterialPalette.Solid(new Color(0.42f, 0.30f, 0.18f)));
            Furnish(root.transform, placement, entrance);
            AddHearthLight(root.transform, placement);

            // F5/facades (DFU recipe): a real hinged door in the entrance gap (proximity-opened, −90° over
            // 1.5s, click sound) and a window on each side wall — the box finally reads as a HOUSE.
            AddDoor(root.transform, placement, entrance);
            AddWindows(root.transform, placement, entrance);
            return root;
        }

        private static void AddDoor(Transform root, BuildingPlacement placement, DoorSide entrance)
        {
            float halfX = placement.SizeX / 2f, halfZ = placement.SizeZ / 2f;
            const float doorH = DoorHeight;

            // Hinge sits at the gap's edge on the entrance wall; the panel swings INTO the room.
            Vector3 hingePos; float yaw;
            switch (entrance)
            {
                case DoorSide.North: hingePos = new Vector3(-DoorWidth / 2f, doorH / 2f, halfZ); yaw = 0f; break;
                case DoorSide.South: hingePos = new Vector3(DoorWidth / 2f, doorH / 2f, -halfZ); yaw = 180f; break;
                case DoorSide.East: hingePos = new Vector3(halfX, doorH / 2f, DoorWidth / 2f); yaw = 90f; break;
                default: hingePos = new Vector3(-halfX, doorH / 2f, -DoorWidth / 2f); yaw = 270f; break;
            }

            var hinge = new GameObject("DoorHinge");
            hinge.transform.SetParent(root, worldPositionStays: false);
            hinge.transform.localPosition = hingePos;
            hinge.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            hinge.AddComponent<RuntimeDoorView>();

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "DoorPanel";
            var col = panel.GetComponent<Collider>();
            if (col != null) Object.Destroy(col); // never trap the player mid-swing
            panel.transform.SetParent(hinge.transform, worldPositionStays: false);
            panel.transform.localPosition = new Vector3(DoorWidth / 2f, 0f, 0f);
            panel.transform.localScale = new Vector3(DoorWidth - 0.08f, doorH, 0.1f);
            panel.GetComponent<MeshRenderer>().sharedMaterial =
                RuntimeMaterialPalette.Solid(new Color(0.33f, 0.22f, 0.11f));
        }

        private static void AddWindows(Transform root, BuildingPlacement placement, DoorSide entrance)
        {
            float halfX = placement.SizeX / 2f, halfZ = placement.SizeZ / 2f;
            // PLAYTEST FIX ("pencereler de yok"): a lone dusk pane vanished against the wall
            // texture. A dark timber FRAME behind a brighter pane finally reads as a window.
            var glass = RuntimeMaterialPalette.Solid(new Color(0.72f, 0.82f, 0.92f));
            var frame = RuntimeMaterialPalette.Solid(new Color(0.24f, 0.17f, 0.10f));
            bool entranceOnX = entrance == DoorSide.North || entrance == DoorSide.South;

            // One window per SIDE wall (perpendicular to the door), proud of the wall by 3cm so it reads.
            // PLAYTEST FIX v3 ("pencereler yine yok"): every doorless wall gets one - the two
            // SIDE walls and the BACK wall - and the panes grew and brightened until they read
            // from the street, framed in dark timber so they cannot melt into the wall texture.
            void Window(string tag, Vector3 at, bool alongX)
            {
                var frameSize = alongX ? new Vector3(1.5f, 1.4f, 0.05f) : new Vector3(0.05f, 1.4f, 1.5f);
                var paneSize = alongX ? new Vector3(1.2f, 1.1f, 0.05f) : new Vector3(0.05f, 1.1f, 1.2f);
                var outward = alongX ? new Vector3(0f, 0f, Mathf.Sign(at.z) * 0.03f) : new Vector3(Mathf.Sign(at.x) * 0.03f, 0f, 0f);
                AddSlab(root, "WindowFrame" + tag, at + outward, frameSize, frame);
                AddSlab(root, "Window" + tag, at + outward * 2f, paneSize, glass);
            }
            if (entranceOnX)
            {
                Window("E", new Vector3(halfX, 1.55f, 0f), alongX: false);
                Window("W", new Vector3(-halfX, 1.55f, 0f), alongX: false);
                Window("Back", new Vector3(0f, 1.55f, entrance == DoorSide.North ? -halfZ : halfZ), alongX: true);
            }
            else
            {
                Window("N", new Vector3(0f, 1.55f, halfZ), alongX: true);
                Window("S", new Vector3(0f, 1.55f, -halfZ), alongX: true);
                Window("Back", new Vector3(entrance == DoorSide.East ? -halfX : halfX, 1.55f, 0f), alongX: false);
            }
        }

        // Deterministic interior dressing: 2-4 pieces against the wall OPPOSITE the door (never blocking the
        // entrance), seeded from the building's position so re-realizing the town reproduces every room.
        private static void Furnish(Transform root, BuildingPlacement placement, DoorSide entrance)
        {
            // Seed from QUANTIZED ints — casting a negative float product straight to uint is undefined
            // behaviour in .NET (can collapse to 0), which would make every room identical or bare.
            int qx = Mathf.RoundToInt(placement.OriginX * 10f);
            int qz = Mathf.RoundToInt(placement.OriginZ * 10f);
            uint seed = unchecked(((uint)qx * 73856093u) ^ ((uint)qz * 19349663u)) | 1u;
            var wood = RuntimeMaterialPalette.Solid(new Color(0.55f, 0.40f, 0.24f));   // lighter: readable in hearth-lit murk
            var cloth = RuntimeMaterialPalette.Solid(new Color(0.62f, 0.45f, 0.42f));

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

            // PLAYTEST FIX ("iki kup, bazen ust uste"): pieces claim DISTINCT lateral slots
            // along the back wall (no overlap possible) and each kind has a readable SHAPE -
            // a bed is a frame with a blanket, a table stands on a leg, a crate is a crate.
            var woodDark = RuntimeMaterialPalette.Solid(new Color(0.36f, 0.26f, 0.15f));
            var blanket = RuntimeMaterialPalette.Solid(new Color(0.55f, 0.20f, 0.18f));
            float[] slots = { -0.66f, 0f, 0.66f };
            int firstSlot = (int)(Hash01(ref seed) * 3f) % 3;
            int pieces = 2 + (int)(Hash01(ref seed) * 2f); // 2..3, one per slot
            for (int i = 0; i < pieces && i < slots.Length; i++)
            {
                float lateral = slots[(firstSlot + i) % slots.Length] * lateralHalf;
                float depth = backDepth - 0.15f;
                var pos = (back * depth) + (side * lateral);
                int kind = (int)(Hash01(ref seed) * 3f);
                switch (kind)
                {
                    case 0:
                        AddSlab(root, "BedFrame", pos + new Vector3(0f, 0.18f, 0f), new Vector3(0.95f, 0.36f, 1.8f), woodDark);
                        AddSlab(root, "BedBlanket", pos + new Vector3(0f, 0.42f, back.z * -0.25f + back.x * -0.25f), new Vector3(0.88f, 0.14f, 1.2f), blanket);
                        break;
                    case 1:
                        AddSlab(root, "TableTop", pos + new Vector3(0f, 0.72f, 0f), new Vector3(1.15f, 0.10f, 0.75f), wood);
                        AddSlab(root, "TableLeg", pos + new Vector3(0f, 0.34f, 0f), new Vector3(0.16f, 0.68f, 0.16f), woodDark);
                        break;
                    default:
                        AddSlab(root, "Crate", pos + new Vector3(0f, 0.33f, 0f), new Vector3(0.66f, 0.66f, 0.66f), wood);
                        AddSlab(root, "CrateLid", pos + new Vector3(0f, 0.69f, 0f), new Vector3(0.72f, 0.06f, 0.72f), woodDark);
                        break;
                }
            }
            Debug.Log($"[Building] furnished {pieces} pieces at ({placement.OriginX:0.#},{placement.OriginZ:0.#})");
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
            // PLAYTEST FIX ("kapinin ust tarafi acik"): a LINTEL closes the gap above the door -
            // the entrance is a doorway now, not a floor-to-roof slot in the facade.
            float lintelH = height - DoorHeight;
            if (lintelH > 0.05f)
                AddWall(parent, new Vector3(0f, DoorHeight + lintelH / 2f, z), new Vector3(DoorWidth, lintelH, WallThickness), material);
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
            float lintelH = height - DoorHeight; // same lintel as AddWallX - see the facade fix note
            if (lintelH > 0.05f)
                AddWall(parent, new Vector3(x, DoorHeight + lintelH / 2f, 0f), new Vector3(WallThickness, lintelH, DoorWidth), material);
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
