using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Domain.Process
{
    /// <summary>Pure plant component attached to one soil cell.</summary>
    public sealed class PlantComponent
    {
        public PlantComponent(
            WorldComponentId id,
            SiteId siteId,
            GridPosition position,
            string speciesId,
            PlantStageId stageId,
            int daysInStage)
        {
            if (id.IsEmpty)
                throw new ArgumentException("WorldComponentId.Empty cannot back a PlantComponent.", nameof(id));
            if (siteId.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot back a PlantComponent.", nameof(siteId));
            if (string.IsNullOrWhiteSpace(speciesId))
                throw new ArgumentException("Plant species id is required.", nameof(speciesId));
            if (stageId.IsEmpty)
                throw new ArgumentException("Plant stage id cannot be empty.", nameof(stageId));
            if (daysInStage < 0)
                throw new ArgumentOutOfRangeException(nameof(daysInStage), "Days in stage cannot be negative.");

            Id = id;
            SiteId = siteId;
            Position = position;
            SpeciesId = speciesId.Trim();
            StageId = stageId;
            DaysInStage = daysInStage;
        }

        public WorldComponentId Id { get; }
        public SiteId SiteId { get; }
        public GridPosition Position { get; }
        public string SpeciesId { get; }
        public PlantStageId StageId { get; }
        public int DaysInStage { get; }

        public PlantComponent WithStage(PlantStageId stageId)
        {
            return new PlantComponent(Id, SiteId, Position, SpeciesId, stageId, 0);
        }

        public PlantComponent WithDaysInStage(int daysInStage)
        {
            return new PlantComponent(Id, SiteId, Position, SpeciesId, StageId, daysInStage);
        }
    }
}
