// Design note:
// ItemQuality names the small starter palette of crafting-quality tiers an item
// can carry, without leaking pricing, bonus, or generator concerns. Mirrors
// SiteKind / EquipmentSlot's tiny enum shape so ItemRecord can carry quality
// as a deterministic value alongside material and slot.
// Atom-map ref: docs/sprint-faz-1-atom-map.md ItemStore sub-area.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Supported item quality tiers for the Faz 1 ItemStore sub-area.</summary>
    public enum ItemQuality
    {
        None = 0,
        Common = 1,
        Fine = 2,
        Masterwork = 3,
    }
}
