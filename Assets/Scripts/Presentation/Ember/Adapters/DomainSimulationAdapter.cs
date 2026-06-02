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

        // ----- IEmberSimulationClock -----
        public void AdvanceTick(int tickIndex)
        {
            DrainMainThreadApply(); // DET-02: apply queued off-thread LLM results on the main thread
            _tick = tickIndex;
            _tickComposer.Advance(_world, tickIndex);
        }

        // DET-02: post-await LLM continuations enqueue their _world / dialog-state writes here instead
        // of mutating shared state on whatever thread the await resumes on. Relying on Unity's
        // SynchronizationContext to marshal them back to the main thread is implicit and null in a
        // headless run, which would reopen the EMB-007 race on _world. Draining here, at the top of the
        // deterministic main-thread tick, guarantees those writes land on the main thread in order.
        private readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> _mainThreadApply
            = new System.Collections.Concurrent.ConcurrentQueue<System.Action>();

        private void DrainMainThreadApply()
        {
            while (_mainThreadApply.TryDequeue(out var apply))
            {
                try { apply(); }
                catch (System.Exception) { /* a queued apply must never break the tick */ }
            }
        }
        public int TickIndex => _tick;

        // ----- IEmberHudReadModel -----
        public string HudText
        {
            get
            {
                var day = 1 + _tick / 240;
                var profile = _world.WorldProfile;
                // Anchor the player in the real generated world: name the settlement they started in.
                var town = ResolveStartingSettlementName();
                var where = string.IsNullOrEmpty(town) ? string.Empty : $"   •   {town}";
                if (profile == null)
                    return $"Tick {_tick:0000}   Day {day:000}{where}";
                // Population is the world the HISTORY simulated (sum of surviving settlement populations),
                // not the static TargetPopulation knob, so the number reflects centuries of growth/decline.
                var population = GeneratedWorld != null ? GeneratedWorld.TotalPopulation : profile.TargetPopulation;
                return $"Tick {_tick:0000}   Day {day:000}   {Spaced(profile.Style)} / {Spaced(profile.Genre)}   Pop {population:N0}{where}";
            }
        }

        // Render a CamelCase enum value as spaced Title Case for the HUD ("LowFantasy" -> "Low Fantasy").
        // The brand codenames were renamed out of the WorldStyle enum (BUG-1), so this is pure display polish.
        private static string Spaced(System.Enum value)
        {
            var s = value.ToString();
            return System.Text.RegularExpressions.Regex.Replace(s, "(?<=[a-z0-9])(?=[A-Z])", " ");
        }

        public CombatHudState CombatHud
        {
            get
            {
                var player = _world.Actors.FirstByRole(ActorRole.Player);
                if (player == null) return new CombatHudState(0, 100, 0, 100, 0, 100, _lastCombatLine);
                var v = player.Vitals;
                return new CombatHudState(
                    v.Health.Current, v.Health.Max,
                    v.Fatigue.Current, v.Fatigue.Max,
                    v.Mana.Current, v.Mana.Max,
                    _lastCombatLine);
            }
        }

        // ----- IWorldViewReadModel -----
        public IReadOnlyList<JobQueueRow> JobQueueRows
        {
            get
            {
                // Codex audit (fourth pass A-P1): previously returned empty.
                // Job sidecar state lives on the save service; expose any
                // tracked jobs. When no jobs are seeded the list stays empty
                // but the panel is no longer locked to fabricated zero rows.
                var jobs = _saveService.Jobs;
                if (jobs == null) return System.Array.Empty<JobQueueRow>();
                var rows = new List<JobQueueRow>();
                foreach (var req in jobs.Requests)
                {
                    var claim = jobs.GetClaimedBy(req.Id);
                    var actorName = claim.IsEmpty ? string.Empty : (_world.Actors.Get(claim)?.Name ?? string.Empty);
                    rows.Add(new JobQueueRow(actorName, req.Kind.ToString(), jobs.GetStatus(req.Id).Code, jobs.GetQueueIndex(req.Id)));
                }
                return rows;
            }
        }

        public IReadOnlyList<ColonyNeedsRow> ColonyNeedsRows
        {
            get
            {
                var rows = new List<ColonyNeedsRow>();
                foreach (var actor in _world.Actors.Records.OrderBy(a => a.Id.Value))
                {
                    rows.Add(new ColonyNeedsRow(
                        actor.Name ?? string.Empty,
                        actor.Needs.Hunger.Value,
                        actor.Needs.Fatigue.Value,
                        actor.Needs.Thirst.Value,
                        actor.Mood.Value));
                }
                return rows;
            }
        }

        public IReadOnlyList<FactionRow> FactionRows
        {
            get
            {
                // Codex audit (fourth pass A-P1): previously hardcoded Neutral.
                // Use the player's faction (first non-empty actor faction id)
                // as the reference vantage point, then list every other
                // faction with its reputation relative to that vantage.
                var rows = new List<FactionRow>();
                if (_world.Factions == null) return rows;

                // Codex audit (seventh pass A-P2 #7): previously emitted every
                // faction at reputation 0 / Neutral, hiding the real diplomacy
                // state held in FactionStore.ReputationRows. Use the FIRST
                // authored faction as the deterministic player vantage and
                // emit each OTHER faction with its real reputation relative
                // to the vantage; the vantage itself is reported at 0 so the
                // HUD still includes the player's home faction. When
                // ActorRecord.FactionId lands the vantage will switch to the
                // player's actual home faction.
                FactionId vantage = default;
                foreach (var faction in _world.Factions.Records)
                {
                    vantage = faction.Id;
                    break;
                }
                foreach (var faction in _world.Factions.Records)
                {
                    int reputation = 0;
                    if (!faction.Id.Equals(vantage))
                    {
                        reputation = _world.Factions.GetReputation(vantage, faction.Id).Value;
                    }
                    var label = FactionRelationKind.FromReputation(reputation).ToString();
                    rows.Add(new FactionRow(faction.Name ?? string.Empty, reputation, label));
                }
                return rows;
            }
        }

        public IReadOnlyList<InventorySlot> InventorySlots
        {
            get
            {
                if (_world.PlayerInventory == null) return System.Array.Empty<InventorySlot>();
                var rows = new List<InventorySlot>();
                foreach (var item in _world.PlayerInventory.Items)
                {
                    rows.Add(new InventorySlot(item.TemplateId ?? string.Empty, item.Quantity));
                }
                return rows;
            }
        }

        public IReadOnlyList<string> SpellSlots
        {
            get
            {
                // Codex audit (fourth pass A-P2): the cooldown tracker only
                // contains spells the actor has CAST (which seeds nothing on
                // a fresh world). The known-spell catalog is the right source.
                //
                // Codex review on PR #196 (P1): MUST preserve catalog index
                // order so slot N in the HUD matches slot N in TryCastSpell.
                // Previously this method sorted alphabetically (flame_bolt /
                // mending_touch / ember_ward becomes ember_ward / flame_bolt /
                // mending_touch), but TryCastSpell still resolved by raw
                // `WorldSpellCatalog.All[index]`, so pressing slot 0 would
                // cast a different spell than the one displayed.
                return EmberCrpg.Simulation.Magic.WorldSpellCatalog.All
                    .Select(s => s.TemplateId)
                    .ToList();
            }
        }

        // Why: reuse the shared snapshot projection so host/UI read the exact same deterministic tail rows.
        public IReadOnlyList<WorldEventRow> RecentWorldEvents(int maxRows)
        {
            return WorldEventTailSnapshot.FromLog(_world?.Events, maxRows, WorldEventInterest.IsHudWorthy).Rows;
        }

        public bool TryReadActor(string actorName, out ActorViewState state)
        {
            state = default;
            if (string.IsNullOrEmpty(actorName) || _world.Actors == null) return false;
            foreach (var actor in _world.Actors.Records)
            {
                if (string.Equals(actor.Name, actorName, System.StringComparison.Ordinal))
                {
                    state = ProjectActor(actor);
                    return true;
                }
            }
            return false;
        }

        // SOUL-04: id-keyed read so the host can sync a billboard from the actor's stable id and see
        // SOUL-03 (ScheduleSystem) grid movement without depending on name uniqueness.
        public bool TryReadActor(ActorId id, out ActorViewState state)
        {
            state = default;
            if (id.IsEmpty || _world.Actors == null) return false;
            if (!_world.Actors.TryGet(id, out var actor) || actor == null) return false;
            state = ProjectActor(actor);
            return true;
        }

        // Single projection so the name and id read paths can never drift: domain grid (X,Y) maps to
        // the world-space XZ plane (Y up stays 0); actors are always visible while alive in the store.
        private static ActorViewState ProjectActor(ActorRecord actor)
        {
            return new ActorViewState(
                new UnityEngine.Vector3(actor.Position.X, 0f, actor.Position.Y),
                UnityEngine.Quaternion.identity,
                visible: true);
        }

        // SOUL-04 (spawn-from-worldgen): hand the host a flat, Domain-free list of candidate
        // billboards. Reuse the SAME grid->world XZ projection as ProjectActor so a spawned view
        // lands exactly where the per-tick sync will then push it (no first-frame jump). The Player
        // actor is excluded — the player is the rig/camera, not a billboard. Deterministic order
        // (ActorStore.Records is insertion-ordered); the host caps/culls, so we never pre-truncate.
        public IReadOnlyList<SpawnableActor> GetSpawnableActors()
        {
            if (_world.Actors == null) return System.Array.Empty<SpawnableActor>();
            var list = new List<SpawnableActor>();
            foreach (var actor in _world.Actors.Records)
            {
                if (actor == null || actor.Role == ActorRole.Player) continue;
                list.Add(new SpawnableActor(
                    actor.Id.Value,
                    actor.Name ?? string.Empty,
                    actor.Position.X,
                    actor.Position.Y));
            }
            return list;
        }

        public bool TryReadWorksite(string siteName, out WorksiteViewState state)
        {
            state = default;
            // Codex audit (fifth pass A-P1): previously returned the
            // synthetic `(isActive: true, queueDepth: 0)` for any site
            // name match — the view never reflected the actual worksite
            // store. Now derive isActive from the WorksiteStore and
            // queueDepth from the JobBoard's request count at that site.
            if (string.IsNullOrEmpty(siteName)) return false;
            EmberCrpg.Domain.Core.SiteId siteId = default;
            foreach (var site in _world.Sites.Records)
            {
                if (string.Equals(site.Name, siteName, System.StringComparison.Ordinal))
                {
                    siteId = site.Id;
                    break;
                }
            }
            if (siteId.IsEmpty) return false;

            var worksites = _saveService.Worksites;
            bool isActive = false;
            if (worksites != null)
            {
                foreach (var record in worksites.Records)
                {
                    if (record.SiteId.Equals(siteId) && record.IsActive)
                    {
                        isActive = true;
                        break;
                    }
                }
            }

            int queueDepth = 0;
            var jobs = _saveService.Jobs;
            if (jobs != null)
            {
                foreach (var req in jobs.Requests)
                {
                    if (req.SiteId.Equals(siteId)) queueDepth++;
                }
            }

            state = new WorksiteViewState(isActive: isActive, queueDepth: queueDepth);
            return true;
        }


    }
}
