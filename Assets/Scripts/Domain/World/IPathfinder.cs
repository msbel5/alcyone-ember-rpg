using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EmberCrpg.Domain.World
{
    /// <summary>Provides deterministic grid path queries for world and process systems.</summary>
    public interface IPathfinder
    {
        /// <summary>Attempts to find a path for the supplied request.</summary>
        /// <param name="request">The path query to resolve.</param>
        /// <param name="result">The deterministic path result when a route is available.</param>
        /// <returns><c>true</c> when a valid route was found; otherwise <c>false</c>.</returns>
        bool TryFindPath(PathfinderRequest request, out PathfinderResult result);

        /// <summary>Returns the next actor movement step for an existing path result.</summary>
        /// <param name="actorId">The actor that will consume the step.</param>
        /// <param name="path">The path result to advance along.</param>
        /// <returns>The next deterministic actor step.</returns>
        ActorPathStep StepActor(int actorId, PathfinderResult path);
    }

    /// <summary>Describes a deterministic grid path request.</summary>
    public readonly struct PathfinderRequest
    {
        /// <summary>Initializes a path request.</summary>
        public PathfinderRequest(int actorId, int startX, int startY, int goalX, int goalY, int actorSize)
        {
            ActorId = actorId;
            StartX = startX;
            StartY = startY;
            GoalX = goalX;
            GoalY = goalY;
            ActorSize = actorSize;
        }

        /// <summary>Gets the actor identifier that owns the path request.</summary>
        public int ActorId { get; }

        /// <summary>Gets the starting grid X coordinate.</summary>
        public int StartX { get; }

        /// <summary>Gets the starting grid Y coordinate.</summary>
        public int StartY { get; }

        /// <summary>Gets the target grid X coordinate.</summary>
        public int GoalX { get; }

        /// <summary>Gets the target grid Y coordinate.</summary>
        public int GoalY { get; }

        /// <summary>Gets the grid footprint size of the moving actor.</summary>
        public int ActorSize { get; }
    }

    /// <summary>Contains the outcome of a deterministic grid path query.</summary>
    public readonly struct PathfinderResult
    {
        private readonly int[] steps;

        /// <summary>Initializes a path result.</summary>
        public PathfinderResult(bool success, IEnumerable<int> steps, int totalCost)
        {
            Success = success;
            this.steps = steps == null ? new int[0] : steps.ToArray();
            TotalCost = totalCost;
        }

        /// <summary>Gets whether the path request found a route.</summary>
        public bool Success { get; }

        /// <summary>Gets the immutable sequence of packed grid steps.</summary>
        public IReadOnlyList<int> Steps
        {
            get
            {
                return new ReadOnlyCollection<int>(steps ?? new int[0]);
            }
        }

        /// <summary>Gets the deterministic total path cost.</summary>
        public int TotalCost { get; }
    }

    /// <summary>Describes one deterministic movement step for an actor.</summary>
    public readonly struct ActorPathStep
    {
        /// <summary>Initializes an actor path step.</summary>
        public ActorPathStep(int newX, int newY, bool arrived)
        {
            NewX = newX;
            NewY = newY;
            Arrived = arrived;
        }

        /// <summary>Gets the actor's new grid X coordinate.</summary>
        public int NewX { get; }

        /// <summary>Gets the actor's new grid Y coordinate.</summary>
        public int NewY { get; }

        /// <summary>Gets whether this step reaches the path destination.</summary>
        public bool Arrived { get; }
    }
}
