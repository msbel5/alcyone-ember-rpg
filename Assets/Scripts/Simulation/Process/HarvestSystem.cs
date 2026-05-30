using System;
using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// HarvestSystem is the narrow Phase 5 output bridge: ripe plant component to
// stockpile inventory item. Item-id allocation stays injected so the system
// remains deterministic and testable.
namespace EmberCrpg.Simulation.Process
{
    /// <summary>Converts a harvestable plant into stockpile output and clears its soil slot.</summary>
    public sealed class HarvestSystem
    {
        public bool TryHarvest(
            PlantSpeciesDef species,
            ComponentStore<PlantComponent> plants,
            ComponentStore<SoilComponent> soils,
            WorldComponentId plantId,
            InventoryState stockpile,
            WorldEventLog eventLog,
            GameTime now,
            Func<string, InventoryItem> createHarvestItem)
        {
            if (species == null)
                throw new ArgumentNullException(nameof(species));
            if (plants == null)
                throw new ArgumentNullException(nameof(plants));
            if (soils == null)
                throw new ArgumentNullException(nameof(soils));
            if (stockpile == null)
                throw new ArgumentNullException(nameof(stockpile));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));
            if (createHarvestItem == null)
                throw new ArgumentNullException(nameof(createHarvestItem));
            if (plantId.IsEmpty)
                throw new ArgumentException("Plant id is required.", nameof(plantId));
            if (!plants.TryGet(plantId, out var plant))
                return false;
            if (!string.Equals(plant.SpeciesId, species.SpeciesId, StringComparison.Ordinal))
                return false;
            if (!species.TryGetStage(plant.StageId, out var stage))
                throw new InvalidOperationException($"Plant {plant.Id} references unknown stage {plant.StageId} for species {species.SpeciesId}.");
            if (!stage.IsHarvestable)
                return false;

            var soilRow = soils.Rows.FirstOrDefault(row => row.Value.HasPlant && row.Value.PlantId.Equals(plantId));
            if (soilRow.Value == null)
                return false;

            var output = createHarvestItem(species.HarvestItemTag);
            if (output == null)
                throw new InvalidOperationException("Harvest output factory returned null.");
            if (!string.Equals(output.TemplateId, species.HarvestItemTag, StringComparison.Ordinal))
                throw new InvalidOperationException("Harvest output factory returned the wrong item template.");

            var projected = stockpile.Clone();
            if (!projected.TryAdd(output))
                return false;

            stockpile.TryAdd(output);
            plants.Remove(plantId);
            soils.Replace(soilRow.Key, soilRow.Value.WithoutPlant());
            eventLog.Append(new WorldEvent(
                now,
                WorldEventKind.PlantHarvested,
                default,
                plant.SiteId,
                $"plant_harvested:{plant.SiteId.Value}:{plantId.Value}:{species.HarvestItemTag}",
                new ReasonTrace(new[]
                {
                    "plant_harvest",
                    $"site:{plant.SiteId.Value}",
                    $"soil:{soilRow.Key.Value}",
                    $"plant:{plantId.Value}",
                    $"species:{species.SpeciesId}",
                    $"item:{species.HarvestItemTag}",
                })));
            return true;
        }
    }
}
