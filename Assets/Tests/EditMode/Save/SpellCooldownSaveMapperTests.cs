using System;
using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic round-trip of SpellCooldownState through the JSON save layer DTO.
// Covers ordering, zero-tick filtering, null tolerance, and faithful tick preservation.
namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>Verifies SpellCooldownSaveMapper preserves cooldown state across save/load.</summary>
    public sealed class SpellCooldownSaveMapperTests
    {
        [Test]
        public void ToData_NullState_ReturnsEmptyEntries()
        {
            var data = SpellCooldownSaveMapper.ToData(null);

            Assert.That(data, Is.Not.Null);
            Assert.That(data.entries, Is.Not.Null);
            Assert.That(data.entries.Length, Is.EqualTo(0));
        }

        [Test]
        public void ToData_EmptyState_ReturnsEmptyEntries()
        {
            var data = SpellCooldownSaveMapper.ToData(new SpellCooldownState());

            Assert.That(data.entries.Length, Is.EqualTo(0));
        }

        [Test]
        public void ToData_NonEmptyState_OrdersEntriesBySpellTemplateId()
        {
            var state = new SpellCooldownState();
            state.SetRemainingTicks("ember.spark", 3);
            state.SetRemainingTicks("ash.bind", 5);
            state.SetRemainingTicks("warden.ward", 1);

            var data = SpellCooldownSaveMapper.ToData(state);

            Assert.That(data.entries.Select(entry => entry.spellTemplateId), Is.EqualTo(new[]
            {
                "ash.bind",
                "ember.spark",
                "warden.ward",
            }));
            Assert.That(data.entries.Single(entry => entry.spellTemplateId == "ash.bind").remainingTicks, Is.EqualTo(5));
            Assert.That(data.entries.Single(entry => entry.spellTemplateId == "ember.spark").remainingTicks, Is.EqualTo(3));
            Assert.That(data.entries.Single(entry => entry.spellTemplateId == "warden.ward").remainingTicks, Is.EqualTo(1));
        }

        [Test]
        public void ToState_NullDto_ReturnsEmptyState()
        {
            var state = SpellCooldownSaveMapper.ToState(null);

            Assert.That(state, Is.Not.Null);
            Assert.That(state.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void ToState_NullEntriesArray_ReturnsEmptyState()
        {
            var state = SpellCooldownSaveMapper.ToState(new SpellCooldownSaveData { entries = null });

            Assert.That(state.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void ToState_SkipsNullEmptyAndZeroTickEntries()
        {
            var data = new SpellCooldownSaveData
            {
                entries = new[]
                {
                    null,
                    new SpellCooldownEntrySaveData { spellTemplateId = "", remainingTicks = 4 },
                    new SpellCooldownEntrySaveData { spellTemplateId = "ember.spark", remainingTicks = 0 },
                    new SpellCooldownEntrySaveData { spellTemplateId = "ash.bind", remainingTicks = 7 },
                },
            };

            var state = SpellCooldownSaveMapper.ToState(data);

            Assert.That(state.GetTrackedSpellTemplateIds(), Is.EqualTo(new[] { "ash.bind" }));
            Assert.That(state.GetRemainingTicks("ash.bind"), Is.EqualTo(7));
            Assert.That(state.GetRemainingTicks("ember.spark"), Is.EqualTo(0));
        }

        [Test]
        public void RoundTrip_PreservesEveryActiveCooldown()
        {
            var state = new SpellCooldownState();
            state.SetRemainingTicks("ember.spark", 3);
            state.SetRemainingTicks("ash.bind", 5);
            state.SetRemainingTicks("warden.ward", 1);

            var data = SpellCooldownSaveMapper.ToData(state);
            var rebuilt = SpellCooldownSaveMapper.ToState(data);

            Assert.That(rebuilt.GetRemainingTicks("ember.spark"), Is.EqualTo(3));
            Assert.That(rebuilt.GetRemainingTicks("ash.bind"), Is.EqualTo(5));
            Assert.That(rebuilt.GetRemainingTicks("warden.ward"), Is.EqualTo(1));
            Assert.That(rebuilt.GetTrackedSpellTemplateIds().Count, Is.EqualTo(3));
        }

        [Test]
        public void RoundTrip_ZeroOnlyState_RebuildsEmpty()
        {
            var state = new SpellCooldownState();

            var data = SpellCooldownSaveMapper.ToData(state);
            var rebuilt = SpellCooldownSaveMapper.ToState(data);

            Assert.That(rebuilt.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }
    }
}
