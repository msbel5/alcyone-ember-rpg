using System.Collections.Generic;
using EmberCrpg.Data.GeneratedAssets;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedAssetMatteAdapter
    {
        public static GeneratedExternalToolResult ExportOrRun(GeneratedAssetGenerationJob job, GeneratedAssetPipelineSettings settings)
        {
            var tokens = new Dictionary<string, string>
            {
                ["{inputPngPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(job.rawGeneratedPngPath),
                ["{outputAlphaPngPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(job.expectedAlphaPngPath),
                ["{jobJson}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(job.outputJsonPath),
            };

            var previewPath = job.outputJsonPath.Replace(".json", ".matte.cmd");
            GeneratedExternalToolRunner.WriteCommandPreview(GeneratedAssetEditorPathUtility.AssetToAbsolute(previewPath), settings.alphaMatteCommand, tokens);
            if (settings.dryRun)
            {
                job.status = GeneratedAssetJobStatus.DryRunWritten;
                return new GeneratedExternalToolResult { success = true };
            }

            var result = GeneratedExternalToolRunner.Run(settings.alphaMatteCommand, tokens, settings.timeoutSeconds);
            job.status = result.success ? GeneratedAssetJobStatus.MatteApplied : GeneratedAssetJobStatus.ExternalToolFailed;
            return result;
        }
    }
}
