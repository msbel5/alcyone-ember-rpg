using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Presentation.Ember.Views;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedBillboardPrefabBuilder
    {
        public static string CreateOrUpdate(GeneratedAssetRecord record, GeneratedAssetPipelineSettings settings)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(record.spritePath);
            if (sprite == null) return string.Empty;

            var rootPath = GeneratedSpritePathPolicy.ResolvePrefabPath(record);
            EmberSceneSavePolicy.EnsureFolderExists(EmberAssetPaths.PrefabsRoot + "/Generated");

            var root = new GameObject(record.displayName);
            var billboard = new GameObject("Billboard");
            billboard.transform.SetParent(root.transform, false);
            billboard.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var renderer = billboard.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;
            billboard.AddComponent<CameraFacingBillboard>();
            FitHeight(billboard.transform, renderer, settings.billboardTargetHeight);

            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, rootPath);
                AssetDatabase.SaveAssets();
                return rootPath;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void FitHeight(Transform transform, SpriteRenderer renderer, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null) return;
            var spriteHeight = renderer.sprite.bounds.size.y;
            if (spriteHeight <= 0.001f) return;
            var scale = Mathf.Clamp(targetHeight / spriteHeight, 0.02f, 3f);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
