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

        /// <summary>F24: sim minutes-of-day (0-1439), published per tick from world-time TRUTH —
        /// the sky's sun/star cycle reads this (tick re-derivations drifted after clock jumps).
        /// Defaults to 08:00, the WorldFactory start, so a pre-tick frame still looks like morning.</summary>
        public static int MinutesOfDay { get; set; } = 8 * 60;

        /// <summary>F25: 1-based world day, published per tick — the deterministic weather pick keys on it.</summary>
        public static int WorldDay { get; set; } = 1;

        public static int PlantCount { get; private set; }

        /// <summary>0 = seed, 1 = sprout, 2 = ripe (dominant stage among the home site's plants).</summary>
        public static int StageIndex { get; private set; }

        public static void Publish(int plantCount, int stageIndex)
        {
            PlantCount = plantCount;
            StageIndex = stageIndex < 0 ? 0 : (stageIndex > 2 ? 2 : stageIndex);
        }

        /// <summary>REFORM #1 (one spatial authority): the CURRENT settlement plants as
        /// sim-projected local cells - the visual field IS the sim field, no polar decor.
        /// HARVEST-CHAIN FIX: SoilId is the plot's identity so the tilled cube outlives the plant —
        /// harvest destroys the stalk (transient) but keeps the soil (persistent), and the
        /// deterministic same-soil replant grows a fresh stalk out of the existing tilled bed.</summary>
        public struct PlantCell { public ulong Id; public ulong SoilId; public int LocalX; public int LocalZ; public int Stage; }
        public static PlantCell[] Plants { get; private set; } = System.Array.Empty<PlantCell>();
        public static int PlantsStamp { get; private set; }
        public static void PublishPlants(PlantCell[] plants)
        {
            Plants = plants ?? System.Array.Empty<PlantCell>();
            PlantsStamp++;
        }

        /// <summary>Sim SOIL rows as their own channel — the plot GameObject follows soil identity,
        /// not plant identity, so an emptied bed stays visible between harvest and replant.</summary>
        public struct SoilCell { public ulong Id; public int LocalX; public int LocalZ; }
        public static SoilCell[] Soils { get; private set; } = System.Array.Empty<SoilCell>();
        public static int SoilsStamp { get; private set; }
        public static void PublishSoils(SoilCell[] soils)
        {
            Soils = soils ?? System.Array.Empty<SoilCell>();
            SoilsStamp++;
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

        /// <summary>REFORM #1: when a SimFieldView drives this stalk, ITS plant stage wins
        /// over the settlement-dominant mirror value (-1 = legacy mirror mode).</summary>
        public int ExternalStage = -1;

        private float _targetHeight = -1f;

        private void Update()
        {
            if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + 2f;
                int stage = ExternalStage >= 0 ? ExternalStage : RuntimeFieldMirror.StageIndex;
                if (stage != _shownStage)
                {
                    _shownStage = stage;
                    _targetHeight = StageHeights[stage];
                    var renderer = GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.sharedMaterial = RuntimeMaterialPalette.Solid(StageColors[stage]);
                }
            }
            // PLAYTEST FIX ("ekinler birden yok oluyor"): stalks GROW and WANE over seconds
            // instead of popping - the eye reads a harvest, not a glitch. (The walking harvester
            // that triggers it is roadmap M6.)
            if (_targetHeight > 0f && !Mathf.Approximately(transform.localScale.y, _targetHeight))
            {
                float h = Mathf.MoveTowards(transform.localScale.y, _targetHeight, Time.deltaTime * 0.18f);
                transform.localScale = new Vector3(0.22f, h, 0.22f);
                var pos = transform.localPosition;
                transform.localPosition = new Vector3(pos.x, h / 2f, pos.z);
            }
        }
    }

    /// <summary>
    /// Realizes the settlement's FARM BELT: plot count derives from population (F7, DF ratios — one farmer
    /// works ~10 plots and feeds 3-7 people; the belt is the symbolic visual of that rural district).
    /// Plots fan out along an arc at the town edge.
    /// </summary>
    public static class RuntimeFieldBuilder
    {
        public static GameObject BuildBelt(Transform parent, float distance, float angleDeg, int plots)
        {
            var belt = new GameObject("FarmBelt");
            belt.transform.SetParent(parent, worldPositionStays: false);
            const float arcStepDeg = 9f; // ~12m apart at the default radius
            float start = angleDeg - (arcStepDeg * (plots - 1) / 2f);
            for (int i = 0; i < plots; i++)
                Build(belt.transform, distance, start + (i * arcStepDeg));
            return belt;
        }

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

    /// <summary>
    /// REFORM #1 (ARCHITECTURE_GAPS #4): crops render AT the sim plants projected cells,
    /// one stalk per PlantComponent, each wearing ITS OWN stage - the seed-angled polar
    /// belt is retired. Plots appear/prune as the sim adds/removes plants.
    /// HARVEST-CHAIN FIX (playtest report "tarla harvestten sonra kaybolmustu"): the plot
    /// GameObject follows SOIL identity (persistent), the stalk follows PLANT identity
    /// (transient). Harvest destroys the stalk only; the tilled bed stays; the deterministic
    /// same-soil replant grows a fresh stalk out of the surviving plot.
    /// </summary>
    public sealed class SimFieldView : MonoBehaviour
    {
        private readonly System.Collections.Generic.Dictionary<ulong, GameObject> _plots
            = new System.Collections.Generic.Dictionary<ulong, GameObject>();
        private readonly System.Collections.Generic.Dictionary<ulong, CropStalkView> _stalks
            = new System.Collections.Generic.Dictionary<ulong, CropStalkView>();
        private readonly System.Collections.Generic.HashSet<ulong> _liveSoils
            = new System.Collections.Generic.HashSet<ulong>();
        private readonly System.Collections.Generic.HashSet<ulong> _livePlants
            = new System.Collections.Generic.HashSet<ulong>();
        private int _seenSoilsStamp = -1;
        private int _seenPlantsStamp = -1;
        private float _nextPoll;

        private void Update()
        {
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 1.5f;

            bool soilsChanged = RuntimeFieldMirror.SoilsStamp != _seenSoilsStamp;
            bool plantsChanged = RuntimeFieldMirror.PlantsStamp != _seenPlantsStamp;
            if (!soilsChanged && !plantsChanged) return;

            if (soilsChanged)
            {
                _seenSoilsStamp = RuntimeFieldMirror.SoilsStamp;
                _liveSoils.Clear();
                foreach (var cell in RuntimeFieldMirror.Soils)
                {
                    _liveSoils.Add(cell.Id);
                    if (!_plots.TryGetValue(cell.Id, out var plot) || plot == null)
                        _plots[cell.Id] = BuildPlot(cell);
                }
                var deadSoils = new System.Collections.Generic.List<ulong>();
                foreach (var kv in _plots)
                    if (!_liveSoils.Contains(kv.Key)) deadSoils.Add(kv.Key);
                foreach (var id in deadSoils)
                {
                    if (_plots[id] != null) Destroy(_plots[id]);
                    _plots.Remove(id);
                }
            }

            if (plantsChanged)
            {
                _seenPlantsStamp = RuntimeFieldMirror.PlantsStamp;
                _livePlants.Clear();
                foreach (var cell in RuntimeFieldMirror.Plants)
                {
                    _livePlants.Add(cell.Id);
                    if (!_stalks.TryGetValue(cell.Id, out var stalk) || stalk == null)
                        _stalks[cell.Id] = stalk = BuildStalk(cell);
                    if (stalk != null) stalk.ExternalStage = cell.Stage;
                }
                var deadPlants = new System.Collections.Generic.List<ulong>();
                foreach (var kv in _stalks)
                    if (!_livePlants.Contains(kv.Key)) deadPlants.Add(kv.Key);
                foreach (var id in deadPlants)
                {
                    // Destroy the STALK only — the parent plot survives so the tilled bed stays.
                    if (_stalks[id] != null) Destroy(_stalks[id].gameObject);
                    _stalks.Remove(id);
                }
            }
        }

        private GameObject BuildPlot(RuntimeFieldMirror.SoilCell cell)
        {
            var plot = new GameObject("SimPlot_" + cell.Id);
            plot.transform.SetParent(transform, worldPositionStays: false);
            plot.transform.localPosition = new Vector3(cell.LocalX, 0f, cell.LocalZ);

            var soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "Soil";
            Object.Destroy(soil.GetComponent<Collider>());
            soil.transform.SetParent(plot.transform, worldPositionStays: false);
            soil.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            soil.transform.localScale = new Vector3(2.4f, 0.08f, 1.8f);
            soil.GetComponent<MeshRenderer>().sharedMaterial =
                RuntimeMaterialPalette.Solid(new Color(0.30f, 0.21f, 0.13f));
            return plot;
        }

        private CropStalkView BuildStalk(RuntimeFieldMirror.PlantCell cell)
        {
            // Prefer the sim-authoritative plot for this plant's soil. Fallback (rare: PublishPlants
            // arrived before PublishSoils in the same frame): synthesize a plot at the plant's
            // local cell so a stalk still renders and the next Soils publish adopts it.
            if (!_plots.TryGetValue(cell.SoilId, out var plot) || plot == null)
            {
                plot = BuildPlot(new RuntimeFieldMirror.SoilCell
                { Id = cell.SoilId, LocalX = cell.LocalX, LocalZ = cell.LocalZ });
                _plots[cell.SoilId] = plot;
            }
            var stalkGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stalkGo.name = "CropStalk";
            Object.Destroy(stalkGo.GetComponent<Collider>());
            stalkGo.transform.SetParent(plot.transform, worldPositionStays: false);
            stalkGo.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            return stalkGo.AddComponent<CropStalkView>();
        }
    }
}

