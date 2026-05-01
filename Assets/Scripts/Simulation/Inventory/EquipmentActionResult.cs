// Design note:
// EquipmentActionResult is the narrow response object for equipment actions.
// Inputs: EquipmentService validation and mutation decisions.
// Outputs: success flag, deterministic error code, and player-facing text.
// Bible reference: Sprint 4 Faz 4 equipment constraints and player UI acceptance.
namespace EmberCrpg.Simulation.Inventory
{
    /// <summary>Result of one equip or unequip request.</summary>
    public sealed class EquipmentActionResult
    {
        private EquipmentActionResult(bool success, EquipmentActionError error, string message)
        {
            Success = success;
            Error = error;
            Message = message;
        }

        public bool Success { get; }
        public EquipmentActionError Error { get; }
        public string Message { get; }

        public static EquipmentActionResult Ok(string message)
        {
            return new EquipmentActionResult(true, EquipmentActionError.None, message);
        }

        public static EquipmentActionResult Fail(EquipmentActionError error, string message)
        {
            return new EquipmentActionResult(false, error, message);
        }
    }
}
