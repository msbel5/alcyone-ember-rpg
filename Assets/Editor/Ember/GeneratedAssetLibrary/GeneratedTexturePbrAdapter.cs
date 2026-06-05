using System.Collections.Generic;
using EmberCrpg.Data.GeneratedAssets;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedTexturePbrAdapter
    {
        public static GeneratedExternalToolResult ExportOrRun(GeneratedAssetRecord record, GeneratedAssetPipelineSettings settings, bool dryRun)
        {
            var tokens = new Dictionary<string, string>
            {
                ["{inputAlbedoPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(string.IsNullOrWhiteSpace(record.deLitAlbedoPath) ? record.albedoPath : record.deLitAlbedoPath),
                ["{outputNormalPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(GeneratedTexturePathPolicy.ResolveNormalPath(record)),
                ["{outputRoughnessPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(GeneratedTexturePathPolicy.ResolveRoughnessPath(record)),
                ["{outputAoPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(GeneratedTexturePathPolicy.ResolveAoPath(record)),
                ["{outputHeightPath}"] = GeneratedAssetEditorPathUtility.AssetToAbsolute(GeneratedTexturePathPolicy.ResolveHeightPath(record)),
            };

            var previewPath = GeneratedAssetEditorPathUtility.AssetToAbsolute(GeneratedTexturePathPolicy.ResolveMaterialPath(record) + ".pbr.cmd");
            GeneratedExternalToolRunner.WriteCommandPreview(previewPath, settings.pbrMapCommand, tokens);
            if (dryRun) return new GeneratedExternalToolResult { success = true };
            return GeneratedExternalToolRunner.Run(settings.pbrMapCommand, tokens, settings.timeoutSeconds);
        }
    }
}
