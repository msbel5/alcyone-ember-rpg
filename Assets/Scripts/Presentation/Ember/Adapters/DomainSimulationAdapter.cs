// Why this file is intentionally long: the aggregate adapter is being split in stages; this root partial keeps shared state only.
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
using WorldEventInterest = EmberCrpg.Presentation.Visual.WorldEventInterest;
// Alias only the two Visual types F1 needs — a broad `using EmberCrpg.Presentation.Visual;` collides with
// Presentation.Ember.UI.ColonyNeedsRow (same simple name in both namespaces).
using WorldEventRow = EmberCrpg.Presentation.Visual.WorldEventRow;
using WorldEventTailSnapshot = EmberCrpg.Presentation.Visual.WorldEventTailSnapshot;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// Aggregate adapter with AI integration for native inference (Phase 2).
    /// </summary>
    public sealed partial class DomainSimulationAdapter : IDomainSimulationAdapter, IDialogSourcePortrait
    {
        private readonly WorldState _world;
        private readonly EmberCrpg.Presentation.Ember.Save.JsonSliceSaveService _saveService;
        private readonly EmberCrpg.Simulation.Composition.WorldTickComposer _tickComposer;
        private int _tick;
        private string _lastCombatLine = string.Empty;
        private string _activeDialogActor = string.Empty;
        private ActorId _activeDialogActorId;
        private NpcId _activeDialogNpcId;
        private string _currentDialogLine = string.Empty;
        private string _currentPortrait = "portrait_npc_placeholder";
        // EMB-020/045: the one per-actor conversation model (current speaker + their role/faction topics).
        private ConversationState _conversation = ConversationState.None;
        private string _pendingFate = string.Empty;
        private bool _isFateThinking;
        private bool _isDialogThinking;
        /// <summary>PLAYTEST FIX ("3 kere sordum, 3unde ayni cumle"): per-conversation ask
        /// counter folded into the seed + prompt so repeats rephrase and add a new detail.</summary>
        private readonly System.Collections.Generic.Dictionary<string, int> _topicAskCounts
            = new System.Collections.Generic.Dictionary<string, int>();
        /// <summary>M3a: the growing streamed answer, shown by GetCurrentLine while thinking.</summary>
        private string _streamingPartialLine;
        // DLG-01: set true when an id-keyed GetDialogSource lookup misses, so the
        // read methods surface an explicit "no one here" state instead of silently
        // dropping the player into the shared global _world.Topics menu. Reset on
        // every successful bind (both the id and the name overloads).
        private bool _suppressGlobalTopicFallback;
        private const ulong RegionSiteOffset = 100_000UL;
        private const ulong SettlementSiteOffset = 200_000UL;
        private const ulong GeneratedNpcActorOffset = 10_000UL;

        public DomainSimulationAdapter(WorldState world)
        {
            _world = world ?? throw new System.ArgumentNullException(nameof(world));
            _saveService = new EmberCrpg.Presentation.Ember.Save.JsonSliceSaveService(
                EmberCrpg.Data.Recipes.ProductionRecipeRegistry.Resolve);
            _tickComposer = new EmberCrpg.Simulation.Composition.WorldTickComposer();

            // SOUL-01: bind the save bridge to the live world so _saveService.Worksites/Jobs/Soils/Plants
            // resolve to the same store instances the WorldTickComposer advances each tick. Without this
            // the seeded worksites/jobs would sit on a detached bridge world and never tick.
            _saveService.BindWorld(_world);

            if (_saveService.Worksites != null && _world.Sites != null)
            {
                foreach (var site in _world.Sites.Records)
                {
                    bool exists = false;
                    foreach (var record in _saveService.Worksites.Records)
                    {
                        var position = CenterOf(site);
                        if (record.SiteId.Equals(site.Id) && record.Position.Equals(position))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists) continue;
                    _saveService.Worksites.Add(new EmberCrpg.Domain.Process.WorksiteRecord(
                        site.Id, CenterOf(site), WorksiteKindFor(site.Name), isActive: true));
                }
            }
        }

        public WorldState World => _world;



    }
}
