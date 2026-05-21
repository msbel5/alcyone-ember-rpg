using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Process
{
    /// <summary>
    /// Advances claimed actors one deterministic grid cell per tick toward their
    /// claimed job's worksite. Emits ActorStepped events for every successful step.
    /// Closes CO-03 in docs/sprint-faz-4-atom-map.md Debt ledger.
    /// </summary>
    public sealed class PathfindingSystem
    {
        private readonly IPathfinder _pathfinder;

        public PathfindingSystem(IPathfinder pathfinder)
        {
            _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
        }

        public void Tick(ActorStore actors, JobBoard board, WorldEventLog events, GameTime now)
        {
            if (actors == null) throw new ArgumentNullException(nameof(actors));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (events == null) throw new ArgumentNullException(nameof(events));

            foreach (var request in board.Requests)
            {
                var actorId = board.GetClaimedBy(request.Id);
                if (actorId.IsEmpty)
                    continue;

                var actor = actors.Get(actorId);
                if (actor == null || actor.Position.Equals(request.WorksitePosition))
                    continue;

                var pathRequest = new PathfinderRequest(
                    actorId: 0,
                    startX: actor.Position.X,
                    startY: actor.Position.Y,
                    goalX: request.WorksitePosition.X,
                    goalY: request.WorksitePosition.Y,
                    actorSize: 1);

                var path = _pathfinder.FindPath(pathRequest);
                if (!path.Success || path.Steps.Count == 0)
                    continue;

                var step = _pathfinder.StepActor(0, path);
                var nextPosition = new GridPosition(step.NewX, step.NewY);
                actor.MoveTo(nextPosition);

                events.Append(new WorldEvent(
                    now,
                    WorldEventKind.ActorStepped,
                    actorId,
                    request.SiteId,
                    BuildReason(actor.Position, nextPosition)));
            }
        }

        private static string BuildReason(GridPosition from, GridPosition to)
        {
            return $"actor_stepped from:{from.X},{from.Y} to:{to.X},{to.Y}";
        }
    }
}
