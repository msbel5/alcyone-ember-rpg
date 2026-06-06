namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedNpcBillboardResolver
    {
        public static bool TryResolveRecord(GeneratedAssetDatabase database, string roleOrId, int seed, out GeneratedAssetRecord record)
        {
            record = null;
            if (database == null) return false;

            var normalizedRole = NormalizeRole(roleOrId);
            if (string.IsNullOrEmpty(normalizedRole)) return false;

            return database.TryResolve(new GeneratedAssetQuery
            {
                kind = GeneratedAssetKind.CharacterBillboard,
                role = normalizedRole,
                seed = seed,
                requireHumanApproved = false,
                excludeForbidden = true,
            }, out record);
        }

        public static string BuildFallbackCoreId(string roleOrId)
        {
            var normalizedRole = NormalizeRole(roleOrId);
            return string.IsNullOrEmpty(normalizedRole) ? string.Empty : "npc_" + normalizedRole;
        }

        private static string NormalizeRole(string roleOrId)
        {
            if (string.IsNullOrWhiteSpace(roleOrId)) return string.Empty;
            var raw = roleOrId.Trim();
            if (raw.StartsWith("npc_", System.StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(4);
            else if (raw.StartsWith("npc-", System.StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(4);
            return GeneratedAssetIdUtility.NormalizeSegment(raw);
        }
    }
}
