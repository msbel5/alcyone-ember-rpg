using EmberCrpg.Data.GeneratedAssets;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.GeneratedAssets
{
    public sealed class GeneratedNpcBillboardResolverTests
    {
        [Test]
        public void TryResolveRecord_UsesRoleAndSeedDeterministically()
        {
            var database = GeneratedAssetDatabase.CreateRuntimeInstance();
            database.Records.Add(NewRecord("npc_guard", "guard", 17, "Assets/Generated/Core/npc_guard.png"));
            database.Records.Add(NewRecord("npc_guard_variant", "guard", 17, "Assets/Generated/Core/npc_guard_variant.png", variantIndex: 1));
            database.RebuildStableIds();

            Assert.That(GeneratedNpcBillboardResolver.TryResolveRecord(database, "npc_guard", 77, out var first), Is.True);
            Assert.That(GeneratedNpcBillboardResolver.TryResolveRecord(database, "guard", 77, out var second), Is.True);
            Assert.That(first.stableId, Is.EqualTo(second.stableId));
        }

        [Test]
        public void BuildFallbackCoreId_NormalizesNpcPrefix()
        {
            Assert.That(GeneratedNpcBillboardResolver.BuildFallbackCoreId("guard"), Is.EqualTo("npc_guard"));
            Assert.That(GeneratedNpcBillboardResolver.BuildFallbackCoreId("npc_guard"), Is.EqualTo("npc_guard"));
        }

        private static GeneratedAssetRecord NewRecord(string name, string role, int seed, string spritePath, int variantIndex = 0)
        {
            var record = new GeneratedAssetRecord
            {
                displayName = name,
                kind = GeneratedAssetKind.CharacterBillboard,
                seed = seed,
                spritePath = spritePath,
                relativeAssetPath = spritePath,
                licenseStatus = GeneratedAssetLicenseStatus.Clean,
                humanApproved = true,
            };
            record.key.role = role;
            record.key.styleVersion = "v1";
            record.key.variantIndex = variantIndex;
            record.SyncIdentity();
            return record;
        }
    }
}
