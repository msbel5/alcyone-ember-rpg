using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Fallout 1 / Hitchhiker-style dialog scaffold. Shows the NPC line on top and a
    /// list of player Ask-About topics underneath. Drives the simulation by handing the
    /// topic key back to <see cref="IDialogSource"/>; the simulation decides the reply.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogBoxPanel : MonoBehaviour
    {
        public IDialogSource Source { get; set; }

        private Text _npcLineLabel;
        private RectTransform _topicsRoot;
        private readonly List<Text> _topicLabels = new List<Text>();

        private void Awake()
        {
            _npcLineLabel = BuildLine(transform, anchorMinY: 0.55f, anchorMaxY: 0.95f, alignTop: true, fontSize: 18);
            _topicsRoot = BuildPanelRoot(transform, anchorMinY: 0.05f, anchorMaxY: 0.5f);
            RebuildTopicLabels();
        }

        private void Update()
        {
            if (Source == null) return;
            _npcLineLabel.text = Source.GetCurrentLine();
            var topics = Source.GetTopics();
            for (int i = 0; i < _topicLabels.Count; i++)
            {
                _topicLabels[i].text = i < topics.Count
                    ? $"{i + 1}. Ask about {topics[i]}"
                    : string.Empty;
            }

            for (int i = 0; i < 9 && i < topics.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    Source.SelectTopic(topics[i]);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Close()
        {
            Source = null;
            gameObject.SetActive(false);
            
            // Re-lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void RebuildTopicLabels()
        {
            _topicLabels.Clear();
            for (int i = 0; i < 6; i++)
            {
                var label = BuildLine(_topicsRoot, anchorMinY: 1f - (i + 1) * 0.16f, anchorMaxY: 1f - i * 0.16f, alignTop: true, fontSize: 16);
                _topicLabels.Add(label);
            }
        }

        private static Text BuildLine(Transform parent, float anchorMinY, float anchorMaxY, bool alignTop, int fontSize)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, anchorMinY);
            rt.anchorMax = new Vector2(1f, anchorMaxY);
            rt.offsetMin = new Vector2(12f, 6f);
            rt.offsetMax = new Vector2(-12f, -6f);
            var text = go.GetComponent<Text>();
            text.alignment = alignTop ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = new Color(0.95f, 0.95f, 0.88f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static RectTransform BuildPanelRoot(Transform parent, float anchorMinY, float anchorMaxY)
        {
            var go = new GameObject("Topics", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, anchorMinY);
            rt.anchorMax = new Vector2(1f, anchorMaxY);
            rt.offsetMin = new Vector2(12f, 6f);
            rt.offsetMax = new Vector2(-12f, -6f);
            return rt;
        }
    }

    public interface IDialogSource
    {
        string GetCurrentLine();
        IReadOnlyList<string> GetTopics();
        void SelectTopic(string topicId);
    }
}
