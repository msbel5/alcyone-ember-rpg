using EmberCrpg.Data.Save;
using EmberCrpg.Presentation.Ember.Save;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class SaveSlotBrowserStateTests
    {
        [Test]
        public void MoveNextAndPrevious_CyclesQuickAutoManualSlots()
        {
            var state = new SaveSlotBrowserState(manualCap: 3);

            Assert.That(state.CurrentSlot, Is.EqualTo(SaveSlotId.Quick));
            state.MoveNext();
            Assert.That(state.CurrentSlot, Is.EqualTo(SaveSlotId.Auto));
            state.MoveNext();
            Assert.That(state.CurrentSlot, Is.EqualTo(SaveSlotId.Manual(0)));
            state.MoveNext();
            Assert.That(state.CurrentSlot, Is.EqualTo(SaveSlotId.Manual(1)));
            state.MoveNext();
            Assert.That(state.CurrentSlot, Is.EqualTo(SaveSlotId.Manual(2)));
            state.MoveNext();
            Assert.That(state.CurrentSlot, Is.EqualTo(SaveSlotId.Quick));

            state.MovePrevious();
            Assert.That(state.CurrentSlot, Is.EqualTo(SaveSlotId.Manual(2)));
        }

        [Test]
        public void DescribeCurrent_UsesMetadataWhenPresent()
        {
            var state = new SaveSlotBrowserState(manualCap: 2);
            state.MoveNext();
            state.MoveNext(); // Manual(0)

            var description = state.DescribeCurrent(new SaveSlotMetadata
            {
                label = "Before Gate",
                sceneName = "GeneratedWorld",
                playtimeMinutes = 87
            });

            Assert.That(description, Does.Contain("Manual 1"));
            Assert.That(description, Does.Contain("Before Gate"));
            Assert.That(description, Does.Contain("GeneratedWorld"));
            Assert.That(description, Does.Contain("87m"));
        }

        [Test]
        public void DescribeCurrent_UsesEmptyPlaceholderWithoutMetadata()
        {
            var state = new SaveSlotBrowserState(manualCap: 1);

            var description = state.DescribeCurrent(null);

            Assert.That(description, Is.EqualTo("Quick | Empty"));
        }
    }
}
