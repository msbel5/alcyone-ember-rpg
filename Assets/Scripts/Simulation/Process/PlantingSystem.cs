using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// PlantingSystem is Phase 5's narrow seed-consumption atom. Growth, harvest,
// weather, jobs, and save/load are later atoms.
namespace EmberCrpg.Simulation.Process
{
    /// <summary>Consumes one seed item and attaches a plant component to empty soil.</summary>
    public sealed class PlantingSystem
    {
        public bool TryPlant(
            PlantSpeciesDef species,
            ComponentStore<SoilComponent> soils,
            ComponentStore<PlantComponent> plants,
            WorldComponentId soilId,
            WorldComponentId plantId,
            InventoryState inventory,
            WorldEventLog eventLog,
            GameTime now)
        {
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));
            // Legacy player-lane signature: the seed-consumption seam delegates so behaviour
            // (and its tests) stays byte-identical while the W33 farm chain injects a
            // stockpile-backed takeSeed instead (W33-01 §7.2 — the town's seed never lives
            // in the player's bag).
            return TryPlant(species, soils, plants, soilId, plantId,
                () => inventory.TryRemoveStackable(species.SeedItemTag, 1),
                eventLog, now, default);
        }

        /// <summary>Seed-source-agnostic overload: takeSeed is called ONCE, after every gate —
        /// a false return aborts with zero world mutation (failure never burns a seed).</summary>
        public bool TryPlant(
            PlantSpeciesDef species,
            ComponentStore<SoilComponent> soils,
            ComponentStore<PlantComponent> plants,
            WorldComponentId soilId,
            WorldComponentId plantId,
            Func<bool> takeSeed,
            WorldEventLog eventLog,
            GameTime now,
            ActorId planterId)
        {
            if (species == null)
                throw new ArgumentNullException(nameof(species));
            if (soils == null)
                throw new ArgumentNullException(nameof(soils));
            if (plants == null)
                throw new ArgumentNullException(nameof(plants));
            if (takeSeed == null)
                throw new ArgumentNullException(nameof(takeSeed));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));
            if (soilId.IsEmpty)
                throw new ArgumentException("Soil id is required.", nameof(soilId));
            if (plantId.IsEmpty)
                throw new ArgumentException("Plant id is required.", nameof(plantId));
            if (!soils.TryGet(soilId, out var soil))
                return false;
            if (soil.HasPlant)
                return false;
            if (plants.Contains(plantId))
                return false;
            if (!takeSeed())
                return false;

            var plant = new PlantComponent(
                plantId,
                soil.SiteId,
                soil.Position,
                species.SpeciesId,
                species.FirstStage.Id,
                daysInStage: 0);

            plants.Add(plantId, plant);
            soils.Replace(soilId, soil.WithPlant(plantId));
            eventLog.Append(new WorldEvent(
                now,
                WorldEventKind.PlantPlanted,
                planterId, // W33: the planter is named — authorship is the F1 story-trace anchor
                soil.SiteId,
                $"plant_planted:{soil.SiteId.Value}:{plantId.Value}",
                new ReasonTrace(new[]
                {
                    "plant_seed",
                    $"site:{soil.SiteId.Value}",
                    $"soil:{soilId.Value}",
                    $"plant:{plantId.Value}",
                    $"species:{species.SpeciesId}",
                    $"stage:{species.FirstStage.Id.Value}",
                })));
            return true;
        }
    }
}
