using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedTextureTileabilityAnalyzer
    {
        public static GeneratedTextureValidationReport Analyze(int width, int height, byte[] rgba, float edgeThreshold, float gradientThreshold)
        {
            if (width <= 1 || height <= 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (rgba == null || rgba.Length != width * height * 4) throw new ArgumentException("RGBA buffer size does not match dimensions.", nameof(rgba));

            var report = new GeneratedTextureValidationReport
            {
                horizontalEdgeDifference = EdgeDifference(width, height, rgba, horizontal: true),
                verticalEdgeDifference = EdgeDifference(width, height, rgba, horizontal: false),
                hasWarmBrightBlob = WarmBrightBlob(width, height, rgba),
                hasStrongGradient = StrongGradient(width, height, rgba, gradientThreshold),
            };

            if (report.horizontalEdgeDifference > edgeThreshold) report.warnings.Add("horizontal_edge_mismatch");
            if (report.verticalEdgeDifference > edgeThreshold) report.warnings.Add("vertical_edge_mismatch");
            if (report.hasWarmBrightBlob) report.warnings.Add("warm_bright_blob");
            if (report.hasStrongGradient) report.warnings.Add("strong_lighting_gradient");
            return report;
        }

        private static float EdgeDifference(int width, int height, byte[] rgba, bool horizontal)
        {
            var total = 0f;
            var samples = horizontal ? height : width;
            for (var i = 0; i < samples; i++)
            {
                var a = horizontal ? PixelIndex(width, 0, i) : PixelIndex(width, i, 0);
                var b = horizontal ? PixelIndex(width, width - 1, i) : PixelIndex(width, i, height - 1);
                total += ChannelDifference(rgba, a, b);
            }

            return total / Math.Max(1, samples);
        }

        private static bool WarmBrightBlob(int width, int height, byte[] rgba)
        {
            var warm = 0;
            var total = width * height;
            for (var i = 0; i < total; i++)
            {
                var index = i * 4;
                if (rgba[index] > 220 && rgba[index + 1] > 140 && rgba[index + 2] < 120) warm++;
            }

            return warm > (total / 40);
        }

        private static bool StrongGradient(int width, int height, byte[] rgba, float gradientThreshold)
        {
            var left = AverageLuma(width, height, rgba, 0, width / 3);
            var right = AverageLuma(width, height, rgba, width - (width / 3), width);
            return Math.Abs(left - right) > gradientThreshold;
        }

        private static float AverageLuma(int width, int height, byte[] rgba, int xStart, int xEnd)
        {
            var total = 0f;
            var count = 0;
            for (var y = 0; y < height; y++)
            for (var x = xStart; x < xEnd; x++)
            {
                var index = PixelIndex(width, x, y);
                total += (rgba[index] * 0.2126f) + (rgba[index + 1] * 0.7152f) + (rgba[index + 2] * 0.0722f);
                count++;
            }

            return count <= 0 ? 0f : total / count;
        }

        private static float ChannelDifference(byte[] rgba, int a, int b)
        {
            return (Math.Abs(rgba[a] - rgba[b]) + Math.Abs(rgba[a + 1] - rgba[b + 1]) + Math.Abs(rgba[a + 2] - rgba[b + 2])) / (255f * 3f);
        }

        private static int PixelIndex(int width, int x, int y)
        {
            return ((y * width) + x) * 4;
        }
    }
}
