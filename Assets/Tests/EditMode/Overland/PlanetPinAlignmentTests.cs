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

            const int W = 1024, H = 512; // the REAL in-game atlas resolution (tile region ~6px, no rounding noise)
            Assert.That(PlanetAtlas.TryRender(map, W, H, out var image), Is.True, "planet atlas must render");

            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var s = map.Settlements[i];
                Assert.That(
                    PlanetAtlas.TryGetTileAnchorPercent(map, s.TilePosition.X, s.TilePosition.Y, out float xPct, out float yPct),
                    Is.True, s.Name + " must anchor to a planet tile");

                int px = System.Math.Clamp((int)(xPct / 100f * W), 0, W - 1);
                int py = System.Math.Clamp((int)(yPct / 100f * H), 0, H - 1);

                // PlanetAtlas stores rows SOUTH-first (Unity LoadRawTextureData convention); pin percents are
                // top-origin (north = 0%), so flip the row to read what the UI shows. At 512px an icosphere
                // tile owns ~3 pixels, so coastal pixel-ownership can wobble by 1px — the UI pin graphic spans
                // many map pixels, so require land within the 3x3 neighbourhood. Real drift bugs (mirror,
                // offset, wrong projection) displace pins by tens of pixels and still fail loudly.
                bool anyLand = false;
                byte cr = 0, cg = 0, cb = 0;
                for (int dy = -1; dy <= 1 && !anyLand; dy++)
                {
                    for (int dx = -1; dx <= 1 && !anyLand; dx++)
                    {
                        int sx = System.Math.Clamp(px + dx, 0, W - 1);
                        int sy = System.Math.Clamp(py + dy, 0, H - 1);
                        int idx = (((H - 1 - sy) * W) + sx) * 4;
                        byte r = image.Rgba[idx], g = image.Rgba[idx + 1], b = image.Rgba[idx + 2];
                        if (dx == 0 && dy == 0) { cr = r; cg = g; cb = b; }
                        // LAKE (40,90,165) and RIVER (70,120,205) are LAND-side paints (PlanetImageSampler.
                        // Color lines 154-155): a lakeside/riverside settlement pin on them is CORRECT.
                        bool inlandWater = (r == 40 && g == 90 && b == 165) || (r == 70 && g == 120 && b == 205);
                        if (inlandWater || !(b > g + 15 && b > r + 15)) anyLand = true;
                    }
                }

                Assert.That(anyLand, Is.True,
                    $"{s.Name} pin at ({px},{py}) has NO land pixel in its 3x3 — centre rgb({cr},{cg},{cb}) — projection drift");
            }
        }
    }
}
