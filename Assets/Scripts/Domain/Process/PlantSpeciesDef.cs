using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EmberCrpg.Domain.Time;

namespace EmberCrpg.Domain.Process
{
    /// <summary>Data row describing one plant species, its item tags, stages, and growth rules.</summary>
    public sealed class PlantSpeciesDef
    {
        private readonly ReadOnlyCollection<PlantGrowthStageDef> _stages;
        private readonly ReadOnlyCollection<PlantGrowthRule> _growthRules;

        public PlantSpeciesDef(
            string speciesId,
            string seedItemTag,
            string harvestItemTag,
            IEnumerable<PlantGrowthStageDef> stages,
            IEnumerable<PlantGrowthRule> growthRules)
        {
            if (string.IsNullOrWhiteSpace(speciesId))
                throw new ArgumentException("Plant species id is required.", nameof(speciesId));
            if (string.IsNullOrWhiteSpace(seedItemTag))
                throw new ArgumentException("Seed item tag is required.", nameof(seedItemTag));
            if (string.IsNullOrWhiteSpace(harvestItemTag))
                throw new ArgumentException("Harvest item tag is required.", nameof(harvestItemTag));
            if (stages == null)
                throw new ArgumentNullException(nameof(stages));
            if (growthRules == null)
                throw new ArgumentNullException(nameof(growthRules));

            var stageRows = stages.ToList();
            if (stageRows.Count == 0)
                throw new ArgumentException("Plant species requires at least one stage.", nameof(stages));
            if (stageRows.Any(stage => stage == null))
                throw new ArgumentException("Plant species stages cannot contain null rows.", nameof(stages));
            if (stageRows.GroupBy(stage => stage.Id).Any(group => group.Count() > 1))
                throw new ArgumentException("Plant species stages must have unique ids.", nameof(stages));

            var ruleRows = growthRules.ToList();
            if (ruleRows.Count == 0)
                throw new ArgumentException("Plant species requires at least one growth rule.", nameof(growthRules));
            if (ruleRows.Any(rule => rule == null))
                throw new ArgumentException("Plant species growth rules cannot contain null rows.", nameof(growthRules));

            SpeciesId = speciesId.Trim();
            SeedItemTag = seedItemTag.Trim();
            HarvestItemTag = harvestItemTag.Trim();
            _stages = new ReadOnlyCollection<PlantGrowthStageDef>(stageRows);
            _growthRules = new ReadOnlyCollection<PlantGrowthRule>(ruleRows);
        }

        public string SpeciesId { get; }
        public string SeedItemTag { get; }
        public string HarvestItemTag { get; }
        public IReadOnlyList<PlantGrowthStageDef> Stages { get { return _stages; } }
        public IReadOnlyList<PlantGrowthRule> GrowthRules { get { return _growthRules; } }
        public PlantGrowthStageDef FirstStage { get { return _stages[0]; } }

        public bool TryGetStage(PlantStageId stageId, out PlantGrowthStageDef stage)
        {
            for (var i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].Id.Equals(stageId))
                {
                    stage = _stages[i];
                    return true;
                }
            }

            stage = null;
            return false;
        }

        public bool TryGetNextStage(PlantStageId currentStageId, out PlantGrowthStageDef nextStage)
        {
            for (var i = 0; i < _stages.Count - 1; i++)
            {
                if (_stages[i].Id.Equals(currentStageId))
                {
                    nextStage = _stages[i + 1];
                    return true;
                }
            }

            nextStage = null;
            return false;
        }

        public bool CanGrow(Season season, bool isSnowing)
        {
            for (var i = 0; i < _growthRules.Count; i++)
            {
                if (_growthRules[i].Matches(season))
                    return _growthRules[i].CanGrow(isSnowing);
            }

            return false;
        }
    }
}
