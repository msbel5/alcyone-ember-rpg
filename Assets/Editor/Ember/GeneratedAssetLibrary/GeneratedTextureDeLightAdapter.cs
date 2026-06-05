using System.Collections.Generic;
using EmberCrpg.Data.GeneratedAssets;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedTextureDeLightAdapter
    {
        public static GeneratedExternalToolResult ExportOrRun(GeneratedAssetRecord record, GeneratedAssetPipelineSettings settings, bool dryRun)
        {
            var tokens = new Dictionary<string, string>
            {
                ["{inputAlbedoPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(record.albedoPath),
                ["{outputAlbedoPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(GeneratedTexturePathPolicy.ResolveDeLitAlbedoPath(record)),
            };

            var previewPath = GeneratedAssetEditorPathUtility.AssetToAbsolute(GeneratedTexturePathPolicy.ResolveDeLitAlbedoPath(record) + ".cmd");
            GeneratedExternalToolRunner.WriteCommandPreview(previewPath, settings.deLightCommand, tokens);
            if (dryRun) return new GeneratedExternalToolResult { success = true };
            return GeneratedExternalToolRunner.Run(settings.deLightCommand, tokens, settings.timeoutSeconds);
        }
    }
}
