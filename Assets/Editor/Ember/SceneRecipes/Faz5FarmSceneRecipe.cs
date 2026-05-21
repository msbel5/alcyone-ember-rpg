using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 5 acceptance: plant a crop, watch the season tick, harvest at the right stage.
    /// Builds a small farm exterior with two crop rows, a farmer actor, and the season HUD.
    /// </summary>
    public sealed class Faz5FarmSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz5SeasonFarm";

        public void Build()
        {
            var groundMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/grass.png", tiling: 12f);
            var dirtMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/dirt_path.png", tiling: 4f);

            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 60f, groundMat, "Field");
            
            // Path to harvest shed
            EmberTerrainBuilder.BuildGroundPlane(new Vector3(2f, 0.01f, 0f), 10f, dirtMat, "Path");

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 1f, 0.95f),
                intensity: 1.4f,
                eulerAngles: new Vector3(60f, 30f, 0f));

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -8f),
                spawnRotation: Quaternion.identity);

            var crops = new GameObject("Crops").transform;
            for (int row = 0; row < 2; row++)
            for (int col = 0; col < 6; col++)
            {
                var crop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crop.transform.SetParent(crops, worldPositionStays: false);
                crop.transform.localPosition = new Vector3(-2.5f + col, 0.35f, row * 1.5f);
                crop.transform.localScale = new Vector3(0.15f, 0.35f, 0.15f);
                crop.name = $"Crop_{row}_{col}";
            }

            EmberWorldspaceBuilder.SpawnActor("Farmer", "warrior", new Vector3(-3f, 0f, -2f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnWorksiteMarker("HarvestShed", new Vector3(4f, 0.75f, 1f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var seasonPanel = EmberUiBuilder.BuildPanel(canvas, "SeasonPanel",
                new Vector2(0.78f, 0.78f), new Vector2(1f, 0.94f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(seasonPanel.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 15f), "Faz6TradeMarket", "→ Faz 6");
        }
    }
}
