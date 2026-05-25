#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmberCrpg.Tests.EditMode.Playability
{
    public sealed class RouteAndSceneRescueTests
    {
        [TestCase("SmithingOverworld")]
        [TestCase("TavernDialog")]
        public void PlayableScenes_HaveExplicitSpawnAndHeroCameraInsideFloor(string sceneName)
        {
            OpenScene(sceneName);

            var spawn = GameObject.Find("PlayerSpawn");
            Assert.That(spawn, Is.Not.Null, sceneName + " missing PlayerSpawn marker.");
            var anchor = GameObject.Find("HeroCameraAnchor");
            Assert.That(anchor, Is.Not.Null, sceneName + " missing HeroCameraAnchor marker.");

            var floor = GameObject.Find("Floor");
            Assert.That(floor, Is.Not.Null, sceneName + " missing Floor object.");
            var bounds = FloorBounds(floor);
            Assert.That(spawn.transform.position.x, Is.InRange(bounds.min.x, bounds.max.x), sceneName + " spawn X outside floor.");
            Assert.That(spawn.transform.position.z, Is.InRange(bounds.min.z, bounds.max.z), sceneName + " spawn Z outside floor.");
            Assert.That(spawn.transform.position.y, Is.GreaterThanOrEqualTo(bounds.min.y + 0.1f), sceneName + " spawn too low.");

            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null, sceneName + " missing MainCamera.");
            var focal = GameObject.Find("FocalContent");
            Assert.That(focal, Is.Not.Null, sceneName + " missing FocalContent marker.");
            var forwardDistance = Vector3.Dot(camera.transform.forward, focal.transform.position - camera.transform.position);
            Assert.That(forwardDistance, Is.GreaterThan(2f), sceneName + " camera does not face focal content.");
        }

        [TestCase("SmithingOverworld", "smithing_warm_stone_floor", "smithing_dark_forge_wall")]
        [TestCase("TavernDialog", "tavern_wood_floor", "tavern_plaster_stone_wall")]
        public void PlayableScenes_UseDistinctFloorWallTextures(string sceneName, string floorNeedle, string wallNeedle)
        {
            OpenScene(sceneName);

            var floor = GameObject.Find("Floor");
            Assert.That(floor, Is.Not.Null);
            var terrain = floor.GetComponent<Terrain>();
            Assert.That(terrain, Is.Not.Null);
            var floorPath = AssetDatabase.GetAssetPath(terrain.terrainData.terrainLayers[0].diffuseTexture);
            Assert.That(floorPath, Does.Contain(floorNeedle));

            var wall = GameObject.Find("Wall_North");
            Assert.That(wall, Is.Not.Null);
            var wallRenderer = wall.GetComponent<MeshRenderer>();
            Assert.That(wallRenderer, Is.Not.Null);
            var wallTexture = wallRenderer.sharedMaterial.GetTexture("_BaseMap") ?? wallRenderer.sharedMaterial.mainTexture;
            var wallPath = AssetDatabase.GetAssetPath(wallTexture);
            Assert.That(wallPath, Does.Contain(wallNeedle));
            Assert.That(wallPath, Is.Not.EqualTo(floorPath));
        }

        [TestCase("SmithingOverworld")]
        [TestCase("TavernDialog")]
        public void PlayableScenes_UseAtLeastThreeReadableArtAssets(string sceneName)
        {
            OpenScene(sceneName);

            var uniqueArtPaths = new HashSet<string>();
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.sprite == null) continue;
                var path = AssetDatabase.GetAssetPath(renderer.sprite);
                if (!path.StartsWith("Assets/Art/")) continue;
                if (File.Exists(path) && new FileInfo(path).Length >= 4096)
                    uniqueArtPaths.Add(path);
            }

            Assert.That(uniqueArtPaths.Count, Is.GreaterThanOrEqualTo(3),
                sceneName + " must visibly use at least 3 readable existing art assets.");
        }

        private static void OpenScene(string sceneName)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Ember/" + sceneName + ".unity");
        }

        private static Bounds FloorBounds(GameObject floor)
        {
            var terrain = floor.GetComponent<Terrain>();
            if (terrain != null && terrain.terrainData != null)
                return new Bounds(terrain.transform.position + terrain.terrainData.size * 0.5f, terrain.terrainData.size);
            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;
            var collider = floor.GetComponent<Collider>();
            if (collider != null) return collider.bounds;
            return new Bounds(floor.transform.position, Vector3.one);
        }
    }
}
#endif
