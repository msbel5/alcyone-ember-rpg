// Design note:
// SiteKind names the three Phase 1 site categories without leaking any presentation
// concerns. Mirrors EquipmentSlot's tiny enum shape so SiteRecord can carry the
// category as a deterministic value alongside its grid bounds.
namespace EmberCrpg.Domain.World
{
    /// <summary>Supported site categories for the Phase 1 SiteStore sub-area.</summary>
    public enum SiteKind
    {
        None = 0,
        Region = 1,
        Settlement = 2,
        Dungeon = 3,
    }
}
