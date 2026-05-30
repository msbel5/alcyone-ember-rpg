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
            if (species == null)
                throw new ArgumentNullException(nameof(species));
            if (soils == null)
                throw new ArgumentNullException(nameof(soils));
            if (plants == null)
                throw new ArgumentNullException(nameof(plants));
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));
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
            if (!inventory.TryRemoveStackable(species.SeedItemTag, 1))
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
                default,
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
