using EmberCrpg.Data.GeneratedAssets;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.GeneratedAssets
{
    public sealed class GeneratedAssetDatabaseTests
    {
        [Test]
        public void StableId_IsDeterministicForSameKey()
        {
            var key = NewKey();
            var first = key.BuildStableId();
            var second = key.BuildStableId();

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void StableId_ChangesWhenVariantChanges()
        {
            var first = NewKey();
            var second = NewKey();
            second.variantIndex = 1;

            Assert.That(first.BuildStableId(), Is.Not.EqualTo(second.BuildStableId()));
        }

        [Test]
        public void StableId_ChangesWhenSeedChanges()
        {
            var first = NewKey();
            var second = NewKey();
            second.seed = 99;

            Assert.That(first.BuildStableId(), Is.Not.EqualTo(second.BuildStableId()));
        }

        [Test]
        public void Validation_FlagsDuplicateAndForbiddenRecords()
        {
            var database = GeneratedAssetDatabase.CreateRuntimeInstance();
            database.Records.Add(NewRecord("first", 7, GeneratedAssetLicenseStatus.Clean, approved: true));
            database.Records.Add(NewRecord("second", 7, GeneratedAssetLicenseStatus.Forbidden, approved: false));
            database.RebuildStableIds();

            var issues = database.ValidateRecords();

            Assert.That(issues, Has.Some.Matches<GeneratedAssetValidationIssue>(i => i.message.Contains("Duplicate stable id")));
            Assert.That(issues, Has.Some.Matches<GeneratedAssetValidationIssue>(i => i.message.Contains("forbidden")));
            Assert.That(issues, Has.Some.Matches<GeneratedAssetValidationIssue>(i => i.message.Contains("not human approved")));
        }

        [Test]
        public void Resolver_ReturnsSameRecordForSameSeed()
        {
            var database = GeneratedAssetDatabase.CreateRuntimeInstance();
            database.Records.Add(NewRecord("rogue", 11, GeneratedAssetLicenseStatus.Clean, approved: true));
            database.Records.Add(NewRecord("rogue-variant", 11, GeneratedAssetLicenseStatus.Clean, approved: true, variantIndex: 1));
            database.RebuildStableIds();

            var query = new GeneratedAssetQuery
            {
                kind = GeneratedAssetKind.CharacterBillboard,
                archetype = "rogue",
                seed = 77,
            };

            Assert.That(database.TryResolve(query, out var first), Is.True);
            Assert.That(database.TryResolve(query, out var second), Is.True);
            Assert.That(first.stableId, Is.EqualTo(second.stableId));
        }

        [Test]
        public void Resolver_SkipsForbiddenWhenRequested()
        {
            var database = GeneratedAssetDatabase.CreateRuntimeInstance();
            database.Records.Add(NewRecord("guard", 5, GeneratedAssetLicenseStatus.Forbidden, approved: true));
            database.RebuildStableIds();

            var query = new GeneratedAssetQuery
            {
                kind = GeneratedAssetKind.CharacterBillboard,
                archetype = "guard",
                seed = 1,
                excludeForbidden = true,
            };

            Assert.That(database.TryResolve(query, out _), Is.False);
        }

        private static GeneratedAssetKey NewKey()
        {
            return new GeneratedAssetKey
            {
                kind = GeneratedAssetKind.CharacterBillboard,
                archetype = "rogue",
                biome = "temperate",
                culture = "ember",
                role = "scout",
                material = "cloth",
                tier = "common",
                variantIndex = 0,
                styleVersion = "v1",
                seed = 17,
                promptHash = "prompt-a",
            };
        }

        private static GeneratedAssetRecord NewRecord(string name, int seed, GeneratedAssetLicenseStatus license, bool approved, int variantIndex = 0)
        {
            var record = new GeneratedAssetRecord
            {
                displayName = name,
                kind = GeneratedAssetKind.CharacterBillboard,
                seed = seed,
                licenseStatus = license,
                humanApproved = approved,
                relativeAssetPath = "Assets/GeneratedLibrary/characterbillboard/" + name + ".png",
                spritePath = "Assets/GeneratedLibrary/characterbillboard/" + name + ".png",
            };
            record.key.archetype = name.StartsWith("rogue") ? "rogue" : "guard";
            record.key.styleVersion = "v1";
            record.key.variantIndex = variantIndex;
            record.SyncIdentity();
            return record;
        }
    }
}
