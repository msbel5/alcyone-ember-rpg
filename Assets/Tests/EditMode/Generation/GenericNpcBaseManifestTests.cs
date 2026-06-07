using System.Linq;
using EmberCrpg.Domain.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class GenericNpcBaseManifestTests
    {
        [Test]
        public void DefaultManifest_UsesExactlySixShippedSilhouettes()
        {
            var manifest = GenericNpcBaseManifest.CreateDefault();
            Assert.That(manifest.Archetypes.Count, Is.EqualTo(6));
            Assert.That(manifest.Archetypes.Select(a => a.ArchetypeId), Is.EquivalentTo(new[]
            {
                "humanoid_male", "humanoid_female", "beast_quadruped", "undead_humanoid", "construct", "aberration"
            }));
            Assert.That(manifest.Archetypes.All(a => a.SilhouettePath.Length == 0), Is.True);
            Assert.That(manifest.Archetypes.All(a => a.RequiresGeneration == false), Is.True);
            Assert.That(manifest.Archetypes.All(a => !a.RequiresGeneration), Is.True);
        }
    }
}
