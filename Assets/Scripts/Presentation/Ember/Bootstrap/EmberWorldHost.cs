using System.Collections.Generic;
using EmberCrpg.Domain.Configuration;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Sprites;
using EmberCrpg.Presentation.Ember.Tick;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.UI.InGame;
using EmberCrpg.Presentation.Ember.Views;
using EmberCrpg.Presentation.Ember.Runtime;
using UnityEngine;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    /// <summary>
    /// Single MonoBehaviour entrypoint per generated scene. It owns the tick driver,
    /// resolves the active simulation adapter, and binds every visual panel to DTO-only
    /// source interfaces.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class EmberWorldHost : MonoBehaviour, EmberTickDriver.ITickListener,
        IEmberHudSource, IJobQueueSource, IColonyNeedsSource, IDialogSourcePortrait,
        IInventorySource, ISpriteByName, IFactionSource, ICombatHudSource, ISpellBarSource
    {
        [SerializeField] private SpriteRegistry _spriteRegistry;

        private static IReadOnlyList<string> Topics => EmberRuntimeOptionsProvider.Current.WorldHost.DefaultTopics;

        private EmberTickDriver _tick;
        // Codex audit (seventh pass C-P2 #11): the host historically held a
        // single aggregate IDomainSimulationAdapter. Keeping that for the
        // locator registration, but expose narrower role-interface views so
        // host code paths depend on what they actually use (clock + read
        // model + command sink) rather than the full aggregate.
        private IDomainSimulationAdapter _adapter;
        private IEmberSimulationClock _clock;
        private IEmberHudReadModel _hud;
        private IWorldViewReadModel _worldView;
        private IPlayerCommandSink _commands;
        private IConsultFateOracle _oracle;
        private WorldViewProjector _worldViewProjector;
        private OverlandMapPanel _overlandMapPanel; // M-key world map (PRD_overland_map_v1 made visible)
        private InventoryGrid[] _inventoryGrids;
        private int _selectedSpellSlot = 0;
        private string _selectedTopic = "rumors";
        private string _fateLine = string.Empty;
        private float _fateTimer = 0f;
        private float _escHoldTimer = 0f;
        // BUG-2: the standing colony overlay (JobQueue/Faction/ColonyNeeds) is host-ensured once but
        // hidden by default and toggled with 'C', so action scenes are not cluttered by colony readouts.
        private readonly List<CanvasGroup> _colonyPanelGroups = new List<CanvasGroup>();
        private bool _colonyPanelsVisible = false;

        private void Awake()
        {
            // Codex audit (sixth pass A-P3 #7): Unity's fake-null semantics
            // make `??` unreliable on UnityEngine.Object — a destroyed but
            // not-yet-collected component would pass through as non-null.
            // Use an explicit null check for safety even though Awake is
            // typically pre-destruction.
            var existingTick = GetComponent<EmberTickDriver>();
            _tick = existingTick == null ? gameObject.AddComponent<EmberTickDriver>() : existingTick;
            _tick.Listener = this;

            // Codex audit (third pass A-P1): the live scene used to run on
            // PlaceholderSimulationAdapter exclusively, so every HUD row was
            // fabricated state. Prefer a registered domain adapter; if none
            // is registered yet, try to bootstrap a DomainSimulationAdapter
            // backed by a fresh WorldState. Only fall back to the
            // placeholder when both routes fail (UI-only sandbox scenes).
            _adapter = EmberDomainAdapterLocator.Current
                ?? TryCreateDomainAdapter()
                ?? CreateFallbackAdapter();
            // Codex audit (seventh pass C-P2 #11): split the aggregate into
            // narrow role-typed handles so host code paths depend on what
            // they actually use.
            _clock = _adapter;
            _hud = _adapter;
            _worldView = _adapter;
            _commands = _adapter;
            _oracle = _adapter;
            EmberDomainAdapterLocator.Register(_adapter);

            // Codex ninth-pass A-P1: consume any pending world-gen intent
            // from the MainMenu wizard BEFORE the first tick advances state,
            // so this play-through's world reflects the player's answers.
            var pending = EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending;
            if (pending != null && !pending.IsEmpty)
            {
                _commands?.SeedWorld(
                    pending.Mood,
                    pending.Calling,
                    pending.Start,
                    pending.WorldSeed == 0u ? null : pending.WorldSeed);
                if (_adapter is EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter domainAdapter)
                    domainAdapter.ApplyCharacterCreation(pending.PlayerName, pending.CharacterClassId, pending.BirthsignId);
                EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending = null;
            }

            _clock.AdvanceTick(0);

            // World pivot (procedural Scene Director): in the runtime-generated world scene, REALIZE the
            // player's starting settlement from world data — ground / building shells / lighting / player rig —
            // now that SeedWorld has produced the overland. Runs BEFORE EnsureGeneratedActorSpawner() below,
            // which anchors NPC spawning on the "PlayerRig" this creates. Guarded by scene name so the baked
            // vertical-slice scenes keep their authored geometry and are completely untouched.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == EmberScenes.GeneratedWorld)
                EmberCrpg.Presentation.Ember.WorldDirector.WorldSceneDirector.Realize(_worldView);

            // The CharCreation -> worldgen flow shows a DontDestroyOnLoad LoadingScreen overlay
            // and loads this scene underneath it, but nothing dismissed the overlay once the
            // world was live -- so the player saw a permanent "loading" screen on top of a
            // running game (reported as "unplayable"). Now that the world is bootstrapped and
            // the first tick has advanced, release the overlay so the Worldspace is visible.
            EmberCrpg.Presentation.Ember.Loading.LoadingScreen.Dismiss();

            // The UI surface is DontDestroyOnLoad, so the menu-phase panels (CharacterCreation,
            // WorldgenView, LoadingScreen) survive the scene load and stack on top of the live 3D
            // Worldspace -- the real reason the game looked "stuck after world gen". Clearing the
            // surface here removes those leftover panels; the Worldspace HUD is a separate system,
            // and any in-scene panels (dialog, inventory) mount fresh on demand.
            EmberCrpg.Ui.Foundation.UiSurfaceLocator.Current?.Clear();

            // Codex audit (sixth pass E-P2 #E3): if the host re-runs (additive
            // scene loading, domain reload during play, or a scene that
            // already authors EmberSaveService as a sibling component),
            // AddComponent would attach a second instance and Unity would
            // fire two Save/Load handlers per click. Reuse the existing one
            // when present.
            if (gameObject.GetComponent<EmberCrpg.Presentation.Ember.Save.EmberSaveService>() == null)
            {
                gameObject.AddComponent<EmberCrpg.Presentation.Ember.Save.EmberSaveService>();
            }

            // ESCAPE-FIX: gameplay scenes ship a legacy StandaloneInputModule that is dead under
            // activeInputHandler=1 (Input System only) — swap to InputSystemUIInputModule FIRST so the
            // pause menu and dialog-option clicks register again. Must precede the panel ensures.
            EnsureEventSystem();
            EnsureDialogBoxPanel();
            // UI-SINGLE-SOURCE (player report "default UI elements every scene ... ui is coming from
            // one place"): the standard HUD set is now host-owned so every gameplay scene shows the
            // identical surface. Recipes no longer author EmberHud / JobQueue / ColonyNeeds / Faction
            // (the divergent per-scene copies were the orphans the player saw). Ensure them here BEFORE
            // BindUiPanels so the bind loops below find and wire them (Source = this), exactly as they
            // would an authored panel.
            EnsureEmberHud();
            EnsureSidePanels();
            var eventLogHud = EnsureEventLogHudPanel();
            _overlandMapPanel = EnsureOverlandMapPanel(); // M-key: the generated overland made visible
            EnsureInventoryGrid(); // LIVE-2: single inventory in every scene (before the scan below finds it)
            // LIVE-1 (revised): pause menu LAST — top sibling of the overlay canvas, and creating it after
            // the HUD/dialog/panels means their FindFirstObjectByType<Canvas> can't grab a pause sub-canvas.
            EnsurePauseMenu();

            // Mount the redesigned UI-Toolkit overlay LAST: its Awake() retires the full legacy uGUI HUD stack
            // (EmberHud + top-right event log + pause menu), so all three must already exist by this point. It
            // then owns input (InGameUiController.OwnsInput) — the legacy key handlers above yield to it.
            EnsureInGameUi();

            var actorViews = Object.FindObjectsByType<ActorView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var worksiteViews = Object.FindObjectsByType<WorksiteView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _inventoryGrids = Object.FindObjectsByType<InventoryGrid>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // SOUL-04 (spawn-from-worldgen): scenes author only a fixed cast of ~5 ActorViews, so the
            // ~750 generated worldgen NPCs in WorldState.Actors were invisible. Materialise billboards
            // for the nearest few that have no authored view, then RE-SCAN _actorViews so the spawned
            // views join the existing id-keyed PushWorldViews sync below (SOUL-03 movement) on the very
            // first push. Additive + capped + idempotent; no-ops when there is no worldgen population.
            if (EnsureGeneratedActorSpawner().SpawnMissingNearbyActors() > 0)
                actorViews = Object.FindObjectsByType<ActorView>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            _worldViewProjector = new WorldViewProjector(_clock, _worldView, actorViews, worksiteViews, eventLogHud);

            BindUiPanels();
            _worldViewProjector.Project();
            
            // Hide inventory by default
            foreach (var inv in _inventoryGrids)
                inv.gameObject.SetActive(false);
        }

        private void Update()
        {
            // The redesigned PauseView owns Esc now (InGameUiController routes Esc → PauseView / close); the
            // legacy Esc-hold-to-quit must yield so the two don't both react. OwnsInput is true once mounted.
            if (!InGameUiController.OwnsInput) HandleQuitInput();

            if (EmberInput.RegenWorld && !InGameUiController.OwnsInput)
            {
                // The Oracle takes over the dialog: END any NPC conversation first so its topics + replies
                // can't bleed into the Oracle's. This was the reported bug — open the Oracle after an NPC chat,
                // pick a topic, and the previous NPC answered too (the host proxy still forwarded to it, and its
                // in-flight async reply landed in the Oracle box). EndConversation also bumps the conversation
                // serial, so that pending reply is discarded.
                (_adapter as IDialogSource)?.EndConversation();
                // Immediate placeholder line ("The oracle consults the fates…"); the real LLM prophecy
                // resolves async and is swapped in below via TryConsumeResolvedFate (BUG-4).
                _fateLine = _oracle.ConsultFate();
                _fateTimer = EmberRuntimeOptionsProvider.Current.WorldHost.FatePlaceholderSeconds;
                RouteFateToDialog(_fateLine);
            }

            // BUG-4: poll for the resolved oracle prophecy (LLM-flavoured, or the deterministic fate
            // bucket as a floor). When it lands a frame+ later, replace the placeholder in the dialog
            // and extend the dwell so the player can actually read it — previously this only hit the log.
            var resolvedFate = _oracle.TryConsumeResolvedFate();
            if (!string.IsNullOrEmpty(resolvedFate))
            {
                _fateLine = resolvedFate;
                _fateTimer = EmberRuntimeOptionsProvider.Current.WorldHost.FateResolvedSeconds;
                RouteFateToDialog(_fateLine);
            }

            // BUG-2: toggle the standing colony overlay (JobQueue / Faction / ColonyNeeds). Hidden by
            // default so action scenes aren't cluttered; the player opens it on demand.
            if (EmberInput.ToggleColonyPanels && !InGameUiController.OwnsInput)
                SetColonyPanelsVisible(!_colonyPanelsVisible);

            // M: open/close the overland world map. The generated overland is otherwise invisible — this
            // makes the 409,600 km² world (biomes + settlements + the player's home region) legible. Paint
            // on open; the map is static within a session so it needs no per-frame refresh.
            if (EmberInput.KeyDown(KeyCode.M) && !InGameUiController.OwnsInput)
            {
                _overlandMapPanel?.Toggle();
                if (_overlandMapPanel != null && _overlandMapPanel.IsVisible)
                    _overlandMapPanel.Render(_worldView.Overland, _worldView.PlayerOverlandTile, _worldView.StartingSettlementName);
            }

            _fateTimer = WorldHostInputPolicy.StepFateTimer(_fateTimer, Time.deltaTime, () => _fateLine = string.Empty);

            if (EmberInput.ToggleInventory && !InGameUiController.OwnsInput)
            {
                // Codex audit (sixth pass D-P3 #D1): if the scene wires an
                // EmberPlayerInventoryToggle (every Phase* scene does, plus the
                // PlayerRig builder), delegate to its Toggle() so the toggle
                // component is no longer dead code. Falls back to the inline
                // loop for any scene that omits the component.
                var toggle = Object.FindFirstObjectByType<EmberPlayerInventoryToggle>(FindObjectsInactive.Include);
                if (toggle != null)
                {
                    toggle.Toggle();
                }
                else if (_inventoryGrids != null)
                foreach (var inv in _inventoryGrids)
                {
                    bool active = !inv.gameObject.activeSelf;
                    inv.gameObject.SetActive(active);

                    // If opening inventory, unlock cursor. If closing, lock it (if not in dialog)
                    if (active)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        // Simple check: only lock if no dialog is visible
                        bool dialogVisible = false;
                        foreach (var d in Object.FindObjectsByType<DialogBoxPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        {
                            dialogVisible = true;
                            break;
                        }

                        if (!dialogVisible)
                        {
                            Cursor.lockState = CursorLockMode.Locked;
                            Cursor.visible = false;
                        }
                    }
                }
            }

            // Codex audit (sixth pass A-P1 #8): bail out of Alpha1..5 spell
            // selection while a dialog panel is open — those keys belong to
            // the dialog topic chooser. Without this short-circuit, a single
            // "1" press fires SelectTopic(topics[0]) AND mutates
            // _selectedSpellSlot AND queues a spell cast on the next swing.
            _selectedSpellSlot = WorldHostInputPolicy.ResolveSelectedSpellSlot(
                IsModalOpen(),
                _selectedSpellSlot,
                EmberRuntimeOptionsProvider.Current.WorldHost.SpellSlotCount,
                EmberInput.NumberKeyDown);
        }

        /// <summary>
        /// Codex audit (sixth pass A-P1 #8): central modal predicate so
        /// non-dialog input handlers can yield to the dialog panel. Cheap —
        /// FindFirstObjectByType is O(scene size) per call, but only runs
        /// once per Update tick.
        /// </summary>
        internal static bool IsModalOpen()
        {
            // The new in-game UI (InGameUiController) is the canonical modal owner now: when any of its
            // 16 screens or the ☰ browser is open it pauses the world + frees the cursor, so FPS look/move
            // and the interact raycaster must yield to it exactly as they do for the legacy panels.
            return WorldHostInputPolicy.IsModalOpen() || InGameUiController.AnyScreenOpen;
        }

        private void HandleQuitInput()
        {
            _escHoldTimer = WorldHostInputPolicy.StepEscapeHoldTimer(
                _escHoldTimer,
                IsModalOpen(),
                Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include) != null,
                EmberInput.PauseDown,
                EmberInput.PauseHeld,
                Time.unscaledDeltaTime,
                EmberRuntimeOptionsProvider.Current.WorldHost.EscapeHoldQuitSeconds,
                QuitApplication);
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(EmberDomainAdapterLocator.Current, _adapter))
                EmberDomainAdapterLocator.Clear();
        }

        private void BindUiPanels()
        {
            foreach (var hud in Object.FindObjectsByType<EmberHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                hud.Source = this;
            foreach (var q in Object.FindObjectsByType<JobQueuePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                q.Source = this;
            foreach (var n in Object.FindObjectsByType<ColonyNeedsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                n.Source = this;
            foreach (var d in Object.FindObjectsByType<DialogBoxPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                d.Source = this;
            foreach (var inventory in Object.FindObjectsByType<InventoryGrid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                inventory.Source = this;
                inventory.SpriteLookup = this;
            }
            foreach (var faction in Object.FindObjectsByType<FactionPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                faction.Source = this;
            // HUD consistency (T3 + UI-SINGLE-SOURCE): every scene must show the standard EmberHud
            // (vitals pills + numbered hotbar), never the divergent bottom-bar CombatHud. EnsureEmberHud()
            // ran before this bind, so an EmberHud is always present and reads combat vitals via
            // ICombatHudSource (this host) — no combat info is lost. Any CombatHud a scene still carries
            // is therefore redundant; disable it so it never stacks under the standard HUD.
            foreach (var combat in Object.FindObjectsByType<CombatHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (combat == null) continue;
                combat.gameObject.SetActive(false);
            }
            foreach (var spellBar in Object.FindObjectsByType<SpellBar>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                spellBar.Source = this;
                spellBar.SpriteLookup = this;
            }
        }

        public void OnTick(int tickIndex)
        {
            _worldViewProjector?.ProjectTick(tickIndex);
        }

        public string GetHudText() => _hud.HudText;
        IReadOnlyList<JobQueueRow> IJobQueueSource.GetRows() => _worldView.JobQueueRows;
        IReadOnlyList<ColonyNeedsRow> IColonyNeedsSource.GetRows() => _worldView.ColonyNeedsRows;
        IReadOnlyList<FactionRow> IFactionSource.GetRows() => _worldView.FactionRows;
        public IReadOnlyList<InventorySlot> GetSlots() => _worldView.InventorySlots;
        IReadOnlyList<string> ISpellBarSource.GetSlots() => _worldView.SpellSlots;
        int ISpellBarSource.GetSelectedSlot() => _selectedSpellSlot;
        CombatHudState ICombatHudSource.Read() => _hud.CombatHud;
        public Sprite GetSprite(string name)
        {
            var registrySprite = _spriteRegistry != null ? _spriteRegistry.GetSprite(name) : null;
            return registrySprite != null ? registrySprite : GeneratedCoreSpriteLoader.TryLoadPortrait(name);
        }

        /// <summary>
        /// Audit (eighth pass D-P2): static convenience for UI panels that
        /// don't hold a reference to the host but want to resolve a sprite
        /// by name (e.g. DialogBoxPanel portrait lookup).
        /// </summary>
        public static Sprite GetSpriteFromHost(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var host = Object.FindFirstObjectByType<EmberWorldHost>(FindObjectsInactive.Include);
            return host != null ? host.GetSprite(name) : null;
        }

        public string GetCurrentLine()
        {
            // T-Dialog-AskAbout slice 2 fix — transparent proxy. When _adapter is also an
            // IDialogSource (DomainSimulationAdapter implements IDialogSourcePortrait), forward
            // the live line through it so picking a real topic id from GetTopics (e.g.
            // embers/gate/watch) returns the deterministic AskAboutService answer / streaming
            // LLM line instead of the generic fallback below. _fateLine takes precedence so a
            // ConsultFate result still surfaces. Host-owned dialog fallback (no adapter): keep
            // the legacy {work/trade/fate} switch + default flavor line.
            if (!string.IsNullOrEmpty(_fateLine)) return _fateLine;

            if (_adapter is IDialogSource adapterSource)
                return adapterSource.GetCurrentLine();

            switch (_selectedTopic)
            {
                case "work": return "The forge queue is moving. Watch the left panel for job state.";
                case "trade": return "Caravans shift prices as stock moves between settlements.";
                case "fate": return "The oracle can surface a deterministic world query without mutating state.";
                default: return "Ask clean questions. The world remembers what matters.";
            }
        }

        // Async LLM gate — when the adapter is generating an NPC line, the panel renders the
        // "thinking…" placeholder. Default false when host owns the dialog (synchronous path).
        bool IDialogSource.IsThinking => _adapter is IDialogSource adapterSource && adapterSource.IsThinking;

        public IReadOnlyList<string> GetTopics()
        {
            // T-Dialog-AskAbout slice 2 — delegate to the per-NPC adapter source when it has
            // real deterministic topic IDs (DomainSimulationAdapter pulls them from
            // WorldState.Topics, the same source AskAboutService.Ask() reads). Falls back
            // to the {rumors, work, trade, fate} stub when no adapter is wired (offline /
            // editor sketch path). This is what makes TavernDialog finally surface the same
            // real topic IDs (embers/gate/watch/...) that Showroom already showed via the
            // adapter-owned dialog path.
            if (_adapter is IDialogSource adapterSource)
            {
                var adapterTopics = adapterSource.GetTopics();
                if (adapterTopics != null && adapterTopics.Count > 0)
                    return adapterTopics;
            }
            return Topics;
        }

        public string GetPortraitName()
        {
            if (!string.IsNullOrEmpty(_fateLine))
                return DialogPortraitKey.DungeonMaster;

            // Forward to the adapter when it carries a per-NPC portrait id so the dialog panel
            // gets a real sprite name (e.g. "portrait_sage_nera") instead of the gray
            // placeholder. Falls back to the neutral placeholder when no adapter / no portrait.
            if (_adapter is IDialogSourcePortrait portraitSource)
            {
                var name = portraitSource.GetPortraitName();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return DialogPortraitKey.Default;
        }

        public void SelectTopic(string topicId)
        {
            if (string.IsNullOrEmpty(topicId)) return;

            // T-Dialog-AskAbout slice 2 fix — close the loop the prior comment flagged. Slice 2
            // made GetTopics() advertise adapter topics (embers/gate/watch/...). Without this
            // forward, DialogBoxPanel.Source.SelectTopic("embers") only mutated _selectedTopic
            // here and the host's GetCurrentLine fell back to the generic flavor line — the
            // player saw the new topic listed but never the deterministic answer. When the
            // adapter is the real dialog source, forward the selection so its SelectTopic
            // appends the WorldEvent, mutates NpcMemory, and fires the async LLM topic answer.
            if (_adapter is IDialogSource adapterSource)
            {
                adapterSource.SelectTopic(topicId);
                return;
            }

            // Host-owned fallback path (no adapter wired): keep the legacy _selectedTopic
            // mutation so the GetCurrentLine() switch (work/trade/fate) still picks a sane
            // canned line.
            _selectedTopic = topicId;
        }


        private static IDomainSimulationAdapter CreateFallbackAdapter()
        {
            // Codex audit (sixth pass D-P3 #D4): the reflection-based lookup
            // plus EmptySimulationAdapter inner fallback was dead in practice —
            // PlaceholderSimulationAdapter lives in the same assembly as this
            // host, so the typeof reference resolves at compile time. Drop the
            // reflection + the duplicate empty-adapter implementation.
            return new EmberCrpg.Presentation.Ember.Adapters.PlaceholderSimulationAdapter();
        }

        /// <summary>
        /// Codex audit (third pass A-P1): try to bootstrap a real
        /// <see cref="DomainSimulationAdapter"/> over a fresh
        /// <see cref="EmberCrpg.Domain.World.WorldState"/>. Returns
        /// <c>null</c> if WorldFactory throws or if the construction
        /// path is otherwise unavailable; the caller falls through to the
        /// placeholder. Wrapped in try/catch so a missing
        /// Simulation-side dependency never crashes scene bootstrap.
        /// </summary>
        private static IDomainSimulationAdapter TryCreateDomainAdapter()
        {
            try
            {
                var world = new EmberCrpg.Simulation.World.WorldFactory().Create(roomSeed: 1);
                // LIVE-3: standalone scenes (not entered through the worldgen wizard) had no WorldProfile,
                // so their HUD top-bar showed only "Tick/Day" while worldgen-entered scenes showed the full
                // "<Style> / <Genre>  Pop <n>" line — inconsistent across the 10 scenes. Seed a default
                // profile so the top-bar reads IDENTICALLY everywhere; the worldgen path overwrites it with
                // the player's real choices when they come through character creation.
                if (world.WorldProfile == null)
                {
                    var fallback = EmberRuntimeOptionsProvider.Current.WorldHost;
                    world.WorldProfile = new EmberCrpg.Domain.Worldgen.WorldProfile(
                        EmberCrpg.Domain.Worldgen.WorldStyle.LowFantasy,
                        EmberCrpg.Domain.Worldgen.WorldGenre.Survival,
                        seed: fallback.FallbackWorldSeed,
                        targetPopulation: fallback.FallbackTargetPopulation,
                        regionCount: fallback.FallbackRegionCount,
                        factionCount: fallback.FallbackFactionCount,
                        historyYears: fallback.FallbackHistoryYears,
                        moodKeyword: fallback.FallbackMood,
                        playerCallingKeyword: fallback.FallbackCalling,
                        startLocationKeyword: fallback.FallbackStart);
                }
                return new EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter(world);
            }
            catch (System.Exception ex)
            {
                // Codex audit (seventh pass A-P2 #8): the previous catch was
                // silent — a real backend bootstrap failure produced no log
                // line, the host quietly fell through to the placeholder,
                // and Mami saw an empty HUD with no clue why. Surface the
                // exception so the failure is visible in the Editor console
                // and player.log. We still return null so the caller's
                // placeholder fallback runs (game stays bootable), but the
                // operator can now investigate the root cause.
                Debug.LogError("EmberWorldHost: domain adapter bootstrap failed; falling back to PlaceholderSimulationAdapter. " + ex);
                return null;
            }
        }

    }
}
