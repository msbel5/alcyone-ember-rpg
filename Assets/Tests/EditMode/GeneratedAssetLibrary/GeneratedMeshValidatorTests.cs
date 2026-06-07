using EmberCrpg.Data.GeneratedAssets;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.GeneratedAssets
{
    public sealed class GeneratedMeshValidatorTests
    {
        [Test]
        public void Validator_FlagsMissingMeshAndPrefab()
        {
            var record = new GeneratedAssetRecord
            {
                kind = GeneratedAssetKind.SmallPropMesh,
                humanApproved = true,
                licenseStatus = GeneratedAssetLicenseStatus.Clean,
            };

            var report = GeneratedMeshValidator.Validate(record, new GeneratedMeshValidationPolicy());

            Assert.That(report.warnings, Does.Contain("missing_mesh_path"));
            Assert.That(report.warnings, Does.Contain("missing_prefab_path"));
        }

        [Test]
        public void Validator_FlagsTriangleBudgetOverflow()
        {
            var record = new GeneratedAssetRecord
            {
                kind = GeneratedAssetKind.LargeStructureMesh,
                generatedMeshPath = "Assets/Generated/Prefabs/house.glb",
                prefabPath = "Assets/Generated/Prefabs/house.prefab",
                triangleCount = 60000,
                hasUVs = true,
                hasNormals = true,
                hasCollider = true,
                hasLod = false,
            };

            var report = GeneratedMeshValidator.Validate(record, new GeneratedMeshValidationPolicy());

            Assert.That(report.warnings, Does.Contain("triangle_budget_structure"));
            Assert.That(report.warnings, Does.Contain("missing_lod"));
        }

        [Test]
        public void Validator_FlagsForbiddenLicense()
        {
            var record = new GeneratedAssetRecord
            {
                kind = GeneratedAssetKind.SmallPropMesh,
                generatedMeshPath = "Assets/Generated/Prefabs/prop.fbx",
                prefabPath = "Assets/Generated/Prefabs/prop.prefab",
                hasUVs = true,
                hasNormals = true,
                licenseStatus = GeneratedAssetLicenseStatus.Forbidden,
            };

            var report = GeneratedMeshValidator.Validate(record, new GeneratedMeshValidationPolicy());

            Assert.That(report.warnings, Does.Contain("forbidden_license"));
        }
    }
}
