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

            var safeName = Path.GetFileNameWithoutExtension(textureAssetPath);
            var matPath = $"{EmberAssetPaths.MaterialsRoot}/Tile_{safeName}.mat";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
            if (tex != null)
            {
                material.mainTexture = tex;
                material.mainTextureScale = new Vector2(tiling, tiling);
            }
            AssetDatabase.CreateAsset(material, matPath);
            return material;
        }
    }
}
