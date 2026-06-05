using System;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class CombatView
    {
        private readonly VisualElement _overlay;

        public CombatView(VisualElement stageCanvas, Action onClose, Action<string> onAction = null, Action onFlee = null)
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = C(4, 3, 2, 0.72f);
            _overlay.pickingMode = PickingMode.Position;

            _overlay.Add(BuildTopNav(onFlee ?? onClose));

            var center = new VisualElement();
            center.style.position = Position.Absolute;
            center.style.left = 0;
            center.style.right = 0;
            center.style.top = 0;
            center.style.bottom = 0;
            center.style.alignItems = Align.Center;
            center.style.justifyContent = Justify.Center;
            center.Add(EmptyState(
                "Combat",
                "No active encounter is being projected here.",
                "This screen can only show live combatants once an encounter read model exists for the in-game browser."));
            _overlay.Add(center);

            var vitals = new VisualElement();
            vitals.style.position = Position.Absolute;
            vitals.style.left = 26;
            vitals.style.bottom = 92;
            vitals.style.width = 360;
            vitals.Add(Text(IgMockData.Player.Name, Sans, 13, Parch, FontStyle.Bold));

            var bars = Row();
            bars.style.marginTop = 8;
            bars.Add(BuildBar("HP", IgMockData.Player.Hp, IgMockData.Player.HpMax, Health));
            bars.Add(BuildBar("FAT", IgMockData.Player.Fatigue, IgMockData.Player.FatigueMax, Fatigue));
            bars.Add(BuildBar("MP", IgMockData.Player.Mana, IgMockData.Player.ManaMax, Mana));
            vitals.Add(bars);
            _overlay.Add(vitals);

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildTopNav(Action onClose)
        {
            var nav = Row();
            nav.style.position = Position.Absolute;
            nav.style.right = 26;
            nav.style.top = 14;
            nav.style.alignItems = Align.Center;
            var state = Text("NO ACTIVE ENCOUNTER", Sans, 11, PA(0.52f));
            state.style.letterSpacing = 1.4f;
            nav.Add(state);

            var close = new Button(() => onClose?.Invoke()) { text = "CLOSE" };
            ResetButton(close);
            close.style.height = 32;
            close.style.paddingLeft = 12;
            close.style.paddingRight = 12;
            close.style.backgroundColor = C(8, 6, 4, 0.70f);
            close.style.color = PA(0.38f);
            close.style.fontSize = 11;
            ApplyFont(close, Sans);
            Border(close, PA(0.15f), 1);
            Radius(close, 7);
            nav.Add(close);
            return nav;
        }

        private static VisualElement BuildBar(string label, int value, int max, Color color)
        {
            var root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.height = 20;
            root.style.backgroundColor = C(0, 0, 0, 0.4f);
            root.style.marginRight = 6;
            Radius(root, 999);

            var safeMax = Mathf.Max(1, max);
            var fill = new VisualElement();
            fill.style.position = Position.Absolute;
            fill.style.left = 2;
            fill.style.top = 2;
            fill.style.bottom = 2;
            fill.style.width = Length.Percent((float)value / safeMax * 100f);
            fill.style.backgroundColor = color;
            Radius(fill, 999);
            root.Add(fill);

            var text = Text(label + " " + value + "/" + max, Sans, 9, Bone, FontStyle.Bold);
            text.style.letterSpacing = 0.6f;
            text.style.position = Position.Absolute;
            text.style.left = 0;
            text.style.right = 0;
            text.style.top = 0;
            text.style.bottom = 0;
            text.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(text);
            return root;
        }
    }
}
