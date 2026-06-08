using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using EmberCrpg.Presentation.Ember.Worldgen;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        /// <summary>
        /// SOUL-01: give the running world something the per-tick economy systems can actually advance —
        /// a farm plot (soil + a seeded crop that PlantGrowthSystem grows each game-day), a forge
        /// worksite, and one pending JobRequest on the JobBoard. Everything is derived deterministically
        /// from the starting settlement's stable SiteId so the same seed always produces the same setup.
        /// Without this, the newly-wired growth/job systems would tick over empty stores forever.
        /// </summary>
        private void SeedStartingProductionSites()
        {
            if (_world.Sites == null) return;

            if (!TryGetStartingProductionAnchor(out var anchor, out var anchorSite))
                return;

            // Worksite cells: anchored to the site's min corner so they never collide with the
            // center worksite the adapter ctor may have already registered for this site.
            var farmPos = anchorSite.MinBound;
            var forgePos = anchorSite.MinBound.Translate(1, 0);

            // Farm plot: soil + a seeded "wheat" crop at its first growth stage. Growth advances daily
            // via WorldTickComposer -> PlantGrowthSystem (species catalog lives on the composer).
            if (!_world.Worksites.Contains(anchor, farmPos))
                _world.Worksites.Add(new WorksiteRecord(anchor, farmPos, WorksiteKind.Field, isActive: true));

            ulong baseId = anchor.Value;
            var soilId = new WorldComponentId(baseId * 10UL + 1UL);
            var plantId = new WorldComponentId(baseId * 10UL + 2UL);
            if (!_world.Plants.TryGet(plantId, out _))
                _world.Plants.Add(plantId, new PlantComponent(plantId, anchor, farmPos, "wheat", new PlantStageId("seed"), 0));
            if (!_world.Soils.TryGet(soilId, out _))
                _world.Soils.Add(soilId, new SoilComponent(soilId, anchor, farmPos, fertility: 70, moisture: 60, plantId: plantId));

            // Forge worksite + one pending smelting job. JobKind.Smith / WorksiteKind.Furnace /
            // SmeltIronIngot recipe matches the production registry so the job is workable in-game
            // once an actor with a Smith preference is present (see HydrateNpcs).
            if (!_world.Worksites.Contains(anchor, forgePos))
                _world.Worksites.Add(new WorksiteRecord(anchor, forgePos, WorksiteKind.Furnace, isActive: true));

            var jobId = new JobId(baseId * 10UL + 3UL);
            if (!_world.Jobs.Contains(jobId))
            {
                _world.Jobs.Add(new JobRequest(
                    jobId,
                    EmberCrpg.Data.Recipes.ProductionRecipeRegistry.SmeltIronIngotId,
                    anchor,
                    forgePos,
                    WorksiteKind.Furnace,
                    JobKind.Smith,
                    JobPriority.Active(1),
                    quantity: 1,
                    requesterId: anchor.Value == 0UL ? new ActorId(1UL) : new ActorId(anchor.Value)));
            }

            // Player-facing smithing inputs are handed over by the forge quest giver. The worksite/job
            // stay seeded so the settlement has a real forge anchor without making the player craft-ready
            // before speaking to anyone.
        }

    }
}
