using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Overland;
using EmberCrpg.Simulation.Worldgen;
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

        [Test]
        public void Sample_CoastBiome_SeparatesOceanWaterFromCoastLand()
        {
            var parameters = OverlandParameters.Default;
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);
            var map = OverlandWorldgen.Generate(world, parameters);
            var geography = world.Geography;
            int oceanIndex = -1;
            int shoreIndex = -1;

            for (int i = 0; i < map.Tiles.Count; i++)
            {
                if (map.Tiles[i].Biome != BiomeKind.Coast)
                    continue;

                if (geography.LandMask[i] && shoreIndex < 0)
                    shoreIndex = i;
                if (!geography.LandMask[i] && oceanIndex < 0)
                    oceanIndex = i;
            }

            Assert.That(shoreIndex, Is.GreaterThanOrEqualTo(0), "Expected at least one coastline land tile.");
            Assert.That(oceanIndex, Is.GreaterThanOrEqualTo(0), "Expected at least one ocean water tile.");

            var image = OverlandMapImageSampler.Sample(map, map.Width, map.Height);
            var shore = ReadPixel(image, map.Tiles[shoreIndex].X, map.Tiles[shoreIndex].Y);
            var ocean = ReadPixel(image, map.Tiles[oceanIndex].X, map.Tiles[oceanIndex].Y);

            Assert.That(shore.R, Is.GreaterThan(ocean.R + 80));
            Assert.That(shore.G, Is.GreaterThan(ocean.G + 70));
            Assert.That(ocean.B, Is.GreaterThan(ocean.R + 45));
        }

        [Test]
        public void Sample_UpscaledImage_RepeatsExactTileColors()
        {
            var map = OverlandWorldgen.Generate(42u, OverlandParameters.Default);
            var native = OverlandMapImageSampler.Sample(map, map.Width, map.Height);
            var upscaled = OverlandMapImageSampler.Sample(map, map.Width * 2, map.Height * 2);

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var expected = ReadPixel(native, x, y);
                    Assert.That(ReadPixel(upscaled, (x * 2) + 0, (y * 2) + 0), Is.EqualTo(expected));
                    Assert.That(ReadPixel(upscaled, (x * 2) + 1, (y * 2) + 0), Is.EqualTo(expected));
                    Assert.That(ReadPixel(upscaled, (x * 2) + 0, (y * 2) + 1), Is.EqualTo(expected));
                    Assert.That(ReadPixel(upscaled, (x * 2) + 1, (y * 2) + 1), Is.EqualTo(expected));
                }
            }
        }

        private static Pixel ReadPixel(OverlandMapImage image, int x, int y)
        {
            int offset = ((y * image.Width) + x) * 4;
            return new Pixel(image.RgbaBytes[offset], image.RgbaBytes[offset + 1], image.RgbaBytes[offset + 2]);
        }

        private readonly struct Pixel : System.IEquatable<Pixel>
        {
            public Pixel(byte r, byte g, byte b)
            {
                R = r;
                G = g;
                B = b;
            }

            public byte R { get; }
            public byte G { get; }
            public byte B { get; }

            public bool Equals(Pixel other)
            {
                return R == other.R && G == other.G && B == other.B;
            }

            public override bool Equals(object obj)
            {
                return obj is Pixel other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (R << 16) | (G << 8) | B;
            }
        }
    }
}
