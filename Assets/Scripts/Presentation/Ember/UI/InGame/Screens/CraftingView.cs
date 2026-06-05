using System;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class CraftingView
    {
        private readonly VisualElement _overlay;

        public CraftingView(VisualElement stageCanvas, Action onClose, Action<string> onCraft = null)
        {
            _overlay = IgModal.Build("Crafting", false, () => { Close(); onClose?.Invoke(); }, out var content);
            content.Add(EmptyState(
                "Crafting",
                "No recipes are available here yet.",
                "A live crafting or recipe source has not been exposed to this in-game UI."));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }
    }
}
