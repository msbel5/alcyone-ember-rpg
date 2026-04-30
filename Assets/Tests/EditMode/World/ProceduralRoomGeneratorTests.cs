using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin the deterministic one-room generator used by the runtime bootstrap.
// They focus on seed repeatability and spawn placement.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies repeatable room generation for Sprint 1.</summary>
    public sealed class ProceduralRoomGeneratorTests
    {
        [Test]
        public void Generate_SameSeed_RepeatsDimensionsAndSpawns()
        {
            var generator = new ProceduralRoomGenerator();
            var left = generator.Generate(1337);
            var right = generator.Generate(1337);
            Assert.That(new[] { left.Width, left.Height, left.PickupSpawn.X, left.PickupSpawn.Y }, Is.EqualTo(new[] { right.Width, right.Height, right.PickupSpawn.X, right.PickupSpawn.Y }));
        }

        [Test]
        public void Generate_PlacesEnemyInsideRoom()
        {
            var room = new ProceduralRoomGenerator().Generate(1337);
            Assert.That(room.IsWalkable(room.EnemySpawn), Is.True);
        }
    }
}
