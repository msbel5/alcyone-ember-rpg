using EmberCrpg.Data.GeneratedAssets;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedMeshJobBuilder
    {
        public static GeneratedMeshJob Build(GeneratedAssetRecord record, string toolName)
        {
            record.SyncIdentity();
            var job = new GeneratedMeshJob
            {
                stableAssetId = record.stableId,
                kind = record.kind,
                archetype = record.key.archetype,
                biome = record.key.biome,
                culture = record.key.culture,
                faction = record.key.faction,
                role = record.key.role,
                material = record.key.material,
                tier = record.key.tier,
                styleVersion = record.key.styleVersion,
                seed = record.seed,
                sourceImagePath = record.sourceImagePath,
                prompt = record.sourcePrompt,
                negativePrompt = record.negativePrompt,
                externalToolName = toolName ?? string.Empty,
                externalToolLicenseNotes = record.modelLicense ?? string.Empty,
            };
            job.SyncId();
            return job;
        }
    }
}
