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

            // B02 ('cold Continue loses the world'): saves never persist the overland map -
            // it is a pure seed derivative. On a FRESH process liveOverland is null too, so
            // rebuild through the EXACT SeedWorld pipeline. MUST be the planet path +
            // Generate(GeneratedWorld, ...) overload: the uint overload takes the flat
            // worldgen route and yields a DIFFERENT map for the same seed (B28 interlock).
            if (_world.Overland == null && _world.WorldProfile != null)
            {
                var profile = _world.WorldProfile;
                var parameters = EmberCrpg.Simulation.Worldgen.WorldgenParameters.For(profile.Style, profile.Genre);
                var generated = EmberCrpg.Presentation.Ember.Worldgen.PlanetWorldService.GetOrGenerate(profile.Seed, parameters);
                GeneratedWorld = generated;
                StartingRegion = SelectStartingRegion(generated, profile.StartLocationKeyword);
                StartingSettlement = SelectStartingSettlement(generated,
                    ParsePreferredSettlementSize(profile.StartLocationKeyword), profile.StartLocationKeyword);
                StartingFaction = SelectStartingFaction(generated, profile.PlayerCallingKeyword);
                _billboardOriginResolved = false;
                var geo = generated.Geography;
                _world.Overland = EmberCrpg.Simulation.Overland.OverlandWorldgen.Generate(
                    generated,
                    new EmberCrpg.Domain.Overland.OverlandParameters(geo.Width, geo.Height));
                UnityEngine.Debug.Log("[Load] B02 overland rebuilt from profile seed=" + profile.Seed
                    + " settlements=" + (_world.Overland?.Settlements.Count ?? 0));
            }

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
