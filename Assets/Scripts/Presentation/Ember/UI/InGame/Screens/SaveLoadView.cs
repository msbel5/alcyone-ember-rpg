using System;
using EmberCrpg.Data.Save;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class SaveLoadView
    {
        private readonly VisualElement _overlay;
        private readonly VisualElement _body;
        private readonly Action<SaveSlotId> _onSaveSlot;
        private readonly Action<SaveSlotId> _onLoadSlot;
        private readonly string _activeTab;

        public SaveLoadView(
            VisualElement stageCanvas,
            Action onClose,
            Action<SaveSlotId> onSaveSlot = null,
            Action<SaveSlotId> onLoadSlot = null,
            string activeTab = "Save")
        {
            _onSaveSlot = onSaveSlot;
            _onLoadSlot = onLoadSlot;
            _activeTab = activeTab ?? "Save";
            _overlay = IgModal.BuildTabbed(
                "Save / Load",
                false,
                new[] { "Save", "Load" },
                _activeTab,
                tab => { Close(); new SaveLoadView(stageCanvas, onClose, onSaveSlot, onLoadSlot, tab); },
                () => { Close(); onClose?.Invoke(); },
                out _body);

            stageCanvas.Add(_overlay);
            Refresh();
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        public void Refresh()
        {
            _body.Clear();
            _body.style.flexDirection = FlexDirection.Column;
            var data = IgSaveLoadData.Current ?? IgSaveLoadData.Default;

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.minHeight = 0;
            StyleScroll(scroll);
            _body.Add(scroll);

            if (data.Slots == null || data.Slots.Length == 0)
            {
                scroll.Add(EmptyState(_activeTab, "No save slots are available yet.", "The save repository is not exposed in this scene."));
            }
            else
            {
                for (int i = 0; i < data.Slots.Length; i++)
                    scroll.Add(BuildSlot(data.Slots[i]));
            }

            var status = Text(data.StatusLine, Sans, 12, data.StatusLine.StartsWith("Saved", StringComparison.Ordinal) || data.StatusLine.StartsWith("Loaded", StringComparison.Ordinal) ? Success : ParchDim);
            status.style.whiteSpace = WhiteSpace.Normal;
            status.style.marginTop = 12;
            _body.Add(status);
        }

        private VisualElement BuildSlot(SaveSlotViewData slot)
        {
            bool saveMode = string.Equals(_activeTab, "Save", StringComparison.Ordinal);
            var card = new VisualElement();
            card.style.marginBottom = 10;
            card.style.paddingLeft = 16;
            card.style.paddingRight = 16;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.backgroundColor = Dark(0.58f);
            Border(card, Alpha(slot.HasSave ? Gold : ParchDim, 0.35f), 1);
            Radius(card, 10);

            var top = Row();
            top.style.alignItems = Align.Center;
            top.Add(Text(slot.Title, Sans, 13, Parch, FontStyle.Bold));
            var kind = Text(slot.HasSave ? "OCCUPIED" : "EMPTY", Sans, 10, slot.HasSave ? Gold : ParchDim, FontStyle.Bold);
            kind.style.marginLeft = StyleKeyword.Auto;
            kind.style.letterSpacing = 1f;
            top.Add(kind);
            card.Add(top);

            var detail = Text(slot.Detail, Serif, 13, ParchDim);
            detail.style.marginTop = 8;
            detail.style.whiteSpace = WhiteSpace.Normal;
            card.Add(detail);

            var button = new Button(() =>
            {
                if (saveMode) _onSaveSlot?.Invoke(slot.SlotId);
                else _onLoadSlot?.Invoke(slot.SlotId);
            })
            { text = saveMode ? (slot.HasSave ? "OVERWRITE" : "SAVE") : "LOAD" };
            ResetButton(button);
            button.SetEnabled(saveMode || slot.HasSave);
            button.style.height = 32;
            button.style.marginTop = 10;
            button.style.backgroundColor = saveMode || slot.HasSave ? Gold : Alpha(Panel, 0.62f);
            button.style.color = saveMode || slot.HasSave ? Ink : PA(0.55f);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.letterSpacing = 0.8f;
            ApplyFont(button, Sans);
            Border(button, saveMode || slot.HasSave ? Amber : PA(0.18f), 1);
            Radius(button, 7);
            card.Add(button);
            return card;
        }
    }
}
