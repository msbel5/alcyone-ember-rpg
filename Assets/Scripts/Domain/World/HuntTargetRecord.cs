namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// W36 GUARD+COMBAT: an ACTIVE enemy hunt. PursuitRecord's kardeşi for the enemy side:
    /// HunterId → TargetId with a bounded expiry so an orphaned scan cannot pin the ledger.
    /// The Decide phase arms; the Advance phase reads. Newest prey wins per hunter (mirror
    /// of RegisterPursuit's overwrite semantics). Save is a parallel-array triple identical
    /// to GuardPursuits — see WorldSaveMapper.
    /// </summary>
    public sealed class HuntTargetRecord
    {
        public ulong HunterId;
        public ulong TargetId;
        public long UntilMinutes;
    }
}
