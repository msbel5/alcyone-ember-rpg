// Why this file is intentionally long: combat command routing remains in one partial until command handlers are extracted.
// EMB-010: DomainSimulationAdapter combat / IPlayerCommandSink (partial-class split).
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
        // ----- IPlayerCommandSink -----
        public void LogCombat(string message) => _lastCombatLine = message ?? string.Empty;

        public void TakePlayerDamage(int amount)
        {
            if (amount <= 0) return;
            // Codex audit (fourth pass A-P2): previously held a transient
            // _playerDamageTaken counter that the HUD subtracted from. Now
            // we mutate the real player ActorRecord vitals so save/load
            // preserves the damage and other systems see the new HP.
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null) return;
            player.ApplyVitals(player.Vitals.WithHealth(player.Vitals.Health.Damage(amount)));
            _lastCombatLine = $"You take {amount} damage!";
        }

    }
}
