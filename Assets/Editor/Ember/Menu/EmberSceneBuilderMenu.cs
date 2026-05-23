using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using EmberCrpg.Editor.Ember.SceneRecipes;
using EmberCrpg.Editor.Ember.Tools;
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

        [MenuItem(Root + "3. Smithing Overworld")]
        public static void BuildScene3() => RunRecipe(new SmithingOverworldSceneRecipe());

        [MenuItem(Root + "4. Colony Needs")]
        public static void BuildScene4() => RunRecipe(new ColonyNeedsSceneRecipe());

        [MenuItem(Root + "5. Season Farm")]
        public static void BuildScene5() => RunRecipe(new SeasonFarmSceneRecipe());

        [MenuItem(Root + "6. Trade Market")]
        public static void BuildScene6() => RunRecipe(new TradeMarketSceneRecipe());

        [MenuItem(Root + "7. Combat Dungeon")]
        public static void BuildScene7() => RunRecipe(new CombatDungeonSceneRecipe());

        [MenuItem(Root + "8. Ritual Hall")]
        public static void BuildScene8() => RunRecipe(new RitualHallSceneRecipe());

        [MenuItem(Root + "9. Tavern Dialog")]
        public static void BuildScene9() => RunRecipe(new TavernDialogSceneRecipe());

        [MenuItem(Root + "10. Oracle Shrine")]
        public static void BuildScene10() => RunRecipe(new OracleShrineSceneRecipe());

        [MenuItem(Root + "11. Showroom Overview")]
        public static void BuildScene11() => RunRecipe(new ShowroomOverviewSceneRecipe());

        [MenuItem(Root + "12. Tavern Flavour")]
        public static void BuildScene12() => RunRecipe(new TavernFlavourSceneRecipe());

        [MenuItem(Root + "All — Rebuild every gameplay scene")]
        public static void BuildAll()
        {
            SpriteRegistryAutoBuilder.Build();
            RunUiRecipe(new MainMenuSceneRecipe());
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
