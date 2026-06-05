using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    [Serializable]
    public sealed class GeneratedAssetKey
    {
        public GeneratedAssetKind kind = GeneratedAssetKind.ItemBillboard;
        public string archetype = string.Empty;
        public string biome = string.Empty;
        public string culture = string.Empty;
        public string faction = string.Empty;
        public string role = string.Empty;
        public string material = string.Empty;
        public string tier = string.Empty;
        public int variantIndex;
        public string styleVersion = "v1";
        public int seed;
        public string promptHash = string.Empty;

        public string BuildStableId()
        {
            return GeneratedAssetIdUtility.BuildStableId(this);
        }
    }
}
