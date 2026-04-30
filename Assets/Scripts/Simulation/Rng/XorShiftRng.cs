using System;

// Design note:
// XorShiftRng provides a tiny deterministic RNG for combat, room variation, and tests.
// Inputs: non-zero seed and bounded integer requests.
// Outputs: reproducible pseudo-random integers and 1-100 rolls.
// Bible reference: ARCHITECTURE.md deterministic world lock-in, PRD FR-02/FR-03.
namespace EmberCrpg.Simulation.Rng
{
    /// <summary>Simple xorshift32 RNG suitable for repeatable slice mechanics.</summary>
    public sealed class XorShiftRng : IDeterministicRng
    {
        private uint _state;

        public XorShiftRng(uint seed)
        {
            _state = seed == 0 ? 2463534242u : seed;
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax), exclusiveMax, "exclusiveMax must be positive.");
            return (int)(NextUInt() % (uint)exclusiveMax);
        }

        public int RollPercent()
        {
            return NextInt(100) + 1;
        }

        private uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
