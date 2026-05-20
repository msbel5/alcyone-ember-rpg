using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 4 acceptance: actors get hungry, refuse to work, recover after a meal.
    /// Scene composition: settlement floor, three actors with mood-affected portraits,
    /// a tavern marker, and the colony-needs UI panel on the right edge.
    /// </summary>
    public sealed class Faz4ColonyNeedsSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz4ColonyNeeds";

        public void Build()
        {
            var groundMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/wood_floor.png", tiling: 8f);

            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 25f, groundMat, "TavernFloor");

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.88f, 0.7f),
                intensity: 0.9f,
                eulerAngles: new Vector3(45f, 80f, 0f));

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -5f),
                spawnRotation: Quaternion.identity);

            var villagers = new GameObject("Villagers").transform;
            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper", new Vector3( 0f, 0f, 2.5f), villagers);
            EmberWorldspaceBuilder.SpawnActor("Beggar",    "beggar",    new Vector3(-2f, 0f, 1.0f), villagers);
            EmberWorldspaceBuilder.SpawnActor("Guard",     "guard",     new Vector3( 2f, 0f, 1.0f), villagers);

            EmberWorldspaceBuilder.SpawnWorksiteMarker("Hearth", new Vector3(0f, 0.5f, 4f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");

            var needsPanel = EmberUiBuilder.BuildPanel(canvas, "ColonyNeedsPanel",
                new Vector2(0.78f, 0.35f), new Vector2(1f, 0.94f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(needsPanel.gameObject, "EmberCrpg.Presentation.Ember.UI.ColonyNeedsPanel");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 10f), "Faz5SeasonFarm", "→ Faz 5");
        }
    }
}
