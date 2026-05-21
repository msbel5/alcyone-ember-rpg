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
        IEmberHudSource, IJobQueueSource, IColonyNeedsSource, IDialogSource,
        IInventorySource, ISpriteByName, IFactionSource, ICombatHudSource, ISpellBarSource
    {
        [SerializeField] private SpriteRegistry _spriteRegistry;

        private static readonly IReadOnlyList<string> Topics = new List<string> { "rumors", "work", "trade", "fate" };

        private EmberTickDriver _tick;
        private IDomainSimulationAdapter _adapter;
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
            _tick = GetComponent<EmberTickDriver>() ?? gameObject.AddComponent<EmberTickDriver>();
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
            EmberDomainAdapterLocator.Register(_adapter);
            _adapter.AdvanceTick(0);

            gameObject.AddComponent<EmberCrpg.Presentation.Ember.Save.EmberSaveService>();

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
                _fateLine = _adapter.ConsultFate();
                _fateTimer = 3f;
                
                // Ensure a dialog box is visible to show the fate line
                var dialogs = Object.FindObjectsByType<DialogBoxPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var d in dialogs)
                {
                    if (d.name.Contains("Narration") || d.name.Contains("Dialog"))
                    {
                        d.Source = this;
                        d.gameObject.SetActive(true);
                    }
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

            // Spell selection
            for (int i = 0; i < 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    _selectedSpellSlot = i;
                }
            }
        }

        private void HandleQuitInput()
        {
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
            _adapter.AdvanceTick(tickIndex);
            PushWorldViews();
        }

        private void PushWorldViews()
        {
            for (int i = 0; i < _actorViews.Length; i++)
            {
                var actor = _actorViews[i];
                if (_adapter.TryReadActor(actor.name, out var state))
                    actor.SetTarget(state);
            }

            for (int i = 0; i < _worksiteViews.Length; i++)
            {
                var worksite = _worksiteViews[i];
                if (_adapter.TryReadWorksite(worksite.name, out var state))
                    worksite.SetState(state);
            }
        }

        public string GetHudText() => _adapter.HudText;
        IReadOnlyList<JobQueueRow> IJobQueueSource.GetRows() => _adapter.JobQueueRows;
        IReadOnlyList<ColonyNeedsRow> IColonyNeedsSource.GetRows() => _adapter.ColonyNeedsRows;
        IReadOnlyList<FactionRow> IFactionSource.GetRows() => _adapter.FactionRows;
        public IReadOnlyList<InventorySlot> GetSlots() => _adapter.InventorySlots;
        IReadOnlyList<string> ISpellBarSource.GetSlots() => _adapter.SpellSlots;
        int ISpellBarSource.GetSelectedSlot() => _selectedSpellSlot;
        CombatHudState ICombatHudSource.Read() => _adapter.CombatHud;
        public Sprite GetSprite(string name) => _spriteRegistry != null ? _spriteRegistry.GetSprite(name) : null;

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

        public void SelectTopic(string topicId)
        {
            if (!string.IsNullOrEmpty(topicId))
                _selectedTopic = topicId;
        }

        private static IDomainSimulationAdapter CreateFallbackAdapter()
        {
            var type = System.Type.GetType(
                "EmberCrpg.Presentation.Ember.Adapters.PlaceholderSimulationAdapter, EmberCrpg.Presentation");
            if (type != null && typeof(IDomainSimulationAdapter).IsAssignableFrom(type))
                return (IDomainSimulationAdapter)System.Activator.CreateInstance(type);

            return new EmptySimulationAdapter();
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
            catch (System.Exception)
            {
                return null;
            }
        }

        private sealed class EmptySimulationAdapter : IDomainSimulationAdapter
        {
            private static readonly IReadOnlyList<JobQueueRow> EmptyJobs = System.Array.Empty<JobQueueRow>();
            private static readonly IReadOnlyList<ColonyNeedsRow> EmptyNeeds = System.Array.Empty<ColonyNeedsRow>();
            private static readonly IReadOnlyList<FactionRow> EmptyFactions = System.Array.Empty<FactionRow>();
            private static readonly IReadOnlyList<InventorySlot> EmptyInventory = System.Array.Empty<InventorySlot>();
            private static readonly IReadOnlyList<string> EmptySpells = System.Array.Empty<string>();

            public void AdvanceTick(int tickIndex) { }
            public int TickIndex => 0;
            public string HudText => "Tick 0   Day 1   Spring";
            public IReadOnlyList<JobQueueRow> JobQueueRows => EmptyJobs;
            public IReadOnlyList<ColonyNeedsRow> ColonyNeedsRows => EmptyNeeds;
            public IReadOnlyList<FactionRow> FactionRows => EmptyFactions;
            public IReadOnlyList<InventorySlot> InventorySlots => EmptyInventory;
            public IReadOnlyList<string> SpellSlots => EmptySpells;
            public CombatHudState CombatHud => new CombatHudState(0, 100, 0, 100, 0, 100, string.Empty);
            public bool TryReadActor(string actorName, out ActorViewState state) { state = default; return false; }
            public bool TryReadWorksite(string siteName, out WorksiteViewState state) { state = default; return false; }
            public IDialogSource GetDialogSource(string actorName) => null;
            public void LogCombat(string message) { }
            public void TakePlayerDamage(int amount) { }
            public string ConsultFate() => string.Empty;
            // Codex audit Batch 2 / Finding 3 — fallback adapter has nothing to
            // round-trip. Return empty / no-op so save/load lifecycle still runs.
            public string ExportStateJson() => string.Empty;
            public void RestoreStateJson(string json) { }
        }
    }
}
