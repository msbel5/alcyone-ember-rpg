using EmberCrpg.Presentation.Ember.UI;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    public sealed class CharacterCreationSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "CharacterCreation";

        public void Build()
        {
            var canvas = new GameObject("CharacterCreationCanvas");
            canvas.AddComponent<CharacterCreationUI>();
        }
    }
}
