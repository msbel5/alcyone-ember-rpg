using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class StaticPromptCatalogTests
    {
        [Test]
        public void DefaultManifestPrompts_ResolveAndUseStyleEnvelope()
        {
            var catalog = StaticPromptCatalog.CreateDefault();
            foreach (var entry in CoreAssetManifest.CreateDefault().Entries)
            {
                if (string.IsNullOrEmpty(entry.StaticPromptKey)) continue;
                Assert.That(catalog.TryGetPrompt(entry.StaticPromptKey, out var prompt), Is.True, entry.StaticPromptKey);
                Assert.That(prompt, Does.StartWith(StaticPromptCatalog.EmberStyleHeader), entry.StaticPromptKey);
                Assert.That(prompt, Does.EndWith(StaticPromptCatalog.EmberNegativeFooter), entry.StaticPromptKey);
            }
        }
    }
}
