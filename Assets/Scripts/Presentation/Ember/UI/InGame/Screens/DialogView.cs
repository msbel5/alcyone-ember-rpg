using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public readonly struct DialogTopicOption
    {
        public DialogTopicOption(string id, string label)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public string Id { get; }
        public string Label { get; }
    }

    public sealed class DialogView
    {
        private readonly VisualElement _overlay;
        private readonly Label _responseLabel;
        private Label _lineLabel;                    // the NPC's current spoken line (polled live by the controller)
        private VisualElement _portraitBox;          // portrait frame; background image is set once the sprite loads
        private Label _portraitGlyph;                // placeholder initial, hidden once a real portrait resolves
        public bool HasPortrait { get; private set; }

        public DialogView(VisualElement stageCanvas, Action onClose)
            : this(
                stageCanvas,
                onClose,
                "No Conversation",
                null,
                "No one is speaking with you right now.",
                Array.Empty<DialogTopicOption>(),
                _ => { },
                onClose)
        {
        }

        public DialogView(VisualElement stageCanvas, Action onClose, string npcName, string portraitPath, string greeting,
            IReadOnlyList<DialogTopicOption> topics, Action<string> onTopic, Action onFarewell)
            : this(stageCanvas, onClose, npcName, portraitPath, greeting, topics, onTopic, onFarewell, true)
        {
        }

        private DialogView(VisualElement stageCanvas, Action onClose, string npcName, string portraitPath, string greeting,
            IReadOnlyList<DialogTopicOption> topics, Action<string> onTopic, Action onFarewell, bool _)
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = C(4, 3, 2, 0.52f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.FlexEnd;
            _overlay.style.paddingBottom = Length.Percent(5);
            _overlay.pickingMode = PickingMode.Position;

            var panel = new VisualElement();
            panel.style.width = Length.Percent(72);
            panel.style.maxWidth = 980;
            panel.style.backgroundColor = Parch;
            Border(panel, Gold, 1);
            Radius(panel, 20);
            panel.style.paddingTop = 26;
            panel.style.paddingBottom = 26;
            panel.style.paddingLeft = 26;
            panel.style.paddingRight = 26;
            _overlay.Add(panel);

            var top = Row();
            top.style.marginBottom = 14;
            panel.Add(top);

            top.Add(BuildPortraitPane(npcName, portraitPath));

            var right = new VisualElement();
            right.style.flexGrow = 1;
            _lineLabel = Text(greeting ?? string.Empty, Serif, 17, Ink);
            _lineLabel.style.whiteSpace = WhiteSpace.Normal;
            _lineLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            _lineLabel.style.minHeight = 60;
            right.Add(_lineLabel);
            bool hasTopics = topics != null && topics.Count > 0;
            _responseLabel = Text(hasTopics ? "Choose a topic." : "There is no active conversation to continue.", Serif, 14, Alpha(Ink, 0.50f), FontStyle.Italic);
            _responseLabel.style.whiteSpace = WhiteSpace.Normal;
            _responseLabel.style.marginTop = 14;
            right.Add(_responseLabel);
            top.Add(right);

            var topicsPane = new VisualElement();
            topicsPane.style.borderTopWidth = 1;
            topicsPane.style.borderTopColor = Alpha(Ink, 0.12f);
            topicsPane.style.paddingTop = 10;
            panel.Add(topicsPane);

            if (hasTopics)
            {
                for (int i = 0; i < topics.Count; i++)
                {
                    var topic = topics[i];
                    topicsPane.Add(BuildTopicButton(i + 1, topic, onTopic));
                }
            }
            else
            {
                topicsPane.Add(EmptyState(
                    "Dialogue Idle",
                    "Open this screen from a real NPC interaction to see live topics and replies.",
                    "The standalone browser entry does not have a conversation source behind it."));
            }

            var bottom = Row();
            bottom.style.justifyContent = Justify.SpaceBetween;
            bottom.style.alignItems = Align.Center;
            bottom.style.marginTop = 10;
            bottom.style.borderTopWidth = 1;
            bottom.style.borderTopColor = Alpha(Ink, 0.10f);
            bottom.style.paddingTop = 10;
            bottom.Add(Text(hasTopics ? "ESC · Farewell when finished" : "ESC · Close", Sans, 11, Alpha(Ink, 0.38f)));

            var close = new Button(() => (onFarewell ?? onClose)?.Invoke()) { text = hasTopics ? "FAREWELL" : "CLOSE" };
            ResetButton(close);
            close.style.height = 32;
            close.style.paddingLeft = 16;
            close.style.paddingRight = 16;
            close.style.backgroundColor = Alpha(Ink, 0.10f);
            close.style.color = Ink;
            close.style.fontSize = 12;
            ApplyFont(close, Sans);
            Border(close, Alpha(Ink, 0.22f), 1);
            Radius(close, 7);
            bottom.Add(close);
            panel.Add(bottom);

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        /// <summary>Stream the response line from outside — the controller polls the real IDialogSource's current
        /// line so the async LLM reply appears after a topic is selected (the mock path sets it synchronously).</summary>
        public void SetResponseLine(string text)
        {
            if (_responseLabel != null && !string.IsNullOrEmpty(text)) _responseLabel.text = text;
        }

        /// <summary>The NPC's current spoken line — the controller polls IDialogSource.GetCurrentLine() each frame
        /// so the greeting/answer streams in (the LLM resolves from "{name} thinks…" to the real line).</summary>
        public void SetCurrentLine(string text)
        {
            if (_lineLabel != null && !string.IsNullOrEmpty(text)) _lineLabel.text = text;
        }

        /// <summary>Show the resolved portrait sprite (the controller resolves it from the host sprite registry,
        /// retrying each frame because forge portraits generate asynchronously).</summary>
        public void SetPortrait(Sprite sprite)
        {
            if (sprite == null || _portraitBox == null) return;
            _portraitBox.style.backgroundImage = new StyleBackground(sprite);
            if (_portraitGlyph != null) _portraitGlyph.style.display = DisplayStyle.None;
            HasPortrait = true;
        }

        private VisualElement BuildPortraitPane(string npcName, string portraitPath)
        {
            var left = new VisualElement();
            left.style.width = 120;
            left.style.flexShrink = 0;

            _portraitBox = new VisualElement();
            _portraitBox.style.width = 120;
            _portraitBox.style.height = 150;
            _portraitBox.style.backgroundColor = InputBg;
            Border(_portraitBox, Alpha(Ink, 0.35f), 1);
            Radius(_portraitBox, 12);
            _portraitBox.style.alignItems = Align.Center;
            _portraitBox.style.justifyContent = Justify.FlexEnd;
            _portraitGlyph = Text(string.IsNullOrWhiteSpace(npcName) ? "?" : npcName.Substring(0, 1).ToUpperInvariant(), Serif, 78, Alpha(Ink, 0.22f));
            _portraitBox.Add(_portraitGlyph);
            left.Add(_portraitBox);

            var name = Text((npcName ?? "Unknown").ToUpperInvariant(), Sans, 11, Ink, FontStyle.Bold);
            name.style.letterSpacing = 0.6f;
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            name.style.marginTop = 8;
            left.Add(name);

            if (!string.IsNullOrWhiteSpace(portraitPath))
            {
                var path = Text("PORTRAIT LINKED", Sans, 9, Alpha(Ink, 0.28f));
                path.style.letterSpacing = 0.8f;
                path.style.unityTextAlign = TextAnchor.MiddleCenter;
                path.style.marginTop = 4;
                left.Add(path);
            }

            return left;
        }

        private Button BuildTopicButton(int index, DialogTopicOption topic, Action<string> onTopic)
        {
            var button = new Button(() =>
            {
                onTopic?.Invoke(topic.Id);
                _responseLabel.text = "You ask about " + topic.Label + ".";
            })
            { text = index + ". Ask about " + topic.Label };
            ResetButton(button);
            button.style.width = Length.Percent(100);
            button.style.height = 38;
            button.style.marginBottom = 6;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.backgroundColor = Alpha(Ink, 0.06f);
            button.style.color = Ink;
            button.style.fontSize = 14;
            ApplyFont(button, Sans);
            Border(button, Alpha(Ink, 0.14f), 1);
            Radius(button, 8);
            return button;
        }
    }
}
