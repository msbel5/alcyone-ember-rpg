// Design note:
// ActorRole is the narrow Sprint 1 archetype list needed by runtime bootstrap and tests.
// Inputs: role selection during deterministic slice creation.
// Outputs: stable actor categories for UI and interactions.
// Bible reference: PRD FR-03/FR-04.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Actor categories required by the tiny vertical slice.</summary>
    public enum ActorRole
    {
        Player = 0,
        Talker = 1,
        Merchant = 2,
        Guard = 3,
        Enemy = 4,
    }
}
