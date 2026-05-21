using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Top bar HUD. Renders the current tick, in-world day/season, and weather label.
    /// Data is pulled from an injected <see cref="IEmberHudSource"/>; the panel knows
    /// nothing about the domain types behind it.
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
        }

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshIntervalSeconds;
            _label.text = Source != null ? Source.GetHudText() : "Tick 0  ·  Day 1  ·  Calm";
        }

        private TMP_Text BuildLabel()
        {
            var go = new GameObject("HudLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(60f, 10f);
            rt.offsetMax = new Vector2(-60f, -10f);
var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            if (_font != null) text.font = _font;
            text.fontSize = 22;
            text.color = new Color(0.15f, 0.1f, 0.05f);
return text;
        }
    }

    /// <summary>Adapter the HUD reads each refresh. Implement on the simulation/host side.</summary>
    public interface IEmberHudSource
    {
        string GetHudText();
    }
}
