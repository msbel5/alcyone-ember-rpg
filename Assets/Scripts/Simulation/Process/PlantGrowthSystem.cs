using System;
using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;

// Design note:
// PlantGrowthSystem advances existing plant components by deterministic daily
// ticks. It does not create plants, harvest crops, or read live weather; those
// are supplied by earlier/later atoms as typed inputs.
namespace EmberCrpg.Simulation.Process
{
    /// <summary>Advances plant age and stage using data-defined season/weather rules.</summary>
    public sealed class PlantGrowthSystem
    {
        public int AdvanceOneDay(
            PlantSpeciesDef species,
            ComponentStore<PlantComponent> plants,
            WorldEventLog eventLog,
            GameTime now,
            Season season,
            bool isSnowing)
        {
            if (species == null)
                throw new ArgumentNullException(nameof(species));
            if (plants == null)
                throw new ArgumentNullException(nameof(plants));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));
            if (!species.CanGrow(season, isSnowing))
                return 0;

            var stageAdvanceCount = 0;
            foreach (var row in plants.Rows.ToList())
            {
                var plant = row.Value;
                if (!string.Equals(plant.SpeciesId, species.SpeciesId, StringComparison.Ordinal))
                    continue;
                if (!species.TryGetStage(plant.StageId, out var currentStage))
                    throw new InvalidOperationException($"Plant {plant.Id} references unknown stage {plant.StageId} for species {species.SpeciesId}.");
                if (currentStage.DaysToNextStage <= 0)
                    continue;

                var nextDays = plant.DaysInStage + 1;
                if (nextDays < currentStage.DaysToNextStage)
                {
                    plants.Replace(row.Key, plant.WithDaysInStage(nextDays));
                    continue;
                }

                if (!species.TryGetNextStage(plant.StageId, out var nextStage))
                {
                    plants.Replace(row.Key, plant.WithDaysInStage(currentStage.DaysToNextStage));
                    continue;
                }

                var advancedPlant = plant.WithStage(nextStage.Id);
                plants.Replace(row.Key, advancedPlant);
                eventLog.Append(new WorldEvent(
                    now,
                    WorldEventKind.PlantStageAdvanced,
                    default,
                    advancedPlant.SiteId,
                    $"plant_stage_advanced:{advancedPlant.SiteId.Value}:{advancedPlant.Id.Value}:{nextStage.Id.Value}",
                    new ReasonTrace(new[]
                    {
                        "plant_growth",
                        $"site:{advancedPlant.SiteId.Value}",
                        $"plant:{advancedPlant.Id.Value}",
                        $"species:{species.SpeciesId}",
                        $"from:{plant.StageId.Value}",
                        $"to:{nextStage.Id.Value}",
                    })));
                stageAdvanceCount++;
            }

            return stageAdvanceCount;
        }
    }
}
