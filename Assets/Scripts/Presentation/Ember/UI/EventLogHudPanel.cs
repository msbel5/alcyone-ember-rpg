using System.Collections.Generic;
using System.Text;
using EmberCrpg.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pattern: Tick-driven view adapter — projects tail rows into a compact, self-built HUD text panel.
namespace EmberCrpg.Presentation.Ember.UI
{
    [DisallowMultipleComponent]
    public sealed class EventLogHudPanel : MonoBehaviour
    {
        private const int VisibleLineCap = 8;
        private readonly List<string> _lastKeys = new List<string>();
        private TMP_Text _label;

        // Why: ensure the panel always has a readable text surface even in scene recipes with no authored children.
        private void Awake()
        {
            var background = GetComponent<Image>();
            if (background != null)
                background.color = new Color(0f, 0f, 0f, 0.60f);

            _label = GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (_label == null)
                _label = BuildLabel();
            ApplyLabelStyle(_label);
        }

        // Why: refresh one deterministic text block per tick and mirror only unseen rows into Player.log.
        public void Render(IReadOnlyList<WorldEventRow> rows, WorldEventNarrator narrator)
        {
            if (_label == null || narrator == null)
                return;

            if (rows == null || rows.Count == 0)
            {
                _label.text = "<size=95%><b>EVENT LOG</b></size>\n\n(waiting for events)";
                _lastKeys.Clear();
                return;
            }

            var keys = new List<string>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
                keys.Add(RowKey(rows[i]));

            int overlap = FindSuffixPrefixOverlap(_lastKeys, keys);
            for (int i = overlap; i < rows.Count; i++)
                Debug.Log("[EmberEventLog] " + narrator.ToLine(rows[i]));

            _lastKeys.Clear();
            _lastKeys.AddRange(keys);

            int start = rows.Count > VisibleLineCap ? rows.Count - VisibleLineCap : 0;
            var sb = new StringBuilder(512);
            sb.AppendLine("<size=95%><b>EVENT LOG</b></size>");
            sb.AppendLine();
            for (int i = start; i < rows.Count; i++)
                sb.AppendLine(narrator.ToLine(rows[i]));
            _label.text = sb.ToString();
        }

        // Why: produce a stable row identity so duplicate logging is prevented across overlapping tail snapshots.
        private static string RowKey(WorldEventRow row)
        {
            return row.Tick.TotalMinutes + "|" + row.KindCode + "|" + row.ActorId.Value + "|" + row.SiteId.Value + "|" + row.Reason;
        }

        // Why: detect which trailing rows were already present in the previous render when the tail window slides.
        private static int FindSuffixPrefixOverlap(List<string> previous, List<string> current)
        {
            int max = previous.Count < current.Count ? previous.Count : current.Count;
            for (int length = max; length > 0; length--)
            {
                int prevStart = previous.Count - length;
                bool match = true;
                for (int i = 0; i < length; i++)
                {
                    if (!string.Equals(previous[prevStart + i], current[i], System.StringComparison.Ordinal))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return length;
            }
            return 0;
        }

        // Why: build a default TMP label so the panel is self-contained and does not rely on prefab wiring.
        private TMP_Text BuildLabel()
        {
            var go = new GameObject("EventLogLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10f, 10f);
            rt.offsetMax = new Vector2(-10f, -10f);

            return go.GetComponent<TextMeshProUGUI>();
        }

        // Why: normalize both authored and generated labels to the same readable top-right HUD style.
        private static void ApplyLabelStyle(TMP_Text text)
        {
            text.alignment = TextAlignmentOptions.TopLeft;
            text.fontSize = 13f;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.color = new Color(0.90f, 0.92f, 0.85f);
            text.raycastTarget = false;
        }
    }
}
