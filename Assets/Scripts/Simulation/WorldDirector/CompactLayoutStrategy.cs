namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// Layout for small/peripheral settlements (Hamlet / Inn / Shrine / Dungeon): a tight cluster of 1..3
    /// shells on a smaller plot. Composes the village ring with a lower count + radius rather than
    /// duplicating the placement maths (DRY) — the deterministic behaviour is inherited unchanged.
    /// </summary>
    public sealed class CompactLayoutStrategy : ISettlementLayoutStrategy
    {
        private readonly VillageLayoutStrategy _inner = new VillageLayoutStrategy(minBuildings: 1, maxBuildings: 3, ringRadius: 5f);

        public SettlementLayout Plan(in SettlementContext context) => _inner.Plan(context);
    }
}
