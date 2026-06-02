namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// Strategy: deterministically plans the building + player layout for one settlement. Implementations
    /// must be PURE functions of the <see cref="SettlementContext"/> (seed-driven, no clock/UnityRandom),
    /// so the same world always realizes the same town. New settlement shapes = new strategy class.
    /// </summary>
    public interface ISettlementLayoutStrategy
    {
        SettlementLayout Plan(in SettlementContext context);
    }
}
