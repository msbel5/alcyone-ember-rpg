using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 12 acceptance: three NPCs in a tavern exchange flavour lines validated by the
    /// LLM gate; none mutates the world. Reuses the tavern interior and exposes a wide
    /// dialog/narration panel that the LLM client writes to via the same source contract.
    /// </summary>
    public sealed class Faz12LlmFlavourSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz12TavernFlavour";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/tavern_floor.png", tiling: 4f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/wood_floor.png", tiling: 3f);

            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 18f, floorMat, "FlavourFloor");
            EmberTerrainBuilder.BuildWall(new Vector3(0f, 1.5f,  9f), new Vector3(18f, 3f, 0.4f), wallMat, "NorthWall");
            EmberTerrainBuilder.BuildWall(new Vector3(0f, 1.5f, -9f), new Vector3(18f, 3f, 0.4f), wallMat, "SouthWall");
            EmberTerrainBuilder.BuildWall(new Vector3(-9f, 1.5f, 0f), new Vector3(0.4f, 3f, 18f), wallMat, "WestWall");
            EmberTerrainBuilder.BuildWall(new Vector3( 9f, 1.5f, 0f), new Vector3(0.4f, 3f, 18f), wallMat, "EastWall");

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.7f, 0.45f),
                intensity: 0.7f,
                eulerAngles: new Vector3(55f, 200f, 0f));

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -6f),
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Bard",      "bard",       new Vector3(-2f, 0f, 1f));
            EmberWorldspaceBuilder.SpawnActor("Knight",    "knight",     new Vector3( 0f, 0f, 1f));
            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper",  new Vector3( 2f, 0f, 1f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Hearth", new Vector3(0f, 0.5f, 3.5f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var narration = EmberUiBuilder.BuildPanel(canvas, "NarrationBox",
                new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.32f),
                new Color(0f, 0f, 0f, 0.75f));
            EmberUiBuilder.AttachRuntimeScript(narration.gameObject, "EmberCrpg.Presentation.Ember.UI.DialogBoxPanel");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 10f), "Faz3SmithingOverworld", "→ Faz 3");
        }
    }
}
