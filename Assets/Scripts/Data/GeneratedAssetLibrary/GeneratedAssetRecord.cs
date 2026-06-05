using System;
using System.Collections.Generic;

namespace EmberCrpg.Data.GeneratedAssets
{
    // Saves and runtime systems should persist stable ids or key fields, not file paths. Paths are editor
    // metadata only and can move as long as the same stable id remains registered in the library.
    [Serializable]
    public sealed class GeneratedAssetRecord
    {
        public string stableId = string.Empty;
        public string displayName = string.Empty;
        public GeneratedAssetKind kind = GeneratedAssetKind.ItemBillboard;
        public GeneratedAssetKey key = new GeneratedAssetKey();
        public List<string> tags = new List<string>();
        public string sourcePrompt = string.Empty;
        public string negativePrompt = string.Empty;
        public int seed;
        public string modelName = string.Empty;
        public string modelLicense = string.Empty;
        public string toolchainNotes = string.Empty;
        public string importedAtUtc = string.Empty;
        public string relativeAssetPath = string.Empty;
        public string previewPath = string.Empty;
        public string spritePath = string.Empty;
        public string materialPath = string.Empty;
        public string prefabPath = string.Empty;
        public GeneratedAssetLicenseStatus licenseStatus = GeneratedAssetLicenseStatus.Unknown;
        public bool humanApproved;
        public string notes = string.Empty;

        public void SyncIdentity()
        {
            if (key == null) key = new GeneratedAssetKey();
            key.kind = kind;
            key.seed = seed;
            stableId = key.BuildStableId();
        }
    }
}
