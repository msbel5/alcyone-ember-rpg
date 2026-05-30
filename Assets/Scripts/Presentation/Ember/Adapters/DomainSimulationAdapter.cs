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
        private string _currentDialogLine = string.Empty;
        private string _currentPortrait = "portrait_npc_placeholder";
        // EMB-020/045: the one per-actor conversation model (current speaker + their role/faction topics).
        private ConversationState _conversation = ConversationState.None;
        private string _pendingFate = string.Empty;
        private bool _isFateThinking;
        private bool _isDialogThinking;
        private const ulong RegionSiteOffset = 100_000UL;
        private const ulong SettlementSiteOffset = 200_000UL;
        private const ulong GeneratedNpcActorOffset = 10_000UL;

        public DomainSimulationAdapter(WorldState world)
        {
            _world = world ?? throw new System.ArgumentNullException(nameof(world));
            _saveService = new EmberCrpg.Presentation.Ember.Save.JsonSliceSaveService(
                EmberCrpg.Data.Recipes.ProductionRecipeRegistry.Resolve);
            _tickComposer = new EmberCrpg.Simulation.Composition.WorldTickComposer();

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
                if (profile == null)
                    return $"Tick {_tick:0000}   Day {day:000}";
                return $"Tick {_tick:0000}   Day {day:000}   {profile.Style}/{profile.Genre}   Pop {profile.TargetPopulation:N0}";
            }
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

        public bool TryReadActor(string actorName, out ActorViewState state)
        {
            state = default;
            if (string.IsNullOrEmpty(actorName)) return false;
            foreach (var actor in _world.Actors.Records)
            {
                if (string.Equals(actor.Name, actorName, System.StringComparison.Ordinal))
                {
                    state = new ActorViewState(
                        new UnityEngine.Vector3(actor.Position.X, 0f, actor.Position.Y),
                        UnityEngine.Quaternion.identity,
                        visible: true);
                    return true;
                }
            }
            return false;
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

        public IDialogSource GetDialogSource(string actorName)
        {
            _activeDialogActor = actorName ?? string.Empty;
            _currentDialogLine = "You approach " + _activeDialogActor + "...";
            _currentPortrait = "portrait_npc_placeholder";

            // If we have an NpcSeedRecord for this actor, use its name and role for a persona-driven greeting
            var npc = _world.NpcSeeds.FirstOrDefault(n => string.Equals(n.Name, _activeDialogActor, System.StringComparison.Ordinal));
            if (npc != null)
            {
                if (!string.IsNullOrEmpty(npc.PortraitAssetPath)) _currentPortrait = npc.PortraitAssetPath;

                // EMB-045: this NPC's Ask-About topics come from THEIR role + faction (+ a little shared
                // world lore), not the global _world.Topics menu. EMB-020: bind speaker + portrait +
                // topics into the one ConversationState the dialog surface reads.
                var perActorTopics = NpcTopicCatalog.For(npc.Role, npc.Faction.Value, _world.Topics);
                _conversation = new ConversationState(_activeDialogActor, _currentPortrait, perActorTopics);

                // Fire async greeting
                _ = GenerateNpcGreetingAsync(npc);
            }
            else
            {
                // No seed record (ad-hoc actor): fall back to the shared world topics, still funneled
                // through the one ConversationState model so GetTopics/SelectTopic have a single source.
                _conversation = new ConversationState(
                    _activeDialogActor, _currentPortrait, _world.Topics ?? new List<AskAboutTopic>());
            }

            return this;
        }

        private async Task GenerateNpcGreetingAsync(NpcSeedRecord npc)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null) return;

            _isDialogThinking = true;
            var request = new LlmRequest(
                "npc_greeting",
                "npc:" + npc.Id.Value,
                null,
                100,
                npc.Id.Value,
                $"You are {npc.Name}, a {npc.Role} in a {_world.WorldProfile?.Style} world. Greet the player character briefly in character.",
                new List<string>()
            );

            // EMB-007: only the blocking LLM call runs off the main thread. The shared-state
            // mutations are applied AFTER the await, which resumes on Unity's main-thread
            // SynchronizationContext — previously _currentDialogLine / _isDialogThinking were
            // written from the worker thread, racing the main-thread dialog reader.
            var response = await Task.Run(() => router.Complete(request, out _));
            // DET-02: apply the result on the main-thread tick, not on the await's resumption thread.
            _mainThreadApply.Enqueue(() =>
            {
                if (response != null && !string.IsNullOrEmpty(response.Text))
                    _currentDialogLine = response.Text.Trim();
                _isDialogThinking = false;
            });
        }

        // Ninth-pass FOUNDATION worldgen: SeedWorld now runs the deterministic
// WorldgenService (Assets/Scripts/Simulation/Worldgen/) so the
        // mood/calling/start tuple from the main-menu wizard actually
        // produces a ~50-region, ~200-settlement, ~750-NPC world instead
        // of vanishing into a log line. The generated bundle is held on
        // the adapter so subsequent reads (UI panels, save/load) can
        // inspect it through the IDomainSimulationAdapter handle.
        public EmberCrpg.Simulation.Worldgen.GeneratedWorld GeneratedWorld { get; private set; }

        /// <summary>The starting region selected from the wizard's start-location string. Empty when no world has been seeded.</summary>
        public RegionId StartingRegion { get; private set; }
        public SettlementId StartingSettlement { get; private set; }
        public FactionId StartingFaction { get; private set; }


        // ----- IDialogSource -----
        // Mami: _isDialogThinking surfaces a "thinking …" placeholder while
        // the NPC LLM (or DM ConsultFate) is still generating, so the panel
        // never shows a stale or empty line during background inference.
        public string GetCurrentLine() => _isDialogThinking
            ? (string.IsNullOrEmpty(_activeDialogActor)
                ? "Thinking…"
                : _activeDialogActor + " thinks…")
            : _currentDialogLine;
        public bool IsThinking => _isDialogThinking;
        public string GetPortraitName() => _currentPortrait;
        // EMB-045: surface THIS actor's topics (role/faction-derived), not the global menu. Falls back
        // to the world list only when no conversation is active.
        public IReadOnlyList<string> GetTopics()
        {
            if (_conversation != null && _conversation.Topics.Count > 0)
                return _conversation.Topics.Select(t => t.Id).ToList();
            return _world.Topics?.Select(t => t.Id).ToList() ?? new List<string>();
        }

        public void SelectTopic(string topicId)
        {
            // Audit (Phase 12 production wire, 2026-05-27): previously only set a
            // deterministic placeholder. Now (a) sets the deterministic fallback
            // from the seeded AskAboutTopic so the panel always renders SOMETHING
            // useful even with the LLM offline, (b) appends the WorldEvent
            // (unchanged), and (c) fires the async LLM topic-answer via
            // ForgeLocator.LlmRouter. The async path replaces _currentDialogLine
            // on success; _isDialogThinking gates the "thinking…" indicator.
            if (string.IsNullOrEmpty(topicId)) return;

            // EMB-045: answer from THIS actor's topic set first; only fall back to the world list. An
            // actor never answers for a topic they did not offer.
            var topic = _conversation?.FindTopic(topicId)
                ?? _world.Topics?.FirstOrDefault(t => string.Equals(t.Id, topicId, System.StringComparison.Ordinal));
            _currentDialogLine = !string.IsNullOrEmpty(topic?.Answer)
                ? topic.Answer
                : $"{_activeDialogActor} considers \"{topicId}\".";

            var actor = _world.Actors.Records.FirstOrDefault(a => string.Equals(a.Name, _activeDialogActor, System.StringComparison.Ordinal));
            if (actor != null && _world.Events != null)
            {
                _world.Events.Append(new WorldEvent(
                    _world.Time,
                    WorldEventKind.ActorTalked,
                    actor.Id,
                    default,
                    $"topic_selected id:{topicId}"));
            }

            // Live LLM topic answer (Phase 12 production wire)
            var npc = _world.NpcSeeds.FirstOrDefault(n => string.Equals(n.Name, _activeDialogActor, System.StringComparison.Ordinal));
            if (npc != null)
            {
                _ = GenerateNpcTopicAnswerAsync(npc, topicId, topic);
            }
        }

        private async Task GenerateNpcTopicAnswerAsync(NpcSeedRecord npc, string topicId, AskAboutTopic topic)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null) return;

            _isDialogThinking = true;
            var topicLabel = !string.IsNullOrEmpty(topic?.Label) ? topic.Label : topicId;
            var worldStyle = _world.WorldProfile?.Style.ToString() ?? "fantasy";

            var request = new LlmRequest(
                "npc_topic_answer",
                "npc:" + npc.Id.Value + ":topic:" + topicId,
                null,
                180,
                npc.Id.Value,
                $"You are {npc.Name}, a {npc.Role} in a {worldStyle} world. The player asks you about \"{topicLabel}\". Answer briefly in character (1-2 sentences). Reference what you know; do not invent new quests.",
                new List<string>());

            // EMB-007/DET-02: blocking LLM call off-thread; shared-state mutations are enqueued and
            // applied on the deterministic main-thread tick (not on the await's resumption thread).
            var response = await Task.Run(() => router.Complete(request, out _));
            _mainThreadApply.Enqueue(() =>
            {
                if (response != null && !string.IsNullOrEmpty(response.Text))
                    _currentDialogLine = response.Text.Trim();
                _isDialogThinking = false;
            });
        }

    }
}
