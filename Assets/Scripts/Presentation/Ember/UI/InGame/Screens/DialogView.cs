// Why this file is intentionally long: the full NPC dialog surface keeps portrait state, ask-about topics, free-text input, and floating thread history together so the approved in-game layout stays readable in one place.
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
        private readonly List<ThreadEntryView> _threadEntries = new List<ThreadEntryView>();
        private readonly ScrollView _thread;
        private readonly VisualElement _threadCard;
        private readonly Label _lineLabel;
        private VisualElement _portraitBox;
        private Label _portraitGlyph;

        public bool HasPortrait { get; private set; }
        public bool HasPendingResponse
        {
            get
            {
                for (int i = 0; i < _threadEntries.Count; i++)
                {
                    if (_threadEntries[i].Loading)
                        return true;
                }

                return false;
            }
        }

        public DialogView(VisualElement stageCanvas, Action onClose)
            : this(
                stageCanvas,
                onClose,
                "No Conversation",
                null,
                "No one is speaking with you right now.",
                Array.Empty<DialogTopicOption>(),
                _ => { },
                _ => { },
                null,
                onClose)
        {
        }

        public DialogView(
            VisualElement stageCanvas,
            Action onClose,
            string npcName,
            string portraitPath,
            string greeting,
            IReadOnlyList<DialogTopicOption> topics,
            Action<string> onTopic,
            Action<string> onFreeAsk,
            Action onTrade,
            Action onFarewell)
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

            _threadCard = BuildThreadCard();
            _thread = new ScrollView();
            _thread.style.maxHeight = 200;
            _thread.style.minHeight = 0;
            StyleScroll(_thread);   // slim gold themed scrollbar, not the default OS up/down arrows
            _threadCard.Add(_thread);
            _overlay.Add(_threadCard);

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
            top.Add(right);

            var hasTopics = topics != null && topics.Count > 0;
            var topicsPane = new VisualElement();
            topicsPane.style.borderTopWidth = 1;
            topicsPane.style.borderTopColor = Alpha(Ink, 0.12f);
            topicsPane.style.paddingTop = 10;
            panel.Add(topicsPane);

            if (hasTopics)
            {
                for (int i = 0; i < topics.Count; i++)
                    topicsPane.Add(BuildTopicButton(i + 1, topics[i], onTopic));
            }
            else
            {
                topicsPane.Add(EmptyState(
                    "Dialogue Idle",
                    "Open this screen from a real NPC interaction to see live topics and replies.",
                    "The standalone browser entry does not have a conversation source behind it."));
            }

            if (hasTopics)
                panel.Add(BuildFreeAskRow(npcName, onFreeAsk));

            var bottom = Row();
            bottom.style.justifyContent = Justify.SpaceBetween;
            bottom.style.alignItems = Align.Center;
            bottom.style.marginTop = 10;
            bottom.style.borderTopWidth = 1;
            bottom.style.borderTopColor = Alpha(Ink, 0.10f);
            bottom.style.paddingTop = 10;
            bottom.Add(Text(hasTopics ? "ESC · 1–4 topics · or type freely" : "ESC · Close", Sans, 11, Alpha(Ink, 0.38f)));

            var actions = Row();
            actions.style.alignItems = Align.Center;
            if (hasTopics && onTrade != null)
            {
                var trade = BuildActionButton("TRADE", Alpha(Gold, 0.18f), Amber, onTrade);
                trade.style.marginRight = 8;
                actions.Add(trade);
            }

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
            actions.Add(close);
            bottom.Add(actions);
            panel.Add(bottom);

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        public void SetResponseLine(string text) => ResolveLatestResponse(text);

        public void SetCurrentLine(string text)
        {
            if (_lineLabel != null && !string.IsNullOrEmpty(text))
                _lineLabel.text = text;
        }

        /// <summary>M3a: pour the growing streamed answer into the newest loading bubble.</summary>
        public void UpdateLatestLoading(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            for (int i = _threadEntries.Count - 1; i >= 0; i--)
            {
                if (_threadEntries[i].Loading)
                {
                    _threadEntries[i].SetAnswer(text, true); // keeps the loading styling until resolve
                    return;
                }
            }
        }

        public void SetPortrait(Sprite sprite)
        {
            if (sprite == null || _portraitBox == null)
                return;

            _portraitBox.style.backgroundImage = new StyleBackground(sprite);
            if (_portraitGlyph != null)
                _portraitGlyph.style.display = DisplayStyle.None;
            HasPortrait = true;
        }

        public void BeginQuestion(string question)
        {
            var trimmed = question == null ? string.Empty : question.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            _threadCard.style.display = DisplayStyle.Flex;
            var entry = BuildThreadPair(trimmed, "Thinking…", true);
            _threadEntries.Add(entry);
            _thread.Add(entry.Root);
            ScrollThreadTo(entry.Root);
        }

        public void ResolveLatestResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            for (int i = _threadEntries.Count - 1; i >= 0; i--)
            {
                if (_threadEntries[i].Loading)
                {
                    _threadEntries[i].SetAnswer(text, false);
                    ScrollThreadTo(_threadEntries[i].Root);
                    return;
                }
            }
        }

        private VisualElement BuildThreadCard()
        {
            var card = new VisualElement();
            card.style.width = Length.Percent(64);
            card.style.maxWidth = 860;
            card.style.maxHeight = 200;
            card.style.marginBottom = 10;
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
            card.style.backgroundColor = C(10, 8, 5, 0.90f);
            Border(card, PA(0.14f), 1);
            Radius(card, 14);
            card.style.display = DisplayStyle.None;
            return card;
        }

        private VisualElement BuildPortraitPane(string npcName, string portraitPath)
        {
            var left = new VisualElement();
            left.style.width = 120;
            left.style.flexShrink = 0;
            left.style.marginRight = 22;

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
                BeginQuestion("Ask about " + topic.Label);
                onTopic?.Invoke(topic.Id);
            })
            {
                text = index + ". Ask about " + topic.Label
            };
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

        private VisualElement BuildFreeAskRow(string npcName, Action<string> onFreeAsk)
        {
            var row = Row();
            row.style.marginTop = 8;
            row.style.marginBottom = 4;

            var field = new TextField();
            field.value = string.Empty;
            field.style.flexGrow = 1;
            field.style.height = 40;
            field.style.color = Ink;
            field.style.borderTopWidth = 1;
            field.style.borderBottomWidth = 1;
            field.style.borderLeftWidth = 1;
            field.style.borderRightWidth = 1;
            field.style.borderTopColor = Alpha(Ink, 0.22f);
            field.style.borderBottomColor = Alpha(Ink, 0.22f);
            field.style.borderLeftColor = Alpha(Ink, 0.22f);
            field.style.borderRightColor = Alpha(Ink, 0.22f);
            Radius(field, 8);
            ApplyFont(field, Serif);
            var input = field.Q("unity-text-input");
            if (input != null)
            {
                input.style.backgroundColor = Alpha(Ink, 0.09f);
                input.style.color = Ink;
                input.style.fontSize = 14;
                input.style.unityFontStyleAndWeight = FontStyle.Italic;
                ApplyFont(input, Serif);
            }

            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                    return;

                SubmitFreeAsk(field, onFreeAsk);
                evt.StopPropagation();
            });
            row.Add(field);

            var ask = new Button(() => SubmitFreeAsk(field, onFreeAsk)) { text = "ASK" };
            ResetButton(ask);
            ask.style.height = 40;
            ask.style.marginLeft = 8;
            ask.style.paddingLeft = 18;
            ask.style.paddingRight = 18;
            ask.style.backgroundColor = Gold;
            ask.style.color = Ink;
            ask.style.fontSize = 11;
            ask.style.letterSpacing = 1f;
            ask.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(ask, Sans);
            Border(ask, Amber, 1);
            Radius(ask, 8);
            row.Add(ask);

            var hint = Text("Ask " + ((npcName ?? "them").Split(' ')[0]) + " anything…", Serif, 12, Alpha(Ink, 0.36f), FontStyle.Italic);
            hint.style.position = Position.Absolute;
            hint.style.left = 16;
            hint.style.top = 10;
            hint.pickingMode = PickingMode.Ignore;
            row.Add(hint);

            field.RegisterValueChangedCallback(evt =>
            {
                hint.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
            });

            return row;
        }

        private void SubmitFreeAsk(TextField field, Action<string> onFreeAsk)
        {
            if (field == null || onFreeAsk == null)
                return;

            var question = field.value == null ? string.Empty : field.value.Trim();
            if (string.IsNullOrEmpty(question))
                return;

            field.value = string.Empty;
            BeginQuestion(question);
            onFreeAsk(question);
        }

        private static Button BuildActionButton(string text, Color background, Color border, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text };
            ResetButton(button);
            button.style.height = 32;
            button.style.paddingLeft = 16;
            button.style.paddingRight = 16;
            button.style.backgroundColor = background;
            button.style.color = Ink;
            button.style.fontSize = 12;
            button.style.letterSpacing = 0.7f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(button, Sans);
            Border(button, border, 1);
            Radius(button, 7);
            return button;
        }

        private void ScrollThreadTo(VisualElement target)
        {
            if (_thread == null || target == null)
                return;

            _thread.schedule.Execute(() => _thread.ScrollTo(target)).StartingIn(0);
        }

        private static ThreadEntryView BuildThreadPair(string question, string answer, bool loading)
        {
            var wrap = new VisualElement();
            wrap.style.paddingTop = 8;
            wrap.style.paddingBottom = 8;
            wrap.style.borderTopWidth = 1;
            wrap.style.borderTopColor = PA(0.07f);

            var qBubble = new VisualElement();
            qBubble.style.alignSelf = Align.FlexEnd;
            qBubble.style.maxWidth = Length.Percent(70);
            qBubble.style.marginLeft = StyleKeyword.Auto;
            qBubble.style.paddingTop = 5;
            qBubble.style.paddingBottom = 5;
            qBubble.style.paddingLeft = 12;
            qBubble.style.paddingRight = 12;
            qBubble.style.backgroundColor = C(60, 40, 10, 0.80f);
            Border(qBubble, C(154, 122, 18, 0.30f), 1);
            Radius(qBubble, 10);
            qBubble.Add(Text(question, Sans, 12, ParchDim));
            wrap.Add(qBubble);

            var aBubble = new VisualElement();
            aBubble.style.marginTop = 4;
            aBubble.style.maxWidth = Length.Percent(78);
            aBubble.style.paddingTop = 5;
            aBubble.style.paddingBottom = 5;
            aBubble.style.paddingLeft = 12;
            aBubble.style.paddingRight = 12;
            aBubble.style.backgroundColor = C(22, 16, 8, 0.70f);
            Border(aBubble, PA(0.10f), 1);
            Radius(aBubble, 10);
            var answerLabel = Text(answer, Serif, loading ? 11 : 13, loading ? C(230, 217, 179, 0.45f) : C(230, 217, 179, 0.75f), FontStyle.Italic);
            answerLabel.style.whiteSpace = WhiteSpace.Normal;
            aBubble.Add(answerLabel);
            wrap.Add(aBubble);

            return new ThreadEntryView(wrap, aBubble, answerLabel, loading);
        }

        private sealed class ThreadEntryView
        {
            private readonly VisualElement _answerBubble;
            private readonly Label _answerLabel;

            public ThreadEntryView(VisualElement root, VisualElement answerBubble, Label answerLabel, bool loading)
            {
                Root = root;
                _answerBubble = answerBubble;
                _answerLabel = answerLabel;
                Loading = loading;
            }

            public VisualElement Root { get; }
            public bool Loading { get; private set; }

            public void SetAnswer(string answer, bool loading)
            {
                Loading = loading;
                _answerLabel.text = answer;
                _answerLabel.style.fontSize = loading ? 11 : 13;
                _answerLabel.style.color = loading ? C(230, 217, 179, 0.45f) : C(230, 217, 179, 0.75f);
                var border = loading ? PA(0.10f) : Alpha(Amber, 0.27f);
                _answerBubble.style.borderTopColor = border;
                _answerBubble.style.borderBottomColor = border;
                _answerBubble.style.borderLeftColor = border;
                _answerBubble.style.borderRightColor = border;
            }
        }
    }
}
