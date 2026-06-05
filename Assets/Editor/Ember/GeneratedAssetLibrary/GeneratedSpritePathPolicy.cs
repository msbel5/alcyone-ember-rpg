using System.IO;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.Common;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedSpritePathPolicy
    {
        public static void ConfigureJobPaths(GeneratedAssetRecord record, GeneratedAssetGenerationJob job, GeneratedAssetPipelineSettings settings)
        {
            record.SyncIdentity();
            job.stableAssetId = record.stableId;
            job.sourceRecordStableId = record.stableId;
            job.kind = record.kind;
            if (string.IsNullOrWhiteSpace(job.jobId)) job.SyncId();
            job.outputPngPath = ResolveRawAssetPath(settings, job);
            job.outputJsonPath = ResolveJobJsonPath(settings, job);
            job.expectedAlphaPngPath = ResolveMatteAssetPath(settings, job);
            job.rawGeneratedPngPath = job.outputPngPath;
            job.mattePngPath = job.expectedAlphaPngPath;
            job.croppedSpritePath = ResolveImportedSpritePath(record);
        }

        public static string ResolveRawAssetPath(GeneratedAssetPipelineSettings settings, GeneratedAssetGenerationJob job)
        {
            return Path.Combine(settings.defaultOutputRoot, job.jobId, "raw.png").Replace('\\', '/');
        }

        public static string ResolveMatteAssetPath(GeneratedAssetPipelineSettings settings, GeneratedAssetGenerationJob job)
        {
            return Path.Combine(settings.defaultOutputRoot, job.jobId, "matte.png").Replace('\\', '/');
        }

        public static string ResolveJobJsonPath(GeneratedAssetPipelineSettings settings, GeneratedAssetGenerationJob job)
        {
            return Path.Combine(settings.defaultOutputRoot, job.jobId, "job.json").Replace('\\', '/');
        }

        public static string ResolveImportedSpritePath(GeneratedAssetRecord record)
        {
            var folder = record.kind == GeneratedAssetKind.ItemBillboard
                ? EmberAssetPaths.ItemsDir + "/Generated"
                : EmberAssetPaths.CharactersDir + "/Generated";
            return Path.Combine(folder, record.stableId + ".png").Replace('\\', '/');
        }

        public static string ResolvePrefabPath(GeneratedAssetRecord record)
        {
            return Path.Combine(EmberAssetPaths.PrefabsRoot, "Generated", record.stableId + ".prefab").Replace('\\', '/');
        }
    }
}
