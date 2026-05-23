using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 3 acceptance: two smith actors queue at a furnace and craft ingots.
    /// Builds the smithing exterior as a first-person walkable scene: stone floor,
    /// two smiths, a furnace marker, sun light, UI overlay scaffold.
    /// </summary>
    public sealed class SmithingOverworldSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "SmithingOverworld";

        public void Build()
        {
            var groundMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/stone_floor.png", tiling: 8f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/brick.png", tiling: 4f);

            // Create a small enclosure for the forge area
            var room = EmberTerrainBuilder.BuildRoom(new Vector3(0, 0, 5), 20f, 15f, 4f, groundMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);
            
            // Exterior ground
            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 50f, groundMat, "Ground");

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.98f, 0.9f),
                intensity: 1.3f,
                eulerAngles: new Vector3(50f, 45f, 0f));

            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: spawnPosition,
                spawnRotation: Quaternion.identity,
                fov: 70f);

            var smiths = new GameObject("Smiths").transform;
            EmberWorldspaceBuilder.SpawnActor("Smith_A", "blacksmith", new Vector3(-1.2f, 0f, 0f), smiths, "Warden");
            EmberWorldspaceBuilder.SpawnActor("Smith_B", "blacksmith", new Vector3( 1.2f, 0f, 0f), smiths, "Quartermaster Ivo");

            EmberWorldspaceBuilder.SpawnWorksiteMarker("Furnace", new Vector3(0f, 0.75f, 3f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                anchorMin: new Vector2(0f, 0.94f), anchorMax: new Vector2(1f, 1f),
                background: new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");

            var jobPanel = EmberUiBuilder.BuildPanel(canvas, "JobQueuePanel",
                anchorMin: new Vector2(0f, 0.35f), anchorMax: new Vector2(0.22f, 0.94f),
                background: new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(jobPanel.gameObject, "EmberCrpg.Presentation.Ember.UI.JobQueuePanel");

            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(SmithingOverworldSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "ColonyNeeds", "→ Colony Needs");
        }
    }
}
