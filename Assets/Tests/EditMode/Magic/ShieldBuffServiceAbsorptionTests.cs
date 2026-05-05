using System;
using System.Linq;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the Sprint 5 shield-buff damage absorption seam: deterministic ordinal-keyed magnitude
// consumption against a single ShieldBuffState bag with full-buff removal on magnitude-exhaust,
// preservation of remaining ticks on partial absorption, untouched buffs left in place when the
// incoming damage is fully absorbed earlier, and the ordered consume/expire trace returned to
// the caller. Pure Simulation: no Unity types, no save coupling, no actor-keyed dispatch.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic shield buff damage absorption behavior.</summary>
    public sealed class ShieldBuffServiceAbsorptionTests
    {
        [Test]
        public void AbsorbDamage_NullState_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() => service.AbsorbDamage(null, 5));
        }

        [Test]
        public void AbsorbDamage_NegativeDamage_Throws()
        {
            var state = new ShieldBuffState();
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentOutOfRangeException>(() => service.AbsorbDamage(state, -1));
        }

        [Test]
        public void AbsorbDamage_ZeroDamage_NoOpReturnsEmptyTrace()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 0);

            Assert.That(result.IncomingDamage, Is.EqualTo(0));
            Assert.That(result.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(4));
        }

        [Test]
        public void AbsorbDamage_EmptyState_ReturnsRemainingEqualsIncoming()
        {
            var state = new ShieldBuffState();
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 7);

            Assert.That(result.IncomingDamage, Is.EqualTo(7));
            Assert.That(result.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(result.RemainingDamage, Is.EqualTo(7));
            Assert.That(result.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
        }

        [Test]
        public void AbsorbDamage_SingleBuff_PartiallyConsumed_ReducesMagnitudeAndPreservesTicks()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 10);
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 4);

            Assert.That(result.IncomingDamage, Is.EqualTo(4));
            Assert.That(result.AbsorbedDamage, Is.EqualTo(4));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(6));
            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(state.IsActive("ember_ward"), Is.True);
        }

        [Test]
        public void AbsorbDamage_SingleBuff_ExactlyConsumed_RemovesBuffEvenWhenTicksRemain()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 4);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(4));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(state.IsActive("ember_ward"), Is.False);
            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(0));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(0));
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void AbsorbDamage_SingleBuff_OverConsumed_RemovesBuffAndReturnsRemainingDamage()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 9);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(4));
            Assert.That(result.RemainingDamage, Is.EqualTo(5));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(state.IsActive("ember_ward"), Is.False);
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void AbsorbDamage_MultipleBuffs_DeterministicOrdinalOrder_ConsumesAegisBeforeEmber()
        {
            var state = new ShieldBuffState();
            // Insert ember_ward first to prove ordering does NOT depend on insertion order:
            // ascending ordinal sort puts "aegis_pulse" before "ember_ward".
            state.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 5);
            state.SetActiveBuff("aegis_pulse", remainingTicks: 10, magnitude: 3);
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 2);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(2));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "aegis_pulse" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(state.GetMagnitude("aegis_pulse"), Is.EqualTo(1));
            Assert.That(state.GetRemainingTicks("aegis_pulse"), Is.EqualTo(10));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(5));
            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(20));
        }

        [Test]
        public void AbsorbDamage_MultipleBuffs_FirstFullyConsumedThenSecondPartial()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 5);
            state.SetActiveBuff("aegis_pulse", remainingTicks: 10, magnitude: 3);
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 4);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(4));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "aegis_pulse", "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.EqualTo(new[] { "aegis_pulse" }));
            Assert.That(state.IsActive("aegis_pulse"), Is.False);
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(20));
        }

        [Test]
        public void AbsorbDamage_MultipleBuffs_ExhaustsAllAndReturnsRemainingDamage()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 5);
            state.SetActiveBuff("aegis_pulse", remainingTicks: 10, magnitude: 3);
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 12);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(8));
            Assert.That(result.RemainingDamage, Is.EqualTo(4));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "aegis_pulse", "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.EqualTo(new[] { "aegis_pulse", "ember_ward" }));
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void AbsorbDamage_StopsOnceDamageIsZero_LeavesLaterBuffsUntouched()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("aegis_pulse", remainingTicks: 10, magnitude: 8);
            state.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 5);
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 3);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(3));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "aegis_pulse" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(state.GetMagnitude("aegis_pulse"), Is.EqualTo(5));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(5));
            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(20));
        }

        [Test]
        public void AbsorbDamage_SkipsZeroMagnitudeBuffs_WithoutMarkingThemConsumed()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("aegis_pulse", remainingTicks: 6, magnitude: 0);
            state.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 4);
            var service = new ShieldBuffService();

            var result = service.AbsorbDamage(state, 3);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(3));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(state.GetMagnitude("aegis_pulse"), Is.EqualTo(0));
            Assert.That(state.GetRemainingTicks("aegis_pulse"), Is.EqualTo(6));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(1));
            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(20));
        }

        [Test]
        public void AbsorbDamage_RepeatedCallsAccumulateMagnitudeReduction()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 10);
            var service = new ShieldBuffService();

            var first = service.AbsorbDamage(state, 3);
            var second = service.AbsorbDamage(state, 4);

            Assert.That(first.AbsorbedDamage, Is.EqualTo(3));
            Assert.That(second.AbsorbedDamage, Is.EqualTo(4));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(3));
            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(state.IsActive("ember_ward"), Is.True);
        }

        [Test]
        public void AbsorbDamage_DoesNotChangeRemainingTicks_OnPartialConsume()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 7, magnitude: 6);
            var service = new ShieldBuffService();

            service.AbsorbDamage(state, 1);

            Assert.That(state.GetRemainingTicks("ember_ward"), Is.EqualTo(7));
            Assert.That(state.GetMagnitude("ember_ward"), Is.EqualTo(5));
        }

        [Test]
        public void AbsorbDamage_AbsorbedPlusRemaining_AlwaysEqualsIncoming()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 5, magnitude: 2);
            var service = new ShieldBuffService();

            foreach (var damage in new[] { 0, 1, 2, 3, 7 })
            {
                var stateForCall = new ShieldBuffState();
                stateForCall.SetActiveBuff("ember_ward", remainingTicks: 5, magnitude: 2);
                var result = service.AbsorbDamage(stateForCall, damage);
                Assert.That(result.AbsorbedDamage + result.RemainingDamage, Is.EqualTo(damage), $"damage={damage}");
            }
        }

        [Test]
        public void AbsorbDamage_AfterAdvanceTicksExpiresBuff_BuffNoLongerAbsorbs()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff("ember_ward", remainingTicks: 4, magnitude: 6);
            var service = new ShieldBuffService();

            service.AdvanceTicks(state, 4);
            var result = service.AbsorbDamage(state, 5);

            Assert.That(state.IsActive("ember_ward"), Is.False);
            Assert.That(result.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(result.RemainingDamage, Is.EqualTo(5));
            Assert.That(result.ConsumedSpellTemplateIds, Is.Empty);
        }
    }
}
