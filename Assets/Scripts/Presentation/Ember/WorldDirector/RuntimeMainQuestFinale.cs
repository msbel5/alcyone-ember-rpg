using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>F31: the adapter raises the flag when the FINAL delve's Warden falls; the rig-side
    /// view answers with the finale overlay. Static channel (the field-mirror family).</summary>
    public static class RuntimeMainQuestMirror
    {
        public static bool FinaleRequested;
    }

    /// <summary>
    /// F31: the FINAL SCREEN — a runtime-built full-screen overlay (dark veil, title, the closing
    /// line, credits) that rises once when the main quest completes. Built entirely from UGUI
    /// primitives (no assets), same build-safe path as the rest of the runtime UI.
    /// </summary>
    public sealed class RuntimeFinaleView : MonoBehaviour
    {
        private bool _shown;

        private void Update()
        {
            if (_shown || !RuntimeMainQuestMirror.FinaleRequested) return;
            _shown = true;
            RuntimeMainQuestMirror.FinaleRequested = false;
            BuildOverlay();
            Debug.Log("[MainQuest] finale overlay raised — credits on screen.");
        }

        private void BuildOverlay()
        {
            var root = new GameObject("MainQuestFinale");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600; // above HUD + modals — the run is over
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var veil = new GameObject("Veil");
            veil.transform.SetParent(root.transform, false);
            var veilImage = veil.AddComponent<Image>();
            veilImage.color = new Color(0.02f, 0.015f, 0.02f, 0.92f);
            Stretch(veil.GetComponent<RectTransform>());

            AddLine(root.transform, "E M B E R", 64, new Color(1f, 0.62f, 0.25f), 140f);
            AddLine(root.transform, "The Warden falls and the old stones go quiet.", 24, new Color(0.9f, 0.86f, 0.8f), 40f);
            AddLine(root.transform, "The ember's name is yours.", 24, new Color(0.9f, 0.86f, 0.8f), 0f);
            AddLine(root.transform, "— a world that lives whether you watch it or not —", 16, new Color(0.6f, 0.58f, 0.55f), -70f);
            AddLine(root.transform, "CREDITS", 20, new Color(1f, 0.62f, 0.25f), -130f);
            AddLine(root.transform, "design + code: msbel · forge: SDXL · engine: Unity 6", 16, new Color(0.7f, 0.68f, 0.65f), -165f);
            AddLine(root.transform, "built with Claude — every claim proven before it was written", 16, new Color(0.7f, 0.68f, 0.65f), -195f);
        }

        private static void AddLine(Transform parent, string text, int size, Color color, float y)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            // Unity UI Text CLIPS lines taller than their rect (the 64pt title vanished in the
            // first proof frame) — let the glyphs overflow instead of silently truncating.
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 60f);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
