using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Bottom-bar combat HUD. Renders three horizontal bars (health, stamina, mana) and
    /// a damage log line. Reads from an injected <see cref="ICombatHudSource"/>;
    /// nothing simulation-specific lives in the view.
    /// Now with TMP support, lerped bars, and low-HP flashing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHud : MonoBehaviour
    {
        public ICombatHudSource Source { get; set; }

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _panelFrame;

        private Bar _health;
        private Bar _stamina;
        private Bar _mana;
        private TMP_Text _damageLog;
        private Image _background;

        private void Awake()
        {
            _background = GetComponent<Image>();
            if (_panelFrame != null && _background != null)
            {
                _background.sprite = _panelFrame;
                _background.type = Image.Type.Sliced;
            }

            // Mami fix: HUD bars used to occupy 30% × 30% of the screen each
            // (anchored 0.55-0.85 vertical = giant blocks covering gameplay).
            // Daggerfall-style: compact bottom-row bars, ~22% wide × 4% tall
            // each, anchored to the lower edge with a small gutter.
            _health      = Bar.Build(transform, "Health",  new Vector2(0.02f, 0.02f), new Vector2(0.24f, 0.06f), new Color(0.85f, 0.2f, 0.15f), _font);
            _stamina     = Bar.Build(transform, "Fatigue", new Vector2(0.26f, 0.02f), new Vector2(0.48f, 0.06f), new Color(0.85f, 0.7f, 0.1f), _font);
            _mana        = Bar.Build(transform, "Mana",    new Vector2(0.50f, 0.02f), new Vector2(0.72f, 0.06f), new Color(0.2f, 0.45f, 0.95f), _font);
            _damageLog   = BuildLogLine(transform, new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.5f), _font);
        }

        private void Update()
        {
            if (Source == null) return;
            var s = Source.Read();
            
            _health.SetRatio (Ratio(s.Health,  s.HealthMax));
            _stamina.SetRatio(Ratio(s.Stamina, s.StaminaMax));
            _mana.SetRatio   (Ratio(s.Mana,    s.ManaMax));
            
            _damageLog.text = string.IsNullOrEmpty(s.LastEventLine) ? "—" : s.LastEventLine;

            // Flash health if low
            if (Ratio(s.Health, s.HealthMax) < 0.25f)
            {
                _health.SetColor(Color.Lerp(new Color(0.85f, 0.2f, 0.15f), Color.white, Mathf.PingPong(Time.time * 4f, 1f)));
            }
            else
            {
                _health.SetColor(new Color(0.85f, 0.2f, 0.15f));
            }
        }

        private static float Ratio(int value, int max) => max > 0 ? Mathf.Clamp01((float)value / max) : 0f;

        private static TMP_Text BuildLogLine(Transform parent, Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font)
        {
            var go = new GameObject("DamageLog", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(8f, 4f);
            rt.offsetMax = new Vector2(-8f, -4f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            if (font != null) text.font = font;
            text.fontSize = 18;
            text.color = new Color(0.15f, 0.1f, 0.05f);
return text;
        }

        private sealed class Bar
        {
            private RectTransform _fill;
            private Image _fillImage;
            private float _targetRatio = 1f;
            private float _currentRatio = 1f;

            public static Bar Build(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Color color, TMP_FontAsset font)
            {
                var bar = new Bar();
                var go = new GameObject(label, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, worldPositionStays: false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(go.transform, worldPositionStays: false);
                bar._fill = fillGo.GetComponent<RectTransform>();
                bar._fill.anchorMin = Vector2.zero;
                bar._fill.anchorMax = new Vector2(1f, 1f);
                bar._fill.offsetMin = new Vector2(2f, 2f);
                bar._fill.offsetMax = new Vector2(-2f, -2f);
                bar._fillImage = fillGo.GetComponent<Image>();
                bar._fillImage.color = color;

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, worldPositionStays: false);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                var labelText = labelGo.GetComponent<TextMeshProUGUI>();
                labelText.text = label.ToUpper();
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.fontSize = 12;
                if (font != null) labelText.font = font;
                labelText.color = Color.white;

                bar.SetRatio(1f);
                return bar;
            }

            public void SetRatio(float ratio)
            {
                _targetRatio = Mathf.Clamp01(ratio);
                // Simple internal update for lerping
                _currentRatio = Mathf.Lerp(_currentRatio, _targetRatio, Time.unscaledDeltaTime * 10f);
                _fill.anchorMax = new Vector2(_currentRatio, 1f);
            }

            public void SetColor(Color color) => _fillImage.color = color;
        }
    }

    public readonly struct CombatHudState
    {
        public readonly int Health, HealthMax;
        public readonly int Stamina, StaminaMax;
        public readonly int Mana, ManaMax;
        public readonly string LastEventLine;
        public CombatHudState(int health, int healthMax, int stamina, int staminaMax, int mana, int manaMax, string lastEventLine)
        {
            Health = health; HealthMax = healthMax;
            Stamina = stamina; StaminaMax = staminaMax;
            Mana = mana; ManaMax = manaMax;
            LastEventLine = lastEventLine;
        }
    }

    public interface ICombatHudSource
    {
        CombatHudState Read();
    }
}
