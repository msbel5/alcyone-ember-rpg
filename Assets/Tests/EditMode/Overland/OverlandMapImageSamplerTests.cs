using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Overland;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Overland
{
    public sealed class OverlandMapImageSamplerTests
    {
        [Test]
        public void Sample_SameSeed_ProducesIdenticalBytes()
        {
            var mapA = OverlandWorldgen.Generate(42u, OverlandParameters.Default);
            var mapB = OverlandWorldgen.Generate(42u, OverlandParameters.Default);

            var imageA = OverlandMapImageSampler.Sample(mapA, 64, 64);
            var imageB = OverlandMapImageSampler.Sample(mapB, 64, 64);

            Assert.That(imageA.CacheKey, Is.EqualTo(imageB.CacheKey));
            Assert.That(imageA.RgbaBytes, Is.EqualTo(imageB.RgbaBytes));
        }

        [Test]
        public void Sample_DifferentSeed_ChangesBytes()
        {
            var mapA = OverlandWorldgen.Generate(42u, OverlandParameters.Default);
            var mapB = OverlandWorldgen.Generate(43u, OverlandParameters.Default);

            var imageA = OverlandMapImageSampler.Sample(mapA, 64, 64);
            var imageB = OverlandMapImageSampler.Sample(mapB, 64, 64);

            Assert.That(imageA.CacheKey, Is.Not.EqualTo(imageB.CacheKey));
            Assert.That(imageA.RgbaBytes, Is.Not.EqualTo(imageB.RgbaBytes));
        }
    }
}
