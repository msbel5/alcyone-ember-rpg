using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.GeneratedAssets
{
    public sealed class CoreAssetLibraryRecordBuilderTests
    {
        [Test]
        public void BuildForNpc_MapsToCharacterBillboard()
        {
            var entry = new ManifestEntry("npc_rogue", "npc", "Assets/Generated/Core/npc_rogue.png", "npc_rogue", 896, 1344, true, 300, "sd15-lcm");

            var record = CoreAssetLibraryRecordBuilder.Build(entry, "prompt", "negative", "hash", "2026-06-06T00:00:00Z");

            Assert.That(record.kind, Is.EqualTo(GeneratedAssetKind.CharacterBillboard));
            Assert.That(record.key.role, Is.EqualTo("rogue"));
            Assert.That(record.spritePath, Is.EqualTo(entry.ExpectedPath));
            Assert.That(record.key.promptHash, Is.EqualTo("hash"));
            Assert.That(record.seed, Is.Not.EqualTo(0));
        }

        [Test]
        public void BuildForWall_MapsToTileableWall()
        {
            var entry = new ManifestEntry("wall_tavernflavour", "wall", "Assets/Generated/Core/wall_tavernflavour.png", "wall_tavernflavour", 512, 512, true, 300, "sd15-lcm");

            var record = CoreAssetLibraryRecordBuilder.Build(entry, "prompt", "negative", "hash", "2026-06-06T00:00:00Z");

            Assert.That(record.kind, Is.EqualTo(GeneratedAssetKind.TileableWall));
            Assert.That(record.albedoPath, Is.EqualTo(entry.ExpectedPath));
            Assert.That(record.isTileable, Is.True);
            Assert.That(record.deLit, Is.True);
            Assert.That(record.key.material, Is.EqualTo("tavernflavour"));
        }
    }
}
