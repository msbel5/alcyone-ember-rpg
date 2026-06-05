using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.World;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter : ILevelUpSource, ILevelUpCommandSink
    {
        private readonly PlayerLevelUpService _levelUpService = new PlayerLevelUpService();

        LevelUpScreenState ILevelUpSource.ReadLevelUpState()
        {
            var player = _world.Actors?.FirstByRole(EmberCrpg.Domain.Actors.ActorRole.Player);
            if (player == null)
                return new LevelUpScreenState("Unknown", 1, PlayerLevelUpService.PointsPerLevel, System.Array.Empty<LevelUpStatRow>(), System.Array.Empty<LevelUpSpellRow>());

            var stats = new[]
            {
                new LevelUpStatRow("MIG", "MIG", player.Stats.Mig),
                new LevelUpStatRow("AGI", "AGI", player.Stats.Agi),
                new LevelUpStatRow("END", "END", player.Stats.End),
                new LevelUpStatRow("MND", "MND", player.Stats.Mnd),
                new LevelUpStatRow("INS", "INS", player.Stats.Ins),
                new LevelUpStatRow("PRE", "PRE", player.Stats.Pre),
            };

            var learnable = new List<LevelUpSpellRow>();
            foreach (var spell in WorldSpellCatalog.All)
            {
                if (_world.PlayerKnownSpellIds != null && _world.PlayerKnownSpellIds.Contains(spell.TemplateId))
                    continue;
                learnable.Add(new LevelUpSpellRow(
                    spell.TemplateId,
                    spell.DisplayName,
                    spell.School.ToString(),
                    spell.ManaCost,
                    DescribeSpell(spell)));
            }

            return new LevelUpScreenState(player.Name, _world.PlayerLevel, PlayerLevelUpService.PointsPerLevel, stats, learnable);
        }

        LevelUpActionResult ILevelUpCommandSink.ApplyLevelUp(LevelUpSelection selection)
        {
            var choice = new PlayerLevelUpChoice(
                selection.MigDelta,
                selection.AgiDelta,
                selection.EndDelta,
                selection.MndDelta,
                selection.InsDelta,
                selection.PreDelta,
                selection.SelectedSpellId);
            var success = _levelUpService.TryApply(_world, choice, out var message);
            return new LevelUpActionResult(success, message);
        }

        private static string DescribeSpell(EmberCrpg.Domain.Magic.SpellDefinition spell)
        {
            if (spell == null || spell.Effects == null || spell.Effects.Count == 0)
                return "Unknown effect";
            var effect = spell.Effects[0];
            return effect.Magnitude + " " + effect.Kind.Code.Replace('_', ' ');
        }
    }
}
