using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using EmberCrpg.Editor.Ember.SceneRecipes;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EmberCrpg.Editor.Ember.Menu
{
    public static class EmberSceneBuilderMenu
    {
        private const string Root = "Ember/Build Scene/";

        [MenuItem(Root + "Main Menu")]
        public static void BuildMainMenu() => RunUiRecipe(new MainMenuSceneRecipe());

        [MenuItem(Root + "Character Creation")]
        public static void BuildCharacterCreation() => RunUiRecipe(new CharacterCreationSceneRecipe());

        // World pivot: the near-empty host scene the runtime World Scene Director fills from world data.
        [MenuItem(Root + "1. Generated World (runtime-directed host)")]
        public static void BuildGeneratedWorld() => RunRecipe(new GeneratedWorldSceneRecipe());

        [MenuItem(Root + "All - Rebuild surviving scenes")]
        public static void BuildAll()
        {
            RunUiRecipe(new MainMenuSceneRecipe());
            RunUiRecipe(new CharacterCreationSceneRecipe());
            RunRecipe(new GeneratedWorldSceneRecipe());
        }

        public static void RunRecipe(IEmberSceneRecipe recipe)
        {
            EmberSceneFactory.CreateEmpty();
            EmberTerrainBuilder.BeginScene(recipe.SceneName);
            recipe.Build();
            EmberRuntimeHostBuilder.EnsureHost();
            EmberSceneSurfaceSanitizer.ApplyToOpenScene();

#pragma warning disable CS0618
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore CS0618

            var path = EmberSceneSavePolicy.ResolveScenePath(recipe.SceneName);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            AssetDatabase.Refresh();
        }

        private static void RunUiRecipe(IEmberSceneRecipe recipe)
        {
            EmberSceneFactory.CreateEmpty();
            EmberTerrainBuilder.BeginScene(recipe.SceneName);
            recipe.Build();
            EmberSceneSurfaceSanitizer.ApplyToOpenScene();
            var path = EmberSceneSavePolicy.ResolveScenePath(recipe.SceneName);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            AssetDatabase.Refresh();
        }
    }
}
