using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>
    /// Pins deterministic shortest-path behaviour for <see cref="GridPathfinder"/>.
    /// Closes CO-02 row in docs/sprint-phase-4-atom-map.md Debt ledger.
    /// </summary>
    public sealed class GridPathfinderTests
    {
        private static int Pack(int x, int y) => (y * 1000) + x;

        [Test]
        public void Identity_StartEqualsGoal_ReturnsEmptyPath()
        {
            var pathfinder = new GridPathfinder();
            var request = new PathfinderRequest(1, 2, 2, 2, 2, 1);

            var result = pathfinder.FindPath(request);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Steps, Is.Empty);
            Assert.That(result.TotalCost, Is.EqualTo(0));
        }

        [Test]
        public void StraightLine_East_ReturnsSequentialCells()
        {
            var pathfinder = new GridPathfinder();
            var request = new PathfinderRequest(1, 0, 0, 3, 0, 1);

            var result = pathfinder.FindPath(request);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Steps, Is.EqualTo(new[] { Pack(1, 0), Pack(2, 0), Pack(3, 0) }));
            Assert.That(result.TotalCost, Is.EqualTo(3));
        }

        [Test]
        public void StraightLine_North_ReturnsSequentialCells()
        {
            var pathfinder = new GridPathfinder();
            var request = new PathfinderRequest(1, 0, 0, 0, 2, 1);

            var result = pathfinder.FindPath(request);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(2));
            Assert.That(result.Steps[0], Is.EqualTo(Pack(0, 1)));
            Assert.That(result.Steps[1], Is.EqualTo(Pack(0, 2)));
        }

        [Test]
        public void Diagonal_PrefersDeterministicOrder()
        {
            var pathfinder = new GridPathfinder();
            var request = new PathfinderRequest(1, 0, 0, 2, 2, 1);

            var first = pathfinder.FindPath(request);
            var second = pathfinder.FindPath(request);

            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(first.Steps, Is.EqualTo(second.Steps));
            Assert.That(first.TotalCost, Is.EqualTo(second.TotalCost));
            // Four-connected manhattan: distance is 4 steps.
            Assert.That(first.Steps.Count, Is.EqualTo(4));
            Assert.That(first.TotalCost, Is.EqualTo(4));
        }

        [Test]
        public void ResultSteps_AreReadOnly_AndDefensivelyCopied()
        {
            var pathfinder = new GridPathfinder();
            var request = new PathfinderRequest(1, 0, 0, 1, 0, 1);

            var result = pathfinder.FindPath(request);

            var mutable = result.Steps as System.Collections.Generic.IList<int>;
            Assert.That(mutable, Is.Not.Null);
            Assert.That(mutable.IsReadOnly, Is.True);
        }
    }
}
