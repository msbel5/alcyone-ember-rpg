using System.IO;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedAssetPathUtility
    {
        public const string DefaultRoot = "Assets/GeneratedLibrary";

        public static string PreviewFolder(GeneratedAssetRecord record, GeneratedAssetPromptPreset preset)
        {
            if (preset != null && !string.IsNullOrWhiteSpace(preset.outputFolderTemplate))
                return GeneratedAssetTemplateExpander.Expand(preset.outputFolderTemplate, record);

            record ??= new GeneratedAssetRecord();
            record.SyncIdentity();
            var kind = GeneratedAssetIdUtility.NormalizeSegment(record.kind.ToString());
            var archetype = GeneratedAssetIdUtility.NormalizeSegment(record.key.archetype);
            if (string.IsNullOrEmpty(archetype)) archetype = "misc";
            return Path.Combine(DefaultRoot, kind, archetype).Replace('\\', '/');
        }
    }
}
