// EMB-010: DomainSimulationAdapter IEmberSaveBridge (partial-class split).
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
        // ----- IEmberSaveBridge -----
        public string ExportStateJson()
        {
            // Codex review PR #195 (P2): rethrow so EmberSaveService can show
            // "Save partial: domain export failed." instead of swallowing.
            return _saveService.SaveToJson(_world);
        }

        public void RestoreStateJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            // Codex audit (sixth pass A-P2 #14): the previous field-by-field
            // copy silently dropped any new field added to SliceWorldState.
            // Mirror EVERY public instance field via reflection so the copy
            // stays in lockstep with the type. Properties (the obsolete
            // role accessors) are intentionally skipped — they delegate to
            // the actor store which is itself a field.
            var restored = _saveService.LoadFromJson(json);
            if (restored == null) return;

            var type = typeof(SliceWorldState);
            foreach (var field in type.GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                var value = field.GetValue(restored);
                field.SetValue(_world, value);
            }

            // EMB-013: the reflection copy above mirrors fields verbatim, so a corrupt/partial save
            // could leave a store or list null and crash the next tick. Re-establish the non-null
            // collection/store invariants explicitly before anything reads the restored world.
            _world.EnsureInvariants();

            // DET-01: re-derive the composer's hourly/daily tick accumulators from the restored
            // world time (the single source of truth) so save/load is replay-equivalent on a COLD
            // load too — not just a same-session reload. ResetAnchor() alone preserved the in-memory
            // accumulators, which are 0 after a fresh process start, desyncing the needs/caravan
            // cadence vs a continuous run. RebuildAccumulatorsFrom subsumes the anchor reset
            // (_lastTickIndex = -1) and re-aligns the cadence to absolute game time.
            _tickComposer.RebuildAccumulatorsFrom(_world.Time);
        }
    }
}
