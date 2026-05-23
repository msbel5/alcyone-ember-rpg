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
        public static void BuildFaz3() => RunRecipe(new SmithingOverworldSceneRecipe());

        [MenuItem(Root + "Faz 4 — Colony Needs")]
        public static void BuildFaz4() => RunRecipe(new ColonyNeedsSceneRecipe());

        [MenuItem(Root + "Faz 5 — Season Farm")]
        public static void BuildFaz5() => RunRecipe(new SeasonFarmSceneRecipe());

        [MenuItem(Root + "Faz 6 — Trade Market")]
        public static void BuildFaz6() => RunRecipe(new TradeMarketSceneRecipe());

        [MenuItem(Root + "Faz 7 — Combat Dungeon")]
        public static void BuildFaz7() => RunRecipe(new CombatDungeonSceneRecipe());

        [MenuItem(Root + "Faz 8 — Ritual Hall")]
        public static void BuildFaz8() => RunRecipe(new RitualHallSceneRecipe());

        [MenuItem(Root + "Faz 9 — Tavern Dialog")]
        public static void BuildFaz9() => RunRecipe(new TavernDialogSceneRecipe());

        [MenuItem(Root + "Faz 10 — Oracle Shrine")]
        public static void BuildFaz10() => RunRecipe(new OracleShrineSceneRecipe());

        [MenuItem(Root + "Faz 11 — Showroom Overview")]
        public static void BuildFaz11() => RunRecipe(new ShowroomOverviewSceneRecipe());

        [MenuItem(Root + "Faz 12 — Tavern Flavour (LLM)")]
        public static void BuildFaz12() => RunRecipe(new TavernFlavourSceneRecipe());

        [MenuItem(Root + "Character Creation")]
        public static void BuildCharacterCreation() => RunUiRecipe(new CharacterCreationSceneRecipe());

        [MenuItem(Root + "All — Build every faz scene")]
        public static void BuildAll()
        {
            SpriteRegistryAutoBuilder.Build();
            RunUiRecipe(new CharacterCreationSceneRecipe());
            RunRecipe(new SmithingOverworldSceneRecipe());
            RunRecipe(new ColonyNeedsSceneRecipe());
            RunRecipe(new SeasonFarmSceneRecipe());
            RunRecipe(new TradeMarketSceneRecipe());
            RunRecipe(new CombatDungeonSceneRecipe());
            RunRecipe(new RitualHallSceneRecipe());
            RunRecipe(new TavernDialogSceneRecipe());
            RunRecipe(new OracleShrineSceneRecipe());
            RunRecipe(new ShowroomOverviewSceneRecipe());
            RunRecipe(new TavernFlavourSceneRecipe());
        }

        public static void RunRecipe(IEmberSceneRecipe recipe)
        {
            EmberSceneFactory.CreateEmpty();
            recipe.Build();
            EmberRuntimeHostBuilder.EnsureHost();
            
            // AAA Polish: Bake NavMesh.
            // Unity 6 deprecated UnityEditor.AI.NavMeshBuilder in favor of the
            // com.unity.ai.navigation package's NavMeshSurface component.
            // Until that package lands in manifest.json (tracked under the
            // Faz 14 navigation sprint), keep the legacy synchronous bake —
            // it still works and produces the same NavMesh.asset. The pragma
            // silences the CS0618 warning so the build stays green.
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
            recipe.Build();
            var path = EmberSceneSavePolicy.ResolveScenePath(recipe.SceneName);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            AssetDatabase.Refresh();
        }
}
}
