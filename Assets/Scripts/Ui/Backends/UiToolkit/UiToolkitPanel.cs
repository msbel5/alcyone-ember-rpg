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
        private readonly VisualElement _container;
        private readonly UiTokens _tokens;

        public UiToolkitPanel(string id, VisualElement root, UiTokens tokens)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "Panel" : id;
            Kind = Id;
            _tokens = tokens;
            _container = new VisualElement { name = Id };
            _container.style.flexGrow = 1;
            _container.style.backgroundColor = tokens != null ? tokens.Panel : Color.black;
            root?.Add(_container);
        }

        public string Id { get; }
        public string Kind { get; }

        public void SetText(string slot, string text)
        {
            GetLabel(slot).text = text ?? string.Empty;
        }

        public void SetProgress(string slot, float normalized)
        {
            var bar = GetProgress(slot);
            bar.value = Mathf.Clamp01(normalized) * 100f;
        }

        public void LogLine(string slot, UiLogSeverity severity, string line)
        {
            var label = new Label(line ?? string.Empty);
            label.style.color = _tokens != null ? _tokens.SeverityColor(severity) : Color.white;
            GetContainer(slot).Add(label);
        }

        public void SetThumbnail(string slot, Texture2D texture)
        {
            GetImage(slot).image = texture;
        }

        public void SetVisible(string slot, bool visible)
        {
            GetContainer(slot).style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetButtonHandler(string slot, Action onClick)
        {
            var button = GetButton(slot);
            button.clickable = new Clickable(() => onClick?.Invoke());
        }

        public void SetImage(string slot, Texture2D texture) => SetThumbnail(slot, texture);
        public void AppendLog(string slot, UiLogSeverity severity, string line) => LogLine(slot, severity, line);

        public void Dispose()
        {
            _container?.RemoveFromHierarchy();
            _slots.Clear();
        }

        private Label GetLabel(string slot) => GetOrCreate(slot, () => new Label()) as Label;
        private ProgressBar GetProgress(string slot) => GetOrCreate(slot, () => new ProgressBar { lowValue = 0f, highValue = 100f }) as ProgressBar;
        private Image GetImage(string slot) => GetOrCreate(slot, () => new Image()) as Image;
        private Button GetButton(string slot) => GetOrCreate(slot, () => new Button()) as Button;
        private VisualElement GetContainer(string slot) => GetOrCreate(slot, () => new VisualElement());

        private VisualElement GetOrCreate(string slot, Func<VisualElement> create)
        {
            var key = string.IsNullOrWhiteSpace(slot) ? "default" : slot;
            if (_slots.TryGetValue(key, out var element)) return element;
            element = create();
            element.name = key;
            _slots[key] = element;
            _container?.Add(element);
            return element;
        }
    }
}
