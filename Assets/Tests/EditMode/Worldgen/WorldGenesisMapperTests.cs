using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Worldgen;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Worldgen
{
    public sealed class WorldGenesisMapperTests
    {
        [TestCase("intrigue", WorldGenre.PoliticalIntrigue)]
        [TestCase("hunt", WorldGenre.MonsterHunt)]
        [TestCase("merchant", WorldGenre.MerchantEmpire)]
        [TestCase("pilgrimage", WorldGenre.Pilgrimage)]
        public void ToGenre_UsesStableCallingIds(string calling, WorldGenre expected)
        {
            var genre = WorldGenesisMapper.ToGenre("low", calling, "ashford");
            Assert.That(genre, Is.EqualTo(expected));
        }

        [Test]
        public void ToGenre_IntrigueTextStillMapsToPoliticalIntrigue()
        {
            var genre = WorldGenesisMapper.ToGenre("grim", "court envoy", "capital");
            Assert.That(genre, Is.EqualTo(WorldGenre.PoliticalIntrigue));
        }

        [Test]
        public void ToStyle_UsesMoodIdMappings()
        {
            Assert.That(WorldGenesisMapper.ToStyle("mythic"), Is.EqualTo(WorldStyle.AncientMythology));
            Assert.That(WorldGenesisMapper.ToStyle("low"), Is.EqualTo(WorldStyle.LowFantasy));
            Assert.That(WorldGenesisMapper.ToStyle("heroic"), Is.EqualTo(WorldStyle.HighFantasy));
        }
    }
}
