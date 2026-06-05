using System;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class CraftingView
    {
        private readonly VisualElement _overlay;
        private readonly VisualElement _content;
        private readonly Action<string> _onCraft;

        public CraftingView(VisualElement stageCanvas, Action onClose, Action<string> onCraft)
        {
            _onCraft = onCraft;
            _overlay = IgModal.Build("Crafting", false, () => { Close(); onClose?.Invoke(); }, out _content);
            stageCanvas.Add(_overlay);
            Refresh();
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        public void Refresh()
        {
            _content.Clear();
            _content.style.flexDirection = FlexDirection.Column;
            var data = IgCraftingData.Current ?? IgCraftingData.Default;
            _content.Add(BuildHeader(data));

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.minHeight = 0;
            StyleScroll(scroll);
            _content.Add(scroll);

            if (data.Recipes == null || data.Recipes.Length == 0)
            {
                scroll.Add(EmptyState("Crafting", "No recipes are available here yet.", "A live crafting source has not been exposed to this in-game UI."));
            }
            else
            {
                for (int i = 0; i < data.Recipes.Length; i++)
                    scroll.Add(BuildRecipe(data.Recipes[i]));
            }

            var status = Text(data.StatusLine, Sans, 12, data.StatusLine.StartsWith("Crafted", StringComparison.Ordinal) ? Success : ParchDim);
            status.style.whiteSpace = WhiteSpace.Normal;
            status.style.marginTop = 12;
            _content.Add(status);
        }

        private static VisualElement BuildHeader(CraftingScreenData data)
        {
            var header = new VisualElement();
            header.style.marginBottom = 12;
            header.Add(Text(data.StationName, Sans, 14, Parch, FontStyle.Bold));
            var sub = Text("AVAILABLE RECIPES", Sans, 10, Gold, FontStyle.Bold);
            sub.style.marginTop = 4;
            sub.style.letterSpacing = 1.4f;
            header.Add(sub);
            return header;
        }

        private VisualElement BuildRecipe(CraftingRecipeData recipe)
        {
            var card = new VisualElement();
            card.style.marginBottom = 10;
            card.style.paddingLeft = 16;
            card.style.paddingRight = 16;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.backgroundColor = Dark(0.58f);
            Border(card, Alpha(recipe.CanCraft ? Success : Gold, 0.35f), 1);
            Radius(card, 10);

            var top = Row();
            top.style.alignItems = Align.Center;
            top.Add(Text(recipe.Name, Sans, 13, Parch, FontStyle.Bold));
            var tag = Text(recipe.CanCraft ? "READY" : "LOCKED", Sans, 10, recipe.CanCraft ? Success : Gold, FontStyle.Bold);
            tag.style.marginLeft = StyleKeyword.Auto;
            tag.style.letterSpacing = 1f;
            top.Add(tag);
            card.Add(top);

            var meta = Text(recipe.Station + " · " + recipe.Outputs, Sans, 10, PA(0.38f));
            meta.style.marginTop = 4;
            card.Add(meta);

            var ingredients = Text(recipe.Ingredients, Serif, 13, ParchDim);
            ingredients.style.marginTop = 8;
            ingredients.style.whiteSpace = WhiteSpace.Normal;
            card.Add(ingredients);

            var availability = Text(recipe.Availability, Sans, 11, recipe.CanCraft ? Success : Gold, FontStyle.Italic);
            availability.style.marginTop = 8;
            card.Add(availability);

            var button = new Button(() => _onCraft?.Invoke(recipe.RecipeId)) { text = "CRAFT" };
            ResetButton(button);
            button.SetEnabled(recipe.CanCraft);
            button.style.height = 32;
            button.style.marginTop = 10;
            button.style.backgroundColor = recipe.CanCraft ? Gold : Alpha(Panel, 0.62f);
            button.style.color = recipe.CanCraft ? Ink : PA(0.55f);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.letterSpacing = 0.8f;
            ApplyFont(button, Sans);
            Border(button, recipe.CanCraft ? Amber : PA(0.18f), 1);
            Radius(button, 7);
            card.Add(button);
            return card;
        }
    }
}
