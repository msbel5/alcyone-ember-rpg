using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Domain.Actors;

// Design note:
// EmberHudFormatter turns the current slice state into one compact on-screen HUD string.
// Inputs: world snapshot, save path, last status, and encounter-turn hint.
// Outputs: presentation-only text with no gameplay mutation.
// Bible reference: PRD Sprint 1 FR-08, Sprint 2 FR-01.
namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>Builds the compact debug HUD used by the runtime slice shell.</summary>
    public static class EmberHudFormatter
    {
        public static string Format(WorldState world, string savePath, string status, string nextActor, EmberAtmosphereCueSet atmosphere)
        {
            var hasWrit = world.PlayerInventory.Contains(WorldItemCatalog.GateWritTemplateId) ? "yes" : "no";
            var stock = world.MerchantInventory.Contains(WorldItemCatalog.GateWritTemplateId) ? "gate writ ready" : "stock spent";
            var doorState = world.DoorOpen ? "open" : "closed";
            var clearance = world.GuardDoorAccessGranted ? "cleared" : $"blocked ({world.GuardWarningCount} warnings)";
            var cues = atmosphere ?? EmberAtmosphereCueSet.Silent("hud-missing");

            return $@"Sprint 4 Slice
WASD + mouse look | I inventory | Z equip weapon | X unequip | E pickup | M trade | G guard | T door | F encounter | 1/2/3 Ask About | Q Ask DM | R Think | F5/F9 save/load
Player HP {world.Actors.FirstByRole(ActorRole.Player).Vitals.Health.Current}/{world.Actors.FirstByRole(ActorRole.Player).Vitals.Health.Max} | Enemy HP {world.Actors.FirstByRole(ActorRole.Enemy).Vitals.Health.Current}/{world.Actors.FirstByRole(ActorRole.Enemy).Vitals.Health.Max}
Inventory {world.PlayerInventory.Items.Count}/{world.PlayerInventory.Capacity} | {InventoryEquipmentFormatter.FormatEquipmentLine(world)} | Gate writ: {hasWrit} | Merchant: {stock}
Door: {doorState} | Guard clearance: {clearance} | Next actor: {nextActor}
Atmosphere: {cues.AmbienceId} | Music: {cues.MusicId} | SFX: {cues.SfxId}
Save {savePath}

{status}";
        }
    }
}
