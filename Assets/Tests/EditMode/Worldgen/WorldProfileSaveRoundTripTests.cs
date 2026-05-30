using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Worldgen
{
    public sealed class WorldProfileSaveRoundTripTests
    {
        [Test]
        public void SliceSaveMapper_RoundTripsWorldProfile()
        {
            var world = new WorldFactory().Create(roomSeed: 42);
            world.WorldProfile = new WorldProfile(
                WorldStyle.DarkFantasyGrim,
                WorldGenre.PoliticalIntrigue,
                seed: 42u,
                targetPopulation: 1_043_217,
                regionCount: 47,
                factionCount: 32,
                historyYears: 100,
                moodKeyword: "grim",
                playerCallingKeyword: "diplomat",
                startLocationKeyword: "capital");

            var data = WorldSaveMapper.ToData(world);
            var loaded = WorldSaveMapper.ToWorld(data, WorldSaveRehydration.CreateSeedWorld((int)data.roomSeed));

            Assert.That(loaded.WorldProfile, Is.EqualTo(world.WorldProfile));
        }
    }
}
