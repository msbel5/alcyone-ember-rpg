using System;
using System.Collections.Generic;
using System.Reflection;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies the pathfinder contract stays deterministic and C# 9 compatible.</summary>
    public sealed class IPathfinderTests
    {
        [Test]
        public void TryFindPath_ForFixtureMap_ReturnsDeterministicSteps()
        {
            var pathfinder = new FixturePathfinder();
            var request = new PathfinderRequest(42, 0, 0, 2, 1, 1);

            var first = pathfinder.FindPath(request);
            var second = pathfinder.FindPath(request);

            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(second.Success, Is.EqualTo(first.Success));
            Assert.That(second.TotalCost, Is.EqualTo(first.TotalCost));
            Assert.That(second.Steps, Is.EqualTo(first.Steps));
            Assert.That(first.Steps, Is.EqualTo(new[] { Pack(0, 1), Pack(1, 1), Pack(2, 1) }));
        }

        [Test]
        public void PathfinderResult_Steps_AreReadOnlyAndCopied()
        {
            var sourceSteps = new List<int> { Pack(1, 0), Pack(2, 0) };
            var result = new PathfinderResult(true, sourceSteps, sourceSteps.Count);
            sourceSteps[0] = Pack(9, 9);

            var property = typeof(PathfinderResult).GetProperty(nameof(PathfinderResult.Steps));
            Assert.That(property, Is.Not.Null);
            Assert.That(property.PropertyType, Is.EqualTo(typeof(IReadOnlyList<int>)));
            Assert.That(property.SetMethod, Is.Null);
            Assert.That(result.Steps, Is.EqualTo(new[] { Pack(1, 0), Pack(2, 0) }));

            var mutableView = result.Steps as IList<int>;
            Assert.That(mutableView, Is.Not.Null);
            Assert.That(mutableView.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutableView[0] = Pack(3, 0));
        }

        private static int Pack(int x, int y)
        {
            return (y * 1000) + x;
        }

        private sealed class FixturePathfinder : IPathfinder
        {
            public PathfinderResult FindPath(PathfinderRequest request)
            {
                if (request.StartX != 0 || request.StartY != 0 || request.GoalX != 2 || request.GoalY != 1)
                {
                    return new PathfinderResult(false, new int[0], 0);
                }

                var steps = new[] { Pack(0, 1), Pack(1, 1), Pack(2, 1) };
                return new PathfinderResult(true, steps, steps.Length);
            }

            public ActorPathStep StepActor(int actorId, PathfinderResult path)
            {
                var firstStep = path.Steps[0];
                var x = firstStep % 1000;
                var y = firstStep / 1000;
                return new ActorPathStep(x, y, path.Steps.Count == 1);
            }
        }
    }
}
