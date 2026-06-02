using EmberCrpg.Domain.Overland;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Overland
{
    /// <summary>
    /// Locks the open-world scale goal in code so it cannot silently regress. The generated overland must
    /// stay an actual open world (~2x the ~200,000 km2 Daggerfall reference), not a small demo grid. This is
    /// the headless counterpart to the OverlandAreaKm2 line the --ember-world-proof run prints.
    /// </summary>
    public sealed class OverlandScaleTests
    {
        // Daggerfall's overland is commonly cited at ~200,000 km2; the project goal is "at least 2x".
        private const double DaggerfallReferenceKm2 = 200_000d;

        [Test]
        public void DefaultParameters_AreA16x16RegionGrid()
        {
            var parameters = OverlandParameters.Default;

            Assert.That(parameters.Width, Is.EqualTo(16));
            Assert.That(parameters.Height, Is.EqualTo(16));
            Assert.That(parameters.Width * parameters.Height, Is.EqualTo(256), "16x16 is 256 region tiles.");
        }

        [Test]
        public void DefaultRegionTile_Is40kmEdge_So1600Km2Each()
        {
            var parameters = OverlandParameters.Default;

            Assert.That(parameters.RegionEdgeKm, Is.EqualTo(40d));
            Assert.That(parameters.RegionAreaKm2, Is.EqualTo(1600d), "A 40km x 40km region tile is 1600 km2.");
        }

        [Test]
        public void DefaultTotalArea_IsAtLeastTwiceDaggerfall()
        {
            var parameters = OverlandParameters.Default;

            // 16 * 16 region tiles x (40km x 40km) = 409,600 km2.
            Assert.That(parameters.TotalAreaKm2, Is.EqualTo(409_600d));
            Assert.That(parameters.TotalAreaKm2, Is.GreaterThanOrEqualTo(DaggerfallReferenceKm2 * 2d),
                "The default overland must be at least 2x Daggerfall's ~200,000 km2 scale goal.");
        }

        [Test]
        public void TotalArea_ScalesWithGridAndRegionEdge()
        {
            // A larger, denser-tiled world (e.g. a second planet) stays consistent: area == w * h * edge^2.
            var bigger = new OverlandParameters(width: 24, height: 24, biomeSeedCount: 16, regionEdgeKm: 40d);

            Assert.That(bigger.TotalAreaKm2, Is.EqualTo(24 * 24 * 1600d));
            Assert.That(bigger.TotalAreaKm2, Is.GreaterThan(OverlandParameters.Default.TotalAreaKm2));
        }

        [Test]
        public void RegionEdgeKm_MustBePositive()
        {
            Assert.That(
                () => new OverlandParameters(regionEdgeKm: 0d),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
