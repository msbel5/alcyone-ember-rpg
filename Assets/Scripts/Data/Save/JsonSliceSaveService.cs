using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using UnityEngine;

// Design note:
// JsonSliceSaveService converts Sprint 1 world state to and from JSON text.
// Inputs: pure world snapshots or JSON strings.
// Outputs: pretty JSON and reconstructed world state via DTO mapping.
// Bible reference: PRD FR-06.
namespace EmberCrpg.Data.Save
{
    /// <summary>JsonUtility-backed save/load bridge for the vertical slice.</summary>
    public sealed class JsonSliceSaveService
    {
        private readonly Func<RecipeId, RecipeDef> _resolveRecipe;
        private List<RecipeWorkOrder> _recipeWorkOrders = new List<RecipeWorkOrder>();
        private WorksiteStore _worksites = new WorksiteStore();
        private JobBoard _jobs = new JobBoard();
        private ComponentStore<SoilComponent> _soils = new ComponentStore<SoilComponent>();
        private ComponentStore<PlantComponent> _plants = new ComponentStore<PlantComponent>();

        public JsonSliceSaveService(Func<RecipeId, RecipeDef> resolveRecipe = null)
        {
            _resolveRecipe = resolveRecipe;
        }

        /// <summary>Process worksites carried by this save bridge until the full world-root process store lands.</summary>
        public WorksiteStore Worksites
        {
            get { return _worksites; }
            set { _worksites = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>Pending and claimed process jobs carried by this save bridge until the full world-root process store lands.</summary>
        public JobBoard Jobs
        {
            get { return _jobs; }
            set { _jobs = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>Soil components carried by this save bridge until they move onto the full world root.</summary>
        public ComponentStore<SoilComponent> Soils
        {
            get { return _soils; }
            set { _soils = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>Plant components carried by this save bridge until they move onto the full world root.</summary>
        public ComponentStore<PlantComponent> Plants
        {
            get { return _plants; }
            set { _plants = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>Active recipe work orders loaded by the latest JSON round-trip.</summary>
        public IReadOnlyList<RecipeWorkOrder> RecipeWorkOrders => _recipeWorkOrders;

        public void ReplaceRecipeWorkOrders(IEnumerable<RecipeWorkOrder> orders)
        {
            if (orders == null)
                throw new ArgumentNullException(nameof(orders));
            _recipeWorkOrders = orders.Select(order => order ?? throw new ArgumentException("Recipe work orders cannot contain null entries.", nameof(orders))).ToList();
        }

        public string SaveToJson(SliceWorldState world)
        {
            var data = SliceSaveMapper.ToData(world);
            data.worksites = SliceSaveMapper.ToWorksiteData(_worksites);
            data.recipeWorkOrders = _recipeWorkOrders.Select(SliceSaveMapper.ToRecipeWorkOrderData).ToArray();
            data.jobs = SliceSaveMapper.ToJobBoardData(_jobs);
            data.soils = SliceSaveMapper.ToSoilComponentData(_soils);
            data.plants = SliceSaveMapper.ToPlantComponentData(_plants);
            return JsonUtility.ToJson(data, true);
        }

        public SliceWorldState LoadFromJson(string json)
        {
            // Codex audit (A/P3): JsonUtility.FromJson<T>(null/empty) returns
            // an empty SliceSaveData with default everything, which would
            // round-trip into a vanilla world but mask a caller-side data
            // outage (corrupt PlayerPrefs, dropped HTTP body, etc.). Fail
            // fast instead so the caller can decide whether to fall back to
            // NewGame or surface a save-corruption notice.
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Save JSON must be non-empty.", nameof(json));

            var data = JsonUtility.FromJson<SliceSaveData>(json);
            if (data == null)
                throw new InvalidOperationException("Save JSON did not deserialize into a SliceSaveData payload.");
            var world = SliceSaveMapper.ToWorld(data);
            _worksites = SliceSaveMapper.ToWorksiteStore(data.worksites);
            _recipeWorkOrders = ToRecipeWorkOrders(data.recipeWorkOrders);
            _jobs = SliceSaveMapper.ToJobBoard(data.jobs);
            _soils = SliceSaveMapper.ToSoilComponentStore(data.soils);
            _plants = SliceSaveMapper.ToPlantComponentStore(data.plants);
            return world;
        }

        private List<RecipeWorkOrder> ToRecipeWorkOrders(RecipeWorkOrderSaveData[] data)
        {
            if (data == null || data.Length == 0)
                return new List<RecipeWorkOrder>();
            if (_resolveRecipe == null)
                throw new InvalidOperationException("JsonSliceSaveService needs a recipe resolver to load active recipe work orders.");

            return data.Select(order => SliceSaveMapper.ToRecipeWorkOrder(order, _resolveRecipe)).ToList();
        }
    }
}
