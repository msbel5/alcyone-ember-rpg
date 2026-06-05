// Why this file is intentionally long: the complete Oracle surface keeps portrait state, question thread, and async answer presentation together so the in-game design stays in one readable UI Toolkit view.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class ConsulFateView
    {
        private readonly VisualElement _overlay;
        private readonly List<ThreadEntryView> _threadEntries = new List<ThreadEntryView>();
        private Label _line;   // the Oracle's current spoken line (controller streams the real prophecy in)
        private VisualElement _portraitBox;
        private Image _portraitImage;
        private VisualElement _portraitRing;
        private Label _portraitWait;
        private ScrollView _thread;

        public bool HasPortrait { get; private set; }

        public ConsulFateView(VisualElement stageCanvas, Action onClose, Action<string> onAsk = null)
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = C(4, 3, 2, 0.92f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _overlay.style.flexDirection = FlexDirection.Column;
            _overlay.pickingMode = PickingMode.Position;

            var close = new Button(() => { Close(); onClose?.Invoke(); }) { text = "✕" };
            ResetButton(close);
            close.style.position = Position.Absolute;
            close.style.top = 28;
            close.style.right = 32;
            close.style.fontSize = 18;
            close.style.color = PA(0.30f);
            ApplyFont(close, Sans);
            _overlay.Add(close);

            var shell = Row();
            shell.style.width = Length.Percent(70);
            shell.style.maxWidth = 960;
            _overlay.Add(shell);

            // TODO(real-data): no host source yet.
            shell.Add(BuildOraclePane());
            shell.Add(BuildConversationPane(onAsk));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        /// <summary>Show the Oracle's line — the controller calls IConsultFateOracle.ConsultFate(question) on Ask
        /// and polls TryConsumeResolvedFate() each frame, streaming the real LLM prophecy in here.</summary>
        public void SetOracleLine(string text)
        {
            if (_line != null && !string.IsNullOrEmpty(text)) _line.text = text;
        }

        public void SetPortrait(Sprite sprite)
        {
            if (sprite == null || _portraitImage == null)
                return;

            _portraitImage.sprite = sprite;
            _portraitImage.style.display = DisplayStyle.Flex;
            if (_portraitRing != null)
                _portraitRing.style.display = DisplayStyle.None;
            if (_portraitWait != null)
                _portraitWait.style.display = DisplayStyle.None;
            HasPortrait = true;
        }

        public void BeginQuestion(string question)
        {
            if (string.IsNullOrWhiteSpace(question) || _thread == null)
                return;

            var entry = BuildThreadPair(question.Trim(), "The Oracle contemplates…", true);
            _threadEntries.Add(entry);
            _thread.Add(entry.Root);
            ScrollThreadTo(entry.Root);
        }

        public void ResolveLatestAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return;

            for (int i = _threadEntries.Count - 1; i >= 0; i--)
            {
                if (_threadEntries[i].Loading)
                {
                    _threadEntries[i].SetAnswer(answer, false);
                    ScrollThreadTo(_threadEntries[i].Root);
                    return;
                }
            }
        }

        private VisualElement BuildOraclePane()
        {
            var pane = new VisualElement();
            pane.style.width = 220;
            pane.style.flexShrink = 0;
            pane.style.marginRight = 22;
            pane.style.alignItems = Align.Center;

            _portraitBox = new VisualElement();
            _portraitBox.style.width = 220;
            _portraitBox.style.height = 280;
            _portraitBox.style.backgroundColor = Alpha(Panel, 0.65f);
            Border(_portraitBox, Amber, 2);
            Radius(_portraitBox, 14);
            _portraitBox.style.overflow = Overflow.Hidden;
            _portraitBox.style.alignItems = Align.Center;
            _portraitBox.style.justifyContent = Justify.Center;
            _portraitBox.style.flexDirection = FlexDirection.Column;

            _portraitImage = new Image();
            _portraitImage.scaleMode = ScaleMode.ScaleToFit;
            _portraitImage.style.position = Position.Absolute;
            _portraitImage.style.left = 0;
            _portraitImage.style.right = 0;
            _portraitImage.style.top = 0;
            _portraitImage.style.bottom = 0;
            _portraitImage.style.display = DisplayStyle.None;
            _portraitBox.Add(_portraitImage);

            _portraitRing = BuildRing();
            _portraitBox.Add(_portraitRing);
            _portraitWait = Text("Awaiting\nForge Portrait", Serif, 11, GA(0.38f), FontStyle.Italic);
            _portraitWait.style.unityTextAlign = TextAnchor.MiddleCenter;
            _portraitWait.style.marginTop = 14;
            _portraitBox.Add(_portraitWait);
            pane.Add(_portraitBox);

            var title = Text(IgMockData.Oracle.Name, Serif, 15, Gold, FontStyle.Bold);
            title.style.marginTop = 12;
            pane.Add(title);
            pane.Add(Text(IgMockData.Oracle.Subtitle, Serif, 12, GA(0.45f), FontStyle.Italic));
            return pane;
        }

        private VisualElement BuildConversationPane(Action<string> onAsk)
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.maxHeight = 580;

            var head = Text("CONSUL FATE · THE ORACLE SPEAKS", Sans, 9, Amber, FontStyle.Bold);
            head.style.letterSpacing = 2.8f;
            pane.Add(head);

            _line = Text("The Oracle awaits. Ask, and the fates will answer.", Serif, 20, ParchDim, FontStyle.Italic);
            _line.style.whiteSpace = WhiteSpace.Normal;
            pane.Add(_line);

            var prompts = Row();
            prompts.style.flexWrap = Wrap.Wrap;
            for (int i = 0; i < IgMockData.OraclePrompts.Length; i++)
            {
                var chip = new VisualElement();
                chip.style.marginRight = 8;
                chip.style.marginBottom = 8;
                chip.style.paddingTop = 6;
                chip.style.paddingBottom = 6;
                chip.style.paddingLeft = 16;
                chip.style.paddingRight = 16;
                chip.style.backgroundColor = Alpha(Panel, 0.55f);
                Border(chip, GA(0.20f), 1);
                Radius(chip, 20);
                var promptText = IgMockData.OraclePrompts[i];
                chip.Add(Text(promptText, Serif, 13, GA(0.60f), FontStyle.Italic));
                chip.AddManipulator(new Clickable(() => onAsk?.Invoke(promptText)));   // suggested question → real Oracle
                prompts.Add(chip);
            }
            pane.Add(prompts);

            _thread = new ScrollView();
            _thread.style.flexGrow = 1;
            _thread.style.minHeight = 0;
            pane.Add(_thread);

            var askRow = Row();
            askRow.style.height = 48;
            var field = new TextField();
            field.value = string.Empty;
            field.style.flexGrow = 1;
            field.style.height = 48;
            field.style.color = Bone;
            field.style.borderTopWidth = 1;
            field.style.borderBottomWidth = 1;
            field.style.borderLeftWidth = 1;
            field.style.borderRightWidth = 1;
            field.style.borderTopColor = GA(0.30f);
            field.style.borderBottomColor = GA(0.30f);
            field.style.borderLeftColor = GA(0.30f);
            field.style.borderRightColor = GA(0.30f);
            Radius(field, 10);
            ApplyFont(field, Serif);
            var input = field.Q("unity-text-input");
            if (input != null)
            {
                input.style.backgroundColor = Alpha(InputBg, 0.85f);
                input.style.color = Bone;
                input.style.fontSize = 15;
                ApplyFont(input, Serif);
            }
            askRow.Add(field);

            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                    return;

                SubmitAsk(field, onAsk);
                evt.StopPropagation();
            });

            var ask = new Button(() => SubmitAsk(field, onAsk)) { text = "ASK" };
            ResetButton(ask);
            ask.style.width = 104;
            ask.style.height = 48;
            ask.style.backgroundColor = Gold;
            ask.style.color = Ink;
            ask.style.fontSize = 12;
            ask.style.letterSpacing = 1.2f;
            ask.style.unityFontStyleAndWeight = FontStyle.Bold;
            Border(ask, Amber, 1);
            Radius(ask, 10);
            ApplyFont(ask, Sans);
            askRow.Add(ask);
            pane.Add(askRow);
            return pane;
        }

        private static VisualElement BuildRing()
        {
            var ring = new VisualElement();
            ring.style.width = 80;
            ring.style.height = 80;
            Border(ring, GA(0.55f), 2);
            Radius(ring, 999);
            ring.style.alignItems = Align.Center;
            ring.style.justifyContent = Justify.Center;
            ring.Add(Text("⌖", Serif, 32, Amber));
            return ring;
        }

        private void SubmitAsk(TextField field, Action<string> onAsk)
        {
            if (field == null || onAsk == null)
                return;

            var question = field.value == null ? string.Empty : field.value.Trim();
            if (string.IsNullOrEmpty(question))
                return;

            field.value = string.Empty;
            onAsk(question);
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
            wrap.style.paddingTop = 10;
            wrap.style.paddingBottom = 10;
            wrap.style.borderTopWidth = 1;
            wrap.style.borderTopColor = WA(0.06f);

            var qBubble = new VisualElement();
            qBubble.style.alignSelf = Align.FlexEnd;
            qBubble.style.maxWidth = Length.Percent(72);
            qBubble.style.marginLeft = StyleKeyword.Auto;
            qBubble.style.paddingTop = 8;
            qBubble.style.paddingBottom = 8;
            qBubble.style.paddingLeft = 14;
            qBubble.style.paddingRight = 14;
            qBubble.style.backgroundColor = Alpha(Panel, 0.75f);
            Border(qBubble, GA(0.28f), 1);
            Radius(qBubble, 12);
            qBubble.Add(Text(question, Sans, 13, Parch));
            wrap.Add(qBubble);

            var aBubble = new VisualElement();
            aBubble.style.marginTop = 6;
            aBubble.style.maxWidth = Length.Percent(82);
            aBubble.style.paddingTop = 8;
            aBubble.style.paddingBottom = 8;
            aBubble.style.paddingLeft = 14;
            aBubble.style.paddingRight = 14;
            aBubble.style.backgroundColor = Alpha(VoidWarm, 0.85f);
            Border(aBubble, loading ? GA(0.10f) : Alpha(Amber, 0.27f), 1);
            Radius(aBubble, 12);
            var answerLabel = Text(answer, Serif, loading ? 13 : 15, loading ? GA(0.45f) : ParchDim, FontStyle.Italic);
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
                _answerLabel.style.fontSize = loading ? 13 : 15;
                _answerLabel.style.color = loading ? GA(0.45f) : ParchDim;
                _answerBubble.style.borderTopColor = loading ? GA(0.10f) : Alpha(Amber, 0.27f);
                _answerBubble.style.borderBottomColor = loading ? GA(0.10f) : Alpha(Amber, 0.27f);
                _answerBubble.style.borderLeftColor = loading ? GA(0.10f) : Alpha(Amber, 0.27f);
                _answerBubble.style.borderRightColor = loading ? GA(0.10f) : Alpha(Amber, 0.27f);
            }
        }
    }
}
