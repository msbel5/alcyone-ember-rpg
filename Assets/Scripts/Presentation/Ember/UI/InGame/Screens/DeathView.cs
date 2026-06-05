using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class DeathView
    {
        private readonly VisualElement _overlay;

        public DeathView(VisualElement stageCanvas, Action onClose, Action onLoadLastSave = null, Action onMainMenu = null)
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = C(2, 1, 1, 0.97f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _overlay.pickingMode = PickingMode.Position;

            var wrap = new VisualElement();
            wrap.style.maxWidth = 620;
            wrap.style.paddingLeft = 40;
            wrap.style.paddingRight = 40;
            wrap.style.alignItems = Align.Center;
            _overlay.Add(wrap);

            var top = Text("YOU HAVE FALLEN.", Serif, 11, Alpha(Health, 0.60f), FontStyle.Bold);
            top.style.letterSpacing = 8f;
            top.style.unityTextAlign = TextAnchor.MiddleCenter;
            top.style.marginBottom = 28;
            wrap.Add(top);

            var p1 = Text(IgMockData.Player.Name + " lies still. The path from here belongs to memory alone.", Serif, 24, PA(0.62f), FontStyle.Italic);
            p1.style.whiteSpace = WhiteSpace.Normal;
            p1.style.unityTextAlign = TextAnchor.MiddleCenter;
            wrap.Add(p1);

            var p2 = Text("What remains of your choices waits in the embers.", Serif, 16, PA(0.38f));
            p2.style.whiteSpace = WhiteSpace.Normal;
            p2.style.unityTextAlign = TextAnchor.MiddleCenter;
            p2.style.marginTop = 14;
            p2.style.marginBottom = 40;
            wrap.Add(p2);

            var actions = Row();
            var load = new Button(() => onLoadLastSave?.Invoke()) { text = "LOAD LAST SAVE" };
            ResetButton(load);
            load.style.height = 46;
            load.style.paddingLeft = 32;
            load.style.paddingRight = 32;
            load.style.backgroundColor = Gold;
            load.style.color = Ink;
            load.style.fontSize = 13;
            load.style.letterSpacing = 1.2f;
            load.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(load, Sans);
            Radius(load, 10);
            actions.Add(load);

            var menu = new Button(() => onMainMenu?.Invoke()) { text = "MAIN MENU" };
            ResetButton(menu);
            menu.style.height = 46;
            menu.style.paddingLeft = 32;
            menu.style.paddingRight = 32;
            menu.style.backgroundColor = Alpha(InputBg, 0.70f);
            menu.style.color = PA(0.45f);
            menu.style.fontSize = 13;
            menu.style.letterSpacing = 1f;
            ApplyFont(menu, Sans);
            Border(menu, PA(0.18f), 1);
            Radius(menu, 10);
            actions.Add(menu);
            wrap.Add(actions);

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }
    }
}
