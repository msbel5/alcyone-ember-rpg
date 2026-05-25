using System.IO;
using EmberCrpg.Editor.Ember.Common;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Builds and caches lightweight materials for tile/wall surfaces. The factory keeps
    /// shader selection in one place so recipes don't pick shaders by string. Materials
    /// are persisted under <see cref="EmberAssetPaths.MaterialsRoot"/>.
    /// </summary>
    public static class EmberMaterialFactory
    {
        public static Material GetOrCreateTileMaterial(string textureAssetPath, float tiling = 4f)
        {
            EmberSceneSavePolicy.EnsureFolderExists(EmberAssetPaths.MaterialsRoot);

            if (string.IsNullOrWhiteSpace(textureAssetPath) || !File.Exists(textureAssetPath))
                textureAssetPath = EmberSceneMaterialLibrary.EnsureTexture(
                    "ember_surface_fallback",
                    new Color(0.18f, 0.15f, 0.12f),
                    new Color(0.28f, 0.23f, 0.18f));

            var safeName = Path.GetFileNameWithoutExtension(textureAssetPath);
            var matPath = $"{EmberAssetPaths.MaterialsRoot}/Tile_{safeName}.mat";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            
            Material material;
            if (existing != null)
            {
                material = existing;
                if (material.shader != shader) material.shader = shader;
            }
            else
            {
                material = new Material(shader);
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
            if (tex != null)
            {
                material.mainTexture = tex;
                material.mainTextureScale = new Vector2(tiling, tiling);
                if (shader.name.Contains("Universal Render Pipeline/Lit"))
                {
                    material.SetTexture("_BaseMap", tex);
                    material.SetVector("_BaseMap_ST", new Vector4(tiling, tiling, 0, 0));
                    material.SetColor("_BaseColor", new Color(0.72f, 0.66f, 0.58f, 1f));
                }
                else
                {
                    material.color = new Color(0.72f, 0.66f, 0.58f, 1f);
                }
            }
            
            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, matPath);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        public static Material GetOrCreateSolidMaterial(string materialName, Color color)
        {
            EmberSceneSavePolicy.EnsureFolderExists(EmberAssetPaths.MaterialsRoot);
            var matPath = $"{EmberAssetPaths.MaterialsRoot}/{materialName}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, matPath);
            }

            if (material.shader != shader) material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Texture2D ResolveMainTexture(Material material)
        {
            if (material == null) return null;
            if (material.mainTexture is Texture2D main) return main;
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") is Texture2D baseMap) return baseMap;
            return null;
        }
    }
}
