using System;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 cooldown seam: start on success, tick down over time, and expire
// cleanly without Unity dependencies.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic spell cooldown state and tick-down behavior.</summary>
    public sealed class SpellCooldownServiceTests
    {
        [Test]
        public void StartCooldown_CooldownSpell_PersistsDeclaredTicks()
        {
            var spell = CreateSpell(cooldownTicks: 6);
            var cooldownState = new SpellCooldownState();
            var service = new SpellCooldownService();

            service.StartCooldown(spell, cooldownState);

            Assert.That(service.IsOnCooldown(spell, cooldownState), Is.True);
            Assert.That(service.GetRemainingTicks(spell, cooldownState), Is.EqualTo(6));
        }

        [Test]
        public void StartCooldown_ZeroCooldownSpell_LeavesStateReady()
        {
            var spell = CreateSpell(cooldownTicks: 0);
            var cooldownState = new SpellCooldownState();
            var service = new SpellCooldownService();

            service.StartCooldown(spell, cooldownState);

            Assert.That(service.IsOnCooldown(spell, cooldownState), Is.False);
            Assert.That(service.GetRemainingTicks(spell, cooldownState), Is.EqualTo(0));
        }

        [Test]
        public void AdvanceTicks_ReducesRemainingTicksAndExpiresAtZero()
        {
            var spell = CreateSpell(cooldownTicks: 6);
            var cooldownState = new SpellCooldownState();
            var service = new SpellCooldownService();
            service.StartCooldown(spell, cooldownState);

            service.AdvanceTicks(cooldownState, 2);
            Assert.That(service.GetRemainingTicks(spell, cooldownState), Is.EqualTo(4));

            service.AdvanceTicks(cooldownState, 10);
            Assert.That(service.GetRemainingTicks(spell, cooldownState), Is.EqualTo(0));
            Assert.That(service.IsOnCooldown(spell, cooldownState), Is.False);
        }

        [Test]
        public void AdvanceTicks_NegativeElapsed_Throws()
        {
            var service = new SpellCooldownService();
            var cooldownState = new SpellCooldownState();

            Assert.Throws<ArgumentOutOfRangeException>(() => service.AdvanceTicks(cooldownState, -1));
        }

        [Test]
        public void State_SetRemainingTicks_ZeroRemovesTrackedSpell()
        {
            var cooldownState = new SpellCooldownState();
            cooldownState.SetRemainingTicks("cooldown_spell", 3);
            cooldownState.SetRemainingTicks("cooldown_spell", 0);

            Assert.That(cooldownState.GetRemainingTicks("cooldown_spell"), Is.EqualTo(0));
            Assert.That(cooldownState.GetTrackedSpellTemplateIds(), Is.Empty);
        }

        private static SpellDefinition CreateSpell(int cooldownTicks)
        {
            return new SpellDefinition(
                "cooldown_spell",
                "Cooldown Spell",
                MagicSchool.Destruction,
                SpellTargetKind.SingleTarget,
                manaCost: 5,
                rangeInTiles: 8,
                cooldownTicks: cooldownTicks,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 4, 0) });
        }
    }
}
