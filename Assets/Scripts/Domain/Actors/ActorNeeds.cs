using System;

// Design note:
// ActorNeeds is Faz 4's actor-local need component. It stores only current
// pressure values; ticking, recovery, mood derivation, save/load, and job
// refusal are later atoms that consume this component.
// Atom-map ref: DOCS/sprint-faz-4-atom-map.md Pure needs component rail.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Immutable actor need snapshot for hunger, fatigue, and thirst.</summary>
    public readonly struct ActorNeeds : IEquatable<ActorNeeds>
    {
        public ActorNeeds(NeedValue hunger, NeedValue fatigue, NeedValue thirst)
        {
            Hunger = hunger;
            Fatigue = fatigue;
            Thirst = thirst;
        }

        public static ActorNeeds Comfortable
        {
            get { return default; }
        }

        public NeedValue Hunger { get; }
        public NeedValue Fatigue { get; }
        public NeedValue Thirst { get; }

        public NeedValue Get(NeedKind kind)
        {
            switch (kind)
            {
                case NeedKind.Hunger:
                    return Hunger;
                case NeedKind.Fatigue:
                    return Fatigue;
                case NeedKind.Thirst:
                    return Thirst;
                default:
                    throw new ArgumentException("ActorNeeds requires a concrete need kind.", nameof(kind));
            }
        }

        public ActorNeeds With(NeedKind kind, NeedValue value)
        {
            switch (kind)
            {
                case NeedKind.Hunger:
                    return WithHunger(value);
                case NeedKind.Fatigue:
                    return WithFatigue(value);
                case NeedKind.Thirst:
                    return WithThirst(value);
                default:
                    throw new ArgumentException("ActorNeeds requires a concrete need kind.", nameof(kind));
            }
        }

        public ActorNeeds WithHunger(NeedValue hunger)
        {
            return new ActorNeeds(hunger, Fatigue, Thirst);
        }

        public ActorNeeds WithFatigue(NeedValue fatigue)
        {
            return new ActorNeeds(Hunger, fatigue, Thirst);
        }

        public ActorNeeds WithThirst(NeedValue thirst)
        {
            return new ActorNeeds(Hunger, Fatigue, thirst);
        }

        public bool Equals(ActorNeeds other)
        {
            return Hunger == other.Hunger
                && Fatigue == other.Fatigue
                && Thirst == other.Thirst;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorNeeds other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hunger, Fatigue, Thirst);
        }

        public override string ToString()
        {
            return $"ActorNeeds(hunger={Hunger.Value}, fatigue={Fatigue.Value}, thirst={Thirst.Value})";
        }

        public static bool operator ==(ActorNeeds left, ActorNeeds right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorNeeds left, ActorNeeds right)
        {
            return !left.Equals(right);
        }
    }
}
