using System;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 magic outer-tick driver: a single AdvanceTicks call
// that decays both spell cooldown state and timed shield-buff state with parity to
// SpellCooldownService.AdvanceTicks and ShieldBuffService.AdvanceTicks.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic combined cooldown + shield-buff tick advancement.</summary>
    public sealed class MagicTickDriverTests
    {
        private static MagicTickDriver NewDriver()
        {
            return new MagicTickDriver(new SpellCooldownService(), new ShieldBuffService());
        }

        [Test]
        public void Constructor_NullCooldownService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MagicTickDriver(null, new ShieldBuffService()));
        }

        [Test]
        public void Constructor_NullShieldBuffService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MagicTickDriver(new SpellCooldownService(), null));
        }

        [Test]
        public void AdvanceTicks_NullCooldownState_Throws()
        {
            var driver = NewDriver();

            Assert.Throws<ArgumentNullException>(() => driver.AdvanceTicks(null, new ShieldBuffState(), 5));
        }

        [Test]
        public void AdvanceTicks_NullShieldBuffState_Throws()
        {
            var driver = NewDriver();

            Assert.Throws<ArgumentNullException>(() => driver.AdvanceTicks(new SpellCooldownState(), (ShieldBuffState)null, 5));
        }

        [Test]
        public void AdvanceTicks_NegativeElapsed_Throws()
        {
            var driver = NewDriver();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => driver.AdvanceTicks(new SpellCooldownState(), new ShieldBuffState(), -1));
        }

        [Test]
        public void AdvanceTicks_ZeroElapsed_IsNoOp()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("ember_bolt", 12);
            var shieldBuffState = new ShieldBuffState();
            shieldBuffState.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            NewDriver().AdvanceTicks(cooldownState, shieldBuffState, 0);

            Assert.That(cooldownState.GetRemainingTicks("ember_bolt"), Is.EqualTo(12));
            Assert.That(shieldBuffState.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(shieldBuffState.GetMagnitude("ember_ward"), Is.EqualTo(4));
        }

        [Test]
        public void AdvanceTicks_DecaysBothBagsByExactlyElapsedTicks()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("ember_bolt", 20);
            var shieldBuffState = new ShieldBuffState();
            shieldBuffState.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            NewDriver().AdvanceTicks(cooldownState, shieldBuffState, 7);

            Assert.That(cooldownState.GetRemainingTicks("ember_bolt"), Is.EqualTo(13));
            Assert.That(shieldBuffState.GetRemainingTicks("ember_ward"), Is.EqualTo(23));
            Assert.That(shieldBuffState.GetMagnitude("ember_ward"), Is.EqualTo(4));
        }

        [Test]
        public void AdvanceTicks_ExpiresEntriesInBothBagsAtTheSameElapsed()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("ember_bolt", 6);
            var shieldBuffState = new ShieldBuffState();
            shieldBuffState.SetActiveBuff("ember_ward", remainingTicks: 6, magnitude: 4);

            NewDriver().AdvanceTicks(cooldownState, shieldBuffState, 6);

            Assert.That(cooldownState.GetRemainingTicks("ember_bolt"), Is.EqualTo(0));
            Assert.That(cooldownState.GetTrackedSpellTemplateIds(), Is.Empty);
            Assert.That(shieldBuffState.IsActive("ember_ward"), Is.False);
            Assert.That(shieldBuffState.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void AdvanceTicks_EmptyBags_IsSafeNoOp()
        {
            var cooldownState = new SpellCooldownState();
            var shieldBuffState = new ShieldBuffState();

            Assert.DoesNotThrow(() => NewDriver().AdvanceTicks(cooldownState, shieldBuffState, 5));
            Assert.That(cooldownState.GetTrackedSpellTemplateIds(), Is.Empty);
            Assert.That(shieldBuffState.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        [Test]
        public void AdvanceTicks_RepeatedCallsAccumulateDecayAcrossBothBags()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("ember_bolt", 30);
            var shieldBuffState = new ShieldBuffState();
            shieldBuffState.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var driver = NewDriver();

            driver.AdvanceTicks(cooldownState, shieldBuffState, 5);
            driver.AdvanceTicks(cooldownState, shieldBuffState, 7);
            driver.AdvanceTicks(cooldownState, shieldBuffState, 3);

            Assert.That(cooldownState.GetRemainingTicks("ember_bolt"), Is.EqualTo(15));
            Assert.That(shieldBuffState.GetRemainingTicks("ember_ward"), Is.EqualTo(15));
            Assert.That(shieldBuffState.GetMagnitude("ember_ward"), Is.EqualTo(4));
        }

        [Test]
        public void AdvanceTicks_MatchesUnderlyingServicesIndependently()
        {
            var driverCooldown = new SpellCooldownState();
            driverCooldown.SetRemainingTicks("ember_bolt", 20);
            var driverShield = new ShieldBuffState();
            driverShield.SetActiveBuff("ember_ward", remainingTicks: 25, magnitude: 4);

            var soloCooldown = new SpellCooldownState();
            soloCooldown.SetRemainingTicks("ember_bolt", 20);
            var soloShield = new ShieldBuffState();
            soloShield.SetActiveBuff("ember_ward", remainingTicks: 25, magnitude: 4);

            NewDriver().AdvanceTicks(driverCooldown, driverShield, 8);
            new SpellCooldownService().AdvanceTicks(soloCooldown, 8);
            new ShieldBuffService().AdvanceTicks(soloShield, 8);

            Assert.That(driverCooldown.GetRemainingTicks("ember_bolt"),
                Is.EqualTo(soloCooldown.GetRemainingTicks("ember_bolt")));
            Assert.That(driverShield.GetRemainingTicks("ember_ward"),
                Is.EqualTo(soloShield.GetRemainingTicks("ember_ward")));
            Assert.That(driverShield.GetMagnitude("ember_ward"),
                Is.EqualTo(soloShield.GetMagnitude("ember_ward")));
        }
    }
}
