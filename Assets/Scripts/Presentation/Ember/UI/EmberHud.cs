using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// In-world HUD shared by every gameplay scene. Renders a top status line (tick / day /
    /// weather) plus the Ember design-system furniture: three vitals pills (Health / Fatigue /
    /// Mana) bottom-left and a numbered 1-9 hotbar bottom-center. Vitals are read each refresh
    /// from the injected <see cref="IEmberHudSource"/> when it also implements
    /// <see cref="ICombatHudSource"/> (EmberWorldHost does), so the panel stays domain-agnostic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmberHud : MonoBehaviour
    {
        public IEmberHudSource Source { get; set; }

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _backgroundFrame;

        private TMP_Text _label;
        private Image _background;
        private float _refreshTimer;
        private const float RefreshIntervalSeconds = 0.25f;

        private Image _hpFill, _ftFill, _mpFill;
        private TMP_Text _hpText, _ftText, _mpText;

        // Ember design tokens (mirror Assets/Scripts/Ui/Foundation/UiTokens.cs).
        private static readonly Color VitalHealth = new Color(0.851f, 0.2f, 0.122f, 1f);    // #D9331F
        private static readonly Color VitalFatigue = new Color(0.851f, 0.702f, 0.102f, 1f); // #D9B31A
        private static readonly Color VitalMana = new Color(0.2f, 0.451f, 0.949f, 1f);      // #3373F2
        private static readonly Color Parchment = new Color(0.949f, 0.859f, 0.62f, 1f);     // #F2DB9E
        private static readonly Color PanelBrown = new Color(0.18f, 0.14f, 0.09f, 0.92f);   // #2E2417
        private static readonly Color Gold = new Color(1f, 0.851f, 0.298f, 1f);             // #FFD94C
        private static readonly Color Track = new Color(0.07f, 0.06f, 0.05f, 0.9f);
        private static readonly Color Hairline = new Color(0.949f, 0.859f, 0.62f, 0.22f);

        private void Awake()
        {
            _background = GetComponent<Image>();
            if (_backgroundFrame != null && _background != null)
            {
                _background.sprite = _backgroundFrame;
                _background.type = Image.Type.Sliced;
            }

            _label = GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (_label == null) _label = BuildLabel();
            else { _label.color = Parchment; }   // was a near-black color, invisible on the dark world

            BuildVitalsPills();
            BuildHotbar();
        }

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshIntervalSeconds;
            if (_label != null)
                _label.text = Source != null ? Source.GetHudText() : "Tick 0  ·  Day 1  ·  Calm";
            RefreshVitals();
        }

        private void RefreshVitals()
        {
            if (_hpFill == null) return;
            int hp = 80, hpMax = 100, ft = 70, ftMax = 100, mp = 50, mpMax = 100;
            if (Source is ICombatHudSource combat)
            {
                var s = combat.Read();
                hp = s.Health; hpMax = s.HealthMax;
                ft = s.Stamina; ftMax = s.StaminaMax;
                mp = s.Mana; mpMax = s.ManaMax;
            }
            SetPill(_hpFill, _hpText, "HP", hp, hpMax);
            SetPill(_ftFill, _ftText, "FT", ft, ftMax);
            SetPill(_mpFill, _mpText, "MP", mp, mpMax);
        }

        private static void SetPill(Image fill, TMP_Text label, string tag, int cur, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
            var rt = fill.rectTransform;
            rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
            if (label != null) label.text = tag + "  " + cur + " / " + max;
        }

        private TMP_Text BuildLabel()
        {
            var t = NewText("HudLabel", transform, 20, Parchment, TextAlignmentOptions.TopLeft);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(24f, -56f);
            rt.offsetMax = new Vector2(-24f, -16f);
            return t;
        }

        private void BuildVitalsPills()
        {
            var panel = NewRect("VitalsPanel", transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(24f, 22f);
            panel.sizeDelta = new Vector2(248f, 86f);

            _hpFill = MakePill(panel, 0, "HP", VitalHealth, out _hpText);
            _ftFill = MakePill(panel, 1, "FT", VitalFatigue, out _ftText);
            _mpFill = MakePill(panel, 2, "MP", VitalMana, out _mpText);
        }

        private Image MakePill(RectTransform parent, int row, string tag, Color color, out TMP_Text label)
        {
            const float h = 22f, gap = 7f;
            var pill = NewRect("Pill_" + tag, parent);
            pill.anchorMin = new Vector2(0f, 1f);
            pill.anchorMax = new Vector2(1f, 1f);
            pill.pivot = new Vector2(0f, 1f);
            pill.anchoredPosition = new Vector2(0f, -row * (h + gap));
            pill.sizeDelta = new Vector2(0f, h);

            var track = pill.gameObject.AddComponent<Image>();
            track.color = Track;

            var fillRt = NewRect("Fill", pill);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.color = color;

            label = NewText("Label", pill, 12, Parchment, TextAlignmentOptions.MidlineLeft);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8f, 0f);
            lrt.offsetMax = new Vector2(-6f, 0f);
            label.outlineWidth = 0.18f;
            label.outlineColor = new Color32(0, 0, 0, 200);
            return fill;
        }

        private void BuildHotbar()
        {
            var bar = NewRect("Hotbar", transform);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 18f);
            const float slot = 46f, gap = 6f;
            const int count = 9;
            float width = count * slot + (count - 1) * gap;
            bar.sizeDelta = new Vector2(width, slot);

            for (int i = 0; i < count; i++)
            {
                var s = NewRect("Slot" + (i + 1), bar);
                s.anchorMin = new Vector2(0f, 0.5f);
                s.anchorMax = new Vector2(0f, 0.5f);
                s.pivot = new Vector2(0f, 0.5f);
                s.anchoredPosition = new Vector2(i * (slot + gap), 0f);
                s.sizeDelta = new Vector2(slot, slot);

                var fill = s.gameObject.AddComponent<Image>();
                fill.color = PanelBrown;

                var border = NewRect("Border", s);
                border.anchorMin = Vector2.zero;
                border.anchorMax = Vector2.one;
                border.offsetMin = Vector2.zero;
                border.offsetMax = Vector2.zero;
                var bimg = border.gameObject.AddComponent<Image>();
                bimg.color = Hairline;
                bimg.raycastTarget = false;

                var num = NewText("Num", s, 14, Gold, TextAlignmentOptions.TopLeft);
                var nrt = num.rectTransform;
                nrt.anchorMin = Vector2.zero;
                nrt.anchorMax = Vector2.one;
                nrt.offsetMin = new Vector2(5f, 0f);
                nrt.offsetMax = new Vector2(-3f, -3f);
                num.text = (i + 1).ToString();
            }
        }

        private RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            return (RectTransform)go.transform;
        }

        private TMP_Text NewText(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, worldPositionStays: false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (_font != null) t.font = _font;
            return t;
        }
    }

    /// <summary>Adapter the HUD reads each refresh. Implement on the simulation/host side.</summary>
    public interface IEmberHudSource
    {
        string GetHudText();
    }
}
