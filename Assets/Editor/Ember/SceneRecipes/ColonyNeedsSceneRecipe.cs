using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Phase 4 acceptance: actors get hungry, refuse to work, recover after a meal.
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

            var room = EmberTerrainBuilder.BuildRoom(Vector3.zero, 20f, 20f, 3.5f, floorMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);

            EmberLightingBuilder.SetAmbientMood(new Color(0.22f, 0.18f, 0.14f), 0.80f);
            EmberPostProcessBuilder.AddVolume("PostProcess_ColonyIndoor", EmberMoodPreset.NeutralIndoor);

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

            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: spawnPosition,
                spawnRotation: Quaternion.identity);

            var villagers = new GameObject("Villagers").transform;
            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper", new Vector3( 0f, 0f, 2.5f), villagers, "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnActor("Beggar",    "beggar",    new Vector3(-2f, 0f, 1.0f), villagers, "Sage Nera");
            EmberWorldspaceBuilder.SpawnActor("Guard",     "guard",     new Vector3( 2f, 0f, 1.0f), villagers, "Sentinel Rook");

            EmberWorldspaceBuilder.SpawnWorksiteMarker("Hearth", new Vector3(0f, 0.5f, 4f));

            // UI-SINGLE-SOURCE: HUD comes from one place. EmberWorldHost ensures the standard
            // gameplay HUD at runtime (EmberHud TopBar + action bar, the JobQueue/Faction/ColonyNeeds
            // side panels, pause, and the dialog box). This recipe authors only the overlay canvas so
            // those host-ensured panels have a parent + a single EventSystem; the colony-needs panel the
            // player expects here is now the host-ensured one (no per-scene duplicate / orphan).
            EmberUiBuilder.BuildOverlayCanvas("EmberHUD");

            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(ColonyNeedsSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "SeasonFarm", "→ Season Farm");
        }
    }
}
