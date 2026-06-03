using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    internal static class PlanetRng
    {
        public static XorShiftRng Fork(XorShiftRng root, uint stageConstant)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            uint mixed = Mix(stageConstant ^ NextUInt(root));
            mixed = Mix(mixed ^ NextUInt(root));
            return new XorShiftRng(mixed == 0u ? stageConstant : mixed);
        }

        public static double NextUnit(XorShiftRng rng)
        {
            return NextUInt(rng) / 4294967296d;
        }

        public static double NextSignedUnit(XorShiftRng rng)
        {
            return (NextUnit(rng) * 2d) - 1d;
        }

        private static uint NextUInt(XorShiftRng rng)
        {
            uint high = (uint)rng.NextInt(1 << 16);
            uint low = (uint)rng.NextInt(1 << 16);
            return (high << 16) | low;
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }
    }
}
