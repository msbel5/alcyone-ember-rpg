using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using EmberCrpg.Editor.Ember.SceneRecipes;
using EmberCrpg.Editor.Ember.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EmberCrpg.Editor.Ember.Menu
{
    /// <summary>
    /// One MenuItem per faz recipe. Each entry creates a fresh empty scene, runs the
    /// recipe, then saves to <see cref="EmberSceneSavePolicy.ResolveScenePath"/>.
    /// Menu code is intentionally thin so test/automation can call recipes directly.
    /// </summary>
    public static class EmberSceneBuilderMenu
    {
        private const string Root = "Ember/Build Scene/";

        [MenuItem(Root + "Faz 3 — Smithing Overworld")]
        public static void BuildFaz3() => RunRecipe(new Faz3SmithingSceneRecipe());

        [MenuItem(Root + "Faz 4 — Colony Needs")]
        public static void BuildFaz4() => RunRecipe(new Faz4ColonyNeedsSceneRecipe());

        [MenuItem(Root + "Faz 5 — Season Farm")]
        public static void BuildFaz5() => RunRecipe(new Faz5FarmSceneRecipe());

        [MenuItem(Root + "Faz 6 — Trade Market")]
        public static void BuildFaz6() => RunRecipe(new Faz6TradeSceneRecipe());

        [MenuItem(Root + "Faz 7 — Combat Dungeon")]
        public static void BuildFaz7() => RunRecipe(new Faz7CombatSceneRecipe());

        [MenuItem(Root + "Faz 8 — Ritual Hall")]
        public static void BuildFaz8() => RunRecipe(new Faz8MagicSceneRecipe());

        [MenuItem(Root + "Faz 9 — Tavern Dialog")]
        public static void BuildFaz9() => RunRecipe(new Faz9DialogSceneRecipe());

        [MenuItem(Root + "Faz 10 — Oracle Shrine")]
        public static void BuildFaz10() => RunRecipe(new Faz10DmQuerySceneRecipe());

        [MenuItem(Root + "Faz 11 — Showroom Overview")]
        public static void BuildFaz11() => RunRecipe(new Faz11VisualLayerSceneRecipe());

        [MenuItem(Root + "Faz 12 — Tavern Flavour (LLM)")]
        public static void BuildFaz12() => RunRecipe(new Faz12LlmFlavourSceneRecipe());

        [MenuItem(Root + "All — Build every faz scene")]
        public static void BuildAll()
        {
            SpriteRegistryAutoBuilder.Build();
            RunRecipe(new Faz3SmithingSceneRecipe());
            RunRecipe(new Faz4ColonyNeedsSceneRecipe());
            RunRecipe(new Faz5FarmSceneRecipe());
            RunRecipe(new Faz6TradeSceneRecipe());
            RunRecipe(new Faz7CombatSceneRecipe());
            RunRecipe(new Faz8MagicSceneRecipe());
            RunRecipe(new Faz9DialogSceneRecipe());
            RunRecipe(new Faz10DmQuerySceneRecipe());
            RunRecipe(new Faz11VisualLayerSceneRecipe());
            RunRecipe(new Faz12LlmFlavourSceneRecipe());
        }

        public static void RunRecipe(IEmberSceneRecipe recipe)
        {
            EmberSceneFactory.CreateEmpty();
            recipe.Build();
            EmberRuntimeHostBuilder.EnsureHost();
            var path = EmberSceneSavePolicy.ResolveScenePath(recipe.SceneName);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            AssetDatabase.Refresh();
        }
    }
}
