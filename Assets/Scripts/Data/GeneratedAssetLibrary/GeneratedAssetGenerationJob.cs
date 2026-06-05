using System;
using System.Collections.Generic;

namespace EmberCrpg.Data.GeneratedAssets
{
    [Serializable]
    public sealed class GeneratedAssetGenerationJob
    {
        public string jobId = string.Empty;
        public string stableAssetId = string.Empty;
        public GeneratedAssetKind kind = GeneratedAssetKind.ItemBillboard;
        public string presetName = string.Empty;
        public string positivePrompt = string.Empty;
        public string negativePrompt = string.Empty;
        public int width = 512;
        public int height = 512;
        public int seed;
        public int steps = 1;
        public float cfgScale;
        public string sampler = string.Empty;
        public string scheduler = string.Empty;
        public string modelName = string.Empty;
        public string outputPngPath = string.Empty;
        public string outputJsonPath = string.Empty;
        public string expectedAlphaPngPath = string.Empty;
        public string sourceRecordStableId = string.Empty;
        public string rawGeneratedPngPath = string.Empty;
        public string mattePngPath = string.Empty;
        public string croppedSpritePath = string.Empty;
        public string toolLicenseNotes = string.Empty;
        public GeneratedAssetJobStatus status = GeneratedAssetJobStatus.Planned;
        public List<string> validationWarnings = new List<string>();

        public void SyncId()
        {
            var material = string.Join("|", new[]
            {
                stableAssetId ?? string.Empty,
                presetName ?? string.Empty,
                positivePrompt ?? string.Empty,
                negativePrompt ?? string.Empty,
                width.ToString(System.Globalization.CultureInfo.InvariantCulture),
                height.ToString(System.Globalization.CultureInfo.InvariantCulture),
                seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                steps.ToString(System.Globalization.CultureInfo.InvariantCulture),
                sampler ?? string.Empty,
                scheduler ?? string.Empty,
                modelName ?? string.Empty,
            });
            jobId = GeneratedAssetIdUtility.BuildDeterministicToken("job", material);
        }
    }
}
