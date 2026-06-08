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
                    // The Oracle is an intentional takeover (player pressed R): own the panel even if an NPC
                    // source currently holds it, so the Oracle line shows and a topic pick routes to the host —
                    // not the previous NPC. The NPC conversation was already ended in the R handler.
                    d.Source = this;
                    d.gameObject.SetActive(true);
                    routedToPanel = true;
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
        /// exists, mirroring the EnsurePauseMenu / EnsureDialogBoxPanel pattern. The spawner is the single-responsibility component that
        /// materialises billboards for nearby generated NPCs that have no authored ActorView; the host
        /// only owns its lifecycle, not the spawn logic. Idempotent: reuses an existing instance so a
        /// host re-run (additive load / domain reload) never attaches a second.
        /// </summary>
        private EmberGeneratedActorSpawner EnsureGeneratedActorSpawner()
        {
            var existing = GetComponent<EmberGeneratedActorSpawner>();
            var spawner = existing == null ? gameObject.AddComponent<EmberGeneratedActorSpawner>() : existing;
            return spawner;
        }
    }
}
