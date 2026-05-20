using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Renders per-actor needs (hunger, fatigue, thirst) and a derived mood readout.
    /// Each refresh re-builds the text block from rows provided by <see cref="IColonyNeedsSource"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ColonyNeedsPanel : MonoBehaviour
    {
        public IColonyNeedsSource Source { get; set; }

        private Text _label;
        private float _refreshTimer;
        private const float RefreshIntervalSeconds = 0.5f;

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
            _label.text = ComposeText();
        }

        private string ComposeText()
        {
            if (Source == null) return "COLONY NEEDS\n\n(no source bound)";
            var rows = Source.GetRows();
            if (rows == null || rows.Count == 0) return "COLONY NEEDS\n\n(idle)";
            var sb = new System.Text.StringBuilder(256);
            sb.AppendLine("COLONY NEEDS");
            sb.AppendLine();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                sb.AppendLine($"{r.ActorName}");
                sb.AppendLine($"  H {r.Hunger,3}  F {r.Fatigue,3}  T {r.Thirst,3}  M {r.Mood,3}");
            }
            return sb.ToString();
        }

        private Text BuildLabel()
        {
            var go = new GameObject("NeedsLabel", typeof(RectTransform), typeof(Text));
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
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }

    public readonly struct ColonyNeedsRow
    {
        public readonly string ActorName;
        public readonly int Hunger;
        public readonly int Fatigue;
        public readonly int Thirst;
        public readonly int Mood;
        public ColonyNeedsRow(string actorName, int hunger, int fatigue, int thirst, int mood)
        {
            ActorName = actorName;
            Hunger = hunger;
            Fatigue = fatigue;
            Thirst = thirst;
            Mood = mood;
        }
    }

    public interface IColonyNeedsSource
    {
        IReadOnlyList<ColonyNeedsRow> GetRows();
    }
}
