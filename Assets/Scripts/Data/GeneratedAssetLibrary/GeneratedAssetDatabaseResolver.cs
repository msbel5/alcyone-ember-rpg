using System;
using System.Collections.Generic;
using System.Linq;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedAssetDatabaseResolver
    {
        public static bool TryResolve(IReadOnlyList<GeneratedAssetRecord> records, GeneratedAssetQuery query, out GeneratedAssetRecord record)
        {
            record = null;
            if (records == null || query == null) return false;

            var candidates = records
                .Where(r => r != null && Matches(r, query))
                .OrderBy(r => r.stableId, StringComparer.Ordinal)
                .ToList();

            if (candidates.Count == 0) return false;
            var index = GeneratedAssetIdUtility.DeterministicIndex(query.BuildSelectionMaterial(), candidates.Count);
            if (index < 0 || index >= candidates.Count) return false;
            record = candidates[index];
            return true;
        }

        private static bool Matches(GeneratedAssetRecord record, GeneratedAssetQuery query)
        {
            if (record.kind != query.kind) return false;
            if (query.excludeForbidden && record.licenseStatus == GeneratedAssetLicenseStatus.Forbidden) return false;
            if (query.requireHumanApproved && !record.humanApproved) return false;

            return Match(record.key.archetype, query.archetype)
                && Match(record.key.biome, query.biome)
                && Match(record.key.culture, query.culture)
                && Match(record.key.faction, query.faction)
                && Match(record.key.role, query.role)
                && Match(record.key.material, query.material)
                && Match(record.key.tier, query.tier)
                && Match(record.key.styleVersion, query.styleVersion)
                && MatchTags(record.tags, query.requiredTags);
        }

        private static bool Match(string recordValue, string queryValue)
        {
            if (string.IsNullOrWhiteSpace(queryValue)) return true;
            return string.Equals(
                GeneratedAssetIdUtility.NormalizeSegment(recordValue),
                GeneratedAssetIdUtility.NormalizeSegment(queryValue),
                StringComparison.Ordinal);
        }

        private static bool MatchTags(IReadOnlyList<string> recordTags, IReadOnlyList<string> requiredTags)
        {
            if (requiredTags == null || requiredTags.Count == 0) return true;
            var tags = new HashSet<string>((recordTags ?? Array.Empty<string>()).Select(GeneratedAssetIdUtility.NormalizeSegment), StringComparer.Ordinal);
            return requiredTags.All(tag => tags.Contains(GeneratedAssetIdUtility.NormalizeSegment(tag)));
        }
    }
}
