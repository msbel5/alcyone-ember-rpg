using System.Collections.Generic;
using System.Text;
using EmberCrpg.Data.GeneratedAssets;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedMeshToolAdapter
    {
        public static GeneratedExternalToolResult ExportOrRun(GeneratedMeshJob job, GeneratedAssetPipelineSettings settings, bool dryRun)
        {
            var manifestPath = job.outputMeshPath.Replace(".glb", ".json");
            GeneratedAssetEditorPathUtility.WriteAssetBytes(manifestPath, Encoding.UTF8.GetBytes(JsonUtility.ToJson(job, true)));

            var tokens = new Dictionary<string, string>
            {
                ["{jobJson}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(manifestPath),
                ["{prompt}"] = job.prompt,
                ["{negativePrompt}"] = job.negativePrompt,
                ["{sourceImagePath}"] = job.sourceImagePath,
                ["{outputMeshPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(job.outputMeshPath),
                ["{outputTextureFolder}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(job.outputTextureFolder),
            };

            var previewPath = GeneratedAssetEditorPathUtility.AssetToAbsolute(job.outputMeshPath + ".cmd");
            GeneratedExternalToolRunner.WriteCommandPreview(previewPath, settings.meshGenerationCommand, tokens);
            if (dryRun) return new GeneratedExternalToolResult { success = true };
            return GeneratedExternalToolRunner.Run(settings.meshGenerationCommand, tokens, settings.timeoutSeconds);
        }
    }
}
