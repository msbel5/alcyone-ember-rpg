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
        IEmberHudSource, IJobQueueSource, IColonyNeedsSource, IDialogSourcePortrait, IPlayerSheetSource,
        IInventorySource, ISpriteByName, IFactionSource, ICombatHudSource, ICombatScreenSource, ISpellBarSource, IJournalSource,
        ITradeSource, ITradeCommandSink, ICraftingSource, ICraftingCommandSink, ISaveLoadSource, ISaveLoadCommandSink,
        ILevelUpSource, ILevelUpCommandSink
    {
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
            // The old fallback fabricated HUD/gameplay rows here. Prefer a registered domain adapter; if none
            // exists, bootstrap a real DomainSimulationAdapter. If that fails, use an honest disabled adapter
            // that exposes empty/unavailable state rather than fake gameplay.
            var binding = EmberWorldHostAdapterBinding.Create(
                EmberDomainAdapterLocator.Current ?? TryCreateDomainAdapter(),
                CreateFallbackAdapter);
            _adapter = binding.Adapter;
            _clock = binding.Clock;
            _hud = binding.Hud;
            _worldView = binding.WorldView;
            _commands = binding.Commands;
            _oracle = binding.Oracle;
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

        private void OnDestroy()
        {
            if (ReferenceEquals(EmberDomainAdapterLocator.Current, _adapter))
                EmberDomainAdapterLocator.Clear();
        }



    }
}
