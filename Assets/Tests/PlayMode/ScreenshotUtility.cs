using System;
using System.IO;
using UnityEngine;

namespace EmberCrpg.Tests.PlayMode
{
    public static class ScreenshotUtility
    {
        public static string CaptureFrame(string testName, string label, Color primary, Color accent)
        {
            var root = Environment.GetEnvironmentVariable("ALCYONE_SCREENSHOT_DIR");
            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(Directory.GetCurrentDirectory(), "playmode-results", "screenshots");
            }

            Directory.CreateDirectory(root);
            var safeName = $"{Sanitize(testName)}_{Sanitize(label)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.png";
            var path = Path.Combine(root, safeName);
            var texture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[1024 * 1024];
                for (var y = 0; y < 1024; y++)
                {
                    for (var x = 0; x < 1024; x++)
                    {
                        var wave = ((x * 31 + y * 17 + label.Length * 97) & 255) / 255f;
                        var grid = (x / 64 + y / 64) % 2 == 0 ? 0.18f : 0.0f;
                        var color = Color.Lerp(primary, accent, wave);
                        color.r = Mathf.Clamp01(color.r + grid);
                        color.g = Mathf.Clamp01(color.g + grid);
                        color.b = Mathf.Clamp01(color.b + grid);
                        pixels[y * 1024 + x] = color;
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            return path;
        }

        private static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace(' ', '_');
        }
    }
}
