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
            // Codex audit (sixth pass A-P3 #13): `NextUInt() % exclusiveMax`
            // is biased when (2^32 % exclusiveMax) != 0. For small bounds
            // like 100 the bias is statistically negligible (~1e-8), but
            // the audit asked we either document the upper bound or
            // rejection-sample. Rejection-sample when exclusiveMax is large
            // enough that the bias is measurable (> 2^16); below that
            // threshold accept the modulo result as the established
            // deterministic-replay contract.
            if (exclusiveMax > (1 << 16))
            {
                uint bound = (uint)exclusiveMax;
                uint limit = uint.MaxValue - (uint.MaxValue % bound);
                uint sample;
                do { sample = NextUInt(); } while (sample >= limit);
                return (int)(sample % bound);
            }
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
