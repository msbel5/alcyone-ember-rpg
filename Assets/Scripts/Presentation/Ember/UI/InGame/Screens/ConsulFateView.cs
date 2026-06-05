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
        private Label _line;   // the Oracle's current spoken line (controller streams the real prophecy in)

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

        private static VisualElement BuildOraclePane()
        {
            var pane = new VisualElement();
            pane.style.width = 220;
            pane.style.flexShrink = 0;
            pane.style.alignItems = Align.Center;

            var portrait = new VisualElement();
            portrait.style.width = 220;
            portrait.style.height = 280;
            portrait.style.backgroundColor = Alpha(Panel, 0.65f);
            Border(portrait, Amber, 2);
            Radius(portrait, 14);
            portrait.style.alignItems = Align.Center;
            portrait.style.justifyContent = Justify.Center;
            portrait.style.flexDirection = FlexDirection.Column;
            portrait.Add(BuildRing());
            var wait = Text("Awaiting\nForge Portrait", Serif, 11, GA(0.38f), FontStyle.Italic);
            wait.style.unityTextAlign = TextAnchor.MiddleCenter;
            wait.style.marginTop = 14;
            portrait.Add(wait);
            pane.Add(portrait);

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

            var thread = new ScrollView();
            thread.style.flexGrow = 1;
            thread.style.minHeight = 0;
            pane.Add(thread);

            // No mock thread: the real prophecy streams into the oracle line above (SetOracleLine) as the player asks.

            var askRow = Row();
            askRow.style.height = 48;
            var field = new TextField();
            field.value = "Ask the Oracle something…";
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

            var ask = new Button(() => onAsk?.Invoke(field.value)) { text = "ASK" };
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

        private static VisualElement BuildThreadPair(string question, string answer, bool loading)
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
            aBubble.Add(Text(answer, Serif, loading ? 13 : 15, loading ? GA(0.45f) : ParchDim, FontStyle.Italic));
            wrap.Add(aBubble);
            return wrap;
        }
    }
}
