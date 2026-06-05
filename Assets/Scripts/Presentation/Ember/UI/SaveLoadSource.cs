using System.Collections.Generic;
using EmberCrpg.Data.Save;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class SaveLoadScreenState
    {
        public SaveLoadScreenState(int manualSlotCap, IReadOnlyList<SaveSlotMetadata> slots)
        {
            ManualSlotCap = manualSlotCap < 0 ? 0 : manualSlotCap;
            Slots = slots ?? System.Array.Empty<SaveSlotMetadata>();
        }

        public int ManualSlotCap { get; }
        public IReadOnlyList<SaveSlotMetadata> Slots { get; }
    }

    public sealed class SaveLoadActionResult
    {
        public SaveLoadActionResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public string Message { get; }
    }

    public interface ISaveLoadSource
    {
        SaveLoadScreenState ReadSaveLoadState();
    }

    public interface ISaveLoadCommandSink
    {
        SaveLoadActionResult SaveToSlot(SaveSlotId slot);
        SaveLoadActionResult LoadFromSlot(SaveSlotId slot);
        SaveLoadActionResult LoadLatestSave();
    }
}
