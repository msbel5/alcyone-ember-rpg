// Design note:
// JobKind is the first tiny category seed for Faz 3 job assignment. It names
// concrete work only when an active sprint consumer needs it; today that means
// the smithing lane for the furnace/SmeltIronIngot acceptance path. No bakery,
// hauling, combat, or speculative extras belong here until their PR consumes them.
// Atom-map ref: DOCS/sprint-faz-3-atom-map.md Pure job definition rail.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Deterministic category used to match actor preferences to job requests.
    /// </summary>
    public enum JobKind
    {
        /// <summary>No job category; reserved as the empty sentinel.</summary>
        None = 0,

        /// <summary>Smithing work, initially consumed by furnace smelting jobs.</summary>
        Smith = 1
    }
}
