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

    }
}
