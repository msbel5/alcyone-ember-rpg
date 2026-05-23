using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 7 acceptance: melee swing, damage roll, equipped weapon affects outcome.
    /// Builds a dungeon-style interior with stone walls, two enemies, a chest, dim sun.
    /// </summary>
    public sealed class CombatDungeonSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz7CombatDungeon";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/dark_stone.png", tiling: 6f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/stone_wall.png", tiling: 3f);

            EmberTerrainBuilder.BuildRoom(Vector3.zero, 24f, 24f, 4f, floorMat, wallMat);

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.7f, 0.65f, 0.6f),
                intensity: 0.6f,
                eulerAngles: new Vector3(70f, 10f, 0f));
            
            // Blue ambient torches
            for(int i=0; i<4; i++) {
                var torch = new GameObject($"Torch_{i}", typeof(Light));
                torch.transform.position = new Vector3(i % 2 == 0 ? 10 : -10, 2.5f, i < 2 ? 10 : -10);
                var l = torch.GetComponent<Light>();
                l.type = LightType.Point;
                l.range = 12f;
                l.intensity = 0.4f;
                l.color = new Color(0.4f, 0.6f, 1f);
            }

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -7f),
                spawnRotation: Quaternion.identity,
                fov: 60f);

            var enemies = new GameObject("Enemies").transform;
            EmberWorldspaceBuilder.SpawnActor("Goblin_A", "goblin", new Vector3(-2f, 0f, 4f), enemies, "Ash Rat");
            EmberWorldspaceBuilder.SpawnActor("Goblin_B", "goblin", new Vector3( 2f, 0f, 4f), enemies, "Sentinel Rook");
            EmberWorldspaceBuilder.SpawnActor("Bandit_Lord", "bandit", new Vector3(0f, 0f, 7f), enemies, "Warden");

            EmberWorldspaceBuilder.SpawnWorksiteMarker("Chest", new Vector3(0f, 0.5f, 8.5f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var combatHud = EmberUiBuilder.BuildPanel(canvas, "CombatHud",
                new Vector2(0f, 0f), new Vector2(1f, 0.18f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(combatHud.gameObject, "EmberCrpg.Presentation.Ember.UI.CombatHud");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 15f), "Faz8RitualHall", "→ Faz 8");
        }
    }
}
