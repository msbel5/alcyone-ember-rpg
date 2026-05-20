using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 9 acceptance: walk up to an NPC, ask about topics, get answers that consult
    /// memory and faction state. Tavern interior with an innkeeper and a dialog panel
    /// rooted to the lower half of the screen, Fallout 1 / Hitchhiker style.
    /// </summary>
    public sealed class Faz9DialogSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz9TavernDialog";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/tavern_floor.png", tiling: 4f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/wood_floor.png", tiling: 3f);

            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 16f, floorMat, "TavernFloor");

            EmberTerrainBuilder.BuildWall(new Vector3( 0f, 1.5f,  8f),  new Vector3(16f, 3f, 0.4f), wallMat, "NorthWall");
            EmberTerrainBuilder.BuildWall(new Vector3( 0f, 1.5f, -8f),  new Vector3(16f, 3f, 0.4f), wallMat, "SouthWall");
            EmberTerrainBuilder.BuildWall(new Vector3(-8f, 1.5f, 0f),   new Vector3(0.4f, 3f, 16f), wallMat, "WestWall");
            EmberTerrainBuilder.BuildWall(new Vector3( 8f, 1.5f, 0f),   new Vector3(0.4f, 3f, 16f), wallMat, "EastWall");

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.78f, 0.55f),
                intensity: 0.85f,
                eulerAngles: new Vector3(50f, 120f, 0f));

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -5f),
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper",    new Vector3(0f, 0f, 3f));
            EmberWorldspaceBuilder.SpawnActor("Patron",    "warrior",      new Vector3(-3f, 0f, 1f));
            EmberWorldspaceBuilder.SpawnActor("Sage",      "sage",         new Vector3( 3f, 0f, 1f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Hearth", new Vector3(0f, 0.5f, 4f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var dialog = EmberUiBuilder.BuildPanel(canvas, "DialogBox",
                new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.45f),
                new Color(0f, 0f, 0f, 0.7f));
            EmberUiBuilder.AttachRuntimeScript(dialog.gameObject, "EmberCrpg.Presentation.Ember.UI.DialogBoxPanel");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 8f), "Faz10OracleShrine", "→ Faz 10");
        }
    }
}
