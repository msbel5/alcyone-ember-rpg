using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Renders per-actor needs (hunger, fatigue, thirst) and a derived mood readout.
    /// Each refresh re-builds the text block from rows provided by <see cref="IColonyNeedsSource"/>.
    /// Now with TMP support and parchment styling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ColonyNeedsPanel : MonoBehaviour
    {
        public IColonyNeedsSource Source { get; set; }

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

            _label = GetComponentInChildren<TMP_Text>(includeInactive: true);
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
            if (Source == null) return "<b>COLONY NEEDS</b>\n\n(no source bound)";
            var rows = Source.GetRows();
            if (rows == null || rows.Count == 0) return "<b>COLONY NEEDS</b>\n\n(idle)";
            var sb = new System.Text.StringBuilder(256);
            sb.AppendLine("<size=120%><b>COLONY NEEDS</b></size>");
            sb.AppendLine();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                sb.AppendLine($"<color=#ecf0f1>{r.ActorName}</color>");
                sb.AppendLine($"  <size=80%>H: {r.Hunger,3}  F: {r.Fatigue,3}  T: {r.Thirst,3}  <color=#2ecc71>M: {r.Mood,3}</color></size>");
            }
            return sb.ToString();
        }

        private TMP_Text BuildLabel()
        {
            var go = new GameObject("NeedsLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
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
