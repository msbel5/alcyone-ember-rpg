using EmberCrpg.Data.Save;

namespace EmberCrpg.Presentation.Ember.Save
{
    public static class SaveSlotLabelFormatter
    {
        public static string Title(SaveSlotId slot)
        {
            switch (slot.Kind)
            {
                case SaveSlotKind.Quick: return "Quick";
                case SaveSlotKind.Auto: return "Auto";
                case SaveSlotKind.Manual: return "Manual " + (slot.Index + 1);
                default: return "Slot";
            }
        }

        public static string Describe(SaveSlotId slot, SaveSlotMetadata metadata)
        {
            if (metadata == null) return Title(slot) + " | Empty";

            var label = string.IsNullOrWhiteSpace(metadata.label) ? "Saved" : metadata.label;
            var scene = string.IsNullOrWhiteSpace(metadata.sceneName) ? "Unknown" : metadata.sceneName;
            var minutes = metadata.playtimeMinutes < 0 ? 0 : metadata.playtimeMinutes;
            return Title(slot) + " | " + label + " | " + scene + " | " + minutes + "m";
        }
    }
}
