using System.IO;
using EmberCrpg.Data.GeneratedAssets;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedSpriteCropper
    {
        public static GeneratedSpriteAlphaAnalysis CropLargestComponent(string sourceAssetPath, string outputAssetPath, byte threshold, int padding, int minimumLargeComponentPixels)
        {
            var inputBytes = File.ReadAllBytes(GeneratedAssetEditorPathUtility.AssetToAbsolute(sourceAssetPath));
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(inputBytes);

            try
            {
                var pixels = source.GetPixels32();
                var alpha = new byte[pixels.Length];
                for (var i = 0; i < pixels.Length; i++) alpha[i] = pixels[i].a;

                var analysis = GeneratedSpriteAlphaAnalyzer.Analyze(source.width, source.height, alpha, threshold, minimumLargeComponentPixels);
                if (analysis.mainBounds.width <= 0 || analysis.mainBounds.height <= 0)
                    throw new IOException("No opaque component passed the alpha threshold.");

                var x = Mathf.Max(0, analysis.mainBounds.x - padding);
                var y = Mathf.Max(0, analysis.mainBounds.y - padding);
                var w = Mathf.Min(source.width - x, analysis.mainBounds.width + (padding * 2));
                var h = Mathf.Min(source.height - y, analysis.mainBounds.height + (padding * 2));
                var cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
                cropped.SetPixels(source.GetPixels(x, y, w, h));
                cropped.Apply();

                try
                {
                    GeneratedAssetEditorPathUtility.WriteAssetBytes(outputAssetPath, cropped.EncodeToPNG());
                }
                finally
                {
                    Object.DestroyImmediate(cropped);
                }

                return analysis;
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }
    }
}
