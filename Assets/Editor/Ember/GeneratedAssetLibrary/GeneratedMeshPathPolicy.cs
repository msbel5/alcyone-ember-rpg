using System.IO;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.Common;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedMeshPathPolicy
    {
        public static void ConfigureJobPaths(GeneratedAssetRecord record, GeneratedMeshJob job, GeneratedAssetPipelineSettings settings)
        {
            record.SyncIdentity();
            job.stableAssetId = record.stableId;
            job.kind = record.kind;
            if (string.IsNullOrWhiteSpace(job.jobId)) job.SyncId();
            job.outputMeshPath = Path.Combine(settings.defaultOutputRoot, job.jobId, "mesh.glb").Replace('\\', '/');
            job.outputTextureFolder = Path.Combine(settings.defaultOutputRoot, job.jobId, "textures").Replace('\\', '/');
            job.expectedPrefabPath = ResolvePrefabPath(record);
        }

        public static string ResolvePrefabPath(GeneratedAssetRecord record)
        {
            return Path.Combine(EmberAssetPaths.PrefabsRoot, "Generated", record.stableId + "_mesh.prefab").Replace('\\', '/');
        }
    }
}
