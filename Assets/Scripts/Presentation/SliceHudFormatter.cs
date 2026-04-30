using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;

// Design note:
// SliceHudFormatter turns the current slice state into one compact on-screen HUD string.
// Inputs: world snapshot, save path, last status, and encounter-turn hint.
// Outputs: presentation-only text with no gameplay mutation.
// Bible reference: PRD Sprint 1 FR-08, Sprint 2 FR-01, Sprint 3 status surfacing.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Builds the compact debug HUD used by the runtime slice shell.</summary>
    public static class SliceHudFormatter
    {
        public static string Format(SliceWorldState world, string savePath, string status, string nextActor)
        {
            var hasWrit = world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId) ? "yes" : "no";
            var stock = world.MerchantInventory.Contains(SliceItemCatalog.GateWritTemplateId) ? "gate writ ready" : "stock spent";
            var doorState = world.DoorOpen ? "open" : "closed";
            var clearance = world.GuardDoorAccessGranted ? "cleared" : $"blocked ({world.GuardWarningCount} warnings)";
            var weapon = world.PlayerEquipment == null || world.PlayerEquipment.Weapon == null ? "none" : world.PlayerEquipment.Weapon.DisplayName;
            var armor = world.PlayerEquipment == null || world.PlayerEquipment.Armor == null ? "none" : world.PlayerEquipment.Armor.DisplayName;
            var guardAttitude = GuardInteractionService.GetAttitudeLabel(world);

            return $@"Sprint 3 Slice
WASD + mouse look | E pickup | M trade | G guard | T door | F encounter | 1/2/3 Ask About | Q Ask DM | R Think | F5 save | F9 load
Player HP {world.Player.Vitals.Health.Current}/{world.Player.Vitals.Health.Max} | Enemy HP {world.Enemy.Vitals.Health.Current}/{world.Enemy.Vitals.Health.Max}
Inventory {world.PlayerInventory.Items.Count}/{world.PlayerInventory.Capacity} | Equipped: {weapon} / {armor} | Gate writ: {hasWrit}
Merchant: {stock} | Layout: {world.Room.LayoutId} | Guard: {guardAttitude}
Door: {doorState} | Guard clearance: {clearance} | Next actor: {nextActor}
Save {savePath}

{status}";
        }
    }
}
