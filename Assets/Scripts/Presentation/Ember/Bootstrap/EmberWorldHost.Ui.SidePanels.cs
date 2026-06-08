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

    }
}
