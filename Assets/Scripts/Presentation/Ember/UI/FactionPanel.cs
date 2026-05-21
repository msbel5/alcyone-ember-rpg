using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Tabular faction reputation readout: one row per faction with a signed delta tag.
    /// Decoupled from domain types via <see cref="IFactionSource"/> — the panel never
    /// names a concrete faction record.
    /// Now with TMP support and parchment styling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FactionPanel : MonoBehaviour
    {
        public IFactionSource Source { get; set; }

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _panelFrame;

        private TMP_Text _label;
        private Image _background;
        private float _refreshTimer;
        private const float RefreshIntervalSeconds = 0.5f;

        private void Awake()
        {
            _background = GetComponent<Image>();
            if (_panelFrame != null && _background != null)
            {
                _background.sprite = _panelFrame;
                _background.type = Image.Type.Sliced;
            }

            _label = GetComponentInChildren<TMP_Text>(includeInactive: true) ?? BuildLabel();
        }

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshIntervalSeconds;
            _label.text = ComposeText();
        }

        private string ComposeText()
        {
            if (Source == null) return "<b>FACTIONS</b>\n\n(no source)";
            var rows = Source.GetRows();
            if (rows == null || rows.Count == 0) return "<b>FACTIONS</b>\n\n(none seeded)";
            var sb = new System.Text.StringBuilder(256);
            sb.AppendLine("<size=120%><b>FACTIONS</b></size>");
            sb.AppendLine();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var sign = r.Reputation >= 0 ? "+" : string.Empty;
                var color = r.Reputation > 0 ? "2ecc71" : (r.Reputation < 0 ? "e74c3c" : "ecf0f1");
                sb.AppendLine($"<color=#ecf0f1>{r.FactionName,-16}</color> <color=#{color}>{sign}{r.Reputation,4}</color>  <size=80%>{r.StatusLabel}</size>");
            }
            return sb.ToString();
        }

        private TMP_Text BuildLabel()
        {
            var go = new GameObject("FactionLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12f, 12f);
            rt.offsetMax = new Vector2(-12f, -12f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.TopLeft;
            if (_font != null) text.font = _font;
            text.fontSize = 15;
            text.color = new Color(0.15f, 0.1f, 0.05f);
return text;
        }
    }

    public readonly struct FactionRow
    {
        public readonly string FactionName;
        public readonly int Reputation;
        public readonly string StatusLabel;
        public FactionRow(string factionName, int reputation, string statusLabel)
        {
            FactionName = factionName;
            Reputation = reputation;
            StatusLabel = statusLabel;
        }
    }

    public interface IFactionSource
    {
        IReadOnlyList<FactionRow> GetRows();
    }
}
