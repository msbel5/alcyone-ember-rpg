using System.Collections.Generic;
using System.IO;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.AssetImport;
using EmberCrpg.Editor.Ember.Common;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedTextureImportUtility
    {
        public static void ApplyExistingColorTextureSettings(string assetPath, GeneratedAssetPipelineSettings settings, bool repeat, bool sRgb)
        {
            ApplyColorTextureSettings(assetPath, settings, repeat, sRgb);
        }

        public static GeneratedTextureValidationReport ImportAlbedo(GeneratedAssetRecord record, string sourcePngPath, GeneratedAssetPipelineSettings settings)
        {
            var target = GeneratedTexturePathPolicy.ResolveAlbedoPath(record);
            Copy(sourcePngPath, target);
            ApplyColorTextureSettings(target, settings, repeat: true, sRgb: true);
            record.albedoPath = target;
            record.isTileable = true;
            return ValidateAlbedo(target, settings);
        }

        public static void ImportMap(string sourcePngPath, string targetAssetPath, GeneratedAssetPipelineSettings settings, bool normalMap, bool sRgb)
        {
            Copy(sourcePngPath, targetAssetPath);
            var importer = AssetImporter.GetAtPath(targetAssetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = settings.materialFilterMode;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = settings.materialMaxTextureSize;
            importer.sRGBTexture = sRgb;
            importer.textureCompression = settings.materialCompression;
            importer.SaveAndReimport();
        }

        public static GeneratedTextureValidationReport ValidateAlbedo(string assetPath, GeneratedAssetPipelineSettings settings)
        {
            var absolute = GeneratedAssetEditorPathUtility.AssetToAbsolute(assetPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(absolute));
            try
            {
                var pixels = tex.GetPixels32();
                var rgba = new byte[pixels.Length * 4];
                for (var i = 0; i < pixels.Length; i++)
                {
                    var offset = i * 4;
                    rgba[offset] = pixels[i].r;
                    rgba[offset + 1] = pixels[i].g;
                    rgba[offset + 2] = pixels[i].b;
                    rgba[offset + 3] = pixels[i].a;
                }

                return GeneratedTextureTileabilityAnalyzer.Analyze(tex.width, tex.height, rgba, settings.tileabilityEdgeThreshold, settings.gradientThreshold);
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        private static void ApplyColorTextureSettings(string assetPath, GeneratedAssetPipelineSettings settings, bool repeat, bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            var profile = EmberTextureImportRules.For(EmberAssetCategoryClassifier.Classify(assetPath));
            importer.textureType = profile?.TextureType ?? TextureImporterType.Default;
            importer.textureShape = profile?.TextureShape ?? TextureImporterShape.Texture2D;
            importer.filterMode = settings.materialFilterMode;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = settings.materialMaxTextureSize;
            importer.sRGBTexture = sRgb;
            importer.textureCompression = settings.materialCompression;
            importer.SaveAndReimport();
        }

        private static void Copy(string sourcePngPath, string targetAssetPath)
        {
            EmberSceneSavePolicy.EnsureFolderExists(Path.GetDirectoryName(targetAssetPath)?.Replace('\\', '/'));
            File.Copy(sourcePngPath, GeneratedAssetEditorPathUtility.AssetToAbsolute(targetAssetPath), true);
            AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
