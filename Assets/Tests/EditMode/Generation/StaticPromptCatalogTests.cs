using System;
using EmberCrpg.Domain.Worldgen;
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
                // EmberStyleHeader (single centered subject); tileable surfaces use EmberFloorHeader
                // (floors/roofs, top-down) or EmberWallHeader (walls, front-facing) — a deliberate T1.4
                // distinction. Accept any of the three so the test asserts "Ember envelope present".
                Assert.That(
                    prompt.StartsWith(StaticPromptCatalog.EmberStyleHeader)
                        || prompt.StartsWith(StaticPromptCatalog.EmberFloorHeader)
                        || prompt.StartsWith(StaticPromptCatalog.EmberWallHeader),
                    Is.True,
                    entry.StaticPromptKey + " should open with EmberStyleHeader, EmberFloorHeader, or EmberWallHeader");
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

        [Test]
        public void GeneratedNpcPortraitPrompts_ForceExactlyOnePerson()
        {
            var catalog = StaticPromptCatalog.CreateDefault();
            string[] ids =
            {
                "dm_portrait",
                "portrait_npc_blacksmith",
                "portrait_npc_merchant",
                "portrait_npc_innkeeper",
                "portrait_npc_warrior",
                "portrait_npc_knight",
                "portrait_npc_sage"
            };

            foreach (var id in ids)
            {
                Assert.That(catalog.TryGetPrompt(id, out var prompt), Is.True, id);
                Assert.That(prompt, Does.Contain("exactly one person"), id);
                Assert.That(prompt, Does.Contain("centered character bust"), id);
                Assert.That(prompt, Does.Contain("no second person"), id);
                Assert.That(prompt, Does.Contain("no crowd"), id);
                Assert.That(prompt, Does.Contain("no duplicate face"), id);
            }
        }

        [Test]
        public void GeneratedNpcRoleSpritePrompts_CoverEveryNpcRole()
        {
            var catalog = StaticPromptCatalog.CreateDefault();
            foreach (NpcRole role in Enum.GetValues(typeof(NpcRole)))
            {
                if (role == NpcRole.None)
                    continue;

                var id = "npc_" + role.ToString().ToLowerInvariant();
                Assert.That(catalog.TryGetPrompt(id, out var prompt), Is.True, id);
                Assert.That(prompt, Does.StartWith(StaticPromptCatalog.EmberNpcSpriteHeader), id);
                Assert.That(prompt, Does.Contain("exactly one person"), id);
                Assert.That(prompt, Does.Contain("full-body character sprite"), id);
                Assert.That(prompt, Does.Contain("consistent ember-lit palette"), id);
                Assert.That(prompt, Does.Contain("no second person"), id);
                Assert.That(prompt, Does.Contain("no crowd"), id);
                Assert.That(prompt, Does.EndWith(StaticPromptCatalog.EmberNegativeFooter), id);
            }
        }
    }
}
