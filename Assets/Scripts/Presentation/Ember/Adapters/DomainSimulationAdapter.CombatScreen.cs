using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Magic;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter : ICombatScreenSource
    {
        public CombatScreenState ReadCombatScreenState()
        {
            var player = _world.Actors?.FirstByRole(ActorRole.Player);
            // F2/encounters: a bound WORLD opponent (E on an outlaw) takes precedence over the authored
            // slice's room-based Enemy actor; victory settles spoils and unbinds.
            var worldEnemy = WorldEncounterEnemy();
            SettleWorldEncounterIfOver(worldEnemy);
            // F8/music: the BATTLE slot follows the live world encounter.
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeBattleMirror.Active =
                worldEnemy != null && worldEnemy.IsAlive;
            // F30: the BOSS fight carries extra weight — the music director lays a percussion
            // loop over the BATTLE slot while the bound enemy is the delve's Warden.
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeBattleMirror.BossActive =
                worldEnemy != null && worldEnemy.IsAlive && worldEnemy.Name != null
                && worldEnemy.Name.StartsWith("Warden of", System.StringComparison.Ordinal);
            var enemy = worldEnemy ?? _world.Actors?.FirstByRole(ActorRole.Enemy);
            bool hasEncounter = player != null
                && enemy != null
                && enemy.IsAlive
                && (worldEnemy != null || _world.CurrentRoomId == _world.EnemyRoomId);

            var spells = new List<CombatSpellActionRow>();
            if (hasEncounter)
            {
                var knownIds = _world.PlayerKnownSpellIds ?? new List<string>();
                for (int i = 0; i < knownIds.Count; i++)
                {
                    var spell = WorldSpellCatalog.Find(knownIds[i]);
                    if (spell == null)
                        continue;

                    bool enabled = player.Vitals.Mana.Current >= spell.ManaCost;
                    spells.Add(new CombatSpellActionRow(
                        "cast:" + i,
                        spell.DisplayName ?? spell.TemplateId,
                        spell.School.ToString(),
                        spell.ManaCost,
                        enabled));
                }
            }

            return new CombatScreenState(
                hasEncounter,
                player?.Name ?? "Unknown",
                player?.Vitals.Health.Current ?? 0,
                player?.Vitals.Health.Max ?? 0,
                player?.Vitals.Fatigue.Current ?? 0,
                player?.Vitals.Fatigue.Max ?? 0,
                player?.Vitals.Mana.Current ?? 0,
                player?.Vitals.Mana.Max ?? 0,
                enemy?.Name ?? string.Empty,
                hasEncounter ? enemy.Vitals.Health.Current : 0,
                hasEncounter ? enemy.Vitals.Health.Max : 0,
                _lastCombatLine,
                spells);
        }
    }
}
