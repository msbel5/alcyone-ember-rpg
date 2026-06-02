using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Domain.Process
{
    /// <summary>Pure deterministic bridge from generated NPC role buckets to production job lanes.</summary>
    public static class NpcRoleJobMapper
    {
        public static JobKind? ToJobKind(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Blacksmith:
                    return JobKind.Smith;
                case NpcRole.Farmer:
                    return JobKind.Farmer;
                default:
                    return null;
            }
        }
    }
}
