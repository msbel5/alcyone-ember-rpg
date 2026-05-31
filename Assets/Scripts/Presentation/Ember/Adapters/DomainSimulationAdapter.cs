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
            // Legacy/fallback path: callers that only have a display-name string. Try to upgrade the
            // name to the actor's STABLE id so we get identical resolution to the id overload; only
            // when no actor carries that name do we bind by raw name (ad-hoc / scene-authored label).
            var byName = _world.Actors?.Records.FirstOrDefault(
                a => string.Equals(a.Name, actorName, System.StringComparison.Ordinal));
            if (byName != null)
                return GetDialogSource(byName.Id);

            var npc = _world.NpcSeeds.FirstOrDefault(
                n => string.Equals(n.Name, actorName, System.StringComparison.Ordinal));
            BeginConversation(actorName, npc);
            return this;
        }

        // ----- IPlayerCommandSink (DLG-01) -----
        public IDialogSource GetDialogSource(ActorId id)
        {
            // DLG-01: resolve by the stable ActorId rather than by display-name string.
            if (id.IsEmpty || _world.Actors == null || !_world.Actors.TryGet(id, out var actor) || actor == null)
            {
                // A mismatch must be LOUD and must NOT silently drop the player into the shared
                // global topic menu (the old name-resolution bug). Surface an explicit empty state.
                _suppressGlobalTopicFallback = true;
                _activeDialogActor = string.Empty;
                _currentDialogLine = "There is no one here to talk to.";
                _currentPortrait = "portrait_npc_placeholder";
                _isDialogThinking = false;
                _conversation = ConversationState.None;
                UnityEngine.Debug.LogWarning(
                    $"DLG-01: GetDialogSource({id}) found no actor in WorldState.Actors; " +
                    "refusing to fall back to global world topics.");
                return this;
            }

            // Recover the matching NpcSeed for generated NPCs. HydrateNpcs mints ActorIds as
            // GeneratedNpcActorOffset + NpcId.Value, so the seed is recoverable by subtracting the
            // offset. Authored slice actors (ids 1..5) have no seed and fall through to name match.
            NpcSeedRecord npc = null;
            if (id.Value >= GeneratedNpcActorOffset)
            {
                var npcId = new EmberCrpg.Domain.Worldgen.NpcId(id.Value - GeneratedNpcActorOffset);
                npc = _world.NpcSeeds.FirstOrDefault(n => n.Id.Equals(npcId));
            }
            npc ??= _world.NpcSeeds.FirstOrDefault(
                n => string.Equals(n.Name, actor.Name, System.StringComparison.Ordinal));

            BeginConversation(actor.Name, npc);
            return this;
        }

        /// <summary>
        /// DLG-01/EMB-020/EMB-045: single funnel that binds the active speaker, portrait, and the
        /// per-actor topic set into the one <see cref="ConversationState"/> the dialog surface reads.
        /// When <paramref name="npc"/> is non-null the topics come from THEIR role + faction (not the
        /// global menu) and a persona greeting is fired; otherwise the shared world topics back the
        /// conversation. Both <see cref="GetDialogSource(string)"/> and
        /// <see cref="GetDialogSource(ActorId)"/> route through here so the two paths can never drift.
        /// </summary>
        private void BeginConversation(string actorName, NpcSeedRecord npc)
        {
            _suppressGlobalTopicFallback = false;
            _activeDialogActor = actorName ?? string.Empty;
            _currentPortrait = "portrait_npc_placeholder";

            if (npc != null)
            {
                if (!string.IsNullOrEmpty(npc.PortraitAssetPath)) _currentPortrait = npc.PortraitAssetPath;

                var perActorTopics = NpcTopicCatalog.For(npc.Role, npc.Faction.Value, _world.Topics);
                _conversation = new ConversationState(_activeDialogActor, _currentPortrait, perActorTopics);

                // BUG-DIALOG-EMPTY: seed a DETERMINISTIC opening line synchronously, up front, so the
                // panel always renders a real sentence even when native inference returns nothing (no
                // model bytes). Prefer the per-actor AskAbout/persona answer (e.g. a Guard's "watch"
                // topic => "Sentinel Rook keeps count of every footstep, including yours."); only fall
                // back to a role+name greeting when no topic answer is available. The async LLM call
                // below REPLACES this line only if it yields a non-empty (non-whitespace) response.
                _currentDialogLine = DeterministicGreeting(_activeDialogActor, npc, perActorTopics);

                _ = GenerateNpcGreetingAsync(npc);
            }
            else
            {
                // No seed record (ad-hoc / authored slice actor): fall back to the shared world topics,
                // still funneled through the one ConversationState model so GetTopics/SelectTopic have
                // a single source of truth.
                _conversation = new ConversationState(
                    _activeDialogActor, _currentPortrait, _world.Topics ?? new List<AskAboutTopic>());

                // BUG-DIALOG-EMPTY: seed a deterministic, non-empty opening line first so the panel
                // is never blank. Lead with a shared world topic answer when present.
                _currentDialogLine = DeterministicGreeting(_activeDialogActor, npc, _world.Topics);

                // LLM-NOT-FIRING fix: the playable scenes use authored actors with no NpcSeed, so the
                // seeded greeting path above never ran for them and the player ALWAYS saw the
                // deterministic line. Fire a name-based LLM greeting here too; it replaces the
                // deterministic line only if the local model returns real text (else the line stays).
                _ = GenerateAdHocGreetingAsync(_activeDialogActor);
            }
        }

        /// <summary>
        /// BUG-DIALOG-EMPTY: the deterministic, offline-safe opening line for a conversation. NEVER
        /// returns null/empty/whitespace. Prefers the leading per-actor AskAbout answer (the same
        /// deterministic copy NpcTopicCatalog / the WorldFactory seed produce, e.g. "Sentinel Rook
        /// keeps count of every footstep, including yours."); otherwise composes a role+name greeting;
        /// otherwise a generic safe fallback. Used as the synchronous line the async LLM may replace.
        /// </summary>
        private static string DeterministicGreeting(
            string actorName, NpcSeedRecord npc, IReadOnlyList<AskAboutTopic> topics)
        {
            // 1) Lead with the first topic answer that carries real copy — this is the persona/AskAbout
            //    line the player reads as the NPC speaking in character.
            if (topics != null)
            {
                foreach (var t in topics)
                {
                    if (t != null && !string.IsNullOrWhiteSpace(t.Answer))
                        return t.Answer.Trim();
                }
            }

            // 2) No topic copy available: greet from role + name when we have a seed.
            bool hasName = !string.IsNullOrWhiteSpace(actorName);
            if (npc != null)
            {
                return hasName
                    ? $"{actorName} the {npc.Role} regards you. \"Speak your piece.\""
                    : $"The {npc.Role} regards you. \"Speak your piece.\"";
            }

            // 3) Last-resort generic line (still never empty).
            return hasName
                ? $"{actorName} waits for you to speak."
                : "Someone waits for you to speak.";
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
                // DIAG (LLM-not-firing): surface exactly what the native model returned so a runtime
                // log reveals whether inference is empty (len<=0 -> llama.cpp produced no tokens) or
                // working. Remove once the local LLM is confirmed generating in-game.
                UnityEngine.Debug.Log($"[NpcGreeting] npc={npc.Name} llm-len={(response?.Text?.Length ?? -1)} " +
                    $"used={(response != null && !string.IsNullOrWhiteSpace(response.Text))}");
                // BUG-DIALOG-EMPTY: a whitespace-only inference result (the native model returning no
                // real bytes) used to pass the IsNullOrEmpty guard, Trim() to "", and BLANK the good
                // deterministic greeting. Require non-whitespace before replacing; otherwise keep the
                // deterministic line that BeginConversation already set.
                if (response != null && !string.IsNullOrWhiteSpace(response.Text))
                    _currentDialogLine = response.Text.Trim();
                _isDialogThinking = false;
            });
        }

        // LLM-NOT-FIRING fix: name-based greeting for authored/ad-hoc actors (no NpcSeed), so the
        // playable scene cast also gets a local-LLM line instead of only the deterministic one. Same
        // off-main-thread + main-thread-apply pattern as GenerateNpcGreetingAsync.
        private async Task GenerateAdHocGreetingAsync(string actorName)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null || string.IsNullOrEmpty(actorName)) return;

            _isDialogThinking = true;
            // Stable per-name seed (string.GetHashCode is process-randomised, so fold the chars via FNV).
            ulong seed = 1469598103934665603UL;
            foreach (var ch in actorName) { seed ^= ch; seed *= 1099511628211UL; }

            var request = new LlmRequest(
                "npc_greeting",
                "npc:" + actorName,
                null,
                100,
                seed,
                $"You are {actorName}, a character in a {_world.WorldProfile?.Style} world. Greet the player character briefly, in character.",
                new List<string>()
            );

            var response = await Task.Run(() => router.Complete(request, out _));
            _mainThreadApply.Enqueue(() =>
            {
                UnityEngine.Debug.Log($"[NpcGreeting-adhoc] actor={actorName} llm-len={(response?.Text?.Length ?? -1)} " +
                    $"used={(response != null && !string.IsNullOrWhiteSpace(response.Text))}");
                if (response != null && !string.IsNullOrWhiteSpace(response.Text))
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
        public string GetCurrentLine()
        {
            if (_isDialogThinking)
                return string.IsNullOrEmpty(_activeDialogActor) ? "Thinking…" : _activeDialogActor + " thinks…";
            // BUG-DIALOG-EMPTY: final guarantee — the dialog panel renders this string verbatim into a
            // label, so it must NEVER be null/empty/whitespace. BeginConversation/SelectTopic always
            // set a deterministic line, but guard here too so no future caller (or a read before any
            // conversation begins) can surface a blank box.
            return string.IsNullOrWhiteSpace(_currentDialogLine) ? "..." : _currentDialogLine;
        }
        public bool IsThinking => _isDialogThinking;
        public string GetPortraitName() => _currentPortrait;
        // EMB-045: surface THIS actor's topics (role/faction-derived), not the global menu. Falls back
        // to the world list only when no conversation is active.
        public IReadOnlyList<string> GetTopics()
        {
            if (_conversation != null && _conversation.Topics.Count > 0)
                return _conversation.Topics.Select(t => t.Id).ToList();
            // DLG-01: when an id-keyed lookup missed, do NOT leak the global world topics — the
            // player is looking at "no one"; an empty topic list is the honest answer.
            if (_suppressGlobalTopicFallback)
                return new List<string>();
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

            // Live LLM topic answer (Phase 12 production wire). Authored scene actors have no
            // NpcSeed, so route them through the ad-hoc (name-based) path — otherwise selecting a
            // topic ALWAYS showed the deterministic answer (the LLM-NOT-FIRING bug, same as greetings).
            var npc = _world.NpcSeeds.FirstOrDefault(n => string.Equals(n.Name, _activeDialogActor, System.StringComparison.Ordinal));
            if (npc != null)
                _ = GenerateNpcTopicAnswerAsync(npc, topicId, topic);
            else
                _ = GenerateAdHocTopicAnswerAsync(_activeDialogActor, topicId, topic);
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
                // BUG-DIALOG-EMPTY: same whitespace guard as the greeting path — never overwrite the
                // deterministic topic answer with an empty/whitespace inference result.
                if (response != null && !string.IsNullOrWhiteSpace(response.Text))
                    _currentDialogLine = response.Text.Trim();
                _isDialogThinking = false;
            });
        }

        // LLM-NOT-FIRING fix (topic answers): name-based answer for authored/ad-hoc actors with no
        // NpcSeed, so picking an Ask-About topic yields a real local-LLM answer instead of only the
        // deterministic one. Mirrors GenerateNpcTopicAnswerAsync.
        private async Task GenerateAdHocTopicAnswerAsync(string actorName, string topicId, AskAboutTopic topic)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null || string.IsNullOrEmpty(actorName)) return;

            _isDialogThinking = true;
            var topicLabel = !string.IsNullOrEmpty(topic?.Label) ? topic.Label : topicId;
            var worldStyle = _world.WorldProfile?.Style.ToString() ?? "fantasy";
            ulong seed = 1469598103934665603UL;
            foreach (var ch in actorName) { seed ^= ch; seed *= 1099511628211UL; }
            foreach (var ch in topicId ?? string.Empty) { seed ^= ch; seed *= 1099511628211UL; }

            var request = new LlmRequest(
                "npc_topic_answer",
                "npc:" + actorName + ":topic:" + topicId,
                null,
                180,
                seed,
                $"You are {actorName}, a character in a {worldStyle} world. The player asks you about \"{topicLabel}\". Answer briefly in character (1-2 sentences). Reference what you know; do not invent new quests.",
                new List<string>());

            var response = await Task.Run(() => router.Complete(request, out _));
            _mainThreadApply.Enqueue(() =>
            {
                UnityEngine.Debug.Log($"[NpcTopic-adhoc] actor={actorName} topic={topicId} " +
                    $"llm-len={(response?.Text?.Length ?? -1)} used={(response != null && !string.IsNullOrWhiteSpace(response.Text))}");
                if (response != null && !string.IsNullOrWhiteSpace(response.Text))
                    _currentDialogLine = response.Text.Trim();
                _isDialogThinking = false;
            });
        }

    }
}
