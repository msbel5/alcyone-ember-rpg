using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// SoilComponent is Phase 5's first PROCESS component. It models a tillable site
// cell only; planting, watering, growth, snow blocking, and harvest remain
// separate systems/atoms.
namespace EmberCrpg.Domain.Process
{
    /// <summary>Pure soil component attached to one site grid position.</summary>
    public sealed class SoilComponent
    {
        public SoilComponent(
            WorldComponentId id,
            SiteId siteId,
            GridPosition position,
            int fertility,
            int moisture,
            WorldComponentId plantId)
        {
            if (id.IsEmpty)
                throw new ArgumentException("WorldComponentId.Empty cannot back a SoilComponent.", nameof(id));
            if (siteId.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot back a SoilComponent.", nameof(siteId));

            Id = id;
            SiteId = siteId;
            Position = position;
            Fertility = ClampPercent(fertility);
            Moisture = ClampPercent(moisture);
            PlantId = plantId;
        }

        public WorldComponentId Id { get; }
        public SiteId SiteId { get; }
        public GridPosition Position { get; }
        public int Fertility { get; }
        public int Moisture { get; }
        public WorldComponentId PlantId { get; }
        public bool HasPlant { get { return !PlantId.IsEmpty; } }

        public SoilComponent WithMoisture(int moisture)
        {
            return new SoilComponent(Id, SiteId, Position, Fertility, moisture, PlantId);
        }

        public SoilComponent WithPlant(WorldComponentId plantId)
        {
            if (plantId.IsEmpty)
                throw new ArgumentException("Plant id cannot be empty when attaching a plant.", nameof(plantId));
            return new SoilComponent(Id, SiteId, Position, Fertility, Moisture, plantId);
        }

        public SoilComponent WithoutPlant()
        {
            return new SoilComponent(Id, SiteId, Position, Fertility, Moisture, default);
        }

        private static int ClampPercent(int value)
        {
            if (value < 0)
                return 0;
            if (value > 100)
                return 100;
            return value;
        }
    }
}
