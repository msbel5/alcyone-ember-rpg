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
        // W34 WORK (docs/ruh/w34/02 §5.2): the _recipeWorkOrders park list and
        // ReplaceRecipeWorkOrders are RETIRED. Orders are pure-Domain WorldState.WorkOrders
        // rows now; WorldSaveMapper writes/reads them on the recipeWorkOrders DTO directly.
        // The park list was a one-way street (nothing ever read it back into the world), so a
        // loaded claimed job re-consumed its inputs on the next hour — the double-consumption
        // save wound this retirement closes structurally.

        // SOUL-01: worksites/jobs/soils/plants are now the world root's canonical stores. This bridge
        // world holds them for callers that touch the save service directly (round-trip tests, the
        // pre-world-bound adapter ctor). BindWorld(world) repoints the bridge at the live world so
        // SaveToJson/LoadFromJson and the per-tick systems all read/write the same store instances.
        private WorldState _bridge = new WorldState();

        /// <summary>The resolver parameter is kept for caller compatibility; work-order rows are
        /// pure Domain data now (no RecipeDef rebind on load — the W34 park-list retirement).</summary>
        public JsonSliceSaveService(Func<RecipeId, RecipeDef> resolveRecipe = null)
        {
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

        public string SaveToJson(WorldState world)
        {
            // ToData now reads the four process stores from the world root. When a caller has staged
            // worksites/jobs/soils/plants on this bridge (round-trip tests, or the adapter before it
            // binds the live world), override those DTO fields from the bridge so they still persist.
            // For a bound adapter the bridge IS the saved world, making the override idempotent.
            // W33 fix (found by the farm slice): an UNBOUND service saving a foreign world must NOT
            // clobber that world's process stores with its untouched empty bridge — that silently
            // dropped every soil/plant/job across a standalone save/load (the faction-decay
            // save-replay only stayed green pre-W33 because both runs coincidentally lost them).
            // Each override applies only when the bridge store actually holds staged content.
            var data = WorldSaveMapper.ToData(world);
            if (_bridge.Worksites != null && _bridge.Worksites.Count > 0)
                data.worksites = WorldSaveMapper.ToWorksiteData(_bridge.Worksites);
            // W34: recipeWorkOrders now comes from WorldSaveMapper.ToData (world.WorkOrders) —
            // the retired park-list override used to CLOBBER it with an empty array here.
            if (_bridge.Jobs != null && _bridge.Jobs.Count > 0)
                data.jobs = WorldSaveMapper.ToJobBoardData(_bridge.Jobs);
            if (_bridge.Soils != null && _bridge.Soils.Count > 0)
                data.soils = WorldSaveMapper.ToSoilComponentData(_bridge.Soils);
            if (_bridge.Plants != null && _bridge.Plants.Count > 0)
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
            // W34: work orders already live on world.WorkOrders (WorldSaveMapper.ToWorld) —
            // no park list to fill; the resolver stays for ctor compatibility only.
            return world;
        }
    }
}
