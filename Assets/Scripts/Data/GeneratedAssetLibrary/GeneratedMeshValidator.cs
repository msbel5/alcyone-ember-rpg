using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedMeshValidator
    {
        public static GeneratedMeshValidationReport Validate(GeneratedAssetRecord record, GeneratedMeshValidationPolicy policy)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var report = new GeneratedMeshValidationReport();
            if (string.IsNullOrWhiteSpace(record.generatedMeshPath)) report.warnings.Add("missing_mesh_path");
            if (string.IsNullOrWhiteSpace(record.prefabPath)) report.warnings.Add("missing_prefab_path");
            if (!record.hasUVs) report.warnings.Add("missing_uvs");
            if (!record.hasNormals) report.warnings.Add("missing_normals");
            if ((record.kind == GeneratedAssetKind.LargeStructureMesh || record.kind == GeneratedAssetKind.TerrainMesh) && !record.hasCollider)
                report.warnings.Add("missing_collider");
            if (record.kind == GeneratedAssetKind.LargeStructureMesh && !record.hasLod) report.warnings.Add("missing_lod");
            if (record.kind == GeneratedAssetKind.SmallPropMesh && record.triangleCount > policy.smallPropTriangleWarning) report.warnings.Add("triangle_budget_smallprop");
            if (record.kind == GeneratedAssetKind.LargeStructureMesh && record.triangleCount > policy.largeStructureTriangleWarning) report.warnings.Add("triangle_budget_structure");
            if (record.kind == GeneratedAssetKind.TerrainMesh && record.triangleCount > policy.terrainTriangleWarning) report.warnings.Add("triangle_budget_terrain");
            if (record.licenseStatus == GeneratedAssetLicenseStatus.Forbidden) report.warnings.Add("forbidden_license");
            if (!record.humanApproved) report.warnings.Add("not_human_approved");
            return report;
        }
    }
}
