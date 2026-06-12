using System.Collections.Generic;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F18 MULTI-ROOM DELVE: the single barrow chamber grows into the deterministic 5-10 room graph the
    /// domain's MultiRoomDungeonGenerator has produced since Sprint 4 — finally realized. Rooms sit on a
    /// 16m lattice behind the mouth (grid +X = deeper, grid +Y = sideways); shared walls open a 2.6m gap
    /// at the generator's mid-wall door cells and a corridor connector bridges the lattice gap. The LAST
    /// room is the BOSS room: bigger chest, hotter light. Same primitive + Solid() construction as ever
    /// (banked rule: no runtime alpha-clip materials). The whole footprint crest-floats on the hillside.
    /// </summary>
    public static class RuntimeDungeonBuilder
    {
        private const float Pitch = 16f;        // lattice spacing (max room 12 + breathing room)
        private const float WallHeight = 4.6f;  // skirts ~1m below the floor AND seals the roof line —
                                                // 3.8 left a 0.35m daylight band under every roof slab
                                                // (proof frames read as outdoors, not a delve)
        private const float DoorGap = 2.6f;
        private const float Room0Z = -14f;      // room 0 centre, behind the entry corridor

        public static GameObject Build(Transform parent, float distance, float angleDeg, int dungeonSeed)
        {
            var root = new GameObject("DungeonInterior");
            root.transform.SetParent(parent, worldPositionStays: false);
            float rad = angleDeg * Mathf.Deg2Rad;
            root.transform.localPosition = new Vector3(Mathf.Cos(rad) * distance, 0f, Mathf.Sin(rad) * distance);
            root.transform.localRotation = Quaternion.Euler(0f, -angleDeg + 90f, 0f); // +Z runs from mouth into the hill

            var layout = new EmberCrpg.Simulation.World.MultiRoomDungeonGenerator().Generate(dungeonSeed);
            var rooms = layout.Rooms;

            // Local centre of a room's lattice cell: grid +X digs deeper (-Z), grid +Y slides east (+X).
            Vector3 CellCenter(EmberCrpg.Domain.World.DungeonRoom r) =>
                new Vector3(r.GridY * Pitch, 0f, Room0Z - (r.GridX * Pitch));

            // HILLSIDE CONFORM: sample the ground across the WHOLE footprint and float on the crest.
            float crest = 0f;
            foreach (var room in rooms)
            {
                var c = CellCenter(room);
                crest = Mathf.Max(crest, SampleGroundY(root.transform.TransformPoint(c)));
            }
            crest = Mathf.Max(crest, SampleGroundY(root.transform.TransformPoint(new Vector3(0f, 0f, 1.5f))));
            var rootPos = root.transform.position;
            root.transform.position = new Vector3(rootPos.x, crest + 0.05f, rootPos.z);

            // F19: archetype palette — Mağara/Kripta/Harabe from the same seed that shaped the graph.
            var archetype = RuntimeDungeonArchetype.For(dungeonSeed);
            // F29: the bestiary reads the archetype from the layout info (single-writer rule).
            RuntimeDungeonLayoutInfo.RecordArchetype(archetype.Name);
            var rock = RuntimeMaterialPalette.Solid(archetype.Rock);
            var floor = RuntimeMaterialPalette.Solid(archetype.Floor);

            // Mouth ramp up onto the floated floor (same recipe as the barrow).
            float mouthGround = SampleGroundY(root.transform.TransformPoint(new Vector3(0f, 0f, 1.5f)));
            float rise = root.transform.position.y - mouthGround;
            if (rise > 0.25f)
            {
                float run = Mathf.Max(rise * 1.6f, 2.2f);
                var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = "MouthRamp";
                ramp.transform.SetParent(root.transform, worldPositionStays: false);
                ramp.transform.localPosition = new Vector3(0f, -rise * 0.5f, run * 0.5f + 0.2f);
                ramp.transform.localScale = new Vector3(3.4f, 0.25f, Mathf.Sqrt(run * run + rise * rise) + 0.6f);
                ramp.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0f, 0f);
                ramp.GetComponent<MeshRenderer>().sharedMaterial = floor;
            }

            // Entry corridor: from the mouth to room 0's south wall.
            var room0 = layout.FindRoom(layout.StartRoomId);
            float room0SouthZ = Room0Z + (room0.Height * 0.5f);
            float entryLen = -(-1f) + room0SouthZ; // spans z = -1 .. room0 south wall
            float entryMidZ = (-1f + room0SouthZ) * 0.5f;
            Slab(root.transform, "EntryWallL", new Vector3(-1.6f, 0.9f, entryMidZ), new Vector3(0.4f, WallHeight, Mathf.Abs(entryLen) + 0.4f), rock);
            Slab(root.transform, "EntryWallR", new Vector3(1.6f, 0.9f, entryMidZ), new Vector3(0.4f, WallHeight, Mathf.Abs(entryLen) + 0.4f), rock);
            Slab(root.transform, "EntryRoof", new Vector3(0f, 2.9f, entryMidZ), new Vector3(3.6f, 0.3f, Mathf.Abs(entryLen) + 0.4f), rock);
            Slab(root.transform, "EntryFloor", new Vector3(0f, 0.05f, entryMidZ), new Vector3(3.2f, 0.1f, Mathf.Abs(entryLen) + 0.4f), floor);

            // Door sides per room (grid deltas → wall sides), plus room 0's forced entry gap (south).
            var dwellerSpots = new List<Vector3>();
            int bossRoomId = rooms[rooms.Count - 1].Id;
            uint spotHash = (uint)dungeonSeed * 2654435761u;
            int firstConnectedToStart = -1;
            foreach (var door in layout.Doors)
                if (door.FromRoomId == layout.StartRoomId || door.ToRoomId == layout.StartRoomId)
                {
                    firstConnectedToStart = door.FromRoomId == layout.StartRoomId ? door.ToRoomId : door.FromRoomId;
                    break;
                }

            foreach (var room in rooms)
            {
                var c = CellCenter(room);
                float w = room.Width, h = room.Height;

                bool gapNorth = false, gapSouth = room.Id == layout.StartRoomId, gapEast = false, gapWest = false;
                foreach (var door in layout.Doors)
                {
                    int otherId = door.FromRoomId == room.Id ? door.ToRoomId
                        : door.ToRoomId == room.Id ? door.FromRoomId : -1;
                    if (otherId < 0) continue;
                    var other = layout.FindRoom(otherId);
                    int dx = other.GridX - room.GridX, dy = other.GridY - room.GridY;
                    if (dx > 0) gapNorth = true;      // deeper
                    else if (dx < 0) gapSouth = true; // back toward the mouth
                    else if (dy > 0) gapEast = true;
                    else if (dy < 0) gapWest = true;
                }

                Slab(root.transform, $"R{room.Id}Floor", c + new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, h), floor);
                Slab(root.transform, $"R{room.Id}Roof", c + new Vector3(0f, 3.3f, 0f), new Vector3(w + 0.5f, 0.3f, h + 0.5f), rock);
                WallWithGap(root.transform, $"R{room.Id}N", c + new Vector3(0f, 0.9f, -h * 0.5f), w, alongX: true, gapNorth, rock);
                WallWithGap(root.transform, $"R{room.Id}S", c + new Vector3(0f, 0.9f, h * 0.5f), w, alongX: true, gapSouth, rock);
                WallWithGap(root.transform, $"R{room.Id}E", c + new Vector3(w * 0.5f, 0.9f, 0f), h, alongX: false, gapEast, rock);
                WallWithGap(root.transform, $"R{room.Id}W", c + new Vector3(-w * 0.5f, 0.9f, 0f), h, alongX: false, gapWest, rock);

                bool isBoss = room.Id == bossRoomId;
                Torch(root.transform, c + new Vector3(0f, 2.6f, 0f), isBoss ? 16f : 11f,
                    isBoss ? archetype.BossTorch : archetype.Torch,
                    isBoss ? archetype.TorchIntensity + 0.8f : archetype.TorchIntensity);

                // Dwellers: 0-2 per non-start room (deterministic); the room adjacent to the start is
                // guaranteed one so the chase proof always has a pursuer in sight.
                if (room.Id != layout.StartRoomId && !isBoss)
                {
                    int count = (int)((spotHash >> (room.Id % 16)) % 3u);
                    if (room.Id == firstConnectedToStart && count == 0) count = 1;
                    for (int k = 0; k < count && dwellerSpots.Count < 9; k++)
                    {
                        var offset = new Vector3((k == 0 ? -1f : 1f) * (w * 0.25f), 0f, (k == 0 ? 1f : -1f) * (h * 0.25f));
                        dwellerSpots.Add(root.transform.TransformPoint(c + offset));
                    }
                }
            }

            // Corridor connectors across the lattice gaps (one per door).
            // F20: the connector touching the BOSS room gets the LOCKED DOOR; the first other
            // connector gets the crushing-plate TRAP (visible, on the floor, mid-passage).
            Vector3 trapLocal = Vector3.zero, bossDoorLocal = Vector3.zero;
            bool trapPlaced = false, bossDoorPlaced = false;
            foreach (var door in layout.Doors)
            {
                var a = layout.FindRoom(door.FromRoomId);
                var b = layout.FindRoom(door.ToRoomId);
                var ca = CellCenter(a);
                var cb = CellCenter(b);
                var mid = (ca + cb) * 0.5f;
                bool deeper = a.GridX != b.GridX; // connector runs along Z
                float aHalf = deeper ? a.Height * 0.5f : a.Width * 0.5f;
                float bHalf = deeper ? b.Height * 0.5f : b.Width * 0.5f;
                float span = Pitch - aHalf - bHalf + 0.6f;
                var size = deeper ? new Vector3(DoorGap, 0.1f, span) : new Vector3(span, 0.1f, DoorGap);
                Slab(root.transform, $"C{door.Id}Floor", mid + new Vector3(0f, 0.05f, 0f), size, floor);
                var roofSize = deeper ? new Vector3(DoorGap + 0.8f, 0.3f, span) : new Vector3(span, 0.3f, DoorGap + 0.8f);
                Slab(root.transform, $"C{door.Id}Roof", mid + new Vector3(0f, 2.9f, 0f), roofSize, rock);
                if (deeper)
                {
                    Slab(root.transform, $"C{door.Id}WallL", mid + new Vector3(-(DoorGap * 0.5f + 0.2f), 0.9f, 0f), new Vector3(0.4f, WallHeight, span), rock);
                    Slab(root.transform, $"C{door.Id}WallR", mid + new Vector3(DoorGap * 0.5f + 0.2f, 0.9f, 0f), new Vector3(0.4f, WallHeight, span), rock);
                }
                else
                {
                    Slab(root.transform, $"C{door.Id}WallN", mid + new Vector3(0f, 0.9f, -(DoorGap * 0.5f + 0.2f)), new Vector3(span, WallHeight, 0.4f), rock);
                    Slab(root.transform, $"C{door.Id}WallS", mid + new Vector3(0f, 0.9f, DoorGap * 0.5f + 0.2f), new Vector3(span, WallHeight, 0.4f), rock);
                }

                bool touchesBoss = door.FromRoomId == bossRoomId || door.ToRoomId == bossRoomId;
                if (touchesBoss && !bossDoorPlaced)
                {
                    bossDoorPlaced = true;
                    bossDoorLocal = mid;
                    var doorSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    doorSlab.name = "BossDoor";
                    doorSlab.transform.SetParent(root.transform, worldPositionStays: false);
                    doorSlab.transform.localPosition = mid + new Vector3(0f, 1.35f, 0f);
                    doorSlab.transform.localScale = deeper
                        ? new Vector3(DoorGap + 0.5f, 2.7f, 0.35f)  // block the Z passage
                        : new Vector3(0.35f, 2.7f, DoorGap + 0.5f); // block the X passage
                    doorSlab.GetComponent<MeshRenderer>().sharedMaterial =
                        RuntimeMaterialPalette.Solid(new Color(0.30f, 0.28f, 0.24f));
                    doorSlab.AddComponent<RuntimeLockedDoorView>();
                }
                else if (!trapPlaced)
                {
                    trapPlaced = true;
                    trapLocal = mid;
                    var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plate.name = "CrushingPlate";
                    plate.transform.SetParent(root.transform, worldPositionStays: false);
                    plate.transform.localPosition = mid + new Vector3(0f, 0.13f, 0f);
                    plate.transform.localScale = new Vector3(1.5f, 0.07f, 1.5f);
                    plate.GetComponent<MeshRenderer>().sharedMaterial =
                        RuntimeMaterialPalette.Solid(new Color(0.45f, 0.14f, 0.10f)); // rust-blood metal reads DANGER
                    plate.AddComponent<RuntimeTrapView>();
                }
            }
            if (!trapPlaced) // degenerate two-room graphs: the entry hall carries the plate
            {
                trapLocal = new Vector3(0f, 0f, entryMidZ);
                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = "CrushingPlate";
                plate.transform.SetParent(root.transform, worldPositionStays: false);
                plate.transform.localPosition = trapLocal + new Vector3(0f, 0.13f, 0f);
                plate.transform.localScale = new Vector3(1.5f, 0.07f, 1.5f);
                plate.GetComponent<MeshRenderer>().sharedMaterial =
                    RuntimeMaterialPalette.Solid(new Color(0.45f, 0.14f, 0.10f));
                plate.AddComponent<RuntimeTrapView>();
            }

            // F20 KEY: a pedestal in a deterministic MIDDLE room (never start, never boss).
            var keyEligible = new List<EmberCrpg.Domain.World.DungeonRoom>();
            foreach (var room in rooms)
                if (room.Id != layout.StartRoomId && room.Id != bossRoomId)
                    keyEligible.Add(room);
            var keyRoom = keyEligible.Count > 0
                ? keyEligible[(int)(spotHash % (uint)keyEligible.Count)]
                : layout.FindRoom(layout.StartRoomId);
            var keyC = CellCenter(keyRoom);
            var keyLocal = keyC + new Vector3(-keyRoom.Width * 0.25f, 0f, -keyRoom.Height * 0.25f);
            var pedestal = new GameObject("KeyPedestal");
            pedestal.transform.SetParent(root.transform, worldPositionStays: false);
            pedestal.transform.localPosition = keyLocal;
            Slab(pedestal.transform, "PedestalBase", new Vector3(0f, 0.25f, 0f), new Vector3(0.4f, 0.5f, 0.4f),
                RuntimeMaterialPalette.Solid(new Color(0.30f, 0.30f, 0.33f)));
            Slab(pedestal.transform, "KeyGlint", new Vector3(0f, 0.62f, 0f), new Vector3(0.2f, 0.1f, 0.34f),
                RuntimeMaterialPalette.Solid(new Color(0.88f, 0.74f, 0.28f))); // tarnished gold catches the torch
            pedestal.AddComponent<RuntimeKeyView>();

            // BOSS room: the big chest at the back + the boss spot beside it.
            var bossRoom = layout.FindRoom(bossRoomId);
            var bossC = CellCenter(bossRoom);
            var chestLocal = bossC + new Vector3(0f, 0f, -(bossRoom.Height * 0.5f) + 1.6f);
            var chestRoot = new GameObject("DungeonChest");
            chestRoot.transform.SetParent(root.transform, worldPositionStays: false);
            chestRoot.transform.localPosition = chestLocal;
            chestRoot.transform.localScale = Vector3.one * 1.4f; // the boss hoard reads BIGGER
            Slab(chestRoot.transform, "ChestBody", new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f),
                RuntimeMaterialPalette.Solid(new Color(0.36f, 0.24f, 0.13f)));
            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "ChestLid";
            lid.transform.SetParent(chestRoot.transform, worldPositionStays: false);
            lid.transform.localPosition = new Vector3(0f, 0.78f, -0.43f);
            lid.transform.localScale = new Vector3(1.26f, 0.18f, 0.86f);
            var lidRenderer = lid.GetComponent<MeshRenderer>();
            if (lidRenderer != null) lidRenderer.sharedMaterial = RuntimeMaterialPalette.Solid(new Color(0.30f, 0.20f, 0.11f));
            Slab(chestRoot.transform, "ChestBand", new Vector3(0f, 0.5f, 0.41f), new Vector3(0.25f, 0.5f, 0.06f),
                RuntimeMaterialPalette.Solid(new Color(0.78f, 0.62f, 0.22f)));
            chestRoot.AddComponent<RuntimeChestView>().Bind(lid.transform);

            // Footprint bounds for the proof topdown camera.
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var room in rooms)
            {
                var c = CellCenter(room);
                minX = Mathf.Min(minX, c.x - room.Width * 0.5f); maxX = Mathf.Max(maxX, c.x + room.Width * 0.5f);
                minZ = Mathf.Min(minZ, c.z - room.Height * 0.5f); maxZ = Mathf.Max(maxZ, c.z + room.Height * 0.5f);
            }
            var footprintCenter = root.transform.TransformPoint(new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f));
            float extent = Mathf.Max(maxX - minX, maxZ - minZ);

            RuntimeDungeonLayoutInfo.Record(
                rooms.Count,
                root.transform.TransformPoint(new Vector3(0f, 0f, 1.5f)),
                root.transform.TransformPoint(CellCenter(room0)),
                root.transform.TransformPoint(bossC),
                chestRoot.transform.position,
                footprintCenter,
                extent,
                dwellerSpots,
                root.transform.TransformPoint(chestLocal + new Vector3(1.8f, 0f, 1.2f)),
                root.transform.TransformPoint(trapLocal),
                pedestal.transform.position,
                root.transform.TransformPoint(bossDoorLocal));

            Debug.Log($"[WorldDirector] multi-room delve realized: archetype={archetype.Name} rooms={rooms.Count} " +
                      $"doors={layout.Doors.Count} dwellerSpots={dwellerSpots.Count} bossRoom=R{bossRoomId} seed={dungeonSeed}.");
            return root;
        }

        // A room wall along X or Z, with an optional centred 2.6m door gap (two flanking segments).
        private static void WallWithGap(Transform parent, string name, Vector3 center, float length, bool alongX, bool gap, Material material)
        {
            if (!gap)
            {
                Slab(parent, name, center,
                    alongX ? new Vector3(length + 0.4f, WallHeight, 0.4f) : new Vector3(0.4f, WallHeight, length + 0.4f),
                    material);
                return;
            }
            float seg = (length - DoorGap) * 0.5f;
            if (seg <= 0.2f) return; // doorway wider than the wall — leave it fully open
            float off = (DoorGap + seg) * 0.5f;
            if (alongX)
            {
                Slab(parent, name + "a", center + new Vector3(-off, 0f, 0f), new Vector3(seg + 0.2f, WallHeight, 0.4f), material);
                Slab(parent, name + "b", center + new Vector3(off, 0f, 0f), new Vector3(seg + 0.2f, WallHeight, 0.4f), material);
            }
            else
            {
                Slab(parent, name + "a", center + new Vector3(0f, 0f, -off), new Vector3(0.4f, WallHeight, seg + 0.2f), material);
                Slab(parent, name + "b", center + new Vector3(0f, 0f, off), new Vector3(0.4f, WallHeight, seg + 0.2f), material);
            }
        }

        // Ground height under a world point: ray from high above against all colliders (terrain tiles are
        // built BEFORE the dungeon in Realize, so the cast is reliable at call time).
        private static float SampleGroundY(Vector3 world)
        {
            return Physics.Raycast(world + Vector3.up * 90f, Vector3.down, out var hit, 220f)
                ? hit.point.y
                : 0f;
        }

        private static void Torch(Transform parent, Vector3 localPosition, float range, Color color, float intensity)
        {
            var go = new GameObject("Torch");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPosition;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;

            var flame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flame.name = "Flame";
            flame.transform.SetParent(go.transform, worldPositionStays: false);
            flame.transform.localPosition = Vector3.zero;
            flame.transform.localScale = new Vector3(0.14f, 0.22f, 0.14f);
            Object.Destroy(flame.GetComponent<Collider>());
            flame.GetComponent<MeshRenderer>().sharedMaterial =
                RuntimeMaterialPalette.Solid(new Color(1f, 0.78f, 0.38f));
        }

        private static void Slab(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            slab.transform.SetParent(parent, worldPositionStays: false);
            slab.transform.localPosition = localPosition;
            slab.transform.localScale = size;
            var renderer = slab.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }
    }
}
