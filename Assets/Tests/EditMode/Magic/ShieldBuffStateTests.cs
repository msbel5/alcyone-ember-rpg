using System;
using EmberCrpg.Domain.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 shield-buff foundation: per-spell remaining-tick + magnitude
// bookkeeping with no Unity dependency. Resolution and tick-down land in a follow-up slice.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic shield buff state shape, validation, and lookups.</summary>
    public sealed class ShieldBuffStateTests
    {
        [Test]
        public void GetRemainingTicks_UntrackedSpell_ReturnsZero()
        {
            var state = new ShieldBuffState();

            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(0));
            Assert.That(state.IsActive("ember_ward"), Is.False);
        }

        [Test]
        public void GetMagnitude_UntrackedSpell_ReturnsZero()
        {
            var state = new ShieldBuffState();

            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(0));
        }

        [Test]
        public void SetActiveBuff_PositiveTicks_StoresTicksAndMagnitude()
        {
            var state = new ShieldBuffState();

            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(state.IsActive("ember_ward"), Is.True);
        }

        [Test]
        public void SetActiveBuff_ZeroTicks_RemovesEntry()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            state.SetActiveBuff("ember_ward", remainingTicks: 0, magnitude: 4);

            Assert.That(state.IsActive("ember_ward"), Is.False);
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void SetActiveBuff_ReplacesExistingEntry()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            state.SetActiveBuff("ember_ward", remainingTicks: 12, magnitude: 7);

            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(12));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(7));
        }

        [Test]
        public void SetActiveBuff_NegativeTicks_Throws()
        {
            var state = new ShieldBuffState();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => state.SetActiveBuff("ember_ward", remainingTicks: -1, magnitude: 4));
        }

        [Test]
        public void SetActiveBuff_NegativeMagnitude_Throws()
        {
            var state = new ShieldBuffState();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => state.SetActiveBuff("ember_ward", remainingTicks: 5, magnitude: -1));
        }

        [Test]
        public void SetActiveBuff_BlankSpellId_Throws()
        {
            var state = new ShieldBuffState();

            Assert.Throws<ArgumentException>(
                () => state.SetActiveBuff(" ", remainingTicks: 5, magnitude: 4));
            Assert.Throws<ArgumentException>(
                () => state.SetActiveBuff(null, remainingTicks: 5, magnitude: 4));
        }

        [Test]
        public void Clear_RemovesTrackedEntry()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            state.SetActiveBuff("aegis_pulse", remainingTicks: 10, magnitude: 2);

            state.Clear("ember_ward");

            Assert.That(state.IsActive("ember_ward"), Is.False);
            Assert.That(state.IsActive("aegis_pulse"), Is.True);
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.EquivalentTo(new[] { "aegis_pulse" }));
        }

        [Test]
        public void Clear_UnknownSpell_IsNoOp()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            state.Clear("never_seen");
            state.Clear(null);
            state.Clear(" ");

            Assert.That(state.GetTrackedSpellTemplateIds(), Is.EquivalentTo(new[] { "ember_ward" }));
        }

        [Test]
        public void GetTrackedSpellTemplateIds_ReturnsActiveEntries()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            state.SetActiveBuff("aegis_pulse", remainingTicks: 10, magnitude: 2);

            Assert.That(state.GetTrackedSpellTemplateIds(),
                Is.EquivalentTo(new[] { "ember_ward", "aegis_pulse" }));
        }
    }
}
