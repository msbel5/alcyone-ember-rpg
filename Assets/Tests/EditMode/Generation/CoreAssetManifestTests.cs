using System;
using System.Linq;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Domain.Worldgen;
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

        [Test]
        public void DefaultManifest_IncludesGeneratedNpcArchetypePortraits()
        {
            var entries = CoreAssetManifest.CreateDefault().Entries;
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
                var entry = entries.Single(e => e.Id == id);
                Assert.That(entry.Category, Is.EqualTo("portrait"), id);
                Assert.That(entry.ExpectedPath, Is.EqualTo("Assets/Generated/Core/" + id + ".png"), id);
                Assert.That(entry.RequiresGeneration, Is.True, id);
                Assert.That(entry.Width, Is.EqualTo(512), id);
                Assert.That(entry.Height, Is.EqualTo(512), id);
            }
        }

        [Test]
        public void DefaultManifest_IncludesGeneratedNpcRoleSprites()
        {
            var entries = CoreAssetManifest.CreateDefault().Entries;
            foreach (NpcRole role in Enum.GetValues(typeof(NpcRole)))
            {
                if (role == NpcRole.None)
                    continue;

                var id = "npc_" + role.ToString().ToLowerInvariant();
                var entry = entries.Single(e => e.Id == id);
                Assert.That(entry.Category, Is.EqualTo("npc"), id);
                Assert.That(entry.ExpectedPath, Is.EqualTo("Assets/Generated/Core/" + id + ".png"), id);
                Assert.That(entry.StaticPromptKey, Is.EqualTo(id), id);
                Assert.That(entry.RequiresGeneration, Is.True, id);
                Assert.That(entry.Width, Is.EqualTo(896), id);
                Assert.That(entry.Height, Is.EqualTo(1344), id);
                Assert.That(entry.ModelHint, Is.EqualTo("sd15-lcm"), id);
            }
        }
    }
}
