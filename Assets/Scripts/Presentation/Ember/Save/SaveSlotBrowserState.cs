using System;
using EmberCrpg.Data.Save;

namespace EmberCrpg.Presentation.Ember.Save
{
    public sealed class SaveSlotBrowserState
    {
        private readonly int _manualCap;
        private int _cursor;

        public SaveSlotBrowserState(int manualCap)
        {
            if (manualCap < 0) throw new ArgumentOutOfRangeException(nameof(manualCap), "manualCap must be >= 0.");
            _manualCap = manualCap;
            _cursor = 0;
        }

        public SaveSlotId CurrentSlot => SlotAt(_cursor);

        public void MoveNext()
        {
            var total = TotalSlots();
            if (total <= 0) return;
            _cursor = (_cursor + 1) % total;
        }

        public void MovePrevious()
        {
            var total = TotalSlots();
            if (total <= 0) return;
            _cursor = (_cursor + total - 1) % total;
        }

        public string DescribeCurrent(SaveSlotMetadata metadata)
            => SaveSlotLabelFormatter.Describe(CurrentSlot, metadata);

        private int TotalSlots() => 2 + _manualCap;

        private SaveSlotId SlotAt(int cursor)
        {
            if (cursor <= 0) return SaveSlotId.Quick;
            if (cursor == 1) return SaveSlotId.Auto;
            return SaveSlotId.Manual(cursor - 2);
        }
    }
}
