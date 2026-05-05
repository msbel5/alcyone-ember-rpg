using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic round-trip of ShieldBuffState through the JSON save layer DTO.
// Covers ordering, zero-tick filtering, null tolerance, magnitude preservation, and
// non-mutation of the inverse mapping when malformed entries are present.
namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>Verifies ShieldBuffSaveMapper preserves shield-buff state across save/load.</summary>
    public sealed class ShieldBuffSaveMapperTests
    {
        [Test]
        public void ToData_NullState_ReturnsEmptyEntries()
        {
            var data = ShieldBuffSaveMapper.ToData(null);

            Assert.That(data, Is.Not.Null);
            Assert.That(data.entries, Is.Not.Null);
            Assert.That(data.entries.Length, Is.EqualTo(0));
        }

        [Test]
        public void ToData_EmptyState_ReturnsEmptyEntries()
        {
            var data = ShieldBuffSaveMapper.ToData(new ShieldBuffState());

            Assert.That(data.entries.Length, Is.EqualTo(0));
        }

        [Test]
        public void ToData_NonEmptyState_OrdersEntriesBySpellTemplateId()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember.spark", 3, 5);
            state.SetActiveBuff("ash.bind", 7, 2);
            state.SetActiveBuff("warden.ward", 1, 9);

            var data = ShieldBuffSaveMapper.ToData(state);

            Assert.That(data.entries.Select(entry => entry.spellTemplateId), Is.EqualTo(new[]
            {
                "ash.bind",
                "ember.spark",
                "warden.ward",
            }));
            Assert.That(data.entries.Single(entry => entry.spellTemplateId == "ash.bind").remainingTicks, Is.EqualTo(7));
            Assert.That(data.entries.Single(entry => entry.spellTemplateId == "ash.bind").magnitude, Is.EqualTo(2));
            Assert.That(data.entries.Single(entry => entry.spellTemplateId == "ember.spark").remainingTicks, Is.EqualTo(3));
            Assert.That(data.entries.Single(entry => entry.spellTemplateId == "ember.spark").magnitude, Is.EqualTo(5));
            Assert.That(data.entries.Single(entry => entry.spellTemplateId == "warden.ward").remainingTicks, Is.EqualTo(1));
            Assert.That(data.entries.Single(entry => entry.spellTemplateId == "warden.ward").magnitude, Is.EqualTo(9));
        }

        [Test]
        public void ToState_NullDto_ReturnsEmptyState()
        {
            var state = ShieldBuffSaveMapper.ToState(null);

            Assert.That(state, Is.Not.Null);
            Assert.That(state.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void ToState_NullEntriesArray_ReturnsEmptyState()
        {
            var state = ShieldBuffSaveMapper.ToState(new ShieldBuffSaveData { entries = null });

            Assert.That(state.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void ToState_SkipsNullEmptyZeroTickAndNegativeMagnitudeEntries()
        {
            var data = new ShieldBuffSaveData
            {
                entries = new[]
                {
                    null,
                    new ShieldBuffEntrySaveData { spellTemplateId = "", remainingTicks = 4, magnitude = 3 },
                    new ShieldBuffEntrySaveData { spellTemplateId = "ember.spark", remainingTicks = 0, magnitude = 3 },
                    new ShieldBuffEntrySaveData { spellTemplateId = "stale.bad", remainingTicks = 4, magnitude = -1 },
                    new ShieldBuffEntrySaveData { spellTemplateId = "ash.bind", remainingTicks = 7, magnitude = 2 },
                },
            };

            var state = ShieldBuffSaveMapper.ToState(data);

            Assert.That(state.GetTrackedSpellTemplateIds(), Is.EqualTo(new[] { "ash.bind" }));
            Assert.That(state.GetRemainingTicks("ash.bind"), Is.EqualTo(7));
            Assert.That(state.GetMagnitude("ash.bind"), Is.EqualTo(2));
            Assert.That(state.IsActive("ember.spark"), Is.False);
            Assert.That(state.IsActive("stale.bad"), Is.False);
        }

        [Test]
        public void ToState_PreservesZeroMagnitudeEntries()
        {
            var data = new ShieldBuffSaveData
            {
                entries = new[]
                {
                    new ShieldBuffEntrySaveData { spellTemplateId = "ember.spark", remainingTicks = 4, magnitude = 0 },
                },
            };

            var state = ShieldBuffSaveMapper.ToState(data);

            Assert.That(state.IsActive("ember.spark"), Is.True);
            Assert.That(state.GetRemainingTicks("ember.spark"), Is.EqualTo(4));
            Assert.That(state.GetMagnitude("ember.spark"), Is.EqualTo(0));
        }

        [Test]
        public void RoundTrip_PreservesEveryActiveShieldBuff()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember.spark", 3, 5);
            state.SetActiveBuff("ash.bind", 7, 2);
            state.SetActiveBuff("warden.ward", 1, 9);

            var data = ShieldBuffSaveMapper.ToData(state);
            var rebuilt = ShieldBuffSaveMapper.ToState(data);

            Assert.That(rebuilt.GetRemainingTicks("ember.spark"), Is.EqualTo(3));
            Assert.That(rebuilt.GetMagnitude("ember.spark"), Is.EqualTo(5));
            Assert.That(rebuilt.GetRemainingTicks("ash.bind"), Is.EqualTo(7));
            Assert.That(rebuilt.GetMagnitude("ash.bind"), Is.EqualTo(2));
            Assert.That(rebuilt.GetRemainingTicks("warden.ward"), Is.EqualTo(1));
            Assert.That(rebuilt.GetMagnitude("warden.ward"), Is.EqualTo(9));
            Assert.That(rebuilt.GetTrackedSpellTemplateIds().Count, Is.EqualTo(3));
        }

        [Test]
        public void RoundTrip_ZeroOnlyState_RebuildsEmpty()
        {
            var state = new ShieldBuffState();

            var data = ShieldBuffSaveMapper.ToData(state);
            var rebuilt = ShieldBuffSaveMapper.ToState(data);

            Assert.That(rebuilt.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }
    }
}
