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
    public sealed class ColonyNeedsSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "ColonyNeeds";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/wood_floor.png", tiling: 6f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/stone_wall.png", tiling: 4f);

            EmberTerrainBuilder.BuildRoom(Vector3.zero, 20f, 20f, 3.5f, floorMat, wallMat);

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.92f, 0.8f),
                intensity: 1.1f,
                eulerAngles: new Vector3(45f, 80f, 0f));

            // Warm interior lighting
            var warmLight = new GameObject("TavernLight", typeof(Light));
            warmLight.transform.position = new Vector3(0f, 2.5f, 0f);
            var l = warmLight.GetComponent<Light>();
            l.type = LightType.Point;
            l.range = 15f;
            l.intensity = 0.8f;
            l.color = new Color(1f, 0.6f, 0.3f);

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -5f),
                spawnRotation: Quaternion.identity);

            var villagers = new GameObject("Villagers").transform;
            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper", new Vector3( 0f, 0f, 2.5f), villagers, "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnActor("Beggar",    "beggar",    new Vector3(-2f, 0f, 1.0f), villagers, "Sage Nera");
            EmberWorldspaceBuilder.SpawnActor("Guard",     "guard",     new Vector3( 2f, 0f, 1.0f), villagers, "Sentinel Rook");

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

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 10f), "SeasonFarm", "→ Faz 5");
        }
    }
}
