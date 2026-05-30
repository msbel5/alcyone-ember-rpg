using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Renders a deterministic snapshot of actor → job rows. Each row is a single line of
    /// text built from a row DTO so this panel does not import any domain type.
    /// Now with TMP support and parchment styling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JobQueuePanel : MonoBehaviour
    {
        public IJobQueueSource Source { get; set; }

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
            if (Source == null) return "<b>JOB QUEUE</b>\n\n(no source bound)";
            var rows = Source.GetRows();
            if (rows == null || rows.Count == 0) return "<b>JOB QUEUE</b>\n\nidle";
            var sb = new System.Text.StringBuilder(256);
            sb.AppendLine("<size=120%><b>COLONY JOBS</b></size>");
            sb.AppendLine();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                sb.AppendLine($"<color=#f1c40f>{r.ActorName,-12}</color> {r.JobTag,-8} <color=#bdc3c7>({r.StatusCode})</color>");
            }
            return sb.ToString();
        }

        private TMP_Text BuildLabel()
        {
            var go = new GameObject("JobQueueLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12f, 12f);
            rt.offsetMax = new Vector2(-12f, -12f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.TopLeft;
            if (_font != null) text.font = _font;
            text.fontSize = 16;
            text.color = new Color(0.15f, 0.1f, 0.05f);
return text;
        }
    }

    /// <summary>Row DTO consumed by the queue panel. Matches Phase 11 JobDebugSnapshot.</summary>
    public readonly struct JobQueueRow
    {
        public readonly string ActorName;
        public readonly string JobTag;
        public readonly string StatusCode;
        public readonly int QueueIndex;
        public JobQueueRow(string actorName, string jobTag, string statusCode, int queueIndex)
        {
            ActorName = actorName;
            JobTag = jobTag;
            StatusCode = statusCode;
            QueueIndex = queueIndex;
        }
    }

    public interface IJobQueueSource
    {
        IReadOnlyList<JobQueueRow> GetRows();
    }
}
