using System.Linq;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class CoreAssetRegenerationScopeTests
    {
        [Test]
        public void DefaultSubset_SelectsExpectedVisiblePromptFixEntries()
        {
            var manifest = CoreAssetManifest.CreateDefault();

            var selected = CoreAssetRegenerationSelector.Select(manifest.Entries, CoreAssetRegenerationScope.DefaultSubset);

            Assert.That(selected.Select(entry => entry.Id).ToArray(), Is.EqualTo(new[]
            {
                "npc_rogue",
                "npc_sage",
                "npc_guard",
                "wall_tavernflavour",
                "env_tavernflavour",
            }));
        }

        [Test]
        public void IconsScope_SelectsOnlyUiEntries()
        {
            var manifest = CoreAssetManifest.CreateDefault();

            var selected = CoreAssetRegenerationSelector.Select(manifest.Entries, CoreAssetRegenerationScope.Icons);

            Assert.That(selected, Is.Not.Empty);
            Assert.That(selected.All(entry => entry.Category == "ui"), Is.True);
        }

        [Test]
        public void NpcScope_SelectsOnlyNpcEntries()
        {
            var manifest = CoreAssetManifest.CreateDefault();

            var selected = CoreAssetRegenerationSelector.Select(manifest.Entries, CoreAssetRegenerationScope.NpcBillboards);

            Assert.That(selected, Is.Not.Empty);
            Assert.That(selected.All(entry => entry.Category == "npc"), Is.True);
        }
    }
}
