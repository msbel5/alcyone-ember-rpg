using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Runtime;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    public sealed partial class EmberWorldHost
    {
        /// <summary>
        /// UI-SINGLE-SOURCE: EmberHud (the TopBar tick/day/pop readout + the numbered action bar)
        /// is the one HUD every gameplay scene must show. Recipes used to author it per-scene, which
        /// drifted (CombatDungeon authored a CombatHud instead, SeasonFarm once authored a duplicate).
        /// Ensure exactly one here so the HUD comes from a single source and looks identical in every
        /// scene. EmberHud.Awake self-pins its own RectTransform to full-screen and builds its pills +
        /// hotbar procedurally, so a bare Canvas child + Image is all it needs. Idempotent: a scene that
        /// still carries an EmberHud (or a re-run / additive load) is left untouched.
        /// </summary>
        private void EnsureEmberHud()
        {
            var existing = Object.FindFirstObjectByType<EmberHud>(FindObjectsInactive.Include);
            if (existing != null) return;

            var canvas = ResolveOverlayCanvas();
            var go = new GameObject("EmberHud", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(canvas.transform, worldPositionStays: false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            // Transparent container; EmberHud draws its own furniture. Matches the runtime EmberHud
            // that BindUiPanels used to create under a CombatHud canvas.
            // LIVE-1: this full-screen backdrop is NOT interactive — it MUST NOT be a raycast target, or
            // it sits on top of (and eats every mouse click meant for) the pause menu / dialog / anything
            // behind it. Keyboard F5/F9 bypass UI raycasts, which is exactly why quick-save worked but the
            // Escape-menu SAVE/LOAD/MAIN MENU/QUIT buttons were dead. The HUD's own buttons are children
            // with their own raycast targets, so they stay clickable.
            var hudImg = go.GetComponent<UnityEngine.UI.Image>();
            hudImg.color = new Color(0f, 0f, 0f, 0f);
            hudImg.raycastTarget = false;
            go.AddComponent<EmberHud>().Source = this;
        }

        /// <summary>
        /// LIVE-2 (single UI source): the inventory used to open only in TradeMarket (the one scene that
        /// authored an InventoryGrid). Host-ensure exactly one in EVERY scene — centered, wired to this
        /// host (IInventorySource + ISpriteByName), and hidden by default — so Tab opens the SAME
        /// inventory everywhere. Idempotent: a scene that authored its own grid is left untouched.
        /// </summary>
        // Phase 1 of the in-game UI redesign: a UI-Toolkit World HUD overlay (top bar + vitals + spell bar +
        // I/C/M/J/K/DM buttons) from the Claude Design handoff. Mounted ADDITIVELY over the existing uGUI EmberHud
        // so the playable game never breaks mid-migration; the old HUD is retired once the full in-game UI lands.
        // The controller owns its own UIDocument and reads live data via the host's HUD interfaces.
        private void EnsureInGameUi()
        {
            var existing = Object.FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>(FindObjectsInactive.Include);
            if (existing == null)
            {
                var go = new GameObject("InGameUi");
                existing = go.AddComponent<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>();
            }
            existing.Bind(this);
        }

        private void EnsureInventoryGrid()
        {
            if (Object.FindFirstObjectByType<InventoryGrid>(FindObjectsInactive.Include) != null) return;

            var canvas = ResolveOverlayCanvas();
            var go = new GameObject("InventoryGrid",
                typeof(RectTransform), typeof(CanvasGroup), typeof(UnityEngine.UI.Image), typeof(InventoryGrid));
            go.transform.SetParent(canvas.transform, worldPositionStays: false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.30f, 0.16f);
            rt.anchorMax = new Vector2(0.70f, 0.84f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var inv = go.GetComponent<InventoryGrid>();
            inv.Source = this;
            inv.SpriteLookup = this;
            // Hidden until Tab; the Awake hide-loop + ToggleInventory handler manage visibility (same as
            // an authored grid). BindUiPanels also re-wires Source/SpriteLookup harmlessly.
            go.SetActive(false);
        }

    }
}
