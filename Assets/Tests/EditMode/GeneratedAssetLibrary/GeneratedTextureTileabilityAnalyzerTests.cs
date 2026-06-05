using EmberCrpg.Data.GeneratedAssets;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.GeneratedAssets
{
    public sealed class GeneratedTextureTileabilityAnalyzerTests
    {
        [Test]
        public void SeamlessTexture_HasLowEdgeDifference()
        {
            var rgba = SolidTexture(4, 4, 100, 110, 120);
            var report = GeneratedTextureTileabilityAnalyzer.Analyze(4, 4, rgba, 0.1f, 35f);

            Assert.That(report.horizontalEdgeDifference, Is.LessThan(0.01f));
            Assert.That(report.verticalEdgeDifference, Is.LessThan(0.01f));
        }

        [Test]
        public void NonSeamlessTexture_HasHighEdgeDifference()
        {
            var rgba = SolidTexture(4, 4, 10, 10, 10);
            PaintColumn(rgba, 4, 3, 250, 250, 250);
            var report = GeneratedTextureTileabilityAnalyzer.Analyze(4, 4, rgba, 0.1f, 35f);

            Assert.That(report.horizontalEdgeDifference, Is.GreaterThan(0.5f));
            Assert.That(report.warnings, Does.Contain("horizontal_edge_mismatch"));
        }

        [Test]
        public void WarmGlowBlob_IsFlagged()
        {
            var rgba = SolidTexture(10, 10, 50, 50, 50);
            Fill(rgba, 10, 2, 2, 4, 4, 255, 180, 40);

            var report = GeneratedTextureTileabilityAnalyzer.Analyze(10, 10, rgba, 0.1f, 35f);

            Assert.That(report.hasWarmBrightBlob, Is.True);
            Assert.That(report.warnings, Does.Contain("warm_bright_blob"));
        }

        private static byte[] SolidTexture(int width, int height, byte r, byte g, byte b)
        {
            var rgba = new byte[width * height * 4];
            for (var i = 0; i < width * height; i++)
            {
                var offset = i * 4;
                rgba[offset] = r;
                rgba[offset + 1] = g;
                rgba[offset + 2] = b;
                rgba[offset + 3] = 255;
            }

            return rgba;
        }

        private static void PaintColumn(byte[] rgba, int width, int x, byte r, byte g, byte b)
        {
            for (var y = 0; y < rgba.Length / (width * 4); y++)
            {
                var offset = ((y * width) + x) * 4;
                rgba[offset] = r;
                rgba[offset + 1] = g;
                rgba[offset + 2] = b;
            }
        }

        private static void Fill(byte[] rgba, int width, int x, int y, int w, int h, byte r, byte g, byte b)
        {
            for (var yy = y; yy < y + h; yy++)
            for (var xx = x; xx < x + w; xx++)
            {
                var offset = ((yy * width) + xx) * 4;
                rgba[offset] = r;
                rgba[offset + 1] = g;
                rgba[offset + 2] = b;
            }
        }
    }
}
