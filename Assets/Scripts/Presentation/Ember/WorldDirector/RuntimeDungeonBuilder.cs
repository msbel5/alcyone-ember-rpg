using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F2/dungeon interiors v1: behind the cave mouth, a torch-lit corridor leads into a dark chamber with
    /// a loot chest — the delve finally has an inside. Enemy encounters bind in the next F2 item (combat
    /// wiring); this realizes the space they will haunt. Same primitive + Solid() construction as interiors.
    /// </summary>
    public static class RuntimeDungeonBuilder
    {
        public static GameObject Build(Transform parent, float distance, float angleDeg)
        {
            var root = new GameObject("DungeonInterior");
            root.transform.SetParent(parent, worldPositionStays: false);
            float rad = angleDeg * Mathf.Deg2Rad;
            root.transform.localPosition = new Vector3(Mathf.Cos(rad) * distance, 0f, Mathf.Sin(rad) * distance);
            root.transform.localRotation = Quaternion.Euler(0f, -angleDeg + 90f, 0f); // +Z runs from mouth into the hill

            // HILLSIDE CONFORM (v0.3 eye-proof finding): the interior used to sit at y=0 while the real
            // terrain rose THROUGH it — the chamber read as a grass-filled void. Sample the ground along
            // the corridor axis and float the whole barrow on the highest point; a ramp walks the player
            // up at the mouth, and wall slabs skirt below the floor so no gap shows. Residual limit
            // (honest): on very steep hillsides the terrain can still clip a corner.
            float crest = 0f;
            for (float z = 2f; z >= -25f; z -= 3f)
                crest = Mathf.Max(crest, SampleGroundY(root.transform.TransformPoint(new Vector3(0f, 0f, z))));
            var rootPos = root.transform.position;
            root.transform.position = new Vector3(rootPos.x, crest + 0.05f, rootPos.z);

            var rock = RuntimeMaterialPalette.Solid(new Color(0.20f, 0.18f, 0.17f));
            var floor = RuntimeMaterialPalette.Solid(new Color(0.14f, 0.13f, 0.12f));

            // Mouth ramp: from the terrain up onto the floated floor (walkable, ≤ ~35°).
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

            // Corridor: 3m wide, 12m deep, walled + roofed; walls skirt 1m below the floor.
            Slab(root.transform, "CorrWallL", new Vector3(-1.7f, 0.9f, -7f), new Vector3(0.4f, 3.8f, 12f), rock);
            Slab(root.transform, "CorrWallR", new Vector3(1.7f, 0.9f, -7f), new Vector3(0.4f, 3.8f, 12f), rock);
            Slab(root.transform, "CorrRoof", new Vector3(0f, 2.9f, -7f), new Vector3(3.8f, 0.3f, 12f), rock);
            Slab(root.transform, "CorrFloor", new Vector3(0f, 0.05f, -7f), new Vector3(3.4f, 0.1f, 12f), floor);

            // Chamber: 11×11, roofed, at the corridor's end (walls carry the same below-floor skirt).
            var c = new Vector3(0f, 0f, -18.5f);
            Slab(root.transform, "ChamberN", c + new Vector3(0f, 0.9f, -5.5f), new Vector3(11f, 5.8f, 0.5f), rock);
            Slab(root.transform, "ChamberW", c + new Vector3(-5.5f, 0.9f, 0f), new Vector3(0.5f, 5.8f, 11f), rock);
            Slab(root.transform, "ChamberE", c + new Vector3(5.5f, 0.9f, 0f), new Vector3(0.5f, 5.8f, 11f), rock);
            Slab(root.transform, "ChamberS_L", c + new Vector3(-3.4f, 0.9f, 5.5f), new Vector3(4.2f, 5.8f, 0.5f), rock);
            Slab(root.transform, "ChamberS_R", c + new Vector3(3.4f, 0.9f, 5.5f), new Vector3(4.2f, 5.8f, 0.5f), rock);
            Slab(root.transform, "ChamberRoof", c + new Vector3(0f, 3.9f, 0f), new Vector3(11.5f, 0.3f, 11.5f), rock);
            Slab(root.transform, "ChamberFloor", c + new Vector3(0f, 0.05f, 0f), new Vector3(10.5f, 0.1f, 10.5f), floor);

            // Torches (corridor mid + chamber) — bright enough to read on dark rock (eye-proof: the
            // chamber was pitch black), each with a small self-lit flame cube so the source shows.
            Torch(root.transform, new Vector3(0f, 2.2f, -7f), 9f);
            Torch(root.transform, c + new Vector3(0f, 2.6f, 0f), 14f);

            // Loot chest against the chamber's back wall: body, lid, gold band. F16: it OPENS —
            // proximity + E grants the tier-up sword (RuntimeChestView), the lid hinges back.
            var chest = new Vector3(0f, 0f, -22.5f);
            var chestRoot = new GameObject("DungeonChest");
            chestRoot.transform.SetParent(root.transform, worldPositionStays: false);
            chestRoot.transform.localPosition = chest;
            Slab(chestRoot.transform, "ChestBody", new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f),
                RuntimeMaterialPalette.Solid(new Color(0.36f, 0.24f, 0.13f)));
            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "ChestLid";
            lid.transform.SetParent(chestRoot.transform, worldPositionStays: false);
            lid.transform.localPosition = new Vector3(0f, 0.78f, -0.43f); // hinge at the back edge
            lid.transform.localScale = new Vector3(1.26f, 0.18f, 0.86f);
            var lidRenderer = lid.GetComponent<MeshRenderer>();
            if (lidRenderer != null) lidRenderer.sharedMaterial = RuntimeMaterialPalette.Solid(new Color(0.30f, 0.20f, 0.11f));
            Slab(chestRoot.transform, "ChestBand", new Vector3(0f, 0.5f, 0.41f), new Vector3(0.25f, 0.5f, 0.06f),
                RuntimeMaterialPalette.Solid(new Color(0.78f, 0.62f, 0.22f)));
            chestRoot.AddComponent<RuntimeChestView>().Bind(lid.transform);
            return root;
        }

        // Ground height under a world point: ray from high above against all colliders (terrain tiles are
        // built BEFORE the dungeon in Realize, so the cast is reliable at call time).
        private static float SampleGroundY(Vector3 world)
        {
            return Physics.Raycast(world + Vector3.up * 90f, Vector3.down, out var hit, 220f)
                ? hit.point.y
                : 0f;
        }

        private static void Torch(Transform parent, Vector3 localPosition, float range)
        {
            var go = new GameObject("Torch");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPosition;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.30f);
            light.intensity = 3.4f;
            light.range = range;
            light.shadows = LightShadows.None;

            // The flame itself: a small cube lit by its own point light reads as the glowing source.
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
