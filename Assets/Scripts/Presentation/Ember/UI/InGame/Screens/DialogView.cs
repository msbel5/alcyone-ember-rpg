using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class DialogView
    {
        private readonly VisualElement _overlay;

        public DialogView(VisualElement stageCanvas, Action onClose)
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

            _overlay.Add(BuildThreadCard());
            _overlay.Add(BuildMainPanel(onClose));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildThreadCard()
        {
            var card = new VisualElement();
            card.style.width = Length.Percent(64);
            card.style.maxWidth = 860;
            card.style.maxHeight = 200;
            card.style.backgroundColor = C(10, 8, 5, 0.90f);
            Border(card, PA(0.14f), 1);
            Radius(card, 14);
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;

            string[] qs = { "What did the caravan carry?", "Who else knows about the road?" };
            string[] answers =
            {
                "Mostly iron, if the tally was honest. A little cloth. Nothing worth dying for, unless someone knew more than I did.",
                "The captain. The raven-feeders at the gate. And maybe the smith himself, if you press him harder than the rest have.",
            };

            for (int i = 0; i < qs.Length; i++)
            {
                var pair = new VisualElement();
                pair.style.paddingTop = 8;
                pair.style.paddingBottom = 8;
                if (i > 0)
                {
                    pair.style.borderTopWidth = 1;
                    pair.style.borderTopColor = PA(0.07f);
                }

                var q = new VisualElement();
                q.style.marginLeft = StyleKeyword.Auto;
                q.style.maxWidth = Length.Percent(70);
                q.style.backgroundColor = C(60, 40, 10, 0.80f);
                Border(q, C(154, 122, 18, 0.30f), 1);
                q.style.paddingTop = 5;
                q.style.paddingBottom = 5;
                q.style.paddingLeft = 12;
                q.style.paddingRight = 12;
                q.style.borderTopLeftRadius = 10;
                q.style.borderTopRightRadius = 10;
                q.style.borderBottomLeftRadius = 3;
                q.style.borderBottomRightRadius = 10;
                q.Add(Text(qs[i], Sans, 12, ParchDim));
                pair.Add(q);

                var a = new VisualElement();
                a.style.marginTop = 4;
                a.style.maxWidth = Length.Percent(78);
                a.style.backgroundColor = C(22, 16, 8, 0.70f);
                Border(a, PA(0.10f), 1);
                a.style.paddingTop = 5;
                a.style.paddingBottom = 5;
                a.style.paddingLeft = 12;
                a.style.paddingRight = 12;
                a.style.borderTopLeftRadius = 3;
                a.style.borderTopRightRadius = 10;
                a.style.borderBottomLeftRadius = 10;
                a.style.borderBottomRightRadius = 10;
                var copy = Text(answers[i], Serif, 13, PA(0.75f), FontStyle.Italic);
                copy.style.whiteSpace = WhiteSpace.Normal;
                a.Add(copy);
                pair.Add(a);
                card.Add(pair);
            }

            return card;
        }

        private static VisualElement BuildMainPanel(Action onClose)
        {
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

            var top = Row();
            top.style.marginBottom = 14;
            panel.Add(top);

            var left = new VisualElement();
            left.style.width = 120;
            left.style.flexShrink = 0;
            var portrait = new VisualElement();
            portrait.style.width = 120;
            portrait.style.height = 150;
            portrait.style.backgroundColor = InputBg;
            Border(portrait, Alpha(Ink, 0.35f), 1);
            Radius(portrait, 12);
            portrait.style.alignItems = Align.Center;
            portrait.style.justifyContent = Justify.FlexEnd;
            portrait.Add(Text("G", Serif, 78, Alpha(Ink, 0.22f)));
            left.Add(portrait);
            var name = Text(IgMockData.DialogNpc.Name.ToUpperInvariant(), Sans, 11, Ink, FontStyle.Bold);
            name.style.letterSpacing = 0.6f;
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            name.style.marginTop = 8;
            left.Add(name);
            top.Add(left);

            var right = new VisualElement();
            right.style.flexGrow = 1;
            var line = Text(IgMockData.DialogNpc.Greeting, Serif, 17, Ink);
            line.style.whiteSpace = WhiteSpace.Normal;
            line.style.unityFontStyleAndWeight = FontStyle.Italic;
            line.style.minHeight = 60;
            right.Add(line);
            top.Add(right);

            var topics = new VisualElement();
            topics.style.borderTopWidth = 1;
            topics.style.borderTopColor = Alpha(Ink, 0.12f);
            topics.style.paddingTop = 10;
            panel.Add(topics);
            for (int i = 0; i < IgMockData.DialogNpc.Topics.Length; i++)
            {
                var row = Text($"{i + 1}. Ask about {IgMockData.DialogNpc.Topics[i].Topic}", Sans, 15, Ink);
                row.style.paddingTop = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 8;
                row.style.paddingRight = 8;
                topics.Add(row);
            }

            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = Alpha(Ink, 0.14f);
            divider.style.marginTop = 8;
            divider.style.marginBottom = 8;
            panel.Add(divider);

            var ask = Row();
            var field = new TextField();
            field.value = $"Ask {IgMockData.DialogNpc.Name.Split(' ')[0]} anything…";
            field.style.flexGrow = 1;
            field.style.height = 40;
            field.style.color = Ink;
            Border(field, Alpha(Ink, 0.22f), 1);
            Radius(field, 8);
            var inner = field.Q("unity-text-input");
            if (inner != null)
            {
                inner.style.backgroundColor = Alpha(Ink, 0.09f);
                inner.style.color = Ink;
                inner.style.fontSize = 14;
                ApplyFont(inner, Serif);
            }
            ask.Add(field);

            var askBtn = new Button { text = "ASK" };
            ResetButton(askBtn);
            askBtn.style.width = 84;
            askBtn.style.height = 40;
            askBtn.style.backgroundColor = Gold;
            askBtn.style.color = Ink;
            askBtn.style.fontSize = 11;
            askBtn.style.letterSpacing = 1f;
            askBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(askBtn, Sans);
            Border(askBtn, Amber, 1);
            Radius(askBtn, 8);
            ask.Add(askBtn);
            panel.Add(ask);

            var bottom = Row();
            bottom.style.justifyContent = Justify.SpaceBetween;
            bottom.style.alignItems = Align.Center;
            bottom.style.marginTop = 10;
            bottom.style.borderTopWidth = 1;
            bottom.style.borderTopColor = Alpha(Ink, 0.10f);
            bottom.style.paddingTop = 10;
            bottom.Add(Text("ESC · 1–4 topics · or type freely", Sans, 11, Alpha(Ink, 0.38f)));

            var close = new Button(() => onClose?.Invoke()) { text = "FAREWELL" };
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

            return panel;
        }
    }
}
