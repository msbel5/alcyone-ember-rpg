using System;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.Inputs;
using EmberCrpg.Presentation.Ember.UI.InGame.Screens;
// ICombatHudSource / ISpellBarSource / IEmberHudSource live in the enclosing EmberCrpg.Presentation.Ember.UI
// namespace, so they resolve here without an explicit using.

namespace EmberCrpg.Presentation.Ember.UI.InGame
{
    /// <summary>
    /// Hosts the UI-Toolkit in-game UI (Phase 1: the World HUD; later phases add the modal screens) over the
    /// live gameplay scene. Mounts its own <see cref="UIDocument"/> so it is independent of the menu/char-creation
    /// surface, builds the self-scaling stage + <see cref="WorldHudView"/>, and binds REAL values each frame
    /// (vitals from <see cref="ICombatHudSource"/>, location from the clock + starting settlement, spell slots +
    /// recent world events). It renders ADDITIVELY over the existing uGUI HUD for now — the old HUD is only
    /// retired once the full in-game UI is verified, so the game never breaks mid-migration.
    ///
    /// Gold / level / class are intentionally NOT shown: the simulation does not yet track them, and a fake
    /// number is exactly the kind of player-facing lie we removed elsewhere. They light up once wired.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InGameUiController : MonoBehaviour
    {
        private WorldHudView _hud;
        private InGameStage _stage;
        private object _host;
        private VisualElement _dropdown;
        private bool _wasOpen;

        /// <summary>True while this controller is active — the legacy EmberWorldHost key handlers (M / Tab / K /
        /// R) yield to it so the redesigned screens own input instead of opening the old uGUI panels.</summary>
        public static bool OwnsInput { get; private set; }

        /// <summary>True while a redesigned screen (a modal or the ☰ browser) is open — folded into
        /// EmberWorldHost.IsModalOpen() so FPS look/move stop, and the controller frees the cursor + pauses.</summary>
        public static bool AnyScreenOpen { get; private set; }

        private void OnEnable() => OwnsInput = true;
        private void OnDisable()
        {
            OwnsInput = false; AnyScreenOpen = false;
            if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = 1f;   // never leave the world paused
        }

        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null) doc = gameObject.AddComponent<UIDocument>();
            if (doc.panelSettings == null)
            {
                doc.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                doc.panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DefaultRuntimeTheme");
            }
            doc.panelSettings.sortingOrder = 100;   // over the world
            var root = doc.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;   // the HUD must not eat world clicks

            _stage = new InGameStage(root);
            _hud = new WorldHudView(_stage.Canvas)
            {
                OnOpenScreen = OpenScreen,
                OnConsulDm = ConsulDm,
            };
            BuildScreenBrowser(_stage.Canvas);   // ☰ pill: every screen reachable for use + inspection

            // uGUI ScreenSpace-Overlay HUD renders OVER UI-Toolkit panels, so the redesigned HUD cannot sit on
            // top of the legacy EmberHud — Phase 1 retires it and the new HUD owns the screen. The action-bar
            // commands are re-bound onto the new spell bar / buttons in later phases.
            foreach (var legacy in FindObjectsByType<EmberHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                legacy.gameObject.SetActive(false);
            // The always-on top-right event log and the legacy uGUI pause menu are part of the old HUD stack the
            // redesigned UI replaces: retire them too so (a) the old log stops showing over the new HUD and
            // (b) Esc no longer pops the old menu — the controller routes Esc to the redesigned PauseView instead.
            // (EnsureInGameUi is mounted LAST in EmberWorldHost.Awake so both already exist when this runs.)
            foreach (var log in FindObjectsByType<EventLogHudPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                log.gameObject.SetActive(false);
            foreach (var pause in FindObjectsByType<PauseMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                pause.gameObject.SetActive(false);
            Debug.Log("[InGameUI] World HUD mounted; legacy EmberHud + event log + pause menu retired.");
        }

        /// <summary>Wire the data + button host (the EmberWorldHost). Called by EmberWorldHost.EnsureInGameUi.</summary>
        public void Bind(MonoBehaviour host)
        {
            _host = host;
        }

        private void Update()
        {
            if (_hud == null) return;
            _stage?.Fit();
            HandleScreenInput();

            // Free the cursor + pause the world whenever a screen is open; restore FPS capture + the clock when
            // everything closes. (Set on transition so we don't fight the first-person controller every frame.)
            bool open = IsAnyOpen();
            AnyScreenOpen = open;
            if (open != _wasOpen)
            {
                _wasOpen = open;
                // Fully-qualified: this file has both `using UnityEngine` and `using UnityEngine.UIElements`,
                // and UIElements also defines a `Cursor` type (CS0104 ambiguity otherwise).
                UnityEngine.Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
                UnityEngine.Cursor.visible = open;
                Time.timeScale = open ? 0f : 1f;
            }

            var d = new WorldHudData();

            if (_host is ICombatHudSource combat)
            {
                var s = combat.Read();
                d.Hp = s.Health; d.HpMax = s.HealthMax;
                d.Fatigue = s.Stamina; d.FatigueMax = s.StaminaMax;
                d.Mana = s.Mana; d.ManaMax = s.ManaMax;
            }
            if (_host is ISpellBarSource spells)
                d.SpellSlots = spells.GetSlots();
            if (_host is IEmberHudSource hud)
                d.Location = hud.GetHudText();   // the real top-bar string (Tick/Day/mood/pop/settlement)

            _hud.Refresh(in d);
        }

        // Every in-game screen, opened by id. One modal at a time: CloseScreen() drops any open IgModal overlay
        // first. The HUD buttons (I/C/M/J/K/DM) + the ☰ screen browser route here.
        private void OpenScreen(string screenId)
        {
            CloseScreen();
            CloseBrowser();
            var c = _stage.Canvas;
            switch (screenId)
            {
                case "inventory": new InventoryView(c, CloseScreen); break;
                case "character": new CharacterView(c, CloseScreen); break;
                case "spellbook": new SpellbookView(c, CloseScreen); break;
                case "journal":   new JournalView(c, CloseScreen); break;
                case "worldmap":  new WorldMapView(c, CloseScreen); break;
                case "colony":    new ColonyView(c, CloseScreen); break;
                case "consul":    new ConsulFateView(c, CloseScreen); break;
                case "dialog":    new DialogView(c, CloseScreen); break;
                case "combat":    new CombatView(c, CloseScreen); break;
                case "loot":      new LootView(c, CloseScreen); break;
                case "trade":     new TradeView(c, CloseScreen); break;
                case "crafting":  new CraftingView(c, CloseScreen); break;
                case "pause":     new PauseView(c, CloseScreen); break;
                case "levelup":   new LevelUpView(c, CloseScreen); break;
                case "death":     new DeathView(c, CloseScreen); break;
                case "savegame":  new SaveLoadView(c, CloseScreen); break;
            }
        }

        private void CloseScreen()
        {
            for (var open = _stage.Canvas.Q("IgModalOverlay"); open != null; open = _stage.Canvas.Q("IgModalOverlay"))
                open.RemoveFromHierarchy();
        }

        private void ConsulDm() => OpenScreen("consul");

        // ── input: Tab toggles the ☰ browser; the letter keys open a screen; Esc closes. The legacy host
        // handlers yield (OwnsInput), so these REPLACE the old uGUI panels. The cursor is locked in FPS play, so
        // the ☰ pill can't be clicked until a key opens a screen — that is why Tab is the entry point.
        private void HandleScreenInput()
        {
            if (EmberInput.KeyDown(KeyCode.Tab))    { ToggleBrowser(); return; }
            if (EmberInput.KeyDown(KeyCode.Escape)) { if (IsAnyOpen()) CloseAll(); else OpenScreen("pause"); return; }
            if (EmberInput.KeyDown(KeyCode.C))      OpenScreen("character");
            else if (EmberInput.KeyDown(KeyCode.I)) OpenScreen("inventory");
            else if (EmberInput.KeyDown(KeyCode.M)) OpenScreen("worldmap");
            else if (EmberInput.KeyDown(KeyCode.J)) OpenScreen("journal");
            else if (EmberInput.KeyDown(KeyCode.K)) OpenScreen("colony");
            else if (EmberInput.KeyDown(KeyCode.R)) OpenScreen("consul");
        }

        private bool IsAnyOpen() =>
            (_dropdown != null && _dropdown.style.display == DisplayStyle.Flex) ||
            (_stage != null && _stage.Canvas.Q("IgModalOverlay") != null);

        private void ToggleBrowser()
        {
            if (_dropdown == null) return;
            bool show = _dropdown.style.display != DisplayStyle.Flex;
            CloseScreen();   // a modal and the browser are mutually exclusive
            _dropdown.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void CloseBrowser() { if (_dropdown != null) _dropdown.style.display = DisplayStyle.None; }
        private void CloseAll() { CloseScreen(); CloseBrowser(); }

        /// <summary>Proof/diagnostic hook: open a screen by id from the screenshot driver (verification tours).</summary>
        public void ProofOpenScreen(string id) => OpenScreen(id);

        private static readonly (string id, string label)[] AllScreens =
        {
            ("inventory", "Inventory"), ("character", "Character"), ("spellbook", "Spellbook"),
            ("journal", "Journal"), ("worldmap", "World Map"), ("colony", "Colony"), ("consul", "Consul · DM"),
            ("dialog", "NPC Dialog"), ("combat", "Combat"), ("loot", "Loot"), ("trade", "Trade"),
            ("crafting", "Crafting"), ("pause", "Pause"), ("levelup", "Level Up"), ("death", "Death"),
            ("savegame", "Save / Load"),
        };

        // A ☰ pill at top-centre (like the design's ScreenBrowser) that drops down every screen — so all 16 are
        // reachable + inspectable while the per-key/per-trigger wiring is migrated off the legacy panels.
        private void BuildScreenBrowser(VisualElement canvas)
        {
            var wrap = new VisualElement();
            // Full-width row, children centred — robustly keeps the pill + dropdown centred under the ☰ (the old
            // left:50% + translate slid the opened dropdown off to the right).
            wrap.style.position = Position.Absolute; wrap.style.top = 8; wrap.style.left = 0; wrap.style.right = 0;
            wrap.style.alignItems = Align.Center;

            _dropdown = new VisualElement();
            var dropdown = _dropdown;
            dropdown.style.display = DisplayStyle.None;
            dropdown.style.flexDirection = FlexDirection.Row; dropdown.style.flexWrap = Wrap.Wrap;
            dropdown.style.maxWidth = 720; dropdown.style.marginTop = 6;
            dropdown.style.backgroundColor = IgDesign.C(8, 6, 4, 0.96f);
            IgDesign.Border(dropdown, IgDesign.PA(0.18f), 1); IgDesign.Radius(dropdown, 12);
            dropdown.style.paddingTop = 12; dropdown.style.paddingBottom = 12;
            dropdown.style.paddingLeft = 14; dropdown.style.paddingRight = 14;
            foreach (var (id, label) in AllScreens)
            {
                var sid = id;
                var b = new Button(() => { OpenScreen(sid); dropdown.style.display = DisplayStyle.None; }) { text = label };
                IgDesign.ResetButton(b);
                b.style.fontSize = 11; b.style.color = IgDesign.ParchDim; IgDesign.ApplyFont(b, IgDesign.Sans);
                b.style.backgroundColor = IgDesign.C(22, 17, 10, 0.65f); IgDesign.Border(b, IgDesign.PA(0.14f), 1);
                IgDesign.Radius(b, 7); b.style.marginRight = 6; b.style.marginTop = 6;
                b.style.paddingLeft = 12; b.style.paddingRight = 12; b.style.paddingTop = 7; b.style.paddingBottom = 7;
                dropdown.Add(b);
            }

            var pill = new Button(() =>
                dropdown.style.display = dropdown.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None)
            { text = "☰ SCREENS" };
            IgDesign.ResetButton(pill);
            pill.style.fontSize = 11; pill.style.letterSpacing = 1.3f; pill.style.color = IgDesign.Gold;
            IgDesign.ApplyFont(pill, IgDesign.Sans); pill.style.unityFontStyleAndWeight = FontStyle.Bold;
            pill.style.backgroundColor = IgDesign.C(6, 5, 3, 0.88f); IgDesign.Border(pill, IgDesign.PA(0.30f), 1);
            IgDesign.Radius(pill, 999); pill.style.paddingLeft = 20; pill.style.paddingRight = 20;
            pill.style.paddingTop = 7; pill.style.paddingBottom = 7;

            wrap.Add(pill); wrap.Add(dropdown);
            canvas.Add(wrap);
        }
    }
}
