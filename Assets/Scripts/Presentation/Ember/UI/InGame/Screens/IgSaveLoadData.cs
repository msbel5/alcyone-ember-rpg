using EmberCrpg.Data.Save;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class SaveSlotViewData
    {
        public SaveSlotViewData(SaveSlotId slotId, string title, string detail, bool hasSave)
        {
            SlotId = slotId;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            HasSave = hasSave;
        }

        public SaveSlotId SlotId { get; }
        public string Title { get; }
        public string Detail { get; }
        public bool HasSave { get; }
    }

    public sealed class SaveLoadScreenData
    {
        public SaveLoadScreenData(string statusLine, SaveSlotViewData[] slots)
        {
            StatusLine = statusLine ?? string.Empty;
            Slots = slots ?? System.Array.Empty<SaveSlotViewData>();
        }

        public string StatusLine { get; }
        public SaveSlotViewData[] Slots { get; }
    }

    public static class IgSaveLoadData
    {
        public static readonly SaveLoadScreenData Default = new SaveLoadScreenData(
            "No save slots are available yet.",
            System.Array.Empty<SaveSlotViewData>());

        public static SaveLoadScreenData Current = Default;
    }
}
