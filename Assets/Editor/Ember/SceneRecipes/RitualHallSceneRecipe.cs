using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Phase 8 acceptance: cast a spell, resolve a data-driven effect, see the world react.
    /// Builds a circular ritual hall: stone floor, a mage actor, an effigy target, a wide
    /// HUD area at the bottom for the spellbar and a side panel for spell descriptions.
    /// </summary>
    public sealed class RitualHallSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "RitualHall";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/marble.png", tiling: 6f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/dark_stone.png", tiling: 4f);

            var room = EmberTerrainBuilder.BuildRoom(Vector3.zero, 25f, 25f, 5f, floorMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);

            EmberLightingBuilder.SetAmbientMood(new Color(0.22f, 0.16f, 0.10f), 0.70f);
            EmberPostProcessBuilder.AddVolume("PostProcess_ShrineAmber", EmberMoodPreset.ShrineAmber);

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.7f, 0.8f, 1f),
                intensity: 0.8f,
                eulerAngles: new Vector3(80f, 0f, 0f));

            // Purple magical lights
            var magicLight = new GameObject("MagicalLight", typeof(Light));
            magicLight.transform.position = new Vector3(0f, 4f, 0f);
            var l = magicLight.GetComponent<Light>();
            l.type = LightType.Point;
            l.range = 20f;
            l.intensity = 1.0f;
            l.color = new Color(0.7f, 0.2f, 1f);

            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: spawnPosition,
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Mage",      "mage",        new Vector3(-1.5f, 0f, 0f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnActor("Apprentice","necromancer", new Vector3( 1.5f, 0f, 0f), domainActorKey: "Ash Rat");
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Effigy", new Vector3(0f, 0.75f, 5f));

            // UI-SINGLE-SOURCE: the standard HUD (EmberHud TopBar + bottom action bar, the colony
            // overlay, pause, dialog) is host-ensured by EmberWorldHost. We only author the empty
            // EmberHUD overlay canvas so the host has a canvas to attach to.
            // BUG-5: the scene-specific "SpellBar" used to sit at the very bottom (0–12% height) and
            // overlapped the host's action bar, so the ritual hall showed TWO stacked slot strips. The
            // action bar's CAST + number-key spell selection already covers casting, so the standalone
            // SpellBar is removed to leave a single, clean bottom bar.
            EmberUiBuilder.BuildOverlayCanvas("EmberHUD");

            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(RitualHallSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "TavernDialog", "→ Tavern Dialog");
        }
    }
}
