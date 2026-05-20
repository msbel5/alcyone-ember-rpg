using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Renders a deterministic snapshot of actor → job rows. Each row is a single line of
    /// text built from a row DTO so this panel does not import any domain type.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JobQueuePanel : MonoBehaviour
    {
        public IJobQueueSource Source { get; set; }

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
            if (Source == null) return "JOB QUEUE\n\n(no source bound)";
            var rows = Source.GetRows();
            if (rows == null || rows.Count == 0) return "JOB QUEUE\n\nidle";
            var sb = new System.Text.StringBuilder(256);
            sb.AppendLine("JOB QUEUE");
            sb.AppendLine();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                sb.AppendLine($"{r.ActorName,-12} {r.JobTag,-8} {r.StatusCode,-10} q{r.QueueIndex}");
            }
            return sb.ToString();
        }

        private Text BuildLabel()
        {
            var go = new GameObject("JobQueueLabel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10f, 10f);
            rt.offsetMax = new Vector2(-10f, -10f);
            var text = go.GetComponent<Text>();
            text.alignment = TextAnchor.UpperLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = new Color(0.95f, 0.95f, 0.88f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }

    /// <summary>Row DTO consumed by the queue panel. Matches Faz 11 JobDebugSnapshot.</summary>
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
