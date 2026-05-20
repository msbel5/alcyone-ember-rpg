using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Tabular faction reputation readout: one row per faction with a signed delta tag.
    /// Decoupled from domain types via <see cref="IFactionSource"/> — the panel never
    /// names a concrete faction record.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FactionPanel : MonoBehaviour
    {
        public IFactionSource Source { get; set; }

        private Text _label;
        private float _refreshTimer;
        private const float RefreshIntervalSeconds = 0.5f;

        private void Awake()
        {
            _label = GetComponentInChildren<Text>(includeInactive: true) ?? BuildLabel();
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
            if (Source == null) return "FACTIONS\n\n(no source)";
            var rows = Source.GetRows();
            if (rows == null || rows.Count == 0) return "FACTIONS\n\n(none seeded)";
            var sb = new System.Text.StringBuilder(256);
            sb.AppendLine("FACTIONS");
            sb.AppendLine();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var sign = r.Reputation >= 0 ? "+" : string.Empty;
                sb.AppendLine($"{r.FactionName,-16} {sign}{r.Reputation,4}  {r.StatusLabel}");
            }
            return sb.ToString();
        }

        private Text BuildLabel()
        {
            var go = new GameObject("FactionLabel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10f, 10f);
            rt.offsetMax = new Vector2(-10f, -10f);
            var text = go.GetComponent<Text>();
            text.alignment = TextAnchor.UpperLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 15;
            text.color = new Color(0.95f, 0.95f, 0.88f);
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
