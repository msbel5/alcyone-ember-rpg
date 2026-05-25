// Why this file is intentionally long: it is the single UI Toolkit adapter that maps the small UI abstraction onto PRD-specific runtime panel slots.
using System;
using System.Collections.Generic;
using EmberCrpg.Ui.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberCrpg.Ui.Backends.UiToolkit
{
    public sealed class UiToolkitPanel : IUiPanel
    {
        private readonly Dictionary<string, VisualElement> _slots = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Action> _buttonHandlers = new Dictionary<string, Action>();
        private readonly VisualElement _container;
        private readonly UiTokens _tokens;

        public UiToolkitPanel(string id, VisualElement root, UiTokens tokens)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "Panel" : id;
            Kind = Id;
            _tokens = tokens;
            _container = new VisualElement { name = Id };
            _container.AddToClassList("ember-panel");
            ApplyPanelStyle(_container);
            root?.Add(_container);
            BuildFrameFor(Id);
        }

        public string Id { get; }
        public string Kind { get; }

        public void SetText(string slot, string text)
        {
            var element = GetOrCreate(slot, CreateForSlot);
            switch (element)
            {
                case Button button:
                    button.text = text ?? string.Empty;
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
            var bar = GetTyped<ProgressBar>(slot, () => new ProgressBar { lowValue = 0f, highValue = 100f });
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

        public void SetImage(string slot, Texture2D texture) => SetThumbnail(slot, texture);
        public void AppendLog(string slot, UiLogSeverity severity, string line) => LogLine(slot, severity, line);

        public void Dispose()
        {
            _container?.RemoveFromHierarchy();
            _slots.Clear();
        }

        private void BuildFrameFor(string id)
        {
            if (id == "LoadingScreen" || id == "BootScreen")
            {
                Register("title", MakeLabel("", 30, true));
                Register("subtitle", MakeLabel("", 16, false));
                Register("area", MakeLabel("", 20, true));
                Register("current", MakeLabel("", 14, false));
                Register("progress", new ProgressBar { lowValue = 0f, highValue = 100f });
                Register("thumbnail", new Image());
                Register("caption", MakeLabel("", 12, false));
                Register("log", MakeLog());
                Register("continue", MakeButton("Continue"));
                return;
            }

            if (id == "CharacterCreation")
            {
                Register("header", MakeLabel("CHARACTER CREATION", 26, true));
                Register("step", MakeLabel("", 16, true));
                Register("progress", new ProgressBar { lowValue = 0f, highValue = 100f });
                Register("body", new ScrollView(ScrollViewMode.Vertical));
                Register("log", MakeLog());
                Register("back", MakeButton("Back"));
                Register("next", MakeButton("Continue"));
                return;
            }

            if (id == "TitleMenu")
            {
                Register("title", MakeLabel("EMBER CRPG", 34, true));
                Register("subtitle", MakeLabel("Visible generation cutover", 16, false));
                Register("new_game", MakeButton("New Game"));
                Register("continue", MakeButton("Continue"));
                Register("load", MakeButton("Load"));
                Register("options", MakeButton("Options"));
                Register("quit", MakeButton("Quit"));
                Register("version", MakeLabel("", 12, false));
                Register("status", MakeLabel("", 14, false));
                return;
            }

            if (id == "WorldgenView")
            {
                Register("title", MakeLabel("WORLD GENERATION", 24, true));
                Register("log", MakeLog());
                Register("question", MakeLabel("", 16, true));
                Register("answer0", MakeButton("Answer 1"));
                Register("answer1", MakeButton("Answer 2"));
                Register("answer2", MakeButton("Answer 3"));
                Register("answer3", MakeButton("Answer 4"));
                Register("auto", MakeButton("Auto-advance"));
                Register("continue", MakeButton("Continue"));
            }
        }

        private VisualElement CreateForSlot(string slot)
        {
            if (slot.Contains("button") || slot == "next" || slot == "back" || slot == "continue" || slot.StartsWith("answer")) return MakeButton(slot);
            if (slot.Contains("progress")) return new ProgressBar { lowValue = 0f, highValue = 100f };
            if (slot.Contains("thumbnail") || slot.Contains("portrait") || slot.Contains("image")) return new Image();
            if (slot == "log") return MakeLog();
            return MakeLabel(string.Empty, 14, false);
        }

        private T GetTyped<T>(string slot, Func<T> create) where T : VisualElement
        {
            var key = string.IsNullOrWhiteSpace(slot) ? "default" : slot;
            if (_slots.TryGetValue(key, out var existing) && existing is T typed) return typed;
            existing?.RemoveFromHierarchy();
            var element = create();
            Register(key, element);
            return element;
        }

        private VisualElement GetOrCreate(string slot, Func<string, VisualElement> create)
        {
            var key = string.IsNullOrWhiteSpace(slot) ? "default" : slot;
            if (_slots.TryGetValue(key, out var element)) return element;
            element = create(key);
            Register(key, element);
            return element;
        }

        private void Register(string key, VisualElement element)
        {
            element.name = key;
            _slots[key] = element;
            _container.Add(element);
        }

        private ScrollView MakeLog()
        {
            var log = new ScrollView(ScrollViewMode.Vertical);
            log.style.flexGrow = 1;
            log.style.minHeight = 140;
            log.style.maxHeight = 360;
            log.style.backgroundColor = new Color(0.03f, 0.025f, 0.02f, 0.86f);
            log.style.paddingLeft = 10;
            log.style.paddingRight = 10;
            log.style.paddingTop = 8;
            log.style.paddingBottom = 8;
            return log;
        }

        private Button MakeButton(string text)
        {
            var button = new Button { text = text ?? string.Empty };
            button.style.height = 42;
            button.style.marginTop = 4;
            button.style.marginBottom = 4;
            button.style.backgroundColor = _tokens != null ? _tokens.AccentMuted : new Color(0.35f, 0.19f, 0.08f);
            button.style.color = _tokens != null ? _tokens.Text : Color.white;
            return button;
        }

        private Label MakeLabel(string text, int size, bool strong)
        {
            var label = new Label(text ?? string.Empty);
            label.style.fontSize = size;
            label.style.unityFontStyleAndWeight = strong ? FontStyle.Bold : FontStyle.Normal;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = _tokens != null ? _tokens.Text : Color.white;
            label.style.marginBottom = 6;
            return label;
        }

        private void ApplyPanelStyle(VisualElement element)
        {
            element.style.flexGrow = 1;
            element.style.paddingLeft = 42;
            element.style.paddingRight = 42;
            element.style.paddingTop = 32;
            element.style.paddingBottom = 32;
            element.style.backgroundColor = _tokens != null ? _tokens.Background : new Color(0.045f, 0.035f, 0.028f);
            element.style.color = _tokens != null ? _tokens.Text : Color.white;
        }
    }
}
