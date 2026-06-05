using System.Collections.Generic;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedAssetPromptBuilder
    {
        public static GeneratedAssetGenerationJob BuildJob(
            GeneratedAssetRecord record,
            GeneratedAssetPromptPreset preset,
            int width,
            int height,
            int steps,
            float cfgScale,
            string sampler,
            string scheduler,
            string modelName,
            IDictionary<string, string> overrides = null)
        {
            record ??= new GeneratedAssetRecord();
            record.SyncIdentity();
            var positive = ApplyOverrides(preset == null ? string.Empty : preset.BuildPositive(record), overrides);
            var negative = ApplyOverrides(preset == null ? string.Empty : preset.BuildNegative(record), overrides);

            var job = new GeneratedAssetGenerationJob
            {
                stableAssetId = record.stableId,
                sourceRecordStableId = record.stableId,
                kind = record.kind,
                presetName = preset == null ? string.Empty : preset.presetName,
                positivePrompt = positive,
                negativePrompt = negative,
                width = width,
                height = height,
                seed = record.seed,
                steps = steps,
                cfgScale = cfgScale,
                sampler = sampler ?? string.Empty,
                scheduler = scheduler ?? string.Empty,
                modelName = string.IsNullOrWhiteSpace(record.modelName) ? modelName ?? string.Empty : record.modelName,
                toolLicenseNotes = record.modelLicense ?? string.Empty,
                status = GeneratedAssetJobStatus.Planned,
            };
            job.SyncId();
            return job;
        }

        private static string ApplyOverrides(string value, IDictionary<string, string> overrides)
        {
            if (string.IsNullOrEmpty(value) || overrides == null) return value ?? string.Empty;
            var result = value;
            foreach (var pair in overrides)
                result = result.Replace(pair.Key, pair.Value ?? string.Empty);
            return result;
        }
    }
}
