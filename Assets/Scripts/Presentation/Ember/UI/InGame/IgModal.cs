using System;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame
{
    /// <summary>
    /// The full-screen modal frame every in-game screen (Inventory, Character, Spellbook, Journal, Map, Colony,
    /// Loot, Trade, Pause, Level Up, Death, Save/Load, Consul) renders inside — a 1:1 port of ig-ds.jsx
    /// <c>GameModal</c> / <c>TabbedModal</c>: a dark scrim, a bordered panel with a Cinzel title + ✕ close, and a
    /// content host. Screen views call <see cref="Build"/> or <see cref="BuildTabbed"/> and fill the returned
    /// content element. Builders here keep all 16 screens visually identical to the design with zero duplication.
    /// </summary>
    public static class IgModal
    {
        /// <summary>Build a modal frame. Returns the overlay (add it to the stage); <paramref name="content"/> is
        /// the element to fill with the screen body.</summary>
        public static VisualElement Build(string title, bool wide, Action onClose, out VisualElement content)
        {
            var overlay = new VisualElement { name = "IgModalOverlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = C(4, 3, 2, 0.86f);
            overlay.style.alignItems = Align.Center; overlay.style.justifyContent = Justify.Center;
            overlay.pickingMode = PickingMode.Position;  // eat clicks behind the modal

            var panel = new VisualElement { name = "IgModalPanel" };
            panel.style.width = Length.Percent(wide ? 96 : 78);
            panel.style.maxWidth = wide ? 1680 : 1240;
            panel.style.height = Length.Percent(88); panel.style.maxHeight = 900;
            panel.style.backgroundColor = C(26, 20, 16);   // ~#1A1410 → VoidWarm gradient (flat fill)
            Border(panel, PA(0.22f), 1); Radius(panel, 18);
            panel.style.overflow = Overflow.Hidden;
            panel.style.flexDirection = FlexDirection.Column;

            // title bar
            var bar = Row();
            bar.style.alignItems = Align.Center; bar.style.flexShrink = 0;
            bar.style.borderBottomWidth = 1; bar.style.borderBottomColor = PA(0.12f);
            bar.style.paddingTop = 16; bar.style.paddingBottom = 16; bar.style.paddingLeft = 28; bar.style.paddingRight = 28;
            var titleLabel = Text(title ?? "", Serif, 18, Parch, FontStyle.Bold);
            titleLabel.style.letterSpacing = 0.7f;
            bar.Add(titleLabel);
            var close = new Button(() => onClose?.Invoke()) { text = "✕" };
            ResetButton(close);
            close.style.marginLeft = StyleKeyword.Auto;
            close.style.fontSize = 18; close.style.color = PA(0.35f); ApplyFont(close, Sans);
            close.style.paddingLeft = 8; close.style.paddingRight = 8; close.style.paddingTop = 4; close.style.paddingBottom = 4;
            bar.Add(close);
            panel.Add(bar);

            // content host
            content = new VisualElement { name = "IgModalContent" };
            content.style.flexGrow = 1; content.style.minHeight = 0; content.style.overflow = Overflow.Hidden;
            panel.Add(content);

            overlay.Add(panel);
            return overlay;
        }

        /// <summary>A tabbed modal — adds a tab bar under the title. Returns the overlay; <paramref name="body"/>
        /// is filled per active tab by the caller, and <paramref name="onTab"/> fires when a tab is clicked.</summary>
        public static VisualElement BuildTabbed(string title, bool wide, string[] tabs, string activeTab,
            Action<string> onTab, Action onClose, out VisualElement body)
        {
            var overlay = Build(title, wide, onClose, out var content);
            content.style.flexDirection = FlexDirection.Column;

            var tabBar = Row();
            tabBar.style.flexShrink = 0;
            tabBar.style.borderBottomWidth = 1; tabBar.style.borderBottomColor = PA(0.10f);
            tabBar.style.paddingTop = 10; tabBar.style.paddingLeft = 24; tabBar.style.paddingRight = 24;
            foreach (var t in tabs)
            {
                var tab = t;
                bool active = string.Equals(t, activeTab, StringComparison.Ordinal);
                var b = new Button(() => onTab?.Invoke(tab)) { text = (t ?? "").ToUpperInvariant() };
                ResetButton(b);
                b.style.fontSize = 11; b.style.letterSpacing = 1.3f;
                b.style.unityFontStyleAndWeight = FontStyle.Bold; ApplyFont(b, Sans);
                b.style.color = active ? Gold : PA(0.40f);
                b.style.borderBottomWidth = 2; b.style.borderBottomColor = active ? Gold : Color.clear;
                b.style.paddingLeft = 14; b.style.paddingRight = 14; b.style.paddingTop = 8; b.style.paddingBottom = 10;
                tabBar.Add(b);
            }
            content.Add(tabBar);

            body = new VisualElement();
            body.style.flexGrow = 1; body.style.minHeight = 0; body.style.overflow = Overflow.Hidden;
            content.Add(body);
            return overlay;
        }
    }
}
