using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    [CreateAssetMenu(menuName = "Ember/Generated Assets/Pipeline Settings", fileName = "GeneratedAssetPipelineSettings")]
    public sealed class GeneratedAssetPipelineSettings : ScriptableObject
    {
        public bool useForgeApi;
        public string forgeApiUrl = string.Empty;
        public GeneratedExternalToolCommand forgeCommand = new GeneratedExternalToolCommand();
        public string pythonExecutablePath = string.Empty;
        public GeneratedExternalToolCommand alphaMatteCommand = new GeneratedExternalToolCommand();
        public GeneratedExternalToolCommand deLightCommand = new GeneratedExternalToolCommand();
        public GeneratedExternalToolCommand pbrMapCommand = new GeneratedExternalToolCommand();
        public GeneratedExternalToolCommand meshGenerationCommand = new GeneratedExternalToolCommand();
        public string defaultOutputRoot = "Assets/GeneratedLibrary/SpriteJobs";
        public bool dryRun = true;
        public int timeoutSeconds = 120;
        public bool writeJobJson = true;
        public bool openOutputFolderAfterGeneration;
        public int spritePixelsPerUnit = 256;
        public int maxSpriteTextureSize = 2048;
        public FilterMode spriteFilterMode = FilterMode.Bilinear;
        public bool spriteMipMaps;
        public TextureImporterCompression spriteCompression = TextureImporterCompression.CompressedHQ;
        public byte alphaThreshold = 8;
        public int cropPadding = 12;
        public int minimumLargeComponentPixels = 1024;
        public float largeComponentWarningRatio = 0.7f;
        public float billboardTargetHeight = 2.1f;
        public int materialMaxTextureSize = 2048;
        public FilterMode materialFilterMode = FilterMode.Trilinear;
        public TextureImporterCompression materialCompression = TextureImporterCompression.CompressedHQ;
        public float tileabilityEdgeThreshold = 0.1f;
        public float gradientThreshold = 35f;
        public string defaultModelName = "sdxl-turbo";
        public int defaultSteps = 1;
        public float defaultCfgScale;
        public string defaultSampler = "euler";
        public string defaultScheduler = "normal";
        public bool autoCreateBillboardPrefab = true;
        public int smallPropTriangleWarning = 5000;
        public int largeStructureTriangleWarning = 50000;
        public int terrainTriangleWarning = 120000;
    }
}
