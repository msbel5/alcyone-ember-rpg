// Why: AAA Sprint B target — CombatDungeon is the player's first combat
// encounter scene. Template for every procedurally generated dungeon entrance
// (per aaa-scene-quality-uplift.md §12). Mood = cold blue Daggerfall dungeon
// with flickering wall torches and atmospheric fog.
using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Phase 7 acceptance: melee swing, damage roll, equipped weapon affects
    /// outcome. Mood: cold-blue Daggerfall dungeon — dim sun, 4 flickering
    /// wall torches, cold-blue ambient fog volume, vignette pushed toward
    /// danger.
    /// </summary>
    public sealed class CombatDungeonSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "CombatDungeon";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/dark_stone.png", tiling: 6f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/stone_wall.png", tiling: 3f);

            // 1. World shell — wider dungeon hall
            var room = EmberTerrainBuilder.BuildRoom(Vector3.zero, 24f, 24f, 4f, floorMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);

            // 2. Mood — cold blue ambient + dungeon cold post-process + URP fog
            EmberLightingBuilder.SetAmbientMood(
                color: new Color(0.10f, 0.13f, 0.18f),
                intensity: 0.55f,
                enableFog: true, fogStart: 8f, fogEnd: 40f,
                fogColor: new Color(0.12f, 0.18f, 0.26f));
            EmberPostProcessBuilder.AddVolume("PostProcess_DungeonCold", EmberMoodPreset.DungeonCold);

            // 3. Lighting — dim cold sun + 4 flickering wall torches
            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.55f, 0.65f, 0.80f),
                intensity: 0.30f,
                eulerAngles: new Vector3(70f, 10f, 0f));

            EmberLightingBuilder.AddFlickeringPointLight(
                name: "Torch_NW", position: new Vector3(-9f, 2.5f, 9f),
                color: new Color(1f, 0.55f, 0.20f),
                baseIntensity: 1.6f, amplitude: 0.40f, range: 9f, speed: 5.8f, seed: 13.7f);
            EmberLightingBuilder.AddFlickeringPointLight(
                name: "Torch_NE", position: new Vector3(9f, 2.5f, 9f),
                color: new Color(1f, 0.55f, 0.20f),
                baseIntensity: 1.6f, amplitude: 0.40f, range: 9f, speed: 5.4f, seed: 22.1f);
            EmberLightingBuilder.AddFlickeringPointLight(
                name: "Torch_SW", position: new Vector3(-9f, 2.5f, -3f),
                color: new Color(1f, 0.55f, 0.20f),
                baseIntensity: 1.6f, amplitude: 0.40f, range: 9f, speed: 6.1f, seed: 7.3f);
            EmberLightingBuilder.AddFlickeringPointLight(
                name: "Torch_SE", position: new Vector3(9f, 2.5f, -3f),
                color: new Color(1f, 0.55f, 0.20f),
                baseIntensity: 1.6f, amplitude: 0.40f, range: 9f, speed: 5.9f, seed: 19.4f);

            // 4. Player rig
            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(spawnPosition, Quaternion.identity, fov: 60f);

            // 5. Enemies — 2 goblins flanking, bandit lord at back
            var enemies = new GameObject("Enemies").transform;
            EmberWorldspaceBuilder.SpawnActor("Goblin_A", "goblin", new Vector3(-2f, 0f, 4f), enemies, "Ash Rat");
            EmberWorldspaceBuilder.SpawnActor("Goblin_B", "goblin", new Vector3( 2f, 0f, 4f), enemies, "Sentinel Rook");
            EmberWorldspaceBuilder.SpawnActor("Bandit_Lord", "bandit", new Vector3(0f, 0f, 7f), enemies, "Warden");

            // 6. Focal props — torch posts on walls, chest at back
            var focal = new GameObject("FocalContent").transform;
            focal.position = new Vector3(0f, 1f, 6f);

            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "TorchPost_NW", new Vector3(-9f, 1.3f, 9f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(0.25f, 2.5f, 0.25f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "TorchPost_NE", new Vector3(9f, 1.3f, 9f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(0.25f, 2.5f, 0.25f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "TorchPost_SW", new Vector3(-9f, 1.3f, -3f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(0.25f, 2.5f, 0.25f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "TorchPost_SE", new Vector3(9f, 1.3f, -3f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(0.25f, 2.5f, 0.25f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Chest", new Vector3(0f, 0.4f, 8.5f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(1.2f, 0.7f, 0.8f));

            // 7. Particles — torch flames + dungeon fog mid-room
            EmberParticleBuilder.AddTorchFlame("TorchFlame_NW", new Vector3(-9f, 2.7f, 9f), focal);
            EmberParticleBuilder.AddTorchFlame("TorchFlame_NE", new Vector3(9f, 2.7f, 9f), focal);
            EmberParticleBuilder.AddTorchFlame("TorchFlame_SW", new Vector3(-9f, 2.7f, -3f), focal);
            EmberParticleBuilder.AddTorchFlame("TorchFlame_SE", new Vector3(9f, 2.7f, -3f), focal);
            EmberParticleBuilder.AddFogVolume("DungeonFog", new Vector3(0f, 1.5f, 5f), focal, radius: 8f);

            // 8. Audio
            var ambience = new GameObject("DungeonAmbientPlaceholderLoop", typeof(AudioSource)).GetComponent<AudioSource>();
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0.65f;
            ambience.transform.position = new Vector3(0f, 1.5f, 4f);

            // 9. HUD overlay
            // UI-SINGLE-SOURCE: HUD comes from one place. The recipe used to author a bottom-bar
            // CombatHud, but EmberWorldHost always ensures the standard EmberHud (which reads combat
            // vitals via ICombatHudSource) and disables any CombatHud at runtime — so authoring one
            // here only produced a redundant panel that was immediately hidden. Author just the overlay
            // canvas; the host mounts the standard HUD set onto it.
            EmberUiBuilder.BuildOverlayCanvas("EmberHUD");

            // 10. Exit portal to RitualHall
            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(CombatDungeonSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "RitualHall", "→ Ritual Hall");
        }
    }
}
