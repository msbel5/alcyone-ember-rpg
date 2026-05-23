using System;

// Design note:
// NpcId is the worldgen FOUNDATION's stable handle for the ~500-1000 named
// NPCs distributed across settlements. It is intentionally distinct from
// ActorId: ActorIds live in the runtime ActorStore (the small slice loaded
// for gameplay), NpcIds enumerate the full population of the simulated
// world. The Faz-N follow-up that "hydrates" an NpcSeedRecord into a real
// runtime ActorRecord will mint a fresh ActorId at that moment and keep a
// back-reference to the NpcId.
namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>
    /// Stable handle to a procedurally-seeded NPC. Value type; default value means no NPC.
    /// </summary>
    public readonly struct NpcId : IEquatable<NpcId>
    {
        private readonly ulong _value;

        public NpcId(ulong value)
        {
            _value = value;
        }

        public ulong Value
        {
            get { return _value; }
        }

        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        public bool Equals(NpcId other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is NpcId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return IsEmpty ? "NpcId.Empty" : $"NpcId({_value})";
        }

        public static bool operator ==(NpcId left, NpcId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NpcId left, NpcId right)
        {
            return !left.Equals(right);
        }
    }
}
