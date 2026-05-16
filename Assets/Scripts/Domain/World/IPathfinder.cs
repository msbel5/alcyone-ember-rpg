namespace EmberCrpg.Domain.World
{
    // CO-01: Deterministic grid pathfinder interface (scaffold)
    public interface IPathfinder
    {
        // Path finding request returns a deterministic PathResult for a given seeded RNG and fixture map
        PathResult FindPath(PathRequest request);

        // Convenience step method used by Pathing systems to advance an actor along a path
        ActorPathStep StepActor(ActorRecord actor, PathResult path);
    }

    // Minimal supporting types (scaffolds) — real definitions live elsewhere; these are lightweight placeholders
    public readonly record struct PathRequest(int ActorId, int StartX, int StartY, int GoalX, int GoalY, int ActorSize);
    public readonly record struct PathResult(bool Success, int[] Steps, int TotalCost);
    public readonly record struct ActorPathStep(int NewX, int NewY, bool Arrived);
    public readonly record struct ActorRecord(int ActorId, int X, int Y);
}
