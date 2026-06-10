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

            var rock = RuntimeMaterialPalette.Solid(new Color(0.14f, 0.13f, 0.12f));
            var floor = RuntimeMaterialPalette.Solid(new Color(0.10f, 0.09f, 0.09f));

            // Corridor: 3m wide, 12m deep, walled + roofed, starting just behind the mouth.
            Slab(root.transform, "CorrWallL", new Vector3(-1.7f, 1.4f, -7f), new Vector3(0.4f, 2.8f, 12f), rock);
            Slab(root.transform, "CorrWallR", new Vector3(1.7f, 1.4f, -7f), new Vector3(0.4f, 2.8f, 12f), rock);
            Slab(root.transform, "CorrRoof", new Vector3(0f, 2.9f, -7f), new Vector3(3.8f, 0.3f, 12f), rock);
            Slab(root.transform, "CorrFloor", new Vector3(0f, 0.05f, -7f), new Vector3(3.4f, 0.1f, 12f), floor);

            // Chamber: 11×11, roofed, at the corridor's end.
            var c = new Vector3(0f, 0f, -18.5f);
            Slab(root.transform, "ChamberN", c + new Vector3(0f, 1.9f, -5.5f), new Vector3(11f, 3.8f, 0.5f), rock);
            Slab(root.transform, "ChamberW", c + new Vector3(-5.5f, 1.9f, 0f), new Vector3(0.5f, 3.8f, 11f), rock);
            Slab(root.transform, "ChamberE", c + new Vector3(5.5f, 1.9f, 0f), new Vector3(0.5f, 3.8f, 11f), rock);
            Slab(root.transform, "ChamberS_L", c + new Vector3(-3.4f, 1.9f, 5.5f), new Vector3(4.2f, 3.8f, 0.5f), rock);
            Slab(root.transform, "ChamberS_R", c + new Vector3(3.4f, 1.9f, 5.5f), new Vector3(4.2f, 3.8f, 0.5f), rock);
            Slab(root.transform, "ChamberRoof", c + new Vector3(0f, 3.9f, 0f), new Vector3(11.5f, 0.3f, 11.5f), rock);
            Slab(root.transform, "ChamberFloor", c + new Vector3(0f, 0.05f, 0f), new Vector3(10.5f, 0.1f, 10.5f), floor);

            // Two torches (corridor mid + chamber) — warm flicker-coloured points, no shadows (cheap).
            Torch(root.transform, new Vector3(0f, 2.2f, -7f), 7f);
            Torch(root.transform, c + new Vector3(0f, 2.6f, 0f), 12f);

            // Loot chest against the chamber's back wall: body, lid, gold band.
            var chest = new Vector3(0f, 0f, -22.5f);
            Slab(root.transform, "ChestBody", chest + new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f),
                RuntimeMaterialPalette.Solid(new Color(0.36f, 0.24f, 0.13f)));
            Slab(root.transform, "ChestLid", chest + new Vector3(0f, 0.78f, 0f), new Vector3(1.26f, 0.18f, 0.86f),
                RuntimeMaterialPalette.Solid(new Color(0.30f, 0.20f, 0.11f)));
            Slab(root.transform, "ChestBand", chest + new Vector3(0f, 0.5f, 0.41f), new Vector3(0.25f, 0.5f, 0.06f),
                RuntimeMaterialPalette.Solid(new Color(0.78f, 0.62f, 0.22f)));
            return root;
        }

        private static void Torch(Transform parent, Vector3 localPosition, float range)
        {
            var go = new GameObject("Torch");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPosition;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.30f);
            light.intensity = 1.6f;
            light.range = range;
            light.shadows = LightShadows.None;
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
