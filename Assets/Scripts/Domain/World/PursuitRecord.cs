namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// P0 (ARCHITECTURE_GAPS #2 - 'pursuit is arithmetically erased'): an ACTIVE guard chase.
    /// The witness report writes it; the PerTick schedule reads it, so the chase moves at the
    /// same cadence as the return-to-post writer instead of losing 60:1 to it.
    /// </summary>
    public sealed class PursuitRecord
    {
        public ulong GuardId;
        public ulong TargetId;
        public long UntilMinutes;
    }
}
