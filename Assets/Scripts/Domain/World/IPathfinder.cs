using System.Collections.Generic;

namespace EmberCrpg.Domain.World
{
    /// <summary>Deterministic grid pathfinder interface scaffold for PROCESS pathing.</summary>
    public interface IPathfinder
    {
        /// <summary>Finds a deterministic path for the supplied request.</summary>
        PathfinderResult FindPath(PathfinderRequest request);

        /// <summary>Advances an actor one deterministic step along a path result.</summary>
        ActorPathStep StepActor(int actorId, PathfinderResult path);
    }

    /// <summary>Immutable request describing actor size and start/goal cells.</summary>
    public readonly struct PathfinderRequest
    {
        public PathfinderRequest(int actorId, int startX, int startY, int goalX, int goalY, int actorSize)
        {
            ActorId = actorId;
            StartX = startX;
            StartY = startY;
            GoalX = goalX;
            GoalY = goalY;
            ActorSize = actorSize;
        }

        /// <summary>Actor requesting the path.</summary>
        public int ActorId { get; }

        /// <summary>Starting X coordinate.</summary>
        public int StartX { get; }

        /// <summary>Starting Y coordinate.</summary>
        public int StartY { get; }

        /// <summary>Goal X coordinate.</summary>
        public int GoalX { get; }

        /// <summary>Goal Y coordinate.</summary>
        public int GoalY { get; }

        /// <summary>Actor footprint size for pathing constraints.</summary>
        public int ActorSize { get; }
    }

    /// <summary>Immutable pathfinding result with deterministic step data.</summary>
    public readonly struct PathfinderResult
    {
        public PathfinderResult(bool success, IReadOnlyList<int> steps, int totalCost)
        {
            Success = success;
            Steps = steps;
            TotalCost = totalCost;
        }

        /// <summary>Whether a path was found.</summary>
        public bool Success { get; }

        /// <summary>Deterministic encoded path steps.</summary>
        public IReadOnlyList<int> Steps { get; }

        /// <summary>Total traversal cost.</summary>
        public int TotalCost { get; }
    }

    /// <summary>Immutable one-step actor movement result.</summary>
    public readonly struct ActorPathStep
    {
        public ActorPathStep(int newX, int newY, bool arrived)
        {
            NewX = newX;
            NewY = newY;
            Arrived = arrived;
        }

        /// <summary>New X coordinate after stepping.</summary>
        public int NewX { get; }

        /// <summary>New Y coordinate after stepping.</summary>
        public int NewY { get; }

        /// <summary>Whether the actor reached the path goal.</summary>
        public bool Arrived { get; }
    }
}
