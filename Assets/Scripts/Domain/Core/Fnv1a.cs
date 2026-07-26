// Design note:
// FNV-1a string folding — the ONE home for the seed-chaos hash used by dialog voice
// signatures, tool-use fingerprints, and worldgen selection dice. Pure math, ZERO state,
// no Unity / no IO / no RNG (ADR-clean Domain primitive). Bit-identical to the inlined
// `hash ^= ch; hash *= prime;` two-liners it retires — callers must pass the same seed
// they used before (two known seeds live at call sites: the standard FNV offset basis
// AND a non-standard 13-digit chaos seed the dialog side started with; changing either
// would fork every saved greeting/topic/tool cache key, so both stay live).
namespace EmberCrpg.Domain.Core
{
    /// <summary>FNV-1a 32-bit / 64-bit string folds. Callers supply their own seed.</summary>
    public static class Fnv1a
    {
        /// <summary>Standard FNV-1a 64-bit offset basis (14695981039346656037). The dialog
        /// callers keep their historical 13-digit chaos seed — pass it explicitly.</summary>
        public const ulong OffsetBasis64 = 14695981039346656037UL;

        /// <summary>Standard FNV-1a 64-bit prime (1099511628211).</summary>
        public const ulong Prime64 = 1099511628211UL;

        /// <summary>Standard FNV-1a 32-bit offset basis (2166136261).</summary>
        public const uint OffsetBasis32 = 2166136261u;

        /// <summary>Standard FNV-1a 32-bit prime (16777619).</summary>
        public const uint Prime32 = 16777619u;

        /// <summary>Fold `s` into an existing 64-bit `seed` using FNV-1a mixing.
        /// Null string is treated as empty. Bit-identical to `foreach (var ch in s) { seed ^= ch; seed *= Prime64; }`.</summary>
        public static ulong Fold64(ulong seed, string s)
        {
            if (s == null) return seed;
            for (int i = 0; i < s.Length; i++)
            {
                seed ^= s[i];
                seed *= Prime64;
            }
            return seed;
        }

        /// <summary>Single-scalar FNV-1a mix — `(seed ^ value) * Prime64`. Convenient for
        /// folding a sequence of ulong Ids where no strings are involved.</summary>
        public static ulong Fold64(ulong seed, ulong value) => (seed ^ value) * Prime64;

        /// <summary>Fold `s` into an existing 32-bit `seed` using FNV-1a mixing.
        /// Null string is treated as empty.</summary>
        public static uint Fold32(uint seed, string s)
        {
            if (s == null) return seed;
            for (int i = 0; i < s.Length; i++)
            {
                seed ^= s[i];
                seed *= Prime32;
            }
            return seed;
        }
    }
}
