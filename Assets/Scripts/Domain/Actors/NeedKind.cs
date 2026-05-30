// Design note:
// NeedKind names Phase 4's actor-local pressure categories. It is only a
// selector for ActorNeeds values; ticking, recovery, refusal, and logging
// belong to later Simulation atoms.
// Atom-map ref: docs/sprint-phase-4-atom-map.md Pure needs component rail.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Actor need categories tracked by the colony-needs slice.</summary>
    public enum NeedKind
    {
        None = 0,
        Hunger = 1,
        Fatigue = 2,
        Thirst = 3
    }
}
