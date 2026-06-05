using System.Collections.Generic;
using EmberCrpg.Data.GeneratedAssets;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedAssetForgeAdapter
    {
        public static GeneratedExternalToolResult ExportOrRun(GeneratedAssetGenerationJob job, GeneratedAssetPipelineSettings settings)
        {
            var tokens = BuildTokens(job);
            if (settings.writeJobJson)
                GeneratedAssetEditorPathUtility.WriteAssetBytes(job.outputJsonPath, System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(job, true)));

            var previewPath = job.outputJsonPath.Replace(".json", ".cmd");
            GeneratedExternalToolRunner.WriteCommandPreview(GeneratedAssetEditorPathUtility.AssetToAbsolute(previewPath), settings.forgeCommand, tokens);

            if (settings.dryRun)
            {
                job.status = GeneratedAssetJobStatus.DryRunWritten;
                return new GeneratedExternalToolResult { success = true, commandLine = settings.forgeCommand.executablePath };
            }

            job.status = GeneratedAssetJobStatus.ExternalToolStarted;
            var result = GeneratedExternalToolRunner.Run(settings.forgeCommand, tokens, settings.timeoutSeconds);
            job.status = result.success ? GeneratedAssetJobStatus.GeneratedPngFound : GeneratedAssetJobStatus.ExternalToolFailed;
            return result;
        }

        private static Dictionary<string, string> BuildTokens(GeneratedAssetGenerationJob job)
        {
            return new Dictionary<string, string>
            {
                ["{jobJson}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(job.outputJsonPath),
                ["{positivePrompt}"] = job.positivePrompt,
                ["{negativePrompt}"] = job.negativePrompt,
                ["{seed}"] = job.seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["{width}"] = job.width.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["{height}"] = job.height.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["{outputPngPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(job.outputPngPath),
            };
        }
    }
}
