using System;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine.UIElements;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class SaveLoadView
    {
        private readonly VisualElement _overlay;

        public SaveLoadView(
            VisualElement stageCanvas,
            Action onClose,
            Action<int> onSaveSlot = null,
            Action<int> onLoadSlot = null,
            string activeTab = "Save")
        {
            _overlay = IgModal.BuildTabbed(
                "Save / Load",
                false,
                new[] { "Save", "Load" },
                activeTab,
                tab => { Close(); new SaveLoadView(stageCanvas, onClose, onSaveSlot, onLoadSlot, tab); },
                () => { Close(); onClose?.Invoke(); },
                out var body);

            bool saveMode = string.Equals(activeTab, "Save", StringComparison.Ordinal);
            body.Add(IgDesign.EmptyState(
                saveMode ? "Save" : "Load",
                saveMode
                    ? "No save-slot list is reachable from this in-game UI yet."
                    : "No save list is reachable from this in-game UI yet.",
                saveMode
                    ? "The save tab is present, but calling the save browser or backend save service is outside this fence."
                    : "The load tab is present, but no live save-browser source is exposed within this UI layer."));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }
    }
}
