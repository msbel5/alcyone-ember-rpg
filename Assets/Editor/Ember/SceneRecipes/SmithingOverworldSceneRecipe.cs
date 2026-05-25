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
            var groundMat = EmberSceneMaterialLibrary.SmithingFloor();
            var wallMat = EmberSceneMaterialLibrary.SmithingWall();

            // Create a small enclosure for the forge area
            var room = EmberTerrainBuilder.BuildRoom(new Vector3(0, 0, 5), 20f, 15f, 4f, groundMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);

            // Exterior ground
            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 50f, groundMat, "Ground");

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.58f, 0.70f, 0.95f),
                intensity: 0.55f,
                eulerAngles: new Vector3(50f, 45f, 0f));

            var forgeLight = new GameObject("ForgeOrangeKeyLight", typeof(Light));
            forgeLight.transform.position = new Vector3(0f, 2.1f, 3f);
            var forge = forgeLight.GetComponent<Light>();
            forge.type = LightType.Point;
            forge.color = new Color(1f, 0.38f, 0.08f);
            forge.intensity = 2.4f;
            forge.range = 9f;

            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: spawnPosition,
                spawnRotation: Quaternion.identity,
                fov: 70f);

            var smiths = new GameObject("Smiths").transform;
            EmberWorldspaceBuilder.SpawnActor("Smith_A", "blacksmith", new Vector3(-1.2f, 0f, 0f), smiths, "Warden");
            EmberWorldspaceBuilder.SpawnActor("Smith_B", "blacksmith", new Vector3( 1.2f, 0f, 0f), smiths, "Quartermaster Ivo");

            var focal = new GameObject("FocalContent");
            focal.transform.position = new Vector3(0f, 1.4f, 3f);

            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Furnace",
                new Vector3(0f, 0.75f, 3f),
                material: EmberSceneMaterialLibrary.EmberLight(),
                scale: new Vector3(2.1f, 1.4f, 1.0f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Anvil",
                new Vector3(-2.4f, 0.45f, 2.1f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(1.5f, 0.5f, 0.8f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Sparks",
                new Vector3(0f, 1.9f, 2.6f),
                material: EmberSceneMaterialLibrary.EmberLight(),
                scale: new Vector3(0.25f, 0.25f, 0.25f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "SmokeColumn",
                new Vector3(0f, 2.8f, 3.2f),
                material: EmberSceneMaterialLibrary.Wall(),
                scale: new Vector3(0.55f, 1.8f, 0.55f));
            EmberWorldspaceBuilder.SpawnDecorSprite("SmithTools", "Assets/Art/Items/smith_tools.png", new Vector3(-3.3f, 0.6f, 2.3f), 0.8f);
            EmberWorldspaceBuilder.SpawnDecorSprite("IronHammer", "Assets/Art/Items/iron_warhammer.png", new Vector3(2.7f, 0.6f, 2.2f), 0.8f);
            EmberWorldspaceBuilder.SpawnDecorSprite("FireEssence", "Assets/Art/Items/fire_essence.png", new Vector3(0f, 1.1f, 2.25f), 0.65f);

            var ambience = new GameObject("ForgeAmbientPlaceholderLoop", typeof(AudioSource)).GetComponent<AudioSource>();
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0.65f;

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
