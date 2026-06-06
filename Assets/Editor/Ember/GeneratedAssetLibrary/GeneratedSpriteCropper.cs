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

                var rgba = new byte[pixels.Length * 4];
                for (var i = 0; i < pixels.Length; i++)
                {
                    var offset = i * 4;
                    rgba[offset + 0] = pixels[i].r;
                    rgba[offset + 1] = pixels[i].g;
                    rgba[offset + 2] = pixels[i].b;
                    rgba[offset + 3] = pixels[i].a;
                }

                var crop = GeneratedSpriteCropUtility.CropRgba(source.width, source.height, rgba, analysis.mainBounds, padding);
                var cropped = new Texture2D(crop.Width, crop.Height, TextureFormat.RGBA32, false);
                cropped.LoadRawTextureData(crop.Rgba);
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
