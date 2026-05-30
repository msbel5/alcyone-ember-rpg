using System;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Stable string lifecycle code for a caravan instance. New states ship as
    /// data, not enum branches.
    /// </summary>
    public readonly struct CaravanState : IEquatable<CaravanState>
    {
        private readonly string _code;

        private CaravanState(string code)
        {
            _code = code;
        }

        public static CaravanState Loading { get; } = new CaravanState("loading");
        public static CaravanState EnRoute { get; } = new CaravanState("en_route");
        public static CaravanState Arrived { get; } = new CaravanState("arrived");
        public static CaravanState Unloading { get; } = new CaravanState("unloading");
        public static CaravanState Idle { get; } = new CaravanState("idle");

        public static CaravanState FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Idle;
            var normalized = code.Trim();
            if (normalized == Loading.Code) return Loading;
            if (normalized == EnRoute.Code) return EnRoute;
            if (normalized == Arrived.Code) return Arrived;
            if (normalized == Unloading.Code) return Unloading;
            if (normalized == Idle.Code) return Idle;
            return new CaravanState(normalized);
        }

        public string Code => _code ?? Idle.Code;
        public bool IsEmpty => string.IsNullOrEmpty(_code);

        public bool Equals(CaravanState other) => Code == other.Code;
        public override bool Equals(object obj) => obj is CaravanState other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(CaravanState a, CaravanState b) => a.Equals(b);
        public static bool operator !=(CaravanState a, CaravanState b) => !a.Equals(b);
    }

    /// <summary>
    /// Stable id for a caravan instance.
    /// </summary>
    public readonly struct CaravanId : IEquatable<CaravanId>
    {
        private readonly ulong _value;

        public CaravanId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;
        public bool IsEmpty => _value == 0UL;

        public bool Equals(CaravanId other) => _value == other._value;
        public override bool Equals(object obj) => obj is CaravanId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => $"CaravanId({_value})";
        public static bool operator ==(CaravanId a, CaravanId b) => a.Equals(b);
        public static bool operator !=(CaravanId a, CaravanId b) => !a.Equals(b);
    }

    /// <summary>
    /// Runtime caravan: current site, lifecycle state, route progress, payload
    /// quantity remaining to deliver. Phase 6 Atom 8.
    /// </summary>
    public sealed class CaravanInstance
    {
        public CaravanInstance(
            CaravanId id,
            TradeRouteId routeId,
            SiteId currentSiteId,
            int payloadRemaining,
            int stepsSinceDeparture,
            CaravanState state)
        {
            if (id.IsEmpty)
                throw new ArgumentException("CaravanId must be non-empty.", nameof(id));
            if (routeId.IsEmpty)
                throw new ArgumentException("TradeRouteId must be non-empty.", nameof(routeId));
            if (payloadRemaining < 0)
                throw new ArgumentOutOfRangeException(nameof(payloadRemaining), "Payload remaining must be non-negative.");
            if (stepsSinceDeparture < 0)
                throw new ArgumentOutOfRangeException(nameof(stepsSinceDeparture), "Steps must be non-negative.");

            Id = id;
            RouteId = routeId;
            CurrentSiteId = currentSiteId;
            PayloadRemaining = payloadRemaining;
            StepsSinceDeparture = stepsSinceDeparture;
            State = state.IsEmpty ? CaravanState.Idle : state;
        }

        public CaravanId Id { get; }
        public TradeRouteId RouteId { get; }
        public SiteId CurrentSiteId { get; private set; }
        public int PayloadRemaining { get; private set; }
        public int StepsSinceDeparture { get; private set; }
        public CaravanState State { get; private set; }

        /// <summary>Advances one route step. Does not validate route bounds; CaravanSystem owns that logic.</summary>
        public void AdvanceStep()
        {
            StepsSinceDeparture++;
            State = CaravanState.EnRoute;
        }

        public void Load(int quantity)
        {
            if (quantity < 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be non-negative.");
            PayloadRemaining += quantity;
            if (quantity > 0)
                State = CaravanState.EnRoute;
        }

        /// <summary>Marks arrival at the destination site.</summary>
        public void Arrive(SiteId siteId)
        {
            if (siteId.IsEmpty)
                throw new ArgumentException("Arrival siteId must be non-empty.", nameof(siteId));
            CurrentSiteId = siteId;
            State = CaravanState.Arrived;
        }

        /// <summary>Unloads up to <paramref name="quantity"/> of payload; returns actual unloaded.</summary>
        public int Unload(int quantity)
        {
            if (quantity < 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be non-negative.");
            var unloaded = quantity > PayloadRemaining ? PayloadRemaining : quantity;
            PayloadRemaining -= unloaded;
            State = PayloadRemaining == 0 ? CaravanState.Idle : CaravanState.Unloading;
            return unloaded;
        }
    }
}
