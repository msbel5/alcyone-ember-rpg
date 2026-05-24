using EmberCrpg.Presentation.Ember.UI;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    public sealed class MainMenuSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "MainMenu";

        public void Build()
        {
            var canvas = new GameObject("MainMenuCanvas");
            canvas.AddComponent<EmberMainMenuUI>();
        }
    }
}
