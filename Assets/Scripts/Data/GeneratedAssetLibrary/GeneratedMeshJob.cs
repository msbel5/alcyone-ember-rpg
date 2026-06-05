using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    [Serializable]
    public sealed class GeneratedMeshJob
    {
        public string jobId = string.Empty;
        public string stableAssetId = string.Empty;
        public GeneratedAssetKind kind = GeneratedAssetKind.SmallPropMesh;
        public string archetype = string.Empty;
        public string biome = string.Empty;
        public string culture = string.Empty;
        public string faction = string.Empty;
        public string role = string.Empty;
        public string material = string.Empty;
        public string tier = string.Empty;
        public string styleVersion = "v1";
        public int seed;
        public string sourceImagePath = string.Empty;
        public string prompt = string.Empty;
        public string negativePrompt = string.Empty;
        public string externalToolName = string.Empty;
        public string externalToolLicenseNotes = string.Empty;
        public string outputMeshPath = string.Empty;
        public string outputTextureFolder = string.Empty;
        public string expectedPrefabPath = string.Empty;
        public GeneratedAssetJobStatus status = GeneratedAssetJobStatus.Planned;

        public void SyncId()
        {
            var materialValue = string.Join("|", new[]
            {
                stableAssetId ?? string.Empty,
                kind.ToString(),
                archetype ?? string.Empty,
                styleVersion ?? string.Empty,
                seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                externalToolName ?? string.Empty,
            });
            jobId = GeneratedAssetIdUtility.BuildDeterministicToken("mesh", materialValue);
        }
    }
}
