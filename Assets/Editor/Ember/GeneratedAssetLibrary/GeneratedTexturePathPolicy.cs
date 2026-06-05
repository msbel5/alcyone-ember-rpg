using System.IO;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.Common;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedTexturePathPolicy
    {
        public static string ResolveAlbedoPath(GeneratedAssetRecord record) => TilePath(record, "albedo");
        public static string ResolveDeLitAlbedoPath(GeneratedAssetRecord record) => TilePath(record, "albedo_delit");
        public static string ResolveNormalPath(GeneratedAssetRecord record) => TilePath(record, "normal");
        public static string ResolveRoughnessPath(GeneratedAssetRecord record) => TilePath(record, "roughness");
        public static string ResolveAoPath(GeneratedAssetRecord record) => TilePath(record, "ao");
        public static string ResolveHeightPath(GeneratedAssetRecord record) => TilePath(record, "height");
        public static string ResolveMaskPath(GeneratedAssetRecord record) => TilePath(record, "mask");

        public static string ResolveMaterialPath(GeneratedAssetRecord record)
        {
            return Path.Combine(EmberAssetPaths.MaterialsRoot, "Generated", record.stableId + ".mat").Replace('\\', '/');
        }

        private static string TilePath(GeneratedAssetRecord record, string suffix)
        {
            return Path.Combine(EmberAssetPaths.TilesDir, "Generated", record.stableId + "_" + suffix + ".png").Replace('\\', '/');
        }
    }
}
