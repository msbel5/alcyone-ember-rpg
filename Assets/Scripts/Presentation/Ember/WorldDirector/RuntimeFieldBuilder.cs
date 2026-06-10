using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// LIVING FIELDS (F1/crops): the adapter publishes the home site's REAL PlantGrowth stage census here
    /// every tick, and the realized farm plot's stalks read it — crops visibly rise from seed stubble to
    /// ripe gold as the sim's days pass. Static channel (same pattern as RuntimeNpcDensity) because the
    /// director runs before the adapter's first tick.
    /// </summary>
    public static class RuntimeFieldMirror
    {
        /// <summary>F6: sim hour-of-day (0-23), published per tick — night-time street staging reads it.</summary>
        public static int HourOfDay { get; set; }

        public static int PlantCount { get; private set; }

        /// <summary>0 = seed, 1 = sprout, 2 = ripe (dominant stage among the home site's plants).</summary>
        public static int StageIndex { get; private set; }

        public static void Publish(int plantCount, int stageIndex)
        {
            PlantCount = plantCount;
            StageIndex = stageIndex < 0 ? 0 : (stageIndex > 2 ? 2 : stageIndex);
        }
    }

    /// <summary>Per-stalk view: rescales + recolours itself to the mirrored growth stage every few seconds.</summary>
    public sealed class CropStalkView : MonoBehaviour
    {
        private static readonly Color[] StageColors =
        {
            new Color(0.45f, 0.38f, 0.22f), // seed stubble
            new Color(0.38f, 0.55f, 0.24f), // green sprout
            new Color(0.78f, 0.65f, 0.25f), // ripe gold
        };
        private static readonly float[] StageHeights = { 0.15f, 0.55f, 1.05f };

        private float _nextPoll;
        private int _shownStage = -1;

        private void Update()
        {
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;
            int stage = RuntimeFieldMirror.StageIndex;
            if (stage == _shownStage) return;
            _shownStage = stage;

            float h = StageHeights[stage];
            transform.localScale = new Vector3(0.22f, h, 0.22f);
            var pos = transform.localPosition;
            transform.localPosition = new Vector3(pos.x, h / 2f, pos.z);
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = RuntimeMaterialPalette.Solid(StageColors[stage]);
        }
    }

    /// <summary>Realizes one tilled farm plot (soil slab + a grid of living stalks) at the settlement edge.</summary>
    public static class RuntimeFieldBuilder
    {
        public static GameObject Build(Transform parent, float distance, float angleDeg)
        {
            var root = new GameObject("FarmPlot");
            root.transform.SetParent(parent, worldPositionStays: false);
            float rad = angleDeg * Mathf.Deg2Rad;
            root.transform.localPosition = new Vector3(Mathf.Cos(rad) * distance, 0f, Mathf.Sin(rad) * distance);

            var soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "Soil";
            soil.transform.SetParent(root.transform, worldPositionStays: false);
            soil.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            soil.transform.localScale = new Vector3(9f, 0.08f, 6.5f);
            soil.GetComponent<MeshRenderer>().sharedMaterial =
                RuntimeMaterialPalette.Solid(new Color(0.30f, 0.21f, 0.13f));

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    var stalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stalk.name = "CropStalk";
                    Object.Destroy(stalk.GetComponent<Collider>()); // walk-through crops
                    stalk.transform.SetParent(root.transform, worldPositionStays: false);
                    stalk.transform.localPosition = new Vector3(-3.2f + (col * 1.6f), 0.1f, -2.0f + (row * 2.0f));
                    stalk.AddComponent<CropStalkView>();
                }
            }
            return root;
        }
    }
}
