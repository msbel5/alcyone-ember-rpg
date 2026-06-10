using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Overland;
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Simulation.Worldgen.Planet;
using EmberCrpg.Simulation.Worldgen.PlanetIntegration;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Overland
{
    /// <summary>
    /// THE map-truth regression guard ("align olmayan iki katman olmasın"): runs the REAL planet pipeline,
    /// renders the REAL atlas via PlanetAtlas, and asserts that EVERY settlement pin's anchored pixel is NOT
    /// ocean-coloured — i.e. the pin projection and the image projection provably share one truth. If any
    /// layer's convention drifts (row flip, lon offset, different nearest-tile rule), this fails with the
    /// settlement name and pixel colour.
    /// </summary>
    public sealed class PlanetPinAlignmentTests
    {
        [Test]
        public void EverySettlementPin_LandsOnNonOceanPixels_OfTheRenderedAtlas()
        {
            var worldParameters = WorldgenParameters.Default;
            // Engine-free planet defaults (subdivision 3) — same pipeline and projection math as the live
            // level-4 path, just fewer tiles, so the alignment guarantee transfers and the test stays fast.
            var field = PlanetGenerator.Generate(42u, PlanetParameters.Default);
            var world = PlanetToWorldMapper.Map(field, worldParameters);
            Assert.That(world.PlanetData, Is.Not.Null, "the planet sidecar must survive projection");

            var overlandParameters = new OverlandParameters(PlanetToWorldMapper.GeographyWidth, PlanetToWorldMapper.GeographyHeight);
            var map = OverlandWorldgen.Generate(world, overlandParameters);
            Assert.That(map.Settlements.Count, Is.GreaterThan(0), "a planet world must project settlements");

            const int W = 512, H = 256;
            Assert.That(PlanetAtlas.TryRender(map, W, H, out var image), Is.True, "planet atlas must render");

            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var s = map.Settlements[i];
                Assert.That(
                    PlanetAtlas.TryGetTileAnchorPercent(map, s.TilePosition.X, s.TilePosition.Y, out float xPct, out float yPct),
                    Is.True, s.Name + " must anchor to a planet tile");

                int px = System.Math.Clamp((int)(xPct / 100f * W), 0, W - 1);
                int py = System.Math.Clamp((int)(yPct / 100f * H), 0, H - 1);
                int idx = ((py * W) + px) * 4;
                byte r = image.Rgba[idx], g = image.Rgba[idx + 1], b = image.Rgba[idx + 2];

                // Ocean pixels are blue-dominant; land is green/tan/grey/white. A pin on its own land tile
                // can never be blue-dominant because the anchor IS the tile that coloured this pixel.
                bool oceanish = b > g + 15 && b > r + 15;
                Assert.That(oceanish, Is.False,
                    $"{s.Name} pin at ({px},{py}) sits on ocean-coloured pixel rgb({r},{g},{b}) — projection drift");
            }
        }
    }
}
