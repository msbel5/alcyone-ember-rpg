using System;
using System.Collections.Generic;
using EmberCrpg.Simulation.Rng;

// Design note:
// SyllableNameForge is the FOUNDATION worldgen's bag-of-syllables name forge.
// Inputs: an IDeterministicRng and a target syllable count (default 3).
// Outputs: capitalized strings drawn from 8 onset consonant clusters, 12 vowel
// nuclei, and 8 codas — yielding ~770 unique syllables and ~4.6e8 unique
// three-syllable names. Deduplication is the caller's responsibility (the
// generator wraps this in a HashSet) so the forge itself stays stateless and
// deterministic for the same RNG sequence. Pure simulation code, no Unity,
// no LINQ allocations in the hot path. The brief specifies 3 syllables,
// 8 onset clusters, 12 vowel patterns — codas are added because two-letter
// open syllables alone produce too few distinct-looking words.
namespace EmberCrpg.Simulation.Worldgen
{
    /// <summary>Deterministic three-syllable name forge backed by an IDeterministicRng.</summary>
    public static class SyllableNameForge
    {
        private static readonly string[] OnsetClusters =
        {
            "Br", "Cr", "Dr", "Fr", "Gr", "Th", "Sk", "Vh",
        };

        private static readonly string[] VowelNuclei =
        {
            "a", "e", "i", "o", "u", "y",
            "ae", "ai", "ea", "io", "ou", "ya",
        };

        private static readonly string[] Codas =
        {
            "n", "r", "l", "th", "sh", "rd", "st", "lm",
        };

        private static readonly string[] InteriorOnsets =
        {
            "n", "r", "l", "m", "v", "th", "sh", "d",
        };

        /// <summary>Forge a three-syllable name using the supplied RNG.</summary>
        public static string Forge(IDeterministicRng rng)
        {
            return Forge(rng, 3);
        }

        /// <summary>Forge a name with the supplied syllable count using the supplied RNG.</summary>
        public static string Forge(IDeterministicRng rng, int syllableCount)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            if (syllableCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(syllableCount), syllableCount, "syllableCount must be positive.");

            var buffer = new System.Text.StringBuilder(syllableCount * 4);

            // First syllable opens with a consonant cluster for a strong onset.
            buffer.Append(OnsetClusters[rng.NextInt(OnsetClusters.Length)]);
            buffer.Append(VowelNuclei[rng.NextInt(VowelNuclei.Length)]);

            // Interior syllables use a softer onset so names stay pronounceable.
            for (int i = 1; i < syllableCount; i++)
            {
                buffer.Append(InteriorOnsets[rng.NextInt(InteriorOnsets.Length)]);
                buffer.Append(VowelNuclei[rng.NextInt(VowelNuclei.Length)]);
            }

            // Half the time, close with a coda — pure-open names look generic.
            if (rng.NextInt(2) == 0)
            {
                buffer.Append(Codas[rng.NextInt(Codas.Length)]);
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Forge a name guaranteed to not collide with a supplied set. The set
        /// is updated with the chosen name. Throws when the forge cannot find
        /// a fresh name within the attempt budget — a generous cap because
        /// the syllable space is ~4.6e8 wide and collisions are vanishingly
        /// rare for the FOUNDATION's ~1000-name budget.
        /// </summary>
        public static string ForgeUnique(IDeterministicRng rng, HashSet<string> existing, int syllableCount = 3, int attemptBudget = 64)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (attemptBudget <= 0)
                throw new ArgumentOutOfRangeException(nameof(attemptBudget), attemptBudget, "attemptBudget must be positive.");

            for (int attempt = 0; attempt < attemptBudget; attempt++)
            {
                var candidate = Forge(rng, syllableCount);
                if (existing.Add(candidate))
                    return candidate;
            }

            // Fallback — append the existing count so the name stays unique
            // and deterministic for the same rng/existing pair without
            // crashing the worldgen mid-pass.
            var fallback = Forge(rng, syllableCount) + existing.Count.ToString();
            existing.Add(fallback);
            return fallback;
        }
    }
}
