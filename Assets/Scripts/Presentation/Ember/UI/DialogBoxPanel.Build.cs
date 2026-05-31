// REF-d (LEFT-020): UI construction/layout helpers split out of DialogBoxPanel.cs (partial, zero behaviour change).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Bootstrap;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed partial class DialogBoxPanel
    {
        private static void BuildBackingFill(Transform parent, int sibling)
        {
            var go = new GameObject("BackingFill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.SetSiblingIndex(sibling);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.051f, 0.051f, 0.071f, 0.92f); // #0D0D12 @ 92%
            img.raycastTarget = false;
        }

        // Gold hairline border (one thin Image per edge so we don't need a 9-slice sprite).
        private static void BuildHairlineFrame(Transform parent, int sibling)
        {
            var frame = new GameObject("HairlineFrame", typeof(RectTransform));
            frame.transform.SetParent(parent, worldPositionStays: false);
            frame.transform.SetSiblingIndex(sibling);
            var frt = (RectTransform)frame.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var gold = new Color(0.949f, 0.859f, 0.620f, 0.30f);
            AddEdge(frt, "Top",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f,  0f), gold);
            AddEdge(frt, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f,  0f), new Vector2(0f,  2f), gold);
            AddEdge(frt, "Left",   new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f,  0f), new Vector2(2f,  0f), gold);
            AddEdge(frt, "Right",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(0f,  0f), gold);
        }

        private static void AddEdge(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private void RebuildTopicLabels()
        {
            foreach (var label in _topicLabels) if(label != null) Destroy(label.gameObject);
            _topicLabels.Clear();
            for (int i = 0; i < 6; i++)
            {
                var label = BuildLine(_topicsRoot, anchorMinX: 0f, anchorMaxX: 1f, anchorMinY: 1f - (i + 1) * 0.16f, anchorMaxY: 1f - i * 0.16f, alignTop: true, fontSize: 16);
                _topicLabels.Add(label);
            }
        }

        private TMP_Text BuildLine(Transform parent, float anchorMinX, float anchorMaxX, float anchorMinY, float anchorMaxY, bool alignTop, int fontSize)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.offsetMin = new Vector2(60f, 40f);
            rt.offsetMax = new Vector2(-60f, -40f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = alignTop ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left;
            if (_font != null) text.font = _font;
            text.fontSize = fontSize;
            // T-Dialog-AskAbout slice 1 — parchment on the void backing for legibility.
            // Legacy "Deep Charcoal Brown" (0.15, 0.1, 0.05) was invisible on the dark world.
            text.color = new Color(0.949f, 0.859f, 0.620f, 1f); // #F2DB9E parchment
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color32(0, 0, 0, 220);
            return text;
        }

        private static Image BuildPortrait(Transform parent)
        {
            // Portrait container — panel-brown frame so the gray placeholder reads as a deliberate
            // bezel rather than a missing texture. The portrait itself sits inside, full alpha so
            // any real sprite loaded later shows at full strength.
            var frame = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(parent, worldPositionStays: false);
            var frt = (RectTransform)frame.transform;
            frt.anchorMin = new Vector2(0.02f, 0.55f);
            frt.anchorMax = new Vector2(0.18f, 0.95f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var frameImg = frame.GetComponent<Image>();
            frameImg.color = new Color(0.180f, 0.140f, 0.090f, 0.92f); // #2E2417 panel-brown @ 92%
            frameImg.raycastTarget = false;

            // Gold hairline around the frame.
            BuildHairlineFrame(frt, sibling: 1);

            // Inner portrait image — sits inside the frame with a 4px bezel inset.
            var inner = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(frt, worldPositionStays: false);
            var rt = (RectTransform)inner.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);
            var img = inner.GetComponent<Image>();
            // Start the inner image fully transparent so the panel-brown frame reads as the
            // placeholder. Update() flips to Color.white when it actually assigns a sprite —
            // see the "img.color = Color.white;" line in the portrait-assignment block above.
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        private static RectTransform BuildPanelRoot(Transform parent, float anchorMinX, float anchorMaxX, float anchorMinY, float anchorMaxY)
        {
            var go = new GameObject("Topics", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.offsetMin = new Vector2(60f, 40f);
            rt.offsetMax = new Vector2(-60f, -40f);
            return rt;
        }
    }
}
