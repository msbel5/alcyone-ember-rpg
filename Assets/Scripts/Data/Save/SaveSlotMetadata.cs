using System;

namespace EmberCrpg.Data.Save
{
    [Serializable]
    public sealed class SaveSlotMetadata
    {
        public int metadataVersion;
        public int envelopeVersion;
        public int schemaVersion;
        public string slotKind;
        public int slotIndex;
        public string label;
        public string sceneName;
        public long playtimeMinutes;
        public string savedAtUtcIso;
        public string thumbnailPath;
    }
}
