using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        public bool TryCastSpell(int spellSlotIndex)
        {
            var knownIds = _world.PlayerKnownSpellIds != null && _world.PlayerKnownSpellIds.Count > 0
                ? new List<string>(_world.PlayerKnownSpellIds)
                : new List<string>(EmberCrpg.Simulation.Magic.WorldSpellCatalog.All.Select(s => s.TemplateId));
            if (spellSlotIndex < 0 || spellSlotIndex >= knownIds.Count)
            {
                LogCombat("No such spell slot.");
                return false;
            }

            var spell = EmberCrpg.Simulation.Magic.WorldSpellCatalog.Find(knownIds[spellSlotIndex]);
            if (spell == null)
            {
                LogCombat("Unknown spell slot.");
                return false;
            }

            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null)
            {
                LogCombat("No caster.");
                return false;
            }
            // Mana gate: pure read; if insufficient mana, refusal.
            if (player.Vitals.Mana.Current < spell.ManaCost)
            {
                LogCombat($"{spell.DisplayName ?? spell.TemplateId}: insufficient mana.");
                return false;
            }
            // Codex audit (seventh pass A-P1 #2): the previous pass routed
            // only TryPrepareCast + CommitPreparedCast, so mana/cooldown
            // updated but the spell's actual effects (damage, heal, buff)
            // never landed on a target. Switch to SpellExecutionService,
            // which composes Cast → Target → Effect → CastRoll, so the live
            // command performs real domain mutation. Target picker selects
            // the closest hostile actor (or the caster for self-buffs); if
            // no hostile target exists, fall back to the caster so single-
            // target effects still resolve.
            var requestedTarget = SelectSpellTarget(spell, player);
            if (requestedTarget == null)
            {
                // Hostile spell with no enemy in range — refuse honestly instead of torching the caster.
                LogCombat($"{spell.DisplayName ?? spell.TemplateId}: no target in range.");
                return false;
            }

            var executionService = new EmberCrpg.Simulation.Magic.SpellExecutionService(
                new EmberCrpg.Simulation.Magic.SpellCastingService(_ => spell),
                new EmberCrpg.Simulation.Magic.SpellTargetValidator(),
                new EmberCrpg.Simulation.Magic.SpellEffectResolutionService(),
                new EmberCrpg.Simulation.Magic.SpellCastRollService());
            var executed = executionService.TryExecute(
                player, spell.TemplateId, knownIds, requestedTarget, _world.PlayerSpellCooldowns);
            if (!executed.Success)
            {
                LogCombat(executed.Message ?? $"{spell.DisplayName ?? spell.TemplateId}: failed.");
                return false;
            }

            _world.Events?.Append(new WorldEvent(
                _world.Time,
                WorldEventKind.SpellResolved,
                player.Id,
                ResolveCombatSiteId(player, requestedTarget),
                $"slice_spell_cast id:{spell.TemplateId} mana:{executed.ManaSpent}"));
            LogCombat(executed.Message);
            // F10/F13 hit feel: a landed hostile spell flashes the target billboard like a melee hit.
            if (!requestedTarget.Id.Equals(player.Id))
                EmberCrpg.Presentation.Ember.WorldDirector.WorldCombatFeedbackFeed.RaiseHit(requestedTarget.Id.Value);
            return true;
        }

    }
}
