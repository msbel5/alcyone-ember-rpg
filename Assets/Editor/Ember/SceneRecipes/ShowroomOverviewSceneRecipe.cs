using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// "Overview" hub scene, reachable in normal play through the OracleShrine -> ShowroomOverview
    /// -> TavernFlavour portal loop, so it must be a consistent PLAYABLE scene rather than a dev
    /// demo wall. UI-AUDIT (BUG-STRAY-UI): the recipe previously authored eight side-by-side
    /// showcase panels (Job/Colony/Faction/Combat/Inventory/Dialog/Spell + HUD) stacked across the
    /// screen. Reduced to the standard playable set (EmberHud + one DialogBoxPanel; pause is added
    /// at runtime by EmberWorldHost) PLUS the three living-world panels that genuinely fit an
    /// overview hub and each appear exactly once: JobQueuePanel, FactionPanel, ColonyNeedsPanel.
    /// The combat HUD, spell bar, and inventory showcase panels were removed (CombatHud was already
    /// auto-disabled at runtime when an EmberHud exists; SpellBar/InventoryGrid are combat/loadout
    /// UI not thematic to an overview hub, and inventory-less is consistent with the other hub
    /// scenes such as ColonyNeeds and SeasonFarm).
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

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");

            var jobPanel = EmberUiBuilder.BuildPanel(canvas, "JobQueuePanel",
                new Vector2(0f, 0.45f), new Vector2(0.22f, 0.94f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(jobPanel.gameObject, "EmberCrpg.Presentation.Ember.UI.JobQueuePanel");

            var needsPanel = EmberUiBuilder.BuildPanel(canvas, "ColonyNeedsPanel",
                new Vector2(0.78f, 0.45f), new Vector2(1f, 0.94f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(needsPanel.gameObject, "EmberCrpg.Presentation.Ember.UI.ColonyNeedsPanel");

            var factions = EmberUiBuilder.BuildPanel(canvas, "FactionPanel",
                new Vector2(0.24f, 0.45f), new Vector2(0.5f, 0.94f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(factions.gameObject, "EmberCrpg.Presentation.Ember.UI.FactionPanel");

            // Standard single dialog box (bottom band). DialogBoxPanel.Awake self-pins to the
            // canonical bottom-centered footprint; seed the same band so it is never a stray tile.
            var dialog = EmberUiBuilder.BuildPanel(canvas, "DialogBox",
                new Vector2(0.14f, 0.05f), new Vector2(0.86f, 0.4f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(dialog.gameObject, "EmberCrpg.Presentation.Ember.UI.DialogBoxPanel");

            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(ShowroomOverviewSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "TavernFlavour", "→ Tavern Flavour");
        }
    }
}
