using System.Collections.Generic;
using System.IO;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.AssetImport;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedSpriteImportUtility
    {
        public static void ImportExistingPng(GeneratedAssetRecord record, string sourcePngPath, GeneratedAssetPipelineSettings settings)
        {
            record.SyncIdentity();
            var job = record.spriteJob ?? new GeneratedAssetGenerationJob();
            if (string.IsNullOrWhiteSpace(job.jobId))
            {
                job.stableAssetId = record.stableId;
                job.sourceRecordStableId = record.stableId;
                job.kind = record.kind;
                job.seed = record.seed;
                job.SyncId();
            }

            GeneratedSpritePathPolicy.ConfigureJobPaths(record, job, settings);
            GeneratedAssetEditorPathUtility.WriteAssetBytes(job.rawGeneratedPngPath, File.ReadAllBytes(sourcePngPath));
            AssetDatabase.ImportAsset(job.rawGeneratedPngPath, ImportAssetOptions.ForceUpdate);

            var analysis = GeneratedSpriteCropper.CropLargestComponent(job.rawGeneratedPngPath, job.croppedSpritePath, settings.alphaThreshold, settings.cropPadding, settings.minimumLargeComponentPixels);
            AssetDatabase.ImportAsset(job.croppedSpritePath, ImportAssetOptions.ForceUpdate);
            ApplySpriteImportSettings(job.croppedSpritePath, settings);

            record.spriteJob = job;
            record.spritePath = job.croppedSpritePath;
            record.relativeAssetPath = job.croppedSpritePath;
            record.previewPath = job.croppedSpritePath;
            record.validationWarnings = new List<string>(analysis.warnings);
            record.spriteJob.validationWarnings = new List<string>(analysis.warnings);
            record.licenseStatus = GeneratedAssetLicenseStatus.NeedsReview;
            record.humanApproved = false;
            record.spriteJob.status = GeneratedAssetJobStatus.Imported;

            if (settings.autoCreateBillboardPrefab)
                record.prefabPath = GeneratedBillboardPrefabBuilder.CreateOrUpdate(record, settings);

            AssetDatabase.SaveAssets();
        }

        public static void ApplySpriteImportSettings(string assetPath, GeneratedAssetPipelineSettings settings)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            var profile = EmberSpriteImportRules.For(EmberAssetCategoryClassifier.Classify(assetPath));

            importer.textureType = profile?.TextureType ?? TextureImporterType.Sprite;
            importer.spriteImportMode = profile?.SpriteMode ?? SpriteImportMode.Single;
            importer.spritePixelsPerUnit = settings.spritePixelsPerUnit;
            importer.filterMode = settings.spriteFilterMode;
            importer.wrapMode = profile?.WrapMode ?? TextureWrapMode.Clamp;
            importer.mipmapEnabled = settings.spriteMipMaps;
            importer.maxTextureSize = settings.maxSpriteTextureSize;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.textureCompression = settings.spriteCompression;
            importer.SaveAndReimport();
        }
    }
}
