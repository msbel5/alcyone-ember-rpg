using System;
using System.IO;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class AssetForgeCacheTests
    {
        [Test]
        public void Cache_HitOnIdenticalRequest_MissOnDifferentSeed()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-forge-cache-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                var cache = new AssetForgeCache(root);
                var profile = new WorldProfile(WorldStyle.DarkFantasyGrim, WorldGenre.PoliticalIntrigue, 42, 1000000, 47, 12, 100, "grim", "mage", "city");
                var npc = new NpcSeedRecord(new NpcId(1), new SettlementId(2), new FactionId(3), "Mira", 920, NpcRole.Merchant);
                var request = PromptComposers.NpcPortrait(npc, profile);

                Assert.That(cache.TryRead(request, out _), Is.False);
                cache.Write(request, new AssetGenerationResult(request.RequestId, new byte[] { 1, 2, 3 }, "image/png", 1, true, ""));
                Assert.That(cache.TryRead(request, out var hit), Is.True);
                Assert.That(hit.ImageBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));

                var changed = new AssetGenerationRequest(request.RequestId, request.Subject, request.Style, request.Genre, request.MoodKeyword, request.PromptHash, request.Width, request.Height, request.Seed + 1, request.Prompt, request.NegativePrompt);
                Assert.That(cache.TryRead(changed, out _), Is.False);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
