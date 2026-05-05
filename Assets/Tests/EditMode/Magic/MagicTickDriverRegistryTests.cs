using System;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 magic outer-tick driver registry overload:
// MagicTickDriver.AdvanceTicks(SpellCooldownState, ShieldBuffStateRegistry, int) decays the
// shared cooldown bag through SpellCooldownService.AdvanceTicks and forwards the registry
// through ShieldBuffService.AdvanceTicksForAllActors so every actor's per-actor shield-buff
// bag decays in the same call. Pure orchestration: tests cover argument guards, no-op
// paths, multi-actor independent decay, parity vs the existing single-bag overload run
// per actor, and per-side parity vs each underlying service.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies registry-aware combined cooldown + per-actor shield-buff tick advancement.</summary>
    public sealed class MagicTickDriverRegistryTests
    {
        private static MagicTickDriver NewDriver()
        {
            return new MagicTickDriver(new SpellCooldownService(), new ShieldBuffService());
        }

        [Test]
        public void AdvanceTicks_NullCooldownState_Throws()
        {
            var driver = NewDriver();

            Assert.Throws<ArgumentNullException>(
                () => driver.AdvanceTicks(null, new ShieldBuffStateRegistry(), 5));
        }

        [Test]
        public void AdvanceTicks_NullRegistry_Throws()
        {
            var driver = NewDriver();

            Assert.Throws<ArgumentNullException>(
                () => driver.AdvanceTicks(new SpellCooldownState(), (ShieldBuffStateRegistry)null, 5));
        }

        [Test]
        public void AdvanceTicks_NegativeElapsed_Throws()
        {
            var driver = NewDriver();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => driver.AdvanceTicks(new SpellCooldownState(), new ShieldBuffStateRegistry(), -1));
        }

        [Test]
        public void AdvanceTicks_ZeroElapsed_IsNoOp()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("ember_bolt", 12);
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            NewDriver().AdvanceTicks(cooldownState, registry, 0);

            Assert.That(cooldownState.GetRemainingTicks("ember_bolt"), Is.EqualTo(12));
            Assert.That(heroBag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(heroBag.GetMagnitude("ember_ward"), Is.EqualTo(4));
        }

        [Test]
        public void AdvanceTicks_EmptyRegistry_StillAdvancesCooldown()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("ember_bolt", 20);
            var registry = new ShieldBuffStateRegistry();

            NewDriver().AdvanceTicks(cooldownState, registry, 7);

            Assert.That(cooldownState.GetRemainingTicks("ember_bolt"), Is.EqualTo(13));
            Assert.That(registry.GetTrackedActorIds(), Is.Empty);
        }

        [Test]
        public void AdvanceTicks_DecaysCooldownAndEveryActorBagByExactlyElapsedTicks()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("ember_bolt", 20);
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var allyBag = registry.GetOrCreate("ally_b");
            allyBag.SetActiveBuff("ember_ward", remainingTicks: 25, magnitude: 6);
            allyBag.SetActiveBuff("flame_ward", remainingTicks: 18, magnitude: 2);

            NewDriver().AdvanceTicks(cooldownState, registry, 7);

            Assert.That(cooldownState.GetRemainingTicks("ember_bolt"), Is.EqualTo(13));
            Assert.That(heroBag.GetRemainingTicks("ember_ward"), Is.EqualTo(23));
            Assert.That(heroBag.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(allyBag.GetRemainingTicks("ember_ward"), Is.EqualTo(18));
            Assert.That(allyBag.GetMagnitude("ember_ward"), Is.EqualTo(6));
            Assert.That(allyBag.GetRemainingTicks("flame_ward"), Is.EqualTo(11));
            Assert.That(allyBag.GetMagnitude("flame_ward"), Is.EqualTo(2));
        }

        [Test]
        public void AdvanceTicks_ExpiresPerActorEntriesIndependently()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("ember_bolt", 20);
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 6, magnitude: 4);
            var allyBag = registry.GetOrCreate("ally_b");
            allyBag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 6);

            NewDriver().AdvanceTicks(cooldownState, registry, 6);

            Assert.That(cooldownState.GetRemainingTicks("ember_bolt"), Is.EqualTo(14));
            Assert.That(heroBag.IsActive("ember_ward"), Is.False);
            Assert.That(heroBag.GetTrackedSpellTemplateIds(), Is.Empty);
            Assert.That(allyBag.GetRemainingTicks("ember_ward"), Is.EqualTo(24));
            Assert.That(allyBag.GetMagnitude("ember_ward"), Is.EqualTo(6));
        }

        [Test]
        public void AdvanceTicks_RepeatedCallsAccumulateDecayAcrossCooldownAndEveryActorBag()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("ember_bolt", 30);
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var allyBag = registry.GetOrCreate("ally_b");
            allyBag.SetActiveBuff("flame_ward", remainingTicks: 30, magnitude: 2);
            var driver = NewDriver();

            driver.AdvanceTicks(cooldownState, registry, 5);
            driver.AdvanceTicks(cooldownState, registry, 7);
            driver.AdvanceTicks(cooldownState, registry, 3);

            Assert.That(cooldownState.GetRemainingTicks("ember_bolt"), Is.EqualTo(15));
            Assert.That(heroBag.GetRemainingTicks("ember_ward"), Is.EqualTo(15));
            Assert.That(heroBag.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(allyBag.GetRemainingTicks("flame_ward"), Is.EqualTo(15));
            Assert.That(allyBag.GetMagnitude("flame_ward"), Is.EqualTo(2));
        }

        [Test]
        public void AdvanceTicks_MatchesSingleBagOverloadAppliedPerActor()
        {
            var driverCooldown = new SpellCooldownState();
            driverCooldown.SetRemainingTicks("ember_bolt", 20);
            var driverRegistry = new ShieldBuffStateRegistry();
            var driverHero = driverRegistry.GetOrCreate("hero_a");
            driverHero.SetActiveBuff("ember_ward", remainingTicks: 25, magnitude: 4);
            var driverAlly = driverRegistry.GetOrCreate("ally_b");
            driverAlly.SetActiveBuff("flame_ward", remainingTicks: 18, magnitude: 6);

            var soloCooldown = new SpellCooldownState();
            soloCooldown.SetRemainingTicks("ember_bolt", 20);
            var soloHero = new ShieldBuffState();
            soloHero.SetActiveBuff("ember_ward", remainingTicks: 25, magnitude: 4);
            var soloAlly = new ShieldBuffState();
            soloAlly.SetActiveBuff("flame_ward", remainingTicks: 18, magnitude: 6);

            NewDriver().AdvanceTicks(driverCooldown, driverRegistry, 8);
            NewDriver().AdvanceTicks(soloCooldown, soloHero, 8);
            new ShieldBuffService().AdvanceTicks(soloAlly, 8);

            Assert.That(driverCooldown.GetRemainingTicks("ember_bolt"),
                Is.EqualTo(soloCooldown.GetRemainingTicks("ember_bolt")));
            Assert.That(driverHero.GetRemainingTicks("ember_ward"),
                Is.EqualTo(soloHero.GetRemainingTicks("ember_ward")));
            Assert.That(driverHero.GetMagnitude("ember_ward"),
                Is.EqualTo(soloHero.GetMagnitude("ember_ward")));
            Assert.That(driverAlly.GetRemainingTicks("flame_ward"),
                Is.EqualTo(soloAlly.GetRemainingTicks("flame_ward")));
            Assert.That(driverAlly.GetMagnitude("flame_ward"),
                Is.EqualTo(soloAlly.GetMagnitude("flame_ward")));
        }

        [Test]
        public void AdvanceTicks_PerSideParity_MatchesUnderlyingServicesIndependently()
        {
            var driverCooldown = new SpellCooldownState();
            driverCooldown.SetRemainingTicks("ember_bolt", 20);
            var driverRegistry = new ShieldBuffStateRegistry();
            var driverHero = driverRegistry.GetOrCreate("hero_a");
            driverHero.SetActiveBuff("ember_ward", remainingTicks: 25, magnitude: 4);

            var soloCooldown = new SpellCooldownState();
            soloCooldown.SetRemainingTicks("ember_bolt", 20);
            var soloRegistry = new ShieldBuffStateRegistry();
            var soloHero = soloRegistry.GetOrCreate("hero_a");
            soloHero.SetActiveBuff("ember_ward", remainingTicks: 25, magnitude: 4);

            NewDriver().AdvanceTicks(driverCooldown, driverRegistry, 8);
            new SpellCooldownService().AdvanceTicks(soloCooldown, 8);
            new ShieldBuffService().AdvanceTicksForAllActors(soloRegistry, 8);

            Assert.That(driverCooldown.GetRemainingTicks("ember_bolt"),
                Is.EqualTo(soloCooldown.GetRemainingTicks("ember_bolt")));
            Assert.That(driverHero.GetRemainingTicks("ember_ward"),
                Is.EqualTo(soloHero.GetRemainingTicks("ember_ward")));
            Assert.That(driverHero.GetMagnitude("ember_ward"),
                Is.EqualTo(soloHero.GetMagnitude("ember_ward")));
        }
    }
}
