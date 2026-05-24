using System.Linq;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class CoreAssetManifestTests
    {
        [Test]
        public void DefaultManifest_HasUniqueIdsAndEnoughEntries()
        {
            var manifest = CoreAssetManifest.CreateDefault();
            Assert.That(manifest.Entries.Count, Is.GreaterThanOrEqualTo(45));
            Assert.That(manifest.Entries.Select(e => e.Id).Distinct().Count(), Is.EqualTo(manifest.Entries.Count));
        }

        [Test]
        public void GeneratedEntries_HavePromptKeysAndPositiveDimensions()
        {
            foreach (var entry in CoreAssetManifest.CreateDefault().Entries)
            {
                Assert.That(entry.Width, Is.GreaterThan(0), entry.Id);
                Assert.That(entry.Height, Is.GreaterThan(0), entry.Id);
                Assert.That(entry.TimeoutSeconds, Is.GreaterThan(0), entry.Id);
                if (entry.RequiresGeneration)
                    Assert.That(entry.StaticPromptKey, Is.Not.Empty, entry.Id);
            }
        }
    }
}
