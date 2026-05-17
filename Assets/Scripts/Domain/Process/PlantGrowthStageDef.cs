using System;

namespace EmberCrpg.Domain.Process
{
    /// <summary>Data row describing one stage in a plant species growth chain.</summary>
    public sealed class PlantGrowthStageDef
    {
        public PlantGrowthStageDef(PlantStageId id, string displayName, int daysToNextStage, bool isHarvestable)
        {
            if (id.IsEmpty)
                throw new ArgumentException("Plant stage id cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Plant stage display name is required.", nameof(displayName));
            if (daysToNextStage < 0)
                throw new ArgumentOutOfRangeException(nameof(daysToNextStage), "Days to next stage cannot be negative.");

            Id = id;
            DisplayName = displayName.Trim();
            DaysToNextStage = daysToNextStage;
            IsHarvestable = isHarvestable;
        }

        public PlantStageId Id { get; }
        public string DisplayName { get; }
        public int DaysToNextStage { get; }
        public bool IsHarvestable { get; }
    }
}
