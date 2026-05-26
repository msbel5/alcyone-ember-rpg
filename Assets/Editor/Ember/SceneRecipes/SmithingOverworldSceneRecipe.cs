// Why: AAA Sprint A.1 — SmithingOverworld is the player's first walked-in
// scene after character creation. This recipe builds the **template** that
// every "smith"-tagged settlement instantiates in the open-world overland map
// (per docs/prds/aaa-scene-quality-uplift.md §12). Mood = warm forge.
using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 3 acceptance: two smiths queue at a furnace and craft ingots.
    /// Mood: warm forge — orange flickering hearth light, sparks particle,
    /// chimney smoke, soft cool fill behind the anvil. Camera framed thirds
    /// on the anvil + smith composition.
    /// </summary>
    public sealed class SmithingOverworldSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "SmithingOverworld";

        public void Build()
        {
            var groundMat = EmberSceneMaterialLibrary.SmithingFloor();
            var wallMat = EmberSceneMaterialLibrary.SmithingWall();

            // 1. World shell
            var room = EmberTerrainBuilder.BuildRoom(new Vector3(0, 0, 5), 20f, 15f, 4f, groundMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);
            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 50f, groundMat, "Ground");

            // 2. Mood — warm forge ambient + bloom + tinted vignette
            EmberLightingBuilder.SetAmbientMood(
                color: new Color(0.32f, 0.20f, 0.13f),
                intensity: 0.85f);
            EmberPostProcessBuilder.AddVolume("PostProcess_SmithingWarmGlow", EmberMoodPreset.SmithingWarmGlow);

            // 3. Lighting rig — cool soft sun + flickering hearth + amber spot rim
            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.58f, 0.70f, 0.95f),
                intensity: 0.38f,
                eulerAngles: new Vector3(50f, 45f, 0f));
            EmberLightingBuilder.AddFlickeringPointLight(
                name: "ForgeHearthFlicker",
                position: new Vector3(0f, 1.35f, 3f),
                color: new Color(1f, 0.42f, 0.10f),
                baseIntensity: 2.6f, amplitude: 0.55f, range: 9f, speed: 5.4f, seed: 11.7f);
            EmberLightingBuilder.AddFlickeringPointLight(
                name: "AnvilSparkGlow",
                position: new Vector3(-2.4f, 1.0f, 2.1f),
                color: new Color(1f, 0.55f, 0.18f),
                baseIntensity: 1.2f, amplitude: 0.30f, range: 4.5f, speed: 7.2f, seed: 31.2f);
            EmberLightingBuilder.AddSpotFill(
                name: "AnvilRimFill",
                position: new Vector3(-4.5f, 2.6f, 4.5f),
                eulerAngles: new Vector3(40f, -135f, 0f),
                color: new Color(0.50f, 0.70f, 1f),
                intensity: 1.2f, range: 10f, spotAngle: 50f);

            // 4. Player rig
            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(spawnPosition, Quaternion.identity, fov: 70f);

            // 5. NPCs — smith routine (smith at anvil, apprentice patrolling)
            var smiths = new GameObject("Smiths").transform;
            EmberWorldspaceBuilder.SpawnActor("Smith_A", "blacksmith", new Vector3(-1.2f, 0f, 2.1f), smiths, "Warden");
            EmberWorldspaceBuilder.SpawnActor("Smith_B", "blacksmith", new Vector3( 1.2f, 0f, 0f),   smiths, "Quartermaster Ivo");

            // 6. Focal props — furnace cluster + anvil + tools
            var focal = new GameObject("FocalContent").transform;
            focal.position = new Vector3(0f, 1.4f, 3f);

            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Furnace", new Vector3(0f, 0.85f, 3f),
                material: EmberSceneMaterialLibrary.EmberLight(),
                scale: new Vector3(2.1f, 1.7f, 1.0f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Anvil", new Vector3(-2.4f, 0.45f, 2.1f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(1.5f, 0.5f, 0.8f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Bellows", new Vector3(2.2f, 0.50f, 3.4f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(0.7f, 0.6f, 1.4f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "AnvilHotSpot", new Vector3(-2.4f, 0.85f, 2.1f),
                material: EmberSceneMaterialLibrary.EmberLight(),
                scale: new Vector3(0.6f, 0.20f, 0.6f));

            // 7. Particles — sparks from anvil + chimney smoke from furnace
            EmberParticleBuilder.AddForgeSparks("Sparks_Anvil", new Vector3(-2.4f, 0.95f, 2.1f), focal);
            EmberParticleBuilder.AddForgeSparks("Sparks_Furnace", new Vector3(0f, 1.7f, 2.85f), focal);
            EmberParticleBuilder.AddChimneySmoke("ChimneySmoke", new Vector3(0f, 3.0f, 3.2f), focal);

            // 8. Decor sprites (AI-generated, already in CoreAssetManifest)
            EmberWorldspaceBuilder.SpawnDecorSprite("SmithTools", "Assets/Art/Items/smith_tools.png", new Vector3(-3.3f, 0.6f, 2.3f), 0.8f);
            EmberWorldspaceBuilder.SpawnDecorSprite("IronHammer", "Assets/Art/Items/iron_warhammer.png", new Vector3(2.7f, 0.6f, 2.2f), 0.8f);
            EmberWorldspaceBuilder.SpawnDecorSprite("FireEssence", "Assets/Art/Items/fire_essence.png", new Vector3(0f, 1.1f, 2.25f), 0.65f);

            // 9. Audio
            var ambience = new GameObject("ForgeAmbientPlaceholderLoop", typeof(AudioSource)).GetComponent<AudioSource>();
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0.65f;
            ambience.transform.position = new Vector3(0f, 1.5f, 3f);

            // 10. HUD overlay
            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                anchorMin: new Vector2(0f, 0.94f), anchorMax: new Vector2(1f, 1f),
                background: new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var jobPanel = EmberUiBuilder.BuildPanel(canvas, "JobQueuePanel",
                anchorMin: new Vector2(0f, 0.35f), anchorMax: new Vector2(0.22f, 0.94f),
                background: new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(jobPanel.gameObject, "EmberCrpg.Presentation.Ember.UI.JobQueuePanel");

            // 11. Exit portal to ColonyNeeds
            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(SmithingOverworldSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "ColonyNeeds", "→ Colony Needs");
        }
    }
}
