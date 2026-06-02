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
            // Cube-only wording: forge-proof showed SDXL-Turbo renders a dice SET (pile) the moment it sees
            // "die"/"dice"/"six-sided", so the catalog describes a single cube with dots instead. The
            // singular constraint lives in the positive ("exactly one cube"); "pips" must never return.
            Assert.That(dicePrompt, Does.Contain("cube"));
            Assert.That(dicePrompt, Does.Contain("dot"));
            Assert.That(dicePrompt, Does.Contain("exactly one cube"));
            Assert.That(dicePrompt.ToLowerInvariant(), Does.Not.Contain("pips"));
            Assert.That(dicePrompt.ToLowerInvariant(), Does.Not.Contain("six-sided"));
        }

        [Test]
        public void ObjectIconItemSpellEntries_UseSdxlTurboAtNative512()
        {
            // SDXL-Turbo is 512-native; at 1024 it tiles the subject (forge-proof: one die -> 40-die grid).
            // Object icons generate at 512 and the display layer downscales to slot size.
            var manifest = CoreAssetManifest.CreateDefault();
            foreach (var entry in manifest.Entries)
            {
                if (entry.Category != "ui" && entry.Category != "item" && entry.Category != "spell") continue;
                Assert.That(entry.Width, Is.EqualTo(512), entry.Id);
                Assert.That(entry.Height, Is.EqualTo(512), entry.Id);
                Assert.That(entry.ModelHint, Is.EqualTo("sdxl-turbo"), entry.Id);
            }
        }
    }
}
