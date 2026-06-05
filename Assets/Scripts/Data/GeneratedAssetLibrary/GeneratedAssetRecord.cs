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
        public string albedoPath = string.Empty;
        public string deLitAlbedoPath = string.Empty;
        public string normalPath = string.Empty;
        public string roughnessPath = string.Empty;
        public string metallicPath = string.Empty;
        public string ambientOcclusionPath = string.Empty;
        public string heightPath = string.Empty;
        public string maskMapPath = string.Empty;
        public string materialPath = string.Empty;
        public string prefabPath = string.Empty;
        public string sourceImagePath = string.Empty;
        public string generatedMeshPath = string.Empty;
        public List<string> materialPaths = new List<string>();
        public List<string> texturePaths = new List<string>();
        public GeneratedAssetGenerationJob spriteJob = new GeneratedAssetGenerationJob();
        public GeneratedMeshJob meshJob = new GeneratedMeshJob();
        public List<string> validationWarnings = new List<string>();
        public float tileSizeMeters = 1f;
        public int pixelsPerMeter = 512;
        public bool isTileable;
        public bool deLit;
        public bool pbrGenerated;
        public GeneratedMeshColliderType colliderType = GeneratedMeshColliderType.None;
        public int triangleCount;
        public int vertexCount;
        public int subMeshCount;
        public int materialCount;
        public bool hasUVs;
        public bool hasNormals;
        public bool hasTangents;
        public bool hasReadableMesh;
        public bool hasCollider;
        public bool hasLod;
        public float scaleMeters = 1f;
        public GeneratedMeshPivotMode pivotMode = GeneratedMeshPivotMode.Center;
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
