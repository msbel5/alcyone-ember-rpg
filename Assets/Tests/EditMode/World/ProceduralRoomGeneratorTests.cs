using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin Sprint 3's richer deterministic room templates.
// They cover layout selection and walkable actor/pickup placement across all variants.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies deterministic room template generation.</summary>
    public sealed class ProceduralRoomGeneratorTests
    {
        [Test]
        public void Generate_SelectsThreeDistinctLayoutsFromSeedModulo()
        {
            var generator = new ProceduralRoomGenerator();
            var layouts = new[]
            {
                generator.Generate(1335).LayoutId,
                generator.Generate(1336).LayoutId,
                generator.Generate(1337).LayoutId,
            };

            Assert.That(layouts.Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void Generate_AllSpawnPointsRemainWalkableWithinBounds()
        {
            var generator = new ProceduralRoomGenerator();

            foreach (var seed in new[] { 1335, 1336, 1337 })
            {
                var room = generator.Generate(seed);
                var occupied = new HashSet<GridPosition>
                {
                    room.PlayerSpawn,
                    room.TalkerSpawn,
                    room.MerchantSpawn,
                    room.GuardSpawn,
                    room.EnemySpawn,
                    room.PickupSpawn,
                };

                Assert.That(room.IsWalkable(room.PlayerSpawn), Is.True, seed + " player");
                Assert.That(room.IsWalkable(room.TalkerSpawn), Is.True, seed + " talker");
                Assert.That(room.IsWalkable(room.MerchantSpawn), Is.True, seed + " merchant");
                Assert.That(room.IsWalkable(room.GuardSpawn), Is.True, seed + " guard");
                Assert.That(room.IsWalkable(room.EnemySpawn), Is.True, seed + " enemy");
                Assert.That(room.IsWalkable(room.PickupSpawn), Is.True, seed + " pickup");
                Assert.That(occupied.Count, Is.EqualTo(6), seed + " unique positions");
            }
        }
    }
}
