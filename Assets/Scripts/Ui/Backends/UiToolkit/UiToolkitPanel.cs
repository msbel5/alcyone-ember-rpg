// Why this file is intentionally long: it is the single UI Toolkit adapter that maps the small UI abstraction onto PRD-specific runtime panel slots.
using System;
using System.Collections.Generic;
using EmberCrpg.Ui.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberCrpg.Ui.Backends.UiToolkit
{
    public sealed partial class UiToolkitPanel : IUiPanel
    {
        private readonly Dictionary<string, VisualElement> _slots = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Action> _buttonHandlers = new Dictionary<string, Action>();
        // Slot-key prefix -> parent container. Lets dynamic slots (e.g. class_button_3) land in a
        // specific column instead of the flat panel root, so grouped layouts don't overlap.
        private readonly List<KeyValuePair<string, VisualElement>> _slotPrefixParents = new List<KeyValuePair<string, VisualElement>>();
        private readonly VisualElement _container;
        private readonly UiTokens _tokens;

        public UiToolkitPanel(string id, VisualElement root, UiTokens tokens)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "Panel" : id;
            _tokens = tokens;
            _container = new VisualElement { name = Id };
            _container.AddToClassList("ember-panel");
            ApplyPanelStyle(_container);
            root?.Add(_container);
            BuildFrameFor(Id);
        }

        public string Id { get; }

        public void SetText(string slot, string text)
        {
            var element = GetOrCreate(slot, CreateForSlot);
            switch (element)
            {
                case Button button:
                    button.text = text ?? string.Empty;
                    // Selection feedback (design system: selection is a gold OUTLINE, not a gold
                    // flood). "[X] ..." rows get a warmer fill + bright gold hairline + ember-gold
                    // text; "[ ] ..." stay muted panel-brown with the faint gold hairline.
                    if ((text ?? string.Empty).StartsWith("[X]", StringComparison.Ordinal))
                    {
                        button.style.backgroundColor = _tokens != null ? _tokens.PanelBrownHover : new Color(0.227f, 0.18f, 0.114f);
                        button.style.color = _tokens != null ? _tokens.EmberGold : new Color(1f, 0.851f, 0.298f);
                        SetHairline(button, _tokens != null ? _tokens.StateSelect : new Color(0.914f, 0.788f, 0.227f));
                    }
                    else if ((text ?? string.Empty).StartsWith("[ ]", StringComparison.Ordinal))
                    {
                        button.style.backgroundColor = _tokens != null ? _tokens.PanelBrown : new Color(0.18f, 0.14f, 0.09f);
                        button.style.color = _tokens != null ? _tokens.Parchment : new Color(0.949f, 0.859f, 0.62f);
                        SetHairline(button, _tokens != null ? _tokens.GoldHairline : new Color(0.949f, 0.859f, 0.62f, 0.22f));
                    }
                    break;
                case TextField field:
                    field.value = text ?? string.Empty;
                    break;
                case Label label:
                    label.text = text ?? string.Empty;
                    break;
                default:
                    element.Clear();
                    element.Add(MakeLabel(text ?? string.Empty, 14, false));
                    break;
            }
        }

        public void SetProgress(string slot, float normalized)
        {
            var bar = GetTyped<ProgressBar>(slot, () => MakeProgress());
            bar.value = Mathf.Clamp01(normalized) * 100f;
        }

        public void LogLine(string slot, UiLogSeverity severity, string line)
        {
            var label = MakeLabel(line ?? string.Empty, 13, false);
            label.style.color = _tokens != null ? _tokens.SeverityColor(severity) : Color.white;
            label.style.whiteSpace = WhiteSpace.Normal;
            var log = GetTyped<ScrollView>(slot, () => new ScrollView(ScrollViewMode.Vertical));
            log.Add(label);
            log.ScrollTo(label);
        }

        public void SetThumbnail(string slot, Texture2D texture)
        {
            GetTyped<Image>(slot, () => new Image()).image = texture;
        }

        // Show/hide the whole panel container. The surface uses this to keep one active panel
        // (full-screen screens shouldn't stack on top of each other).
        public void SetSurfaceVisible(bool visible)
        {
            if (_container != null)
                _container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetThumbnailGrid(string slot, System.Collections.Generic.IReadOnlyList<Texture2D> textures)
        {
            // The slot is expected to be (or become) a VisualElement container. Children are
            // wiped and rebuilt every call so callers can re-publish as new PNGs land on disk.
            var container = GetOrCreate(slot, _ =>
            {
                var v = new VisualElement();
                v.style.flexDirection = FlexDirection.Row;
                v.style.flexWrap = Wrap.Wrap;
                v.style.justifyContent = Justify.Center;
                v.style.marginTop = 8;
                return v;
            });
            container.Clear();
            if (textures == null) return;
            for (int i = 0; i < textures.Count; i++)
            {
                var tex = textures[i];
                if (tex == null) continue;
                var img = new Image { image = tex };
                img.style.width = 40;
                img.style.height = 40;
                img.style.marginLeft = 3;
                img.style.marginRight = 3;
                img.style.marginTop = 3;
                img.style.marginBottom = 3;
                img.scaleMode = ScaleMode.ScaleToFit;
                container.Add(img);
            }
        }

        public void SetVisible(string slot, bool visible)
        {
            GetOrCreate(slot, CreateForSlot).style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetButtonHandler(string slot, Action onClick)
        {
            var button = GetTyped<Button>(slot, () => new Button());
            var key = string.IsNullOrWhiteSpace(slot) ? "default" : slot;
            if (_buttonHandlers.TryGetValue(key, out var existing))
                button.clicked -= existing;
            if (onClick == null)
            {
                _buttonHandlers.Remove(key);
                return;
            }

            _buttonHandlers[key] = onClick;
            button.clicked += onClick;
        }

        public void Dispose()
        {
            _container?.RemoveFromHierarchy();
            _slots.Clear();
        }

    }
}
