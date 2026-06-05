using System.Collections.Generic;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedAssetTemplateExpander
    {
        public static string Expand(string template, GeneratedAssetKey key)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;
            var result = template;
            foreach (var pair in BuildMap(key))
                result = result.Replace(pair.Key, pair.Value ?? string.Empty);
            return result;
        }

        public static string Expand(string template, GeneratedAssetRecord record)
        {
            return Expand(template, record == null ? null : record.key);
        }

        private static Dictionary<string, string> BuildMap(GeneratedAssetKey key)
        {
            key ??= new GeneratedAssetKey();
            return new Dictionary<string, string>
            {
                ["{archetype}"] = key.archetype ?? string.Empty,
                ["{biome}"] = key.biome ?? string.Empty,
                ["{culture}"] = key.culture ?? string.Empty,
                ["{faction}"] = key.faction ?? string.Empty,
                ["{role}"] = key.role ?? string.Empty,
                ["{material}"] = key.material ?? string.Empty,
                ["{tier}"] = key.tier ?? string.Empty,
                ["{styleVersion}"] = key.styleVersion ?? string.Empty,
                ["{seed}"] = key.seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["{variantIndex}"] = key.variantIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["{kind}"] = key.kind.ToString(),
            };
        }
    }
}
