using System;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Immutable data row describing a recurring trade route between two sites.
    /// Origin and destination are distinct sites; cadence is the number of game
    /// days between caravan departures. Faz 6 Atom 7.
    /// </summary>
    public sealed class TradeRouteDef : IEquatable<TradeRouteDef>
    {
        public TradeRouteDef(
            TradeRouteId id,
            SiteId originSiteId,
            SiteId destinationSiteId,
            string itemTag,
            int quantityPerCaravan,
            int cadenceDays)
        {
            if (id.IsEmpty)
                throw new ArgumentException("TradeRouteId must be non-empty.", nameof(id));
            if (originSiteId.IsEmpty)
                throw new ArgumentException("Origin site must be non-empty.", nameof(originSiteId));
            if (destinationSiteId.IsEmpty)
                throw new ArgumentException("Destination site must be non-empty.", nameof(destinationSiteId));
            if (originSiteId.Equals(destinationSiteId))
                throw new ArgumentException("Origin and destination must be distinct sites.");
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Item tag must be non-blank.", nameof(itemTag));
            if (quantityPerCaravan <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantityPerCaravan), "Quantity must be positive.");
            if (cadenceDays <= 0)
                throw new ArgumentOutOfRangeException(nameof(cadenceDays), "Cadence must be positive.");

            Id = id;
            OriginSiteId = originSiteId;
            DestinationSiteId = destinationSiteId;
            ItemTag = itemTag.Trim();
            QuantityPerCaravan = quantityPerCaravan;
            CadenceDays = cadenceDays;
        }

        public TradeRouteId Id { get; }
        public SiteId OriginSiteId { get; }
        public SiteId DestinationSiteId { get; }
        public string ItemTag { get; }
        public int QuantityPerCaravan { get; }
        public int CadenceDays { get; }

        public bool Equals(TradeRouteDef other)
        {
            if (other == null) return false;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj) => Equals(obj as TradeRouteDef);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => $"TradeRouteDef({Id}, {OriginSiteId}->{DestinationSiteId}, {ItemTag} x{QuantityPerCaravan} / {CadenceDays}d)";
    }

    /// <summary>Stable id for a trade route.</summary>
    public readonly struct TradeRouteId : IEquatable<TradeRouteId>
    {
        private readonly ulong _value;

        public TradeRouteId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;
        public bool IsEmpty => _value == 0UL;

        public bool Equals(TradeRouteId other) => _value == other._value;
        public override bool Equals(object obj) => obj is TradeRouteId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => $"TradeRouteId({_value})";
        public static bool operator ==(TradeRouteId a, TradeRouteId b) => a.Equals(b);
        public static bool operator !=(TradeRouteId a, TradeRouteId b) => !a.Equals(b);
    }
}
