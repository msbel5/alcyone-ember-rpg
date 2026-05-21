using System.Collections.Generic;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// Deterministic four-connected A* pathfinder over packed (x, y) cells.
    /// Pure in-memory: no Unity, no I/O. Same request returns the same path.
    /// Closes CO-02 in docs/sprint-faz-4-atom-map.md Debt ledger.
    /// </summary>
    public sealed class GridPathfinder : IPathfinder
    {
        private const int PackStride = 1000;

        public PathfinderResult FindPath(PathfinderRequest request)
        {
            var startKey = Pack(request.StartX, request.StartY);
            var goalKey = Pack(request.GoalX, request.GoalY);

            if (startKey == goalKey)
                return new PathfinderResult(true, new int[0], 0);

            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, int> { [startKey] = 0 };
            var open = new SortedSet<OpenNode>();
            var sequence = 0;
            open.Add(new OpenNode(Heuristic(request.StartX, request.StartY, request.GoalX, request.GoalY), sequence++, startKey));
            var inOpen = new HashSet<int> { startKey };

            while (open.Count > 0)
            {
                var current = open.Min;
                open.Remove(current);
                inOpen.Remove(current.Key);

                if (current.Key == goalKey)
                    return ReconstructPath(cameFrom, current.Key);

                var currentG = gScore[current.Key];
                var (cx, cy) = Unpack(current.Key);

                ExpandNeighbour(cx + 1, cy, current.Key, currentG, goalKey, request.GoalX, request.GoalY, cameFrom, gScore, open, inOpen, ref sequence);
                ExpandNeighbour(cx - 1, cy, current.Key, currentG, goalKey, request.GoalX, request.GoalY, cameFrom, gScore, open, inOpen, ref sequence);
                ExpandNeighbour(cx, cy + 1, current.Key, currentG, goalKey, request.GoalX, request.GoalY, cameFrom, gScore, open, inOpen, ref sequence);
                ExpandNeighbour(cx, cy - 1, current.Key, currentG, goalKey, request.GoalX, request.GoalY, cameFrom, gScore, open, inOpen, ref sequence);
            }

            return new PathfinderResult(false, new int[0], 0);
        }

        public ActorPathStep StepActor(int actorId, PathfinderResult path)
        {
            if (path.Steps == null || path.Steps.Count == 0)
                return new ActorPathStep(0, 0, true);

            var (x, y) = Unpack(path.Steps[0]);
            return new ActorPathStep(x, y, path.Steps.Count == 1);
        }

        private static void ExpandNeighbour(
            int nx,
            int ny,
            int parentKey,
            int parentG,
            int goalKey,
            int goalX,
            int goalY,
            Dictionary<int, int> cameFrom,
            Dictionary<int, int> gScore,
            SortedSet<OpenNode> open,
            HashSet<int> inOpen,
            ref int sequence)
        {
            var neighbourKey = Pack(nx, ny);
            var tentativeG = parentG + 1;
            if (gScore.TryGetValue(neighbourKey, out var existingG) && tentativeG >= existingG)
                return;

            cameFrom[neighbourKey] = parentKey;
            gScore[neighbourKey] = tentativeG;

            if (inOpen.Contains(neighbourKey))
                return;

            open.Add(new OpenNode(tentativeG + Heuristic(nx, ny, goalX, goalY), sequence++, neighbourKey));
            inOpen.Add(neighbourKey);
        }

        private static PathfinderResult ReconstructPath(Dictionary<int, int> cameFrom, int goalKey)
        {
            var stack = new Stack<int>();
            var current = goalKey;
            while (cameFrom.TryGetValue(current, out var parent))
            {
                stack.Push(current);
                current = parent;
            }

            var steps = new int[stack.Count];
            var i = 0;
            while (stack.Count > 0)
                steps[i++] = stack.Pop();
            return new PathfinderResult(true, steps, steps.Length);
        }

        private static int Heuristic(int x, int y, int goalX, int goalY)
        {
            return System.Math.Abs(goalX - x) + System.Math.Abs(goalY - y);
        }

        private static int Pack(int x, int y)
        {
            return (y * PackStride) + x;
        }

        private static (int x, int y) Unpack(int packed)
        {
            return (packed % PackStride, packed / PackStride);
        }

        /// <summary>
        /// Open-set entry ordered by f-score then by deterministic insertion sequence
        /// so identical requests always pop nodes in the same order.
        /// </summary>
        private readonly struct OpenNode : System.IComparable<OpenNode>
        {
            public OpenNode(int fScore, int sequence, int key)
            {
                FScore = fScore;
                Sequence = sequence;
                Key = key;
            }

            public int FScore { get; }
            public int Sequence { get; }
            public int Key { get; }

            public int CompareTo(OpenNode other)
            {
                var byF = FScore.CompareTo(other.FScore);
                if (byF != 0) return byF;
                return Sequence.CompareTo(other.Sequence);
            }
        }
    }
}
