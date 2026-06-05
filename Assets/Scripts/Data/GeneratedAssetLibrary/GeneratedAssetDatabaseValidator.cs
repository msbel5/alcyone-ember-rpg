using System.Collections.Generic;
using System.Linq;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedAssetDatabaseValidator
    {
        public static IReadOnlyList<GeneratedAssetValidationIssue> Validate(IReadOnlyList<GeneratedAssetRecord> records)
        {
            var issues = new List<GeneratedAssetValidationIssue>();
            var counts = new Dictionary<string, int>(System.StringComparer.Ordinal);

            foreach (var record in records ?? new GeneratedAssetRecord[0])
            {
                if (record == null) continue;
                record.SyncIdentity();
                counts[record.stableId] = counts.TryGetValue(record.stableId, out var count) ? count + 1 : 1;

                if (string.IsNullOrWhiteSpace(record.stableId))
                    issues.Add(new GeneratedAssetValidationIssue("<empty>", GeneratedAssetValidationSeverity.Error, "Stable id is empty."));
                if (string.IsNullOrWhiteSpace(record.key.archetype))
                    issues.Add(new GeneratedAssetValidationIssue(record.stableId, GeneratedAssetValidationSeverity.Warning, "Archetype is empty."));
                if (string.IsNullOrWhiteSpace(record.key.styleVersion))
                    issues.Add(new GeneratedAssetValidationIssue(record.stableId, GeneratedAssetValidationSeverity.Warning, "Style version is empty."));
                if (record.licenseStatus == GeneratedAssetLicenseStatus.Forbidden)
                    issues.Add(new GeneratedAssetValidationIssue(record.stableId, GeneratedAssetValidationSeverity.Error, "Record is forbidden by license policy."));
                if (record.licenseStatus == GeneratedAssetLicenseStatus.Unknown || record.licenseStatus == GeneratedAssetLicenseStatus.NeedsReview)
                    issues.Add(new GeneratedAssetValidationIssue(record.stableId, GeneratedAssetValidationSeverity.Warning, "Record requires license review."));
                if (!record.humanApproved)
                    issues.Add(new GeneratedAssetValidationIssue(record.stableId, GeneratedAssetValidationSeverity.Warning, "Record is not human approved."));

                ValidatePath(record.stableId, "relativeAssetPath", record.relativeAssetPath, issues);
                ValidatePath(record.stableId, "previewPath", record.previewPath, issues);
                ValidatePath(record.stableId, "spritePath", record.spritePath, issues);
                ValidatePath(record.stableId, "materialPath", record.materialPath, issues);
                ValidatePath(record.stableId, "prefabPath", record.prefabPath, issues);
            }

            foreach (var pair in counts.Where(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value > 1))
                issues.Add(new GeneratedAssetValidationIssue(pair.Key, GeneratedAssetValidationSeverity.Error, "Duplicate stable id detected."));

            return issues;
        }

        private static void ValidatePath(string stableId, string fieldName, string value, ICollection<GeneratedAssetValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!value.StartsWith("Assets/", System.StringComparison.Ordinal))
            {
                issues.Add(new GeneratedAssetValidationIssue(stableId, GeneratedAssetValidationSeverity.Warning, fieldName + " should be asset-relative."));
            }
        }
    }
}
