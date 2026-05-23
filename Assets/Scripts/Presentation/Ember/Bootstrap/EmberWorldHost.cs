using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Sprites;
using EmberCrpg.Presentation.Ember.Tick;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    /// <summary>
    /// Single MonoBehaviour entrypoint per generated scene. It owns the tick driver,
    /// resolves the active simulation adapter, and binds every visual panel to DTO-only
    /// source interfaces.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmberWorldHost : MonoBehaviour, EmberTickDriver.ITickListener,
        IEmberHudSource, IJobQueueSource, IColonyNeedsSource, IDialogSourcePortrait,
        IInventorySource, ISpriteByName, IFactionSource, ICombatHudSource, ISpellBarSource
    {
        [SerializeField] private SpriteRegistry _spriteRegistry;

        private static readonly IReadOnlyList<string> Topics = new List<string> { "rumors", "work", "trade", "fate" };

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
        private ActorView[] _actorViews;
        private WorksiteView[] _worksiteViews;
        private InventoryGrid[] _inventoryGrids;
        private int _selectedSpellSlot = 0;
        private string _selectedTopic = "rumors";
        private string _fateLine = string.Empty;
        private float _fateTimer = 0f;
        private float _escHoldTimer = 0f;

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
            // backed by a fresh SliceWorldState. Only fall back to the
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
                _commands?.SeedWorld(pending.Mood, pending.Calling, pending.Start);
                if (_adapter is EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter domainAdapter)
                    domainAdapter.ApplyCharacterCreation(pending.PlayerName, pending.CharacterClassId, pending.BirthsignId);
                EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending = null;
            }

            _clock.AdvanceTick(0);

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

            EnsurePauseMenu();

            _actorViews = Object.FindObjectsByType<ActorView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _worksiteViews = Object.FindObjectsByType<WorksiteView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _inventoryGrids = Object.FindObjectsByType<InventoryGrid>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            BindUiPanels();
            PushWorldViews();
            
            // Hide inventory by default
            foreach (var inv in _inventoryGrids)
                inv.gameObject.SetActive(false);
        }

        private void Update()
        {
            HandleQuitInput();

            if (Input.GetKeyDown(KeyCode.R))
            {
                _fateLine = _oracle.ConsultFate();
                _fateTimer = 3f;

                // Codex audit (eighth pass A-P1): the previous unconditional
                // `d.Source = this` clobbered any active NPC dialog adapter
                // (e.g. a DomainActorDialogSource bound via GetDialogSource),
                // silently replacing per-NPC dialog with the host's oracle
                // string. Guard the rebind: only adopt the panel when its
                // Source is null OR already the host. When an adapter-owned
                // source is bound, surface the fate text via the HUD's
                // combat-log path instead so the player still sees it.
                var dialogs = Object.FindObjectsByType<DialogBoxPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                bool routedToPanel = false;
                foreach (var d in dialogs)
                {
                    if (d.name.Contains("Narration") || d.name.Contains("Dialog"))
                    {
                        if (d.Source == null || ReferenceEquals(d.Source, this))
                        {
                            d.Source = this;
                            d.gameObject.SetActive(true);
                            routedToPanel = true;
                        }
                    }
                }
                if (!routedToPanel)
                {
                    // Adapter-owned dialog is active — keep its NPC line on the
                    // panel and surface the fate response via the HUD log.
                    _commands?.LogCombat(_fateLine);
                }
            }

            if (_fateTimer > 0)
            {
                _fateTimer -= Time.deltaTime;
                if (_fateTimer <= 0)
                {
                    _fateLine = string.Empty;
                    // We don't necessarily close the box here, it will just show the normal topic/line
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                // Codex audit (sixth pass D-P3 #D1): if the scene wires an
                // EmberPlayerInventoryToggle (every Faz* scene does, plus the
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
            if (!IsModalOpen())
            {
                // Spell selection
                for (int i = 0; i < 5; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        _selectedSpellSlot = i;
                    }
                }
            }
        }

        /// <summary>
        /// Codex audit (sixth pass A-P1 #8): central modal predicate so
        /// non-dialog input handlers can yield to the dialog panel. Cheap —
        /// FindFirstObjectByType is O(scene size) per call, but only runs
        /// once per Update tick.
        /// </summary>
        internal static bool IsModalOpen()
        {
            var dialog = Object.FindFirstObjectByType<DialogBoxPanel>(FindObjectsInactive.Exclude);
            return dialog != null;
        }

        private void HandleQuitInput()
        {
            // Codex audit (sixth pass A-P2 #9): when a dialog panel is open
            // the DialogBoxPanel owns Escape (Close). Yielding prevents the
            // double-handler race where one frame both Close()s the panel AND
            // toggles cursor lock AND starts the >1s quit hold.
            if (IsModalOpen()) { _escHoldTimer = 0f; return; }

            // Codex audit Batch 3 / Finding D-2: the previous structure
            //   if (GetKey(Escape)) { hold }
            //   else { if (GetKeyDown(Escape)) toggleCursor; reset; }
            // wrapped the GetKeyDown check inside the !GetKey else-branch, but on
            // the very frame Escape is first pressed BOTH GetKey and GetKeyDown
            // are true — so the toggle branch was unreachable forever. Move the
            // GetKeyDown check OUT of the else so a tap toggles the cursor lock,
            // and a >1s hold still quits.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
                // Codex audit (sixth pass A-P2 #9): reset hold timer in the
                // tap branch so a brief focus-loss after the toggle does not
                // accumulate stale partial-hold time toward the >1s quit.
                _escHoldTimer = 0f;
            }

            if (Input.GetKey(KeyCode.Escape))
            {
                _escHoldTimer += Time.unscaledDeltaTime;
                if (_escHoldTimer > 1f)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }
            else
            {
                _escHoldTimer = 0f;
            }
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
            foreach (var combat in Object.FindObjectsByType<CombatHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                combat.Source = this;
            foreach (var spellBar in Object.FindObjectsByType<SpellBar>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                spellBar.Source = this;
                spellBar.SpriteLookup = this;
            }
        }

        public void OnTick(int tickIndex)
        {
            _clock.AdvanceTick(tickIndex);
            PushWorldViews();
        }

        private void PushWorldViews()
        {
            // Codex audit (fourth pass A-P1): the previous lookup used
            // actor.name (the GameObject name like "Smith_A") to resolve a
            // domain ActorRecord, which silently failed because
            // SliceWorldFactory creates actors named "Warden", "Sage Nera",
            // etc. ActorView now exposes a DomainActorKey that scenes can
            // author per-view (falling back to the GameObject name for
            // legacy scenes).
            for (int i = 0; i < _actorViews.Length; i++)
            {
                var actor = _actorViews[i];
                if (_worldView.TryReadActor(actor.DomainActorKey, out var state))
                    actor.SetTarget(state);
            }

            for (int i = 0; i < _worksiteViews.Length; i++)
            {
                var worksite = _worksiteViews[i];
                if (_worldView.TryReadWorksite(worksite.name, out var state))
                    worksite.SetState(state);
            }
        }

        public string GetHudText() => _hud.HudText;
        IReadOnlyList<JobQueueRow> IJobQueueSource.GetRows() => _worldView.JobQueueRows;
        IReadOnlyList<ColonyNeedsRow> IColonyNeedsSource.GetRows() => _worldView.ColonyNeedsRows;
        IReadOnlyList<FactionRow> IFactionSource.GetRows() => _worldView.FactionRows;
        public IReadOnlyList<InventorySlot> GetSlots() => _worldView.InventorySlots;
        IReadOnlyList<string> ISpellBarSource.GetSlots() => _worldView.SpellSlots;
        int ISpellBarSource.GetSelectedSlot() => _selectedSpellSlot;
        CombatHudState ICombatHudSource.Read() => _hud.CombatHud;
        public Sprite GetSprite(string name) => _spriteRegistry != null ? _spriteRegistry.GetSprite(name) : null;

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
            if (!string.IsNullOrEmpty(_fateLine)) return _fateLine;

            switch (_selectedTopic)
            {
                case "work": return "The forge queue is moving. Watch the left panel for job state.";
                case "trade": return "Caravans shift prices as stock moves between settlements.";
                case "fate": return "The oracle can surface a deterministic world query without mutating state.";
                default: return "Ask clean questions. The world remembers what matters.";
            }
        }

        public IReadOnlyList<string> GetTopics() => Topics;
        public string GetPortraitName() => "portrait_npc_placeholder";

        public void SelectTopic(string topicId)
        {
            if (!string.IsNullOrEmpty(topicId))
                _selectedTopic = topicId;
            // Codex audit (eighth pass A-P1): the host's IDialogSource
            // implementation previously only mutated _selectedTopic, so when
            // a DialogBoxPanel routed SelectTopic("trade") into this method
            // while a per-NPC adapter source was actually responsible for
            // the conversation, the adapter never saw the topic change and
            // its line stayed stuck. The R-key path explicitly assigns
            // `d.Source = this`, so host-owned panels still route through
            // the local _selectedTopic above. For adapter-owned sources
            // the panel already calls source.SelectTopic directly (its
            // Source is the adapter, not the host), so no forwarding is
            // required from here. TODO(eighth-pass): if host is ever
            // installed as a proxy that intercepts adapter-owned dialog,
            // re-route via _commands.GetDialogSource(_activeDialogActor).
        }

        /// <summary>
        /// Audit (eighth pass D-P1): PauseMenu was dormant — no scene authored it.
        /// Ensure exactly one PauseMenu exists on a Canvas so Escape actually
        /// surfaces SAVE/LOAD/MAIN MENU/QUIT.
        /// </summary>
        private static void EnsurePauseMenu()
        {
            var existing = Object.FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.PauseMenu>(FindObjectsInactive.Include);
            if (existing != null) return;

            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                var canvasGo = new GameObject(
                    "PauseMenuCanvas",
                    typeof(Canvas),
                    typeof(UnityEngine.UI.CanvasScaler),
                    typeof(UnityEngine.UI.GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2000;
            }

            var pauseGo = new GameObject(
                "PauseMenu",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(EmberCrpg.Presentation.Ember.UI.PauseMenu));
            pauseGo.transform.SetParent(canvas.transform, worldPositionStays: false);
            var rt = pauseGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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
        /// <see cref="EmberCrpg.Domain.World.SliceWorldState"/>. Returns
        /// <c>null</c> if SliceWorldFactory throws or if the construction
        /// path is otherwise unavailable; the caller falls through to the
        /// placeholder. Wrapped in try/catch so a missing
        /// Simulation-side dependency never crashes scene bootstrap.
        /// </summary>
        private static IDomainSimulationAdapter TryCreateDomainAdapter()
        {
            try
            {
                var world = new EmberCrpg.Simulation.World.SliceWorldFactory().Create(roomSeed: 1);
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
