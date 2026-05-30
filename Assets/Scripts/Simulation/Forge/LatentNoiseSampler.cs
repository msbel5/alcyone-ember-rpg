using System;

namespace EmberCrpg.Simulation.Forge
{
    internal static class LatentNoiseSampler
    {
        public static float[] SampleGaussian(uint seed, int length, float sigma)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            // EMB-038: System.Random is intentional here. This noise feeds the generated IMAGE, a
            // per-playthrough visual cache on the player's machine — NOT authoritative world state and
            // NOT part of the deterministic-replay digest (see docs/DETERMINISM.md). The seed gives
            // per-run reproducibility within a machine; cross-runtime bit-stability is not promised.
            var rng = new Random((int)seed);
            var values = new float[length];
            for (int i = 0; i < values.Length; i++)
                values[i] = NextGaussian(rng) * sigma;
            return values;
        }

        private static float NextGaussian(Random random)
        {
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }
    }
}
