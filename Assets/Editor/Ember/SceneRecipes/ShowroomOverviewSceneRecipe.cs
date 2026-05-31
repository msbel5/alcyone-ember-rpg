using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// "Overview" hub scene, reachable in normal play through the OracleShrine -> ShowroomOverview
    /// -> TavernFlavour portal loop, so it must be a consistent PLAYABLE scene rather than a dev
    /// demo wall.
    ///
    /// UI-SINGLE-SOURCE (player report "default UI elements every scene ... ui is coming from one
    /// place"): this recipe used to author the full living-world panel set inline (EmberHud +
    /// JobQueue + ColonyNeeds + Faction + a DialogBox). Those are now the host-ensured standard HUD,
    /// mounted identically in every gameplay scene by EmberWorldHost, so authoring them here only
    /// produced the duplicate/orphan copies the player saw. The recipe now authors just the overlay
    /// canvas (so the host-ensured panels have a parent + a single EventSystem). The earlier-removed
    /// combat HUD, spell bar, and inventory showcase panels stay gone -- they are combat/loadout UI
    /// not thematic to an overview hub.
    /// </summary>
    public sealed class ShowroomOverviewSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "ShowroomOverview";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/stone_floor.png", tiling: 8f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/brick.png", tiling: 4f);

            var room = EmberTerrainBuilder.BuildRoom(Vector3.zero, 30f, 30f, 4f, floorMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);

            EmberLightingBuilder.SetAmbientMood(new Color(0.30f, 0.28f, 0.26f), 0.95f);
            EmberPostProcessBuilder.AddVolume("PostProcess_Showroom", EmberMoodPreset.NeutralIndoor);

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.98f, 0.92f),
                intensity: 1.2f,
                eulerAngles: new Vector3(50f, -30f, 0f));

            // Add some point lights for ambiance
            var lightA = new GameObject("AmbianceLightA", typeof(Light));
            lightA.transform.position = new Vector3(-5f, 2f, 5f);
            lightA.GetComponent<Light>().type = LightType.Point;
            lightA.GetComponent<Light>().range = 10f;
            lightA.GetComponent<Light>().intensity = 0.5f;
            lightA.GetComponent<Light>().color = new Color(1f, 0.7f, 0.4f);

            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: spawnPosition,
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Smith_A",   "blacksmith", new Vector3(-3f, 0f, 1f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnActor("Smith_B",   "blacksmith", new Vector3(-1f, 0f, 1f), domainActorKey: "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper",  new Vector3( 1f, 0f, 1f), domainActorKey: "Sage Nera");
            EmberWorldspaceBuilder.SpawnActor("Mage",      "mage",       new Vector3( 3f, 0f, 1f), domainActorKey: "Sentinel Rook");
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Forge", new Vector3(-2f, 0.75f, 3.5f));

            // UI-SINGLE-SOURCE: standard HUD set (EmberHud + JobQueue/Faction/ColonyNeeds side panels +
            // pause + dialog) is host-ensured by EmberWorldHost. Author only the canvas as their parent.
            EmberUiBuilder.BuildOverlayCanvas("EmberHUD");

            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(ShowroomOverviewSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "TavernFlavour", "→ Tavern Flavour");
        }
    }
}
