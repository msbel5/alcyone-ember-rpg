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
            // ARCH-12: replace the restored state via an explicit, reflection-free CopyFrom on the
            // world itself (see WorldState.CopyFrom). The previous reflection field-walk silently
            // followed any field type/visibility change in this determinism-critical load path and hid
            // ref-aliasing bugs; CopyFrom is type-checked and a reflection coverage test guards that it
            // mirrors every public field.
            var restored = _saveService.LoadFromJson(json);
            if (restored == null) return;

            // PLAYTEST FIX ("olunce load edince M calismiyor"): saves do not persist the
            // overland map, and the loader maps data onto a FRESH factory world — CopyFrom
            // then clobbered the live session's overland with null, killing the world map
            // after every death->load. The session's overland survives the restore.
            var liveOverland = _world.Overland;
            _world.CopyFrom(restored);
            if (_world.Overland == null && liveOverland != null)
                _world.Overland = liveOverland;

            // EMB-013: the reflection copy above mirrors fields verbatim, so a corrupt/partial save
            // could leave a store or list null and crash the next tick. Re-establish the non-null
            // collection/store invariants explicitly before anything reads the restored world.
            _world.EnsureInvariants();

            // DET-01: re-derive the composer's hourly/daily tick accumulators from the restored
            // world time (the single source of truth) so save/load is replay-equivalent on a COLD
            // load too — not just a same-session reload. ResetAnchor() alone preserved the in-memory
            // accumulators, which are 0 after a fresh process start, desyncing the composer's hourly
            // cadence (job assignment, schedule stepping, needs decay) and daily cadence (caravan
            // motion, plant growth, price drift) vs a continuous run. RebuildAccumulatorsFrom subsumes the anchor reset
            // (_lastTickIndex = -1) and re-aligns the cadence to absolute game time.
            _tickComposer.RebuildAccumulatorsFrom(_world.Time);
        }
    }
}
