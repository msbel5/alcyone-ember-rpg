using System;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class JournalView
    {
        private readonly VisualElement _overlay;

        public JournalView(VisualElement stageCanvas, Action onClose)
        {
            _overlay = IgModal.Build("Journal", false, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.backgroundColor = VoidWarm;
            content.Add(EmptyState(
                "Journal",
                "No active quests — the world has asked nothing of you yet.",
                "A live journal read model is not available in the current domain surface."));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }
    }
}
