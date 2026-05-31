// REF-c (LEFT-019/020): vitals + status-label region split out of EmberHud.cs (partial, zero behaviour change).
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmberCrpg.Presentation.Ember.Adapters; // EMB-014: IPlayerCommandSink via EmberDomainAdapterLocator
using EmberCrpg.Presentation.Ember.Inputs;    // EMB-014/015: F1..F12 hotkeys through the input facade

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed partial class EmberHud
    {
        // -------------------------------------------------------------------------------------
        // Vitals  (bottom-left, three labeled bars)
        // -------------------------------------------------------------------------------------
        private void RefreshVitals()
        {
            if (_hpFill == null) return;
            int hp = 80, hpMax = 100, ft = 70, ftMax = 100, mp = 50, mpMax = 100;
            if (Source is ICombatHudSource combat)
            {
                var s = combat.Read();
                hp = s.Health;  hpMax = s.HealthMax;
                ft = s.Stamina; ftMax = s.StaminaMax;
                mp = s.Mana;    mpMax = s.ManaMax;
            }
            SetBar(_hpFill, _hpNumeric, hp, hpMax);
            SetBar(_ftFill, _ftNumeric, ft, ftMax);
            SetBar(_mpFill, _mpNumeric, mp, mpMax);
        }

        private static void SetBar(Image fill, TMP_Text numeric, int cur, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
            var rt = fill.rectTransform;
            rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
            if (numeric != null) numeric.text = cur + " / " + max;
        }

        private void BuildVitalsBars()
        {
            var panel = NewRect("VitalsPanel", transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(24f, 22f);
            panel.sizeDelta = new Vector2(290f, 116f); // 3 rows × 32px + 2 × 6px gap + 16px padding

            _hpFill = MakeLabeledBar(panel, 0, "HEALTH",  VitalHealth,  out _hpNumeric);
            _ftFill = MakeLabeledBar(panel, 1, "FATIGUE", VitalFatigue, out _ftNumeric);
            _mpFill = MakeLabeledBar(panel, 2, "MANA",    VitalMana,    out _mpNumeric);
        }

        // One row = parchment WORD label (left, 70px) + colored bar with numeric overlay (right).
        private Image MakeLabeledBar(RectTransform parent, int row, string word, Color color, out TMP_Text numeric)
        {
            const float h = 32f, gap = 6f, labelW = 72f;
            var rowRt = NewRect("Row_" + word, parent);
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot     = new Vector2(0f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, -row * (h + gap));
            rowRt.sizeDelta = new Vector2(0f, h);

            var label = NewText("Word", rowRt, 14, Parchment, TextAlignmentOptions.MidlineLeft);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0f, 1f);
            labelRt.pivot     = new Vector2(0f, 0.5f);
            labelRt.sizeDelta = new Vector2(labelW, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 0f);
            label.text = word;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 8f;
            label.outlineWidth = 0.18f;
            label.outlineColor = new Color32(0, 0, 0, 200);

            var track = NewRect("Track", rowRt);
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(1f, 1f);
            track.offsetMin = new Vector2(labelW + 6f, 4f);
            track.offsetMax = new Vector2(0f, -4f);
            var trackImg = track.gameObject.AddComponent<Image>();
            trackImg.color = BarTrack;

            var hairline = NewRect("Hairline", track);
            hairline.anchorMin = Vector2.zero;
            hairline.anchorMax = Vector2.one;
            hairline.offsetMin = Vector2.zero;
            hairline.offsetMax = Vector2.zero;
            var hairlineImg = hairline.gameObject.AddComponent<Image>();
            hairlineImg.color = GoldHairline;
            hairlineImg.raycastTarget = false;

            var fillRt = NewRect("Fill", track);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(1f, 1f);
            fillRt.offsetMax = new Vector2(-1f, -1f);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.color = color;

            numeric = NewText("Numeric", track, 12, Parchment, TextAlignmentOptions.Center);
            var numRt = numeric.rectTransform;
            numRt.anchorMin = Vector2.zero;
            numRt.anchorMax = Vector2.one;
            numRt.offsetMin = new Vector2(4f, 0f);
            numRt.offsetMax = new Vector2(-4f, 0f);
            numeric.outlineWidth = 0.22f;
            numeric.outlineColor = new Color32(0, 0, 0, 220);
            return fill;
        }

        // -------------------------------------------------------------------------------------
        // Status label (top-left tick / day / weather — placeholder until T-Clock lands)
        // -------------------------------------------------------------------------------------
        private TMP_Text BuildStatusLabel()
        {
            var t = NewText("HudLabel", transform, 20, Parchment, TextAlignmentOptions.TopLeft);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(24f, -56f);
            rt.offsetMax = new Vector2(-24f, -16f);
            return t;
        }
    }
}
