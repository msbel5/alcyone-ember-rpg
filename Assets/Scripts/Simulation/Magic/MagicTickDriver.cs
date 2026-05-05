using System;
using EmberCrpg.Domain.Magic;

// Design note:
// MagicTickDriver is the deterministic outer-tick coordinator for Sprint 5 magic.
// Inputs: an existing SpellCooldownService + ShieldBuffService pair, the two state
// containers they own, and elapsed simulation ticks. Outputs: a single pure-Simulation
// entry point that advances both the cooldown bag and the timed shield-buff bag in one
// call, so the wider simulation tick loop does not have to know about both seams or
// the order they fire in. This slice is orchestration-only: it does not introduce new
// decay rules, does not change the application paths, and does not call into combat.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic driver that advances cooldown and shield-buff state per tick.</summary>
    public sealed class MagicTickDriver
    {
        private readonly SpellCooldownService _spellCooldownService;
        private readonly ShieldBuffService _shieldBuffService;

        public MagicTickDriver(SpellCooldownService spellCooldownService, ShieldBuffService shieldBuffService)
        {
            _spellCooldownService = spellCooldownService ?? throw new ArgumentNullException(nameof(spellCooldownService));
            _shieldBuffService = shieldBuffService ?? throw new ArgumentNullException(nameof(shieldBuffService));
        }

        public void AdvanceTicks(SpellCooldownState cooldownState, ShieldBuffState shieldBuffState, int elapsedTicks)
        {
            if (cooldownState == null)
                throw new ArgumentNullException(nameof(cooldownState));
            if (shieldBuffState == null)
                throw new ArgumentNullException(nameof(shieldBuffState));
            if (elapsedTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedTicks), elapsedTicks, "Elapsed ticks must be zero or positive.");
            if (elapsedTicks == 0)
                return;

            _spellCooldownService.AdvanceTicks(cooldownState, elapsedTicks);
            _shieldBuffService.AdvanceTicks(shieldBuffState, elapsedTicks);
        }
    }
}
