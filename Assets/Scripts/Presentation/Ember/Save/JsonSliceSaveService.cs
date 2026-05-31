using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using UnityEngine;

// Design note:
// JsonSliceSaveService converts Sprint 1 world state to and from JSON text.
// Inputs: pure world snapshots or JSON strings.
// Outputs: pretty JSON and reconstructed world state via DTO mapping.
// Bible reference: PRD FR-06.
namespace EmberCrpg.Presentation.Ember.Save
{
    /// <summary>JsonUtility-backed save/load bridge for the vertical slice.</summary>
    public sealed class JsonSliceSaveService
    {
        private readonly Func<RecipeId, RecipeDef> _resolveRecipe;
        private List<RecipeWorkOrder> _recipeWorkOrders = new List<RecipeWorkOrder>();

        // SOUL-01: worksites/jobs/soils/plants are now the world root's canonical stores. This bridge
        // world holds them for callers that touch the save service directly (round-trip tests, the
        // pre-world-bound adapter ctor). BindWorld(world) repoints the bridge at the live world so
        // SaveToJson/LoadFromJson and the per-tick systems all read/write the same store instances.
        private WorldState _bridge = new WorldState();

        public JsonSliceSaveService(Func<RecipeId, RecipeDef> resolveRecipe = null)
        {
            _resolveRecipe = resolveRecipe;
        }

        /// <summary>
        /// Repoints this bridge's process stores at the supplied live world so the save service and
        /// the per-tick simulation share one set of Worksite/Job/Soil/Plant instances. Returns the
        /// bound world for fluent use.
        /// </summary>
        public WorldState BindWorld(WorldState world)
        {
            _bridge = world ?? throw new ArgumentNullException(nameof(world));
            _bridge.EnsureInvariants();
            return _bridge;
        }

        /// <summary>Process worksites homed on the world root (exposed here for save-bridge callers).</summary>
        public WorksiteStore Worksites
        {
            get { return _bridge.Worksites; }
            set { _bridge.Worksites = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>Pending and claimed process jobs homed on the world root (exposed here for save-bridge callers).</summary>
        public JobBoard Jobs
        {
            get { return _bridge.Jobs; }
            set { _bridge.Jobs = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>Soil components homed on the world root (exposed here for save-bridge callers).</summary>
        public ComponentStore<SoilComponent> Soils
        {
            get { return _bridge.Soils; }
            set { _bridge.Soils = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>Plant components homed on the world root (exposed here for save-bridge callers).</summary>
        public ComponentStore<PlantComponent> Plants
        {
            get { return _bridge.Plants; }
            set { _bridge.Plants = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>Active recipe work orders loaded by the latest JSON round-trip.</summary>
        public IReadOnlyList<RecipeWorkOrder> RecipeWorkOrders => _recipeWorkOrders;

        public void ReplaceRecipeWorkOrders(IEnumerable<RecipeWorkOrder> orders)
        {
            if (orders == null)
                throw new ArgumentNullException(nameof(orders));
            _recipeWorkOrders = orders.Select(order => order ?? throw new ArgumentException("Recipe work orders cannot contain null entries.", nameof(orders))).ToList();
        }

        public string SaveToJson(WorldState world)
        {
            // ToData now reads the four process stores from the world root. When a caller has staged
            // worksites/jobs/soils/plants on this bridge (round-trip tests, or the adapter before it
            // binds the live world), override those DTO fields from the bridge so they still persist.
            // For a bound adapter the bridge IS the saved world, making the override idempotent.
            var data = WorldSaveMapper.ToData(world);
            data.worksites = WorldSaveMapper.ToWorksiteData(_bridge.Worksites);
            data.recipeWorkOrders = _recipeWorkOrders.Select(EmberCrpg.Simulation.Process.WorldSaveRehydration.ToRecipeWorkOrderData).ToArray();
            data.jobs = WorldSaveMapper.ToJobBoardData(_bridge.Jobs);
            data.soils = WorldSaveMapper.ToSoilComponentData(_bridge.Soils);
            data.plants = WorldSaveMapper.ToPlantComponentData(_bridge.Plants);
            return JsonUtility.ToJson(data, true);
        }

        public WorldState LoadFromJson(string json)
        {
            // Codex audit (A/P3): JsonUtility.FromJson<T>(null/empty) returns
            // an empty WorldSaveData with default everything, which would
            // round-trip into a vanilla world but mask a caller-side data
            // outage (corrupt PlayerPrefs, dropped HTTP body, etc.). Fail
            // fast instead so the caller can decide whether to fall back to
            // NewGame or surface a save-corruption notice.
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Save JSON must be non-empty.", nameof(json));

            var data = JsonUtility.FromJson<WorldSaveData>(json);
            if (data == null)
                throw new InvalidOperationException("Save JSON did not deserialize into a WorldSaveData payload.");
            // Codex audit (seventh pass B-P1 #10): WorldSaveMapper.ToWorld
            // no longer constructs the seed world (would have leaked a
            // Simulation type into Data). Build the seed here, then map.
            var seedWorld = EmberCrpg.Simulation.Process.WorldSaveRehydration.CreateSeedWorld((int)data.roomSeed);
            var world = WorldSaveMapper.ToWorld(data, seedWorld);
            // ToWorld already rehydrated world.Worksites/Jobs/Soils/Plants from the DTO. Mirror those
            // store instances onto the bridge so callers reading service.Worksites/Jobs/Soils/Plants
            // (round-trip tests, view-models) observe the loaded state.
            _bridge.Worksites = world.Worksites;
            _bridge.Jobs = world.Jobs;
            _bridge.Soils = world.Soils;
            _bridge.Plants = world.Plants;
            _recipeWorkOrders = ToRecipeWorkOrders(data.recipeWorkOrders);
            return world;
        }

        private List<RecipeWorkOrder> ToRecipeWorkOrders(RecipeWorkOrderSaveData[] data)
        {
            if (data == null || data.Length == 0)
                return new List<RecipeWorkOrder>();
            if (_resolveRecipe == null)
                throw new InvalidOperationException("JsonSliceSaveService needs a recipe resolver to load active recipe work orders.");

            return data.Select(order => EmberCrpg.Simulation.Process.WorldSaveRehydration.ToRecipeWorkOrder(order, _resolveRecipe)).ToList();
        }
    }
}
