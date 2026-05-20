using UnityEngine;
using UnityEngine.UI;

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

        private Text _label;
        private float _refreshTimer;
        private const float RefreshIntervalSeconds = 0.25f;

        private void Awake()
        {
            _label = GetComponentInChildren<Text>(includeInactive: true);
            if (_label == null) _label = BuildLabel();
        }

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshIntervalSeconds;
            _label.text = Source != null ? Source.GetHudText() : "Tick 0  ·  Day 1  ·  Calm";
        }

        private Text BuildLabel()
        {
            var go = new GameObject("HudLabel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12f, 4f);
            rt.offsetMax = new Vector2(-12f, -4f);
            var text = go.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.color = new Color(0.95f, 0.95f, 0.88f);
            return text;
        }
    }

    /// <summary>Adapter the HUD reads each refresh. Implement on the simulation/host side.</summary>
    public interface IEmberHudSource
    {
        string GetHudText();
    }
}
