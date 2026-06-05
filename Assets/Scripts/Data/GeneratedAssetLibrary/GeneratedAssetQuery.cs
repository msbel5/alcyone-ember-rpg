using System;
using System.Collections.Generic;

namespace EmberCrpg.Data.GeneratedAssets
{
    [Serializable]
    public sealed class GeneratedAssetQuery
    {
        public GeneratedAssetKind kind = GeneratedAssetKind.ItemBillboard;
        public string archetype = string.Empty;
        public string biome = string.Empty;
        public string culture = string.Empty;
        public string faction = string.Empty;
        public string role = string.Empty;
        public string material = string.Empty;
        public string tier = string.Empty;
        public string styleVersion = string.Empty;
        public List<string> requiredTags = new List<string>();
        public int seed;
        public bool requireHumanApproved = true;
        public bool excludeForbidden = true;

        public string BuildSelectionMaterial()
        {
            return string.Join("|", new[]
            {
                GeneratedAssetIdUtility.NormalizeSegment(kind.ToString()),
                GeneratedAssetIdUtility.NormalizeSegment(archetype),
                GeneratedAssetIdUtility.NormalizeSegment(biome),
                GeneratedAssetIdUtility.NormalizeSegment(culture),
                GeneratedAssetIdUtility.NormalizeSegment(faction),
                GeneratedAssetIdUtility.NormalizeSegment(role),
                GeneratedAssetIdUtility.NormalizeSegment(material),
                GeneratedAssetIdUtility.NormalizeSegment(tier),
                GeneratedAssetIdUtility.NormalizeSegment(styleVersion),
                seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });
        }
    }
}
