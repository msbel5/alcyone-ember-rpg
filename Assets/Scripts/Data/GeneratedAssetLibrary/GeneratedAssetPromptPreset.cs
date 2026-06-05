#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace EmberCrpg.Data.GeneratedAssets
{
#if UNITY_5_3_OR_NEWER
    [CreateAssetMenu(menuName = "Ember/Generated Assets/Prompt Preset", fileName = "GeneratedAssetPromptPreset")]
    public sealed class GeneratedAssetPromptPreset : ScriptableObject
#else
    public sealed class GeneratedAssetPromptPreset
#endif
    {
        public string presetName = string.Empty;
        public GeneratedAssetKind kind = GeneratedAssetKind.ItemBillboard;
        public string styleVersion = "v1";
#if UNITY_5_3_OR_NEWER
        [TextArea(3, 8)] public string positiveTemplate = string.Empty;
        [TextArea(3, 8)] public string negativeTemplate = string.Empty;
#else
        public string positiveTemplate = string.Empty;
        public string negativeTemplate = string.Empty;
#endif
        public int defaultWidth = 512;
        public int defaultHeight = 512;
        public string recommendedExternalTools = string.Empty;
#if UNITY_5_3_OR_NEWER
        [TextArea(2, 6)] public string notes = string.Empty;
        [TextArea(2, 4)] public string licenseWarning = string.Empty;
#else
        public string notes = string.Empty;
        public string licenseWarning = string.Empty;
#endif
        public string outputFolderTemplate = "Assets/GeneratedLibrary/{kind}/{archetype}";

        public string BuildPositive(GeneratedAssetRecord record)
        {
            return GeneratedAssetTemplateExpander.Expand(positiveTemplate, record);
        }

        public string BuildNegative(GeneratedAssetRecord record)
        {
            return GeneratedAssetTemplateExpander.Expand(negativeTemplate, record);
        }
    }
}
