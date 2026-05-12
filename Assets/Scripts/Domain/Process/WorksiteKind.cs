// Design note:
// WorksiteKind is the smallest typed WORLD/PROCESS key for Faz 2 worksites.
// It deliberately starts with only the sentinel and Furnace so RecipeSystem can
// validate SmeltIronIngot without committing to the full worksite taxonomy yet.
// Atom-map ref: DOCS/sprint-faz-2-atom-map.md Worksite state sub-area.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Deterministic worksite category used by recipe execution and worksite storage.
    /// </summary>
    public enum WorksiteKind
    {
        /// <summary>No worksite; reserved as the empty sentinel.</summary>
        None = 0,

        /// <summary>Furnace worksite used by the first SmeltIronIngot slice.</summary>
        Furnace = 1,
    }
}
