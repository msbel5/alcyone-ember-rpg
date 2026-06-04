// REF-3 (LEFT-019): UI-ensure cluster extracted from EmberWorldHost.cs as a partial class — the
// host stays the single per-scene entrypoint, but the idempotent "ensure exactly one panel on a
// Canvas" furniture (pause/dialog/HUD/side-panels/spawner) lives here. Zero behaviour change.
using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Runtime;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    public sealed partial class EmberWorldHost
    {
        // ESCAPE-FIX (E7-020 Input System regression): the 10 gameplay scenes bake an EventSystem with the
        // legacy StandaloneInputModule, but the project now runs activeInputHandler=1 (Input System only),
        // so that module receives NO pointer input — every uGUI click (pause menu, dialog options) was dead
        // in gameplay. CC/MainMenu work only because they build their EventSystem in code with
        // InputSystemUIInputModule. Single-source the same guarantee here: ensure exactly one EventSystem
        // driven by InputSystemUIInputModule, retiring any legacy module. No 10-scene YAML edits.
        private static void EnsureEventSystem()
        {
            _ = EmberEventSystemPolicy.EnsureInputSystemEventSystem();
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
                var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new UnityEngine.Vector2(1920f, 1080f);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2000;
            }

            var pauseGo = new GameObject(
                "PauseMenu",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(EmberCrpg.Presentation.Ember.UI.PauseMenu));
            pauseGo.transform.SetParent(canvas.transform, worldPositionStays: false);
            // LIVE-1 (revised): NO sub-canvas. EnsurePauseMenu is now called LAST in Awake, so the pause
            // menu is the top sibling of the shared overlay canvas and renders above the HUD; with the HUD
            // backdrop's raycastTarget off, its buttons receive clicks through the canvas's shared
            // GraphicRaycaster. The earlier sub-canvas (overrideSorting) regressed visibility: it was a
            // SECOND Canvas created BEFORE the HUD/dialog/panels, so their FindFirstObjectByType<Canvas>
            // grabbed IT as their parent and the whole HUD inherited the pause CanvasGroup's hidden alpha
            // (HUD/dialog only appeared on Escape).
            pauseGo.transform.SetAsLastSibling();
            var rt = pauseGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// HUD-02: Ask-About was dead in ~6/10 scenes because no DialogBoxPanel was
        /// authored there, and EmberPlayerInteractRaycaster only opens dialog when one
        /// already exists. Mirror the EnsurePauseMenu pattern: ensure exactly one
        /// DialogBoxPanel exists on a Canvas at runtime, wired the same way BindUiPanels
        /// wires authored panels (Source = this). Created inactive — like scene-authored
        /// panels, it stays hidden until the raycaster's OpenDialog flips it active on
        /// interaction (and Close() flips it back off). Idempotent: never creates a second.
        /// </summary>
        private void EnsureDialogBoxPanel()
        {
            var existing = Object.FindFirstObjectByType<DialogBoxPanel>(FindObjectsInactive.Include);
            if (existing != null) return;

            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                var canvasGo = new GameObject(
                    "DialogCanvas",
                    typeof(Canvas),
                    typeof(UnityEngine.UI.CanvasScaler),
                    typeof(UnityEngine.UI.GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new UnityEngine.Vector2(1920f, 1080f);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
            }

            var dialogGo = new GameObject(
                "DialogBoxPanel",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(UnityEngine.UI.Image),
                typeof(DialogBoxPanel));
            dialogGo.transform.SetParent(canvas.transform, worldPositionStays: false);
            var rt = dialogGo.GetComponent<RectTransform>();
            // DLG-SIZE-01 — do NOT full-screen-stretch the runtime fallback. The earlier
            // anchorMin=0,0/anchorMax=1,1/offsets=0 made ensured dialogs cover the whole
            // screen (Ask-About scenes that authored no panel got a full-screen box).
            // DialogBoxPanel.Awake now self-pins its own RectTransform to the canonical
            // bottom-centered footprint, so it owns the final size regardless of what we
            // set here; we seed the same bottom band purely so the fallback is never
            // full-screen even for a single frame before Awake runs.
            rt.anchorMin = new Vector2(0.14f, 0.05f);
            rt.anchorMax = new Vector2(0.86f, 0.40f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            dialogGo.GetComponent<DialogBoxPanel>().Source = this;
            // Hidden until the player interacts; matches scene-authored DialogBoxPanels,
            // which the raycaster finds via FindObjectsInactive.Include and activates.
            dialogGo.SetActive(false);
        }

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

        /// <summary>
        /// UI-SINGLE-SOURCE: the three living-world side panels — JobQueue (left), Faction (mid-left),
        /// ColonyNeeds (right) — are now host-ensured in EVERY gameplay scene so the player sees the
        /// same standing world readout everywhere instead of the old "needs/population in some scenes,
        /// nothing in others" inconsistency (the reported orphan UI). Recipes no longer author them.
        /// Unlike EmberHud these panels do NOT self-pin, so we set the canonical footprints here (the
        /// same anchors the recipes used to seed). Each is created at most once; an authored copy or a
        /// host re-run short-circuits the matching block.
        /// </summary>
        private void EnsureSidePanels()
        {
            var canvas = ResolveOverlayCanvas();
            _colonyPanelGroups.Clear();

            if (Object.FindFirstObjectByType<JobQueuePanel>(FindObjectsInactive.Include) == null)
            {
                var go = BuildSidePanel(canvas, "JobQueuePanel",
                    new Vector2(0f, 0.45f), new Vector2(0.22f, 0.94f));
                go.AddComponent<JobQueuePanel>().Source = this;
                _colonyPanelGroups.Add(go.GetComponent<CanvasGroup>());
            }

            if (Object.FindFirstObjectByType<FactionPanel>(FindObjectsInactive.Include) == null)
            {
                var go = BuildSidePanel(canvas, "FactionPanel",
                    new Vector2(0.24f, 0.45f), new Vector2(0.5f, 0.94f));
                go.AddComponent<FactionPanel>().Source = this;
                _colonyPanelGroups.Add(go.GetComponent<CanvasGroup>());
            }

            if (Object.FindFirstObjectByType<ColonyNeedsPanel>(FindObjectsInactive.Include) == null)
            {
                var go = BuildSidePanel(canvas, "ColonyNeedsPanel",
                    new Vector2(0.78f, 0.45f), new Vector2(1f, 0.94f));
                go.AddComponent<ColonyNeedsPanel>().Source = this;
                _colonyPanelGroups.Add(go.GetComponent<CanvasGroup>());
            }

            // BUG-2: hidden by default — the player opens the colony overlay with 'C' when they want it.
            SetColonyPanelsVisible(false);
        }

        /// <summary>
        /// F1.1: ensure exactly one top-right runtime event log panel exists so deterministic world events
        /// stay clear of the vitals while sitting below the top status strip.
        /// </summary>
        private EventLogHudPanel EnsureEventLogHudPanel()
        {
            var existing = Object.FindFirstObjectByType<EventLogHudPanel>(FindObjectsInactive.Include);
            if (existing != null)
            {
                ApplyEventLogHudAnchors((RectTransform)existing.transform);
                return existing;
            }

            var canvas = ResolveOverlayCanvas();
            var go = new GameObject(
                "EventLogHudPanel",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(UnityEngine.UI.Image),
                typeof(EventLogHudPanel));
            go.transform.SetParent(canvas.transform, worldPositionStays: false);
            ApplyEventLogHudAnchors(go.GetComponent<RectTransform>());
            return go.GetComponent<EventLogHudPanel>();
        }

        // Why: keep the event-log footprint canonical even when the host reuses an existing panel instance.
        private static void ApplyEventLogHudAnchors(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0.63f, 0.60f);
            rectTransform.anchorMax = new Vector2(0.99f, 0.93f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Ensure exactly one full-screen overland map panel (toggled with M). The panel self-builds its
        /// chrome and starts hidden; it is parented to the overlay canvas so it covers the scene when open.
        /// Data is pushed by <see cref="Update"/> on toggle, so the generated overland is finally visible.
        /// </summary>
        private OverlandMapPanel EnsureOverlandMapPanel()
        {
            var existing = Object.FindFirstObjectByType<OverlandMapPanel>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var canvas = ResolveOverlayCanvas();
            var go = new GameObject("OverlandMapPanel", typeof(RectTransform), typeof(OverlandMapPanel));
            go.transform.SetParent(canvas.transform, worldPositionStays: false);
            return go.GetComponent<OverlandMapPanel>();
        }

        /// <summary>
        /// BUG-2: show/hide the standing colony overlay (JobQueue / Faction / ColonyNeeds) as a group via
        /// their CanvasGroups, so action scenes (combat, tavern, ritual, trade) are not cluttered by the
        /// living-world readout unless the player asks for it. Content still polls while hidden (cheap).
        /// </summary>
        private void SetColonyPanelsVisible(bool visible)
        {
            _colonyPanelsVisible = visible;
            foreach (var g in _colonyPanelGroups)
            {
                if (g == null) continue;
                g.alpha = visible ? 1f : 0f;
                g.interactable = visible;
                g.blocksRaycasts = visible;
            }
        }

        /// <summary>
        /// BUG-4: shared fate→dialog routing — used on R-press (placeholder line) and again when the async
        /// LLM prophecy resolves. Eighth-pass guard preserved: never clobber an active NPC dialog adapter
        /// source; if one is bound, surface the fate via the HUD combat log instead of stealing the panel.
        /// </summary>
        private void RouteFateToDialog(string line)
        {
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
                _commands?.LogCombat(line);
        }

        /// <summary>
        /// Shared scaffold for the host-ensured side panels: a Canvas-child RectTransform anchored to
        /// the given footprint with a translucent backing Image. The panel scripts swap the Image for
        /// the parchment frame when one is assigned and build their own child label, so a plain Image
        /// is sufficient (mirrors the editor EmberUiBuilder.BuildPanel scaffold minus the editor-only
        /// asset lookups).
        /// </summary>
        private static GameObject BuildSidePanel(Canvas canvas, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(canvas.transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.45f);
            return go;
        }

        /// <summary>
        /// Resolve the scene's screen-space overlay canvas, creating a fallback one when a scene has no
        /// canvas at all (UI-only sandbox). Every generated gameplay scene authors an "EmberHUD" canvas
        /// via EmberUiBuilder.BuildOverlayCanvas, so the FindFirstObjectByType branch is the normal path.
        /// </summary>
        private static Canvas ResolveOverlayCanvas()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null) return canvas;

            var canvasGo = new GameObject(
                "EmberHUD",
                typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
                var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new UnityEngine.Vector2(1920f, 1080f);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            return canvas;
        }

        /// <summary>
        /// SOUL-04 (spawn-from-worldgen): ensure exactly one <see cref="EmberGeneratedActorSpawner"/>
        /// exists and is configured with this host's sprite registry, mirroring the EnsurePauseMenu /
        /// EnsureDialogBoxPanel pattern. The spawner is the single-responsibility component that
        /// materialises billboards for nearby generated NPCs that have no authored ActorView; the host
        /// only owns its lifecycle, not the spawn logic. Idempotent: reuses an existing instance so a
        /// host re-run (additive load / domain reload) never attaches a second.
        /// </summary>
        private EmberGeneratedActorSpawner EnsureGeneratedActorSpawner()
        {
            var existing = GetComponent<EmberGeneratedActorSpawner>();
            var spawner = existing == null ? gameObject.AddComponent<EmberGeneratedActorSpawner>() : existing;
            spawner.Configure(_spriteRegistry);
            return spawner;
        }
    }
}
