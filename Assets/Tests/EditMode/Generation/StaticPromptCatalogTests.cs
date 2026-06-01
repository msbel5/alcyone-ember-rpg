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

        [Test]
        public void DicePrompt_UsesGeometricCatalogDescription_AndNeverMentionsPips()
        {
            var catalog = StaticPromptCatalog.CreateDefault();
            Assert.That(catalog.TryGetPrompt("dice", out var dicePrompt), Is.True);
            Assert.That(dicePrompt, Does.Contain("single six-sided game die"));
            Assert.That(dicePrompt, Does.Contain("dot markings"));
            Assert.That(dicePrompt.ToLowerInvariant(), Does.Not.Contain("pips"));
        }

        [Test]
        public void ObjectIconItemSpellEntries_UseSdxlTurboAt1024()
        {
            var manifest = CoreAssetManifest.CreateDefault();
            foreach (var entry in manifest.Entries)
            {
                if (entry.Category != "ui" && entry.Category != "item" && entry.Category != "spell") continue;
                Assert.That(entry.Width, Is.EqualTo(1024), entry.Id);
                Assert.That(entry.Height, Is.EqualTo(1024), entry.Id);
                Assert.That(entry.ModelHint, Is.EqualTo("sdxl-turbo"), entry.Id);
            }
        }
    }
}
