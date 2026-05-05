using System;
using System.Linq;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 shield-buff tick-down seam: per-spell remaining-tick decay
// that preserves magnitude until expiry, and removes expired entries from tracked state.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic shield buff tick-down behavior.</summary>
    public sealed class ShieldBuffServiceTests
    {
        [Test]
        public void AdvanceTicks_ReducesRemainingTicksAndPreservesMagnitude()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var service = new ShieldBuffService();

            service.AdvanceTicks(state, 12);

            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(18));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(state.IsActive("ember_ward"), Is.True);
        }

        [Test]
        public void AdvanceTicks_ExactExpiry_RemovesEntry()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 8, magnitude: 4);
            var service = new ShieldBuffService();

            service.AdvanceTicks(state, 8);

            Assert.That(state.IsActive("ember_ward"), Is.False);
            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(0));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(0));
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void AdvanceTicks_OverExpiry_ClampsToZeroAndRemovesEntry()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 5, magnitude: 4);
            var service = new ShieldBuffService();

            service.AdvanceTicks(state, 99);

            Assert.That(state.IsActive("ember_ward"), Is.False);
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void AdvanceTicks_MultipleBuffs_DecaysIndependentlyAndExpiresOnlyExpired()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 10, magnitude: 4);
            state.SetActiveBuff("aegis_pulse", remainingTicks: 25, magnitude: 2);
            var service = new ShieldBuffService();

            service.AdvanceTicks(state, 10);

            Assert.That(state.IsActive("ember_ward"), Is.False);
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.EquivalentTo(new[] { "aegis_pulse" }));
            Assert.That(state.GetRemainingTicks("aegis_pulse"), Is.EqualTo(15));
            Assert.That(state.GetMagnitude("aegis_pulse"), Is.EqualTo(2));
        }

        [Test]
        public void AdvanceTicks_ZeroElapsed_IsNoOp()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var service = new ShieldBuffService();

            service.AdvanceTicks(state, 0);

            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(4));
        }

        [Test]
        public void AdvanceTicks_EmptyState_IsNoOp()
        {
            var state = new ShieldBuffState();
            var service = new ShieldBuffService();

            Assert.DoesNotThrow(() => service.AdvanceTicks(state, 5));
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void AdvanceTicks_NegativeElapsed_Throws()
        {
            var state = new ShieldBuffState();
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentOutOfRangeException>(() => service.AdvanceTicks(state, -1));
        }

        [Test]
        public void AdvanceTicks_NullState_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() => service.AdvanceTicks(null, 5));
        }

        [Test]
        public void AdvanceTicks_ZeroMagnitudeBuff_DecaysAndExpiresLikeAnyOther()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 4, magnitude: 0);
            var service = new ShieldBuffService();

            service.AdvanceTicks(state, 1);

            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(3));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(0));

            service.AdvanceTicks(state, 3);

            Assert.That(state.IsActive("ember_ward"), Is.False);
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void AdvanceTicks_RepeatedCallsAccumulateDecay()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var service = new ShieldBuffService();

            service.AdvanceTicks(state, 5);
            service.AdvanceTicks(state, 7);
            service.AdvanceTicks(state, 3);

            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(15));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(4));
        }

        [Test]
        public void AdvanceTicks_DoesNotResurrectAlreadyExpiredEntries()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 4, magnitude: 4);
            var service = new ShieldBuffService();

            service.AdvanceTicks(state, 4);
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);

            service.AdvanceTicks(state, 100);

            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
            Assert.That(state.IsActive("ember_ward"), Is.False);
        }
    }
}
