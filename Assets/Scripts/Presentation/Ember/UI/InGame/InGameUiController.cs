using System;
using UnityEngine;
using UnityEngine.UIElements;
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

            // uGUI ScreenSpace-Overlay HUD renders OVER UI-Toolkit panels, so the redesigned HUD cannot sit on
            // top of the legacy EmberHud — Phase 1 retires it and the new HUD owns the screen. The action-bar
            // commands are re-bound onto the new spell bar / buttons in later phases.
            foreach (var legacy in FindObjectsByType<EmberHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                legacy.gameObject.SetActive(false);
            Debug.Log("[InGameUI] World HUD mounted; legacy EmberHud retired.");
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

        private void OpenScreen(string screenId)
        {
            // Phase 1: the modal screens are not built yet. Surface an honest log instead of a dead button or a
            // fake panel; later phases route these to the real InGame modal views.
            Debug.Log("[InGameUI] open screen requested: " + screenId + " (modal not built yet — Phase 3+).");
        }

        private void ConsulDm()
        {
            Debug.Log("[InGameUI] Consul/DM requested (redesign pending — use R for the existing Oracle for now).");
        }
    }
}
