using EmberCrpg.Editor.Ember.Common;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    public static class EmberSceneSurfaceSanitizer
    {
        public static void ApplyToOpenScene()
        {
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled) continue;
                var material = ChooseMaterial(renderer.gameObject.name, renderer.sharedMaterial);
                if (material != null) renderer.sharedMaterial = material;
            }

            foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
                EnsureTerrainLayer(terrain);

            foreach (var spriteRenderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                SanitizeSprite(spriteRenderer);
        }

        private static Material ChooseMaterial(string name, Material current)
        {
            if (name.Contains("Portal")) return EmberSceneMaterialLibrary.Portal();
            if (name.Contains("Wall") || name.Contains("Boundary") || name.Contains("Backdrop")) return EmberSceneMaterialLibrary.Wall();
            if (name.Contains("Floor") || name.Contains("Ground") || name.Contains("Field") || name.Contains("Path")) return EmberSceneMaterialLibrary.Floor();
            if (current == null || LooksDefaultWhite(current)) return EmberSceneMaterialLibrary.Prop();
            return current;
        }

        private static bool LooksDefaultWhite(Material material)
        {
            if (material == null) return true;
            if (material.HasProperty("_BaseColor") && IsNearWhite(material.GetColor("_BaseColor"))) return true;
            return material.HasProperty("_Color") && IsNearWhite(material.color);
        }

        private static bool IsNearWhite(Color color)
        {
            return color.r > 0.92f && color.g > 0.92f && color.b > 0.92f;
        }

        private static void EnsureTerrainLayer(Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null) return;
            var layers = terrain.terrainData.terrainLayers;
            if (layers != null && layers.Length > 0 && layers[0] != null && layers[0].diffuseTexture != null) return;

            var texture = EmberMaterialFactory.ResolveMainTexture(EmberSceneMaterialLibrary.Floor());
            if (texture == null) return;

            Directory.CreateDirectory("Assets/Scenes/Ember/TerrainData");
            var path = "Assets/Scenes/Ember/TerrainData/" + terrain.gameObject.name + "_Sanitized.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = texture;
            layer.tileSize = new Vector2(5f, 5f);
            terrain.terrainData.terrainLayers = new[] { layer };
            EditorUtility.SetDirty(layer);
            EditorUtility.SetDirty(terrain.terrainData);
        }

        private static void SanitizeSprite(SpriteRenderer renderer)
        {
            if (renderer.sprite == null)
                renderer.sprite = LoadFallbackSprite();
            if (renderer.sprite == null) return;

            var height = renderer.sprite.bounds.size.y;
            if (height <= 0.001f) return;
            var scale = Mathf.Clamp(2.1f / height, 0.02f, 3f);
            renderer.transform.localScale = new Vector3(scale, scale, scale);
        }

        private static Sprite LoadFallbackSprite()
        {
            var path = EmberAssetPaths.CharactersDir + "/warrior.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite child) return child;
            return null;
        }
    }
}
