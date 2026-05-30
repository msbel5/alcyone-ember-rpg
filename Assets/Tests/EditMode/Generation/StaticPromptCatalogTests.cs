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
                // Every prompt must open with an Ember style envelope. Icon/portrait/UI prompts use
                // EmberStyleHeader (single centered subject); environment floor textures (env_*) use
                // EmberFloorHeader (seamless tileable surface) — a deliberate T1.4 distinction. Accept
                // either header so the test asserts "Ember envelope present" without forcing icons-only.
                Assert.That(
                    prompt.StartsWith(StaticPromptCatalog.EmberStyleHeader)
                        || prompt.StartsWith(StaticPromptCatalog.EmberFloorHeader),
                    Is.True,
                    entry.StaticPromptKey + " should open with EmberStyleHeader or EmberFloorHeader");
                Assert.That(prompt, Does.EndWith(StaticPromptCatalog.EmberNegativeFooter), entry.StaticPromptKey);
            }
        }
    }
}
