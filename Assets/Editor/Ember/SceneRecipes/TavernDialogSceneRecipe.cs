// Why: AAA Sprint A.2 — TavernDialog is the player's first proper Ask-About
// conversation site. Template for every "inn"-tagged settlement (per
// aaa-scene-quality-uplift.md §12). Mood = candle-warm tavern with hearth.
using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Phase 9 acceptance: walk up to innkeeper, ask about topics, get answers
    /// rooted in memory + faction state. Mood: candle-warm tavern — flickering
    /// hearth on the side wall, candle flames at each table, low warm ambient.
    /// </summary>
    public sealed class TavernDialogSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "TavernDialog";

        public void Build()
        {
            var floorMat = EmberSceneMaterialLibrary.TavernFloor();
            var wallMat = EmberSceneMaterialLibrary.TavernWall();

            // 1. World shell
            var room = EmberTerrainBuilder.BuildRoom(Vector3.zero, 18f, 18f, 3.5f, floorMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);

            // 2. Mood — candle warmth + amber bloom + warm vignette
            EmberLightingBuilder.SetAmbientMood(
                color: new Color(0.25f, 0.18f, 0.12f),
                intensity: 0.75f);
            EmberPostProcessBuilder.AddVolume("PostProcess_TavernCandle", EmberMoodPreset.TavernCandle);

            // 3. Lighting rig — dim outdoor sun + hearth flicker + 3 candle flickers
            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.50f, 0.64f, 0.95f),
                intensity: 0.28f,
                eulerAngles: new Vector3(45f, 135f, 0f));
            EmberLightingBuilder.AddFlickeringPointLight(
                name: "HearthCore",
                position: new Vector3(-6.5f, 1.0f, 4f),
                color: new Color(1f, 0.55f, 0.18f),
                baseIntensity: 2.4f, amplitude: 0.55f, range: 10f, speed: 4.8f, seed: 9.1f);
            EmberLightingBuilder.AddFlickeringPointLight(
                name: "TableCandle_Center",
                position: new Vector3(0f, 1.2f, 2.5f),
                color: new Color(1f, 0.78f, 0.32f),
                baseIntensity: 0.85f, amplitude: 0.20f, range: 3.5f, speed: 6.4f, seed: 17.3f);
            EmberLightingBuilder.AddFlickeringPointLight(
                name: "TableCandle_Left",
                position: new Vector3(-3.5f, 1.2f, 1.5f),
                color: new Color(1f, 0.78f, 0.32f),
                baseIntensity: 0.7f, amplitude: 0.18f, range: 3.2f, speed: 5.9f, seed: 24.9f);
            EmberLightingBuilder.AddFlickeringPointLight(
                name: "TableCandle_Right",
                position: new Vector3(3.5f, 1.2f, 1.5f),
                color: new Color(1f, 0.78f, 0.32f),
                baseIntensity: 0.7f, amplitude: 0.18f, range: 3.2f, speed: 5.6f, seed: 41.5f);

            // 4. Player rig
            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(spawnPosition, Quaternion.identity);

            // 5. NPCs — innkeeper at bar, patron + sage at tables
            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper",    new Vector3(0f, 0f, 3f),  domainActorKey: "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnActor("Patron",    "warrior",      new Vector3(-3f, 0f, 1f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnActor("Sage",      "sage",         new Vector3( 3f, 0f, 1f), domainActorKey: "Sage Nera");

            // 6. Focal props — bar counter + hearth + 2 tables + 2 stools
            var focal = new GameObject("FocalContent").transform;
            focal.position = new Vector3(0f, 1.2f, 4f);

            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "BarCounter", new Vector3(0f, 0.65f, 5.2f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(5.2f, 1.0f, 0.8f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "BarShelves", new Vector3(0f, 1.7f, 5.7f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(5.0f, 0.8f, 0.3f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Hearth", new Vector3(-6.5f, 0.85f, 4f),
                material: EmberSceneMaterialLibrary.EmberLight(),
                scale: new Vector3(0.7f, 1.5f, 2.2f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Table_Center", new Vector3(0f, 0.45f, 2.5f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(1.4f, 0.9f, 1.4f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Table_Left", new Vector3(-3.5f, 0.45f, 1.5f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(1.2f, 0.9f, 1.2f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Table_Right", new Vector3(3.5f, 0.45f, 1.5f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(1.2f, 0.9f, 1.2f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "LeftStool", new Vector3(-2.0f, 0.45f, 2.2f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(0.55f, 0.65f, 0.55f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "RightStool", new Vector3(2.0f, 0.45f, 2.2f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(0.55f, 0.65f, 0.55f));

            // 7. Particles — candle flames on each table + chimney smoke from hearth
            EmberParticleBuilder.AddCandleFlame("Candle_Center", new Vector3(0f, 1.05f, 2.5f), focal);
            EmberParticleBuilder.AddCandleFlame("Candle_Left",   new Vector3(-3.5f, 1.05f, 1.5f), focal);
            EmberParticleBuilder.AddCandleFlame("Candle_Right",  new Vector3(3.5f, 1.05f, 1.5f), focal);
            EmberParticleBuilder.AddChimneySmoke("HearthSmoke", new Vector3(-6.5f, 2.4f, 4f), focal);

            // 8. Decor — bottled items on the bar
            EmberWorldspaceBuilder.SpawnDecorSprite("BottledSunlight", "Assets/Art/Items/bottled_sunlight.png", new Vector3(-1.6f, 1.15f, 5.0f), 0.55f);
            EmberWorldspaceBuilder.SpawnDecorSprite("ManaPotion", "Assets/Art/Items/mana_potion.png", new Vector3(0f, 1.15f, 5.0f), 0.55f);
            EmberWorldspaceBuilder.SpawnDecorSprite("CodedMessage", "Assets/Art/Items/coded_message.png", new Vector3(1.6f, 1.15f, 5.0f), 0.55f);

            // 9. Audio
            var ambience = new GameObject("TavernAmbientPlaceholderLoop", typeof(AudioSource)).GetComponent<AudioSource>();
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0.35f;
            ambience.transform.position = new Vector3(0f, 1.5f, 3f);

            // 10. HUD overlay
            // UI-SINGLE-SOURCE: HUD + dialog come from one place. The recipe used to author a TopBar
            // (EmberHud) and a DialogBox inline, but EmberWorldHost now ensures the standard EmberHud and
            // a single DialogBoxPanel (self-pinned to the canonical bottom-centered footprint) at runtime
            // in every scene. The Ask-About conversation here surfaces through that host-ensured dialog
            // box (single source, no per-scene duplicate). Author only the overlay canvas as their parent.
            EmberUiBuilder.BuildOverlayCanvas("EmberHUD");

            // 11. Exit portal to OracleShrine
            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(TavernDialogSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "OracleShrine", "→ Oracle Shrine");
        }
    }
}
