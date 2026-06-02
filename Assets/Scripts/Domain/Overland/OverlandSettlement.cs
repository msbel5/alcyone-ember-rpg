using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Domain.Overland
{
    /// <summary>Immutable overland settlement instance derived from a generated settlement roster.</summary>
    public sealed class OverlandSettlement
    {
        public OverlandSettlement(SettlementId id, SettlementKind kind, GridPosition tilePosition, string name, string templatePackTag)
        {
            if (id.IsEmpty)
                throw new ArgumentException("SettlementId.Empty cannot back an overland settlement.", nameof(id));
            if (!Enum.IsDefined(typeof(SettlementKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Settlement kind must be defined.");
            if (tilePosition.X < 0 || tilePosition.Y < 0)
                throw new ArgumentOutOfRangeException(nameof(tilePosition), tilePosition, "Settlement tile position must be non-negative.");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Settlement name is required.", nameof(name));
            if (string.IsNullOrWhiteSpace(templatePackTag))
                throw new ArgumentException("Template pack tag is required.", nameof(templatePackTag));

            Id = id;
            Kind = kind;
            TilePosition = tilePosition;
            Name = name;
            TemplatePackTag = templatePackTag;
        }

        public SettlementId Id { get; }
        public SettlementKind Kind { get; }
        public GridPosition TilePosition { get; }
        public string Name { get; }
        public string TemplatePackTag { get; }
    }
}
