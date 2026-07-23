namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// PAPER-DOLL v1 ("her ciftci tipatip ayni"): the same base sprite wears a subtly different
    /// cloth tint per actor - a deterministic palette swap derived from the id, the cheapest
    /// honest layer of the paper-doll plan (overlays come next; forge re-renders never).
    /// Channels stay within 0.80..1.00 so the art reads; only the CAST varies.
    /// </summary>
    public static class NpcVariantTintService
    {
        public const float MinChannel = 0.80f;

        public static (float R, float G, float B) TintFor(ulong actorId)
        {
            ulong h = actorId + 0x9E3779B97F4A7C15UL;
            h ^= h >> 30; h *= 0xBF58476D1CE4E5B9UL;
            h ^= h >> 27; h *= 0x94D049BB133111EBUL;
            h ^= h >> 31;
            float span = 1f - MinChannel;
            float r = 1f - span * ((h & 0xFFUL) / 255f);
            float g = 1f - span * (((h >> 8) & 0xFFUL) / 255f);
            float b = 1f - span * (((h >> 16) & 0xFFUL) / 255f);
            return (r, g, b);
        }
    }
}
