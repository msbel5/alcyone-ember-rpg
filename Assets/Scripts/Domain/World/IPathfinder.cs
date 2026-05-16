namespace EmberCrpg.Domain.World
{
    // CO-01: Deterministic grid pathfinder interface (scaffold)
    public interface IPathfinder
    {
        // Path finding request returns a deterministic PathfinderResult for a given seeded RNG and fixture map
        PathfinderResult FindPath(PathfinderRequest request);

        // Convenience step method used by Pathing systems to advance an actor along a path
        ActorPathStep StepActor(int actorId, PathfinderResult path);
    }

    // Minimal supporting types (scaffolds) — real definitions live elsewhere; these are lightweight placeholders
    public readonly record struct PathfinderRequest(int ActorId, int StartX, int StartY, int GoalX, int GoalY, int ActorSize);
    public readonly record struct PathfinderResult(bool Success, int[] Steps, int TotalCost);
    public readonly record struct ActorPathStep(int NewX, int NewY, bool Arrived);
}
