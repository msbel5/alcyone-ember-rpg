// Design note:
// IDeterministicRng is the single seeded random surface used by Sprint 1 simulation services.
// Inputs: integer range requests and percentage rolls.
// Outputs: reproducible pseudo-random numbers with no Unity dependency.
// Bible reference: ARCHITECTURE.md deterministic world lock-in, PRD NFR deterministic-first.
namespace EmberCrpg.Simulation.Rng
{
    /// <summary>Small deterministic RNG abstraction for pure simulation code.</summary>
    public interface IDeterministicRng
    {
        int NextInt(int exclusiveMax);
        int RollPercent();
    }
}
