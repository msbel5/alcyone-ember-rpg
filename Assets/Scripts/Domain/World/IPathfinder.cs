using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EmberCrpg.Domain.World
{
    /// <summary>Provides deterministic grid path queries for world and process systems.</summary>
    public interface IPathfinder
    {
        /// <summary>Attempts to find a path for the supplied request.</summary>
        bool TryFindPath(PathfinderRequest request, out PathfinderResult result);

        /// <summary>Returns the next actor movement step for an existing path result.</summary>
        ActorPathStep StepActor(int actorId, PathfinderResult path);
    }

    /// <summary>Describes a deterministic grid path request.</summary>
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

        public int ActorId { get; }
        public int StartX { get; }
        public int StartY { get; }
        public int GoalX { get; }
        public int GoalY { get; }
        public int ActorSize { get; }
    }

    /// <summary>Contains the outcome of a deterministic grid path query.</summary>
    public readonly struct PathfinderResult
    {
        private readonly int[] _steps;

        public PathfinderResult(bool success, IEnumerable<int> steps, int totalCost)
        {
            Success = success;
            _steps = steps == null ? new int[0] : steps.ToArray();
            TotalCost = totalCost;
        }

        public bool Success { get; }

        public IReadOnlyList<int> Steps
        {
            get { return new ReadOnlyCollection<int>(_steps ?? new int[0]); }
        }

        public int TotalCost { get; }
    }

    /// <summary>Describes one deterministic movement step for an actor.</summary>
    public readonly struct ActorPathStep
    {
        public ActorPathStep(int newX, int newY, bool arrived)
        {
            NewX = newX;
            NewY = newY;
            Arrived = arrived;
        }

        public int NewX { get; }
        public int NewY { get; }
        public bool Arrived { get; }
    }
}
