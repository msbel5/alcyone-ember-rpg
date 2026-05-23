using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Worldgen
{
    public sealed class NpcSeedSaveRoundTripTests
    {
        [Test]
        public void SliceSaveMapper_RoundTripsNpcSeedPortraitAssetPath()
        {
            var world = new SliceWorldFactory().Create(roomSeed: 42);
            world.NpcSeeds.Add(new NpcSeedRecord(
                new NpcId(7),
                new SettlementId(11),
                new FactionId(13),
                "Brennec the Forge-Walker",
                931,
                NpcRole.Artisan,
                "portrait-cache-hash"));

            var data = SliceSaveMapper.ToData(world);
            var loaded = SliceSaveMapper.ToWorld(data, SliceSaveRehydration.CreateSeedWorld(data.roomSeed));

            Assert.That(loaded.NpcSeeds.Count, Is.EqualTo(1));
            Assert.That(loaded.NpcSeeds[0].Id.Value, Is.EqualTo(7UL));
            Assert.That(loaded.NpcSeeds[0].PortraitAssetPath, Is.EqualTo("portrait-cache-hash"));
        }
    }
}
