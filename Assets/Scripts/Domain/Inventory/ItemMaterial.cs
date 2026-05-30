// Design note:
// ItemMaterial names the small starter palette of MATTER-box substances an item
// can be made of, without leaking presentation, fabrication, or sourcing concerns.
// Mirrors SiteKind / EquipmentSlot's tiny enum shape so ItemRecord can carry the
// material as a deterministic value alongside quality and slot.
// Atom-map ref: docs/sprint-phase-1-atom-map.md ItemStore sub-area.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Supported item materials for the Phase 1 ItemStore sub-area.</summary>
    public enum ItemMaterial
    {
        None = 0,
        Wood = 1,
        Iron = 2,
        Cloth = 3,
        Food = 4,
    }
}
