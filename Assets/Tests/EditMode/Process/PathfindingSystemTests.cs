using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Pins deterministic one-cell-per-tick advancement of claimed actors toward
    /// their assigned worksite, plus the ActorStepped event emission.
    /// Closes CO-03 row in DOCS/sprint-faz-4-atom-map.md Debt ledger.
    /// </summary>
    public sealed class PathfindingSystemTests
    {
        private static readonly SiteId Site = new SiteId(3UL);
        private static readonly GridPosition Worksite = new GridPosition(2, 0);
        private static readonly ActorId Requester = new ActorId(6UL);
        private static readonly ActorId Smith = new ActorId(10UL);

        private static JobRequest MakeFurnaceJob()
        {
            return new JobRequest(
                new JobId(1UL),
                new RecipeId(100UL),
                Site,
                Worksite,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Requester);
        }

        private static ActorRecord MakeSmith()
        {
            return new ActorRecord(
                Smith,
                "Smith",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 1,
                baseDamage: 3);
        }

        [Test]
        public void Tick_MovesClaimedActor_OneCellTowardWorksite()
        {
            var actors = new ActorStore();
            var board = new JobBoard();
            var events = new WorldEventLog();
            actors.Add(MakeSmith());
            board.Add(MakeFurnaceJob());
            board.TryClaim(new JobId(1UL), Smith, out _);

            var system = new PathfindingSystem(new ScriptedPathfinder(new[] { (1, 0), (2, 0) }));
            system.Tick(actors, board, events, default);

            var actor = actors.Get(Smith);
            Assert.That(actor.Position, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(events.Count, Is.EqualTo(1));
            var stepped = events.Events[0];
            Assert.That(stepped.Kind, Is.EqualTo(WorldEventKind.ActorStepped));
            Assert.That(stepped.ActorId, Is.EqualTo(Smith));
            Assert.That(stepped.SiteId, Is.EqualTo(Site));
        }

        [Test]
        public void Tick_DoesNothing_WhenActorAtWorksite()
        {
            var actors = new ActorStore();
            var board = new JobBoard();
            var events = new WorldEventLog();

            // Spawn the smith ON the worksite cell.
            actors.Add(new ActorRecord(
                Smith,
                "Smith",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                Worksite,
                accuracy: 10, dodge: 5, armor: 1, baseDamage: 3));
            board.Add(MakeFurnaceJob());
            board.TryClaim(new JobId(1UL), Smith, out _);

            var system = new PathfindingSystem(new ScriptedPathfinder(new (int x, int y)[0]));
            system.Tick(actors, board, events, default);

            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(actors.Get(Smith).Position, Is.EqualTo(Worksite));
        }

        [Test]
        public void Tick_SkipsUnclaimedJobs()
        {
            var actors = new ActorStore();
            var board = new JobBoard();
            var events = new WorldEventLog();
            actors.Add(MakeSmith());
            board.Add(MakeFurnaceJob());

            var system = new PathfindingSystem(new ScriptedPathfinder(new[] { (1, 0) }));
            system.Tick(actors, board, events, default);

            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(actors.Get(Smith).Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        /// <summary>
        /// Test double that returns a pre-scripted path so the system test does not
        /// depend on GridPathfinder.
        /// </summary>
        private sealed class ScriptedPathfinder : IPathfinder
        {
            private readonly IReadOnlyList<(int x, int y)> _steps;

            public ScriptedPathfinder(IReadOnlyList<(int x, int y)> steps)
            {
                _steps = steps;
            }

            public PathfinderResult FindPath(PathfinderRequest request)
            {
                var packed = new int[_steps.Count];
                for (var i = 0; i < _steps.Count; i++)
                    packed[i] = (_steps[i].y * 1000) + _steps[i].x;
                return new PathfinderResult(_steps.Count > 0, packed, _steps.Count);
            }

            public ActorPathStep StepActor(int actorId, PathfinderResult path)
            {
                if (path.Steps == null || path.Steps.Count == 0)
                    return new ActorPathStep(0, 0, true);
                var step = path.Steps[0];
                return new ActorPathStep(step % 1000, step / 1000, path.Steps.Count == 1);
            }
        }
    }
}
