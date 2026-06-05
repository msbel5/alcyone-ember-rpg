using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class CraftingView
    {
        private readonly VisualElement _overlay;

        public CraftingView(VisualElement stageCanvas, Action onClose, Action<string> onCraft = null)
        {
            _overlay = IgModal.Build("Crafting", false, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.flexDirection = FlexDirection.Row;

            // TODO(real-data): no host source yet.
            var selected = IgMockData.CraftingRecipes[0];
            content.Add(BuildRecipeList(selected.Id));
            content.Add(BuildRecipeDetail(selected, onCraft));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static ScrollView BuildRecipeList(string selectedId)
        {
            var pane = new ScrollView();
            pane.style.width = 320;
            pane.style.flexShrink = 0;
            pane.style.borderRightWidth = 1;
            pane.style.borderRightColor = PA(0.10f);
            pane.style.paddingTop = 16;
            pane.style.paddingBottom = 16;
            pane.style.paddingLeft = 18;
            pane.style.paddingRight = 18;

            var label = Text("RECIPES", Sans, 10, Gold, FontStyle.Bold);
            label.style.letterSpacing = 1.8f;
            label.style.marginBottom = 12;
            pane.Add(label);

            for (int i = 0; i < IgMockData.CraftingRecipes.Length; i++)
            {
                var recipe = IgMockData.CraftingRecipes[i];
                bool selected = recipe.Id == selectedId;
                var row = new VisualElement();
                row.style.marginBottom = 8;
                row.style.paddingTop = 12;
                row.style.paddingBottom = 12;
                row.style.paddingLeft = 14;
                row.style.paddingRight = 14;
                row.style.backgroundColor = selected ? GA(0.10f) : Dark(0.55f);
                Border(row, selected ? Gold : PA(0.10f), selected ? 2 : 1);
                Radius(row, 10);
                row.Add(Text(recipe.Name, Sans, 13, selected ? Parch : ParchDim, FontStyle.Bold));
                var cat = Text(recipe.Category.ToUpperInvariant(), Sans, 10, Amber);
                cat.style.letterSpacing = 0.8f;
                cat.style.marginTop = 3;
                row.Add(cat);
                var desc = Text(recipe.Description, Serif, 12, PA(0.50f), FontStyle.Italic);
                desc.style.whiteSpace = WhiteSpace.Normal;
                desc.style.marginTop = 6;
                row.Add(desc);
                pane.Add(row);
            }

            return pane;
        }

        private static ScrollView BuildRecipeDetail(CraftingRecipeData recipe, Action<string> onCraft)
        {
            var pane = new ScrollView();
            pane.style.flexGrow = 1;
            pane.style.paddingTop = 22;
            pane.style.paddingBottom = 22;
            pane.style.paddingLeft = 26;
            pane.style.paddingRight = 26;

            pane.Add(Text(recipe.Name, Serif, 22, Parch, FontStyle.Bold));
            var cat = Text(recipe.Category.ToUpperInvariant(), Sans, 11, Amber);
            cat.style.letterSpacing = 1f;
            cat.style.marginTop = 4;
            pane.Add(cat);

            var outCard = new VisualElement();
            outCard.style.marginTop = 18;
            outCard.style.marginBottom = 18;
            outCard.style.paddingTop = 18;
            outCard.style.paddingBottom = 18;
            outCard.style.paddingLeft = 18;
            outCard.style.paddingRight = 18;
            outCard.style.backgroundColor = Dark(0.62f);
            Border(outCard, PA(0.12f), 1);
            Radius(outCard, 12);
            var outHead = Text("OUTPUT", Sans, 10, Gold, FontStyle.Bold);
            outHead.style.letterSpacing = 1.8f;
            outHead.style.marginBottom = 8;
            outCard.Add(outHead);
            outCard.Add(Text(recipe.Description, Serif, 15, ParchDim));
            var val = Text($"{recipe.OutputValue} gp value", Sans, 12, Amber, FontStyle.Bold);
            val.style.marginTop = 10;
            outCard.Add(val);
            pane.Add(outCard);

            var ingredients = Text("INGREDIENTS", Sans, 10, Gold, FontStyle.Bold);
            ingredients.style.letterSpacing = 1.8f;
            ingredients.style.marginBottom = 10;
            pane.Add(ingredients);
            for (int i = 0; i < recipe.Ingredients.Length; i++)
                pane.Add(BuildIngredient(recipe.Ingredients[i]));

            var craft = new Button(() => onCraft?.Invoke(recipe.Id)) { text = "CRAFT" };
            ResetButton(craft);
            craft.style.marginTop = 22;
            craft.style.height = 42;
            craft.style.width = 160;
            craft.style.backgroundColor = Gold;
            craft.style.color = Ink;
            craft.style.fontSize = 13;
            craft.style.letterSpacing = 1f;
            craft.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(craft, Sans);
            Border(craft, Amber, 1);
            Radius(craft, 8);
            pane.Add(craft);
            return pane;
        }

        private static VisualElement BuildIngredient(IngredientData ingredient)
        {
            var row = Row();
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = PA(0.07f);
            var dot = new VisualElement();
            dot.style.width = 8;
            dot.style.height = 8;
            dot.style.marginRight = 10;
            dot.style.backgroundColor = ingredient.Available ? Success : Health;
            Radius(dot, 999);
            row.Add(dot);
            var name = Text($"{ingredient.Name} ×{ingredient.Quantity}", Sans, 13, ingredient.Available ? Parch : PA(0.45f));
            name.style.flexGrow = 1;
            row.Add(name);
            row.Add(Text(ingredient.Available ? "READY" : "MISSING", Sans, 10, ingredient.Available ? Success : Health, FontStyle.Bold));
            return row;
        }
    }
}
