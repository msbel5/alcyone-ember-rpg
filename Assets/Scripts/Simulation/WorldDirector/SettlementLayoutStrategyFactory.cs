using EmberCrpg.Domain.Overland;

namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// Factory: maps a <see cref="SettlementKind"/> to its layout strategy. Populous kinds get the village
    /// ring; small/peripheral kinds get the compact cluster; anything unknown defaults to the village ring.
    /// Adding a future kind-specific shape is one switch arm + one strategy class (OCP).
    /// </summary>
    public static class SettlementLayoutStrategyFactory
    {
        private static readonly ISettlementLayoutStrategy Village = new VillageLayoutStrategy();
        private static readonly ISettlementLayoutStrategy Compact = new CompactLayoutStrategy();
        private static readonly ISettlementLayoutStrategy Streets = new StreetLayoutStrategy();

        public static ISettlementLayoutStrategy For(SettlementKind kind)
        {
            switch (kind)
            {
                case SettlementKind.City:
                case SettlementKind.Town:
                    return Streets; // SettlementLayoutGraph v1: radial avenues, not rings
                case SettlementKind.Village:
                    return Village;
                case SettlementKind.Hamlet:
                case SettlementKind.Inn:
                case SettlementKind.Shrine:
                case SettlementKind.Dungeon:
                    return Compact;
                default:
                    return Village;
            }
        }
    }
}
