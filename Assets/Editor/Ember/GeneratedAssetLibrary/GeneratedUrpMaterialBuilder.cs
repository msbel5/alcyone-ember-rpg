using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.Common;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedUrpMaterialBuilder
    {
        public static string CreateOrUpdate(GeneratedAssetRecord record)
        {
            var materialPath = GeneratedTexturePathPolicy.ResolveMaterialPath(record);
            EmberSceneSavePolicy.EnsureFolderExists(EmberAssetPaths.MaterialsRoot + "/Generated");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            AssignTexture(material, "_BaseMap", string.IsNullOrWhiteSpace(record.deLitAlbedoPath) ? record.albedoPath : record.deLitAlbedoPath);
            AssignTexture(material, "_BumpMap", record.normalPath);
            AssignTexture(material, "_OcclusionMap", record.ambientOcclusionPath);
            material.SetFloat("_Smoothness", string.IsNullOrWhiteSpace(record.roughnessPath) ? 0.2f : 0.15f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            record.materialPath = materialPath;
            return materialPath;
        }

        private static void AssignTexture(Material material, string property, string path)
        {
            if (material == null || string.IsNullOrWhiteSpace(path) || !material.HasProperty(property)) return;
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (texture == null) return;
            material.SetTexture(property, texture);
        }
    }
}
