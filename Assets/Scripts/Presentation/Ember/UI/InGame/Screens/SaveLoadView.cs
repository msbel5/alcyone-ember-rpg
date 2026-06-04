using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class SaveLoadView
    {
        private readonly VisualElement _overlay;

        public SaveLoadView(VisualElement stageCanvas, Action onClose)
        {
            _overlay = IgModal.BuildTabbed("Save / Load", false, new[] { "Save", "Load" }, "Save",
                _ => { }, () => { Close(); onClose?.Invoke(); }, out var body);

            var scroll = new ScrollView();
            scroll.style.height = Length.Percent(100);
            scroll.style.paddingTop = 16;
            scroll.style.paddingBottom = 16;
            scroll.style.paddingLeft = 24;
            scroll.style.paddingRight = 24;
            body.Add(scroll);

            for (int i = 0; i < IgMockData.SaveSlots.Length; i++)
                scroll.Add(BuildSlot(IgMockData.SaveSlots[i], true));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildSlot(SaveSlotData slot, bool saveMode)
        {
            bool empty = slot.Location == "Empty";
            var row = Row();
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 8;
            row.style.paddingTop = 14;
            row.style.paddingBottom = 14;
            row.style.paddingLeft = 18;
            row.style.paddingRight = 18;
            row.style.backgroundColor = empty ? Alpha(InputBg, 0.40f) : Alpha(InputBg, 0.65f);
            Border(row, empty ? PA(0.06f) : PA(0.14f), 1);
            Radius(row, 10);

            var num = new VisualElement();
            num.style.width = 32;
            num.style.height = 32;
            num.style.marginRight = 16;
            num.style.backgroundColor = empty ? Dark(0.50f) : Alpha(Panel, 0.72f);
            Border(num, PA(0.12f), 1);
            Radius(num, 7);
            num.style.alignItems = Align.Center;
            num.style.justifyContent = Justify.Center;
            num.Add(Text(slot.Number.ToString(), Sans, 13, empty ? PA(0.22f) : ParchDim, FontStyle.Bold));
            row.Add(num);

            var textWrap = new VisualElement();
            textWrap.style.flexGrow = 1;
            textWrap.Add(Text(slot.Name, Sans, 14, empty ? PA(0.28f) : Parch, FontStyle.Bold));
            if (!empty)
                textWrap.Add(Text($"{slot.Location} · Lv {slot.Level} · {slot.PlayedTime} played", Sans, 11, PA(0.40f)));
            row.Add(textWrap);

            if (!empty)
            {
                var date = Text(slot.Date, Sans, 10, PA(0.28f));
                date.style.marginRight = 16;
                row.Add(date);
            }

            string action = saveMode ? (empty ? "NEW" : "OVERWRITE") : "LOAD";
            var state = Text(action, Sans, 12, saveMode ? Amber : Gold, FontStyle.Bold);
            state.style.letterSpacing = 1f;
            row.Add(state);
            return row;
        }
    }
}
