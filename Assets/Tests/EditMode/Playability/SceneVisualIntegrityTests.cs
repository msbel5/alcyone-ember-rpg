#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmberCrpg.Tests.EditMode.Playability
{
    public sealed class SceneVisualIntegrityTests
    {
        private static readonly string[] CanonicalScenes =
        {
            "SmithingOverworld",
            "ColonyNeeds",
            "SeasonFarm",
            "TradeMarket",
            "CombatDungeon",
            "RitualHall",
            "TavernDialog",
            "OracleShrine",
            "ShowroomOverview",
            "TavernFlavour",
        };

        [Test]
        public void CanonicalScenes_DoNotShipDefaultWhitePlayableSurfaces()
        {
            foreach (var sceneName in CanonicalScenes)
            {
                OpenScene(sceneName);
                var offenders = new List<string>();
                foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (!IsPlayableSurface(renderer.gameObject.name)) continue;
                    var mat = renderer.sharedMaterial;
                    if (mat == null || LooksDefaultWhite(mat))
                        offenders.Add(renderer.gameObject.name);
                }

                foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
                {
                    var layers = terrain.terrainData != null ? terrain.terrainData.terrainLayers : null;
                    if (layers == null || layers.Length == 0 || layers[0] == null || layers[0].diffuseTexture == null)
                        offenders.Add(terrain.gameObject.name + ":terrain-layer");
                }

                Assert.That(offenders, Is.Empty, sceneName + " has default/white playable surfaces.");
            }
        }

        [Test]
        public void CanonicalScenes_BillboardsHaveSpritesAndPlayableScale()
        {
            foreach (var sceneName in CanonicalScenes)
            {
                OpenScene(sceneName);
                foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                {
                    Assert.That(renderer.sprite, Is.Not.Null, sceneName + "/" + renderer.name + " missing sprite.");
                    Assert.That(renderer.sprite.texture.width, Is.GreaterThanOrEqualTo(64), sceneName + "/" + renderer.name + " uses tiny texture.");
                    Assert.That(renderer.sprite.texture.height, Is.GreaterThanOrEqualTo(64), sceneName + "/" + renderer.name + " uses tiny texture.");
                    var height = renderer.bounds.size.y;
                    Assert.That(height, Is.InRange(1.2f, 3.2f), sceneName + "/" + renderer.name + " billboard height is not playable.");
                }
            }
        }

        [Test]
        public void CanonicalScenes_StartCameraCanSeeActorsAtComfortDistance()
        {
            foreach (var sceneName in CanonicalScenes)
            {
                OpenScene(sceneName);
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null, sceneName + " missing MainCamera.");

                var visibleActors = 0;
                foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                {
                    var toActor = renderer.bounds.center - camera.transform.position;
                    var forwardDistance = Vector3.Dot(camera.transform.forward, toActor);
                    if (forwardDistance > 0f)
                        visibleActors++;
                    Assert.That(forwardDistance, Is.GreaterThan(2f), sceneName + "/" + renderer.name + " starts too close or behind camera.");
                }

                Assert.That(visibleActors, Is.GreaterThan(0), sceneName + " has no actor in front of start camera.");
            }
        }

        private static void OpenScene(string sceneName)
        {
            var path = "Assets/Scenes/Ember/" + sceneName + ".unity";
            Assert.That(File.Exists(path), Is.True, "Missing scene: " + path);
            EditorSceneManager.OpenScene(path);
        }

        private static bool IsPlayableSurface(string name)
        {
            return name.Contains("Floor") || name.Contains("Ground") || name.Contains("Field") ||
                   name.Contains("Path") || name.Contains("Wall") || name.Contains("Boundary") ||
                   name.Contains("Furnace") || name.Contains("Forge") || name.Contains("Podium") ||
                   name.Contains("Shed") || name.Contains("Portal");
        }

        private static bool LooksDefaultWhite(Material material)
        {
            if (material.HasProperty("_BaseColor") && IsNearWhite(material.GetColor("_BaseColor"))) return true;
            if (material.HasProperty("_Color") && IsNearWhite(material.color)) return true;
            return material.mainTexture == null && material.name.Contains("Default");
        }

        private static bool IsNearWhite(Color color)
        {
            return color.r > 0.92f && color.g > 0.92f && color.b > 0.92f;
        }
    }
}
#endif
