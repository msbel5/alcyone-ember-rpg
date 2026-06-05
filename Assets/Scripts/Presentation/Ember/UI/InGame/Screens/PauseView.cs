using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class PauseView
    {
        private readonly VisualElement _overlay;

        public PauseView(VisualElement stageCanvas, Action onClose, Action<string> onOpenScreen = null,
            Action onSettings = null, Action onMainMenu = null)
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = C(6, 5, 3, 0.78f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _overlay.pickingMode = PickingMode.Position;

            var panel = new VisualElement();
            panel.style.width = 320;
            panel.style.backgroundColor = Alpha(VoidWarm, 0.96f);
            Border(panel, PA(0.20f), 1);
            Radius(panel, 18);
            panel.style.paddingTop = 36;
            panel.style.paddingBottom = 36;
            panel.style.paddingLeft = 28;
            panel.style.paddingRight = 28;
            panel.style.alignItems = Align.Center;
            _overlay.Add(panel);

            var word = Text("EMBER", Sans, 42, Gold, FontStyle.Bold);
            word.style.letterSpacing = 8f;
            word.style.marginBottom = 32;
            panel.Add(word);

            string[] items = { "Resume", "Inventory", "Character", "Save Game", "Load Game", "Settings", "Main Menu" };
            for (int i = 0; i < items.Length; i++)
                panel.Add(BuildMenuButton(items[i], i == 0, i == items.Length - 1, onClose, onOpenScreen, onSettings, onMainMenu));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static Button BuildMenuButton(string text, bool primary, bool dim, Action onClose,
            Action<string> onOpenScreen, Action onSettings, Action onMainMenu)
        {
            var button = new Button(() =>
            {
                if (text == "Resume") onClose?.Invoke();
                else if (text == "Inventory") onOpenScreen?.Invoke("inventory");
                else if (text == "Character") onOpenScreen?.Invoke("character");
                else if (text == "Save Game") onOpenScreen?.Invoke("savegame");
                else if (text == "Load Game") onOpenScreen?.Invoke("savegame");
                else if (text == "Settings") onSettings?.Invoke();
                else if (text == "Main Menu") onMainMenu?.Invoke();
            }) { text = text };
            ResetButton(button);
            button.style.width = Length.Percent(100);
            button.style.height = 48;
            button.style.marginBottom = 6;
            button.style.backgroundColor = primary ? Alpha(Panel, 0.92f) : dim ? Alpha(InputBg, 0.50f) : Dark(0.50f);
            button.style.color = primary ? Bone : dim ? PA(0.38f) : PA(0.60f);
            if (primary) button.style.backgroundColor = Gold;
            if (primary) button.style.color = Ink;
            Border(button, primary ? Amber : PA(0.10f), 1);
            Radius(button, 10);
            button.style.fontSize = 16;
            button.style.letterSpacing = 0.6f;
            button.style.unityFontStyleAndWeight = primary ? FontStyle.Bold : FontStyle.Normal;
            ApplyFont(button, Sans);
            return button;
        }
    }
}
