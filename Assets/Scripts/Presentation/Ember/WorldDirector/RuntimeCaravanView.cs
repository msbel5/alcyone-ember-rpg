using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// VISIBLE CARAVANS (F1): the adapter publishes how many caravans currently sit at the home site; the
    /// realized trade cart shows itself only while one is in town — arrivals and departures of the sims
    /// daily CaravanSystem become a thing you can watch from the plaza. Same static-mirror pattern as crops.
    /// </summary>
    public static class RuntimeCaravanMirror
    {
        public static int AtSiteCount { get; private set; }

        public static void Publish(int atSiteCount) => AtSiteCount = atSiteCount < 0 ? 0 : atSiteCount;
    }

    /// <summary>Shows/hides the cart body to match the mirror (polled, cheap).</summary>
    public sealed class CaravanCartView : MonoBehaviour
    {
        private float _nextPoll;
        private bool _shown = true;

        private void Update()
        {
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;
            bool show = RuntimeCaravanMirror.AtSiteCount > 0;
            if (show == _shown) return;
            _shown = show;
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(show);
        }
    }

    /// <summary>Realizes one trade cart near the plaza; visible only while a caravan is at the site.</summary>
    public static class RuntimeCaravanBuilder
    {
        public static GameObject Build(Transform parent)
        {
            var root = new GameObject("TradeCart");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = new Vector3(-4.2f, 0f, 4.2f); // plaza edge, across from the banner
            root.AddComponent<CaravanCartView>();

            var wood = RuntimeMaterialPalette.Solid(new Color(0.40f, 0.28f, 0.16f));
            var dark = RuntimeMaterialPalette.Solid(new Color(0.16f, 0.13f, 0.10f));
            var cloth = RuntimeMaterialPalette.Solid(new Color(0.62f, 0.55f, 0.42f));

            Slab(root.transform, "Bed", new Vector3(0f, 0.7f, 0f), new Vector3(2.4f, 0.25f, 1.3f), wood);
            Slab(root.transform, "WheelL", new Vector3(-0.8f, 0.45f, 0.72f), new Vector3(0.9f, 0.9f, 0.12f), dark);
            Slab(root.transform, "WheelR", new Vector3(-0.8f, 0.45f, -0.72f), new Vector3(0.9f, 0.9f, 0.12f), dark);
            Slab(root.transform, "WheelL2", new Vector3(0.8f, 0.45f, 0.72f), new Vector3(0.9f, 0.9f, 0.12f), dark);
            Slab(root.transform, "WheelR2", new Vector3(0.8f, 0.45f, -0.72f), new Vector3(0.9f, 0.9f, 0.12f), dark);
            Slab(root.transform, "Cargo", new Vector3(0f, 1.15f, 0f), new Vector3(1.8f, 0.7f, 1.0f), cloth);
            Slab(root.transform, "Shaft", new Vector3(1.9f, 0.55f, 0f), new Vector3(1.4f, 0.12f, 0.12f), wood);
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
