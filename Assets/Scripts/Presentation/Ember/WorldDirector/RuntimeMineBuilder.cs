using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Realizes a mine mouth at the settlement edge when the planet's ore layer says the local tile is rich
    /// (content phase: the IronOre/Coal data finally reaches the ground; the sim's worksite jobs get their
    /// visible anchor). One stone mound, a dark recessed mouth, a timber frame, and an ore cart — primitive
    /// construction, same bulletproof Solid() materials as the interiors.
    /// </summary>
    public static class RuntimeMineBuilder
    {
        public static GameObject Build(Transform parent, float distance, float angleDeg, bool coal)
        {
            var root = new GameObject(coal ? "CoalMine" : "IronMine");
            root.transform.SetParent(parent, worldPositionStays: false);
            float rad = angleDeg * Mathf.Deg2Rad;
            root.transform.localPosition = new Vector3(Mathf.Cos(rad) * distance, 0f, Mathf.Sin(rad) * distance);
            root.transform.localRotation = Quaternion.Euler(0f, -angleDeg + 90f, 0f); // mouth faces the town

            var stone = RuntimeMaterialPalette.Solid(new Color(0.31f, 0.30f, 0.29f));
            var dark = RuntimeMaterialPalette.Solid(new Color(0.05f, 0.05f, 0.06f));
            var beam = RuntimeMaterialPalette.Solid(new Color(0.42f, 0.31f, 0.18f));
            var ore = RuntimeMaterialPalette.Solid(coal ? new Color(0.12f, 0.12f, 0.13f) : new Color(0.48f, 0.30f, 0.20f));

            Slab(root.transform, "Mound", new Vector3(0f, 1.6f, -1.2f), new Vector3(7f, 3.2f, 5f), stone);
            Slab(root.transform, "Mouth", new Vector3(0f, 1.1f, 1.05f), new Vector3(2.2f, 2.2f, 0.4f), dark);
            Slab(root.transform, "PostL", new Vector3(-1.3f, 1.25f, 1.3f), new Vector3(0.3f, 2.5f, 0.3f), beam);
            Slab(root.transform, "PostR", new Vector3(1.3f, 1.25f, 1.3f), new Vector3(0.3f, 2.5f, 0.3f), beam);
            Slab(root.transform, "Lintel", new Vector3(0f, 2.6f, 1.3f), new Vector3(3.0f, 0.3f, 0.3f), beam);
            Slab(root.transform, "Cart", new Vector3(2.4f, 0.4f, 1.8f), new Vector3(1.1f, 0.8f, 0.8f), beam);
            Slab(root.transform, "OreHeap", new Vector3(2.4f, 0.95f, 1.8f), new Vector3(0.9f, 0.35f, 0.6f), ore);
            return root;
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
