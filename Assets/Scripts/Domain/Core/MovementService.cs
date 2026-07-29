using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;

// Design note:
// W32-03 §5: the ONE home of deterministic grid movement. PRD-02 replaces the
// greedy neighbour probe with a bounded A* search over the existing
// IWorldNavigability seam. No cache or second navigation authority is introduced.
namespace EmberCrpg.Domain.Core
{
    /// <summary>Observable result of one bounded route request.</summary>
    public enum MovementStepOutcome
    {
        Arrived = 0,
        Moved = 1,
        NoRoute = 2,
    }

    /// <summary>One deterministic movement decision and its resulting position.</summary>
    public readonly struct MovementStepResult
    {
        public MovementStepResult(MovementStepOutcome outcome, GridPosition position)
        {
            Outcome = outcome;
            Position = position;
        }

        public MovementStepOutcome Outcome { get; }
        public GridPosition Position { get; }
        public bool Moved => Outcome == MovementStepOutcome.Moved;
    }

    /// <summary>Deterministic, bounded one-tile route step toward a target.</summary>
    public static class MovementService
    {
        // The search admits destinations up to 256 Chebyshev cells away and examines at
        // most 4,096 unique/expanded nodes. These constants are part of the simulation
        // contract: exhausting either budget is an explicit NoRoute, never a freeze tick.
        public const int RouteRadiusBudget = 256;
        public const int RouteNodeBudget = 4096;

        // Fixed absolute neighbour order is the route tie-break. Equal-cost north/south
        // detours choose north; no seed, hash enumeration, or platform ordering participates.
        private static readonly int[] NeighbourDx = { 1, 0, -1, 0, 1, -1, -1, 1 };
        private static readonly int[] NeighbourDy = { 0, 1, 0, -1, 1, 1, -1, -1 };

        /// <summary>Canonical navigability view for a topology known to contain no blockers.</summary>
        public static IWorldNavigability OpenNav => OpenNavigability.Instance;

        /// <summary>
        /// Returns the next cell on a bounded route. <paramref name="goalRadius"/> allows
        /// occupied targets such as beds/benches to be approached without entering them.
        /// </summary>
        public static MovementStepResult RouteToward(
            GridPosition from,
            GridPosition to,
            IWorldNavigability nav,
            int goalRadius = 0)
        {
            return RouteToward(
                from, to, nav, goalRadius, RouteNodeBudget, RouteRadiusBudget);
        }

        /// <summary>
        /// Explicit-budget overload used by boundary tests. Production callers use the
        /// pinned constants above; both overloads execute this single routing algorithm.
        /// </summary>
        public static MovementStepResult RouteToward(
            GridPosition from,
            GridPosition to,
            IWorldNavigability nav,
            int goalRadius,
            int nodeBudget,
            int radiusBudget)
        {
            if (goalRadius < 0)
                throw new ArgumentOutOfRangeException(nameof(goalRadius));
            if (nodeBudget <= 0)
                throw new ArgumentOutOfRangeException(nameof(nodeBudget));
            if (radiusBudget <= 0)
                throw new ArgumentOutOfRangeException(nameof(radiusBudget));
            if (IsGoal(from, to, goalRadius))
                return new MovementStepResult(MovementStepOutcome.Arrived, from);

            // Null-nav compatibility is an open graph, not a second greedy algorithm.
            nav ??= OpenNavigability.Instance;

            if (Heuristic(from, to, goalRadius) > radiusBudget)
                return new MovementStepResult(MovementStepOutcome.NoRoute, from);
            if (goalRadius == 0 && !nav.IsWalkable(to))
                return new MovementStepResult(MovementStepOutcome.NoRoute, from);
            // Closed-form specialization of the same shortest-route contract for a graph
            // proven open. This avoids allocating/expanding the A* frontier once per actor
            // per tick in blocker-free settlements; custom/blocked nav still uses A* below.
            if (ReferenceEquals(nav, OpenNavigability.Instance))
                return OpenGraphStep(from, to);

            var open = new SearchHeap();
            var bestCost = new Dictionary<GridPosition, int>();
            var parent = new Dictionary<GridPosition, GridPosition>();
            var sequence = 0;
            open.Push(new SearchNode(
                from, 0, Heuristic(from, to, goalRadius), ManhattanTieBreak(from, to), sequence++));
            bestCost.Add(from, 0);

            var expanded = 0;
            while (open.Count > 0 && expanded < nodeBudget)
            {
                var current = open.Pop();
                if (!bestCost.TryGetValue(current.Cell, out var recordedCost)
                    || recordedCost != current.Cost)
                    continue; // stale heap row superseded by a shorter deterministic route

                expanded++;
                if (IsGoal(current.Cell, to, goalRadius))
                    return FirstStep(from, current.Cell, parent);

                for (var i = 0; i < NeighbourDx.Length; i++)
                {
                    if (!TryTranslate(
                            current.Cell, NeighbourDx[i], NeighbourDy[i], out var next))
                        continue;
                    if (Distance(from, next) > radiusBudget)
                        continue;
                    if (!CanEnter(current.Cell, next, nav))
                        continue;

                    var nextCost = current.Cost + 1;
                    if (bestCost.TryGetValue(next, out var oldCost) && oldCost <= nextCost)
                        continue;
                    // Fail closed the instant an unseen node would exceed the budget.
                    // Discarding it and later returning a route from the retained subset
                    // could select a non-optimal first step.
                    if (!bestCost.ContainsKey(next) && bestCost.Count >= nodeBudget)
                        return new MovementStepResult(MovementStepOutcome.NoRoute, from);

                    bestCost[next] = nextCost;
                    parent[next] = current.Cell;
                    open.Push(new SearchNode(
                        next, nextCost, Heuristic(next, to, goalRadius),
                        ManhattanTieBreak(next, to), sequence++));
                }
            }

            return new MovementStepResult(MovementStepOutcome.NoRoute, from);
        }

        /// <summary>
        /// Compatibility projection for non-action callers. Action advancers consume
        /// <see cref="RouteToward"/> directly so NoRoute cannot masquerade as no movement.
        /// </summary>
        public static GridPosition StepToward(
            GridPosition from,
            GridPosition to,
            IWorldNavigability nav = null)
        {
            // Compatibility callers (schedule and ambient critters) can be arbitrarily far
            // from their target. Keep their original allocation-free one-step primitive;
            // action advancers use RouteToward directly and therefore retain bounded,
            // observable NoRoute semantics.
            var dx = Math.Sign((long)to.X - from.X);
            var dy = Math.Sign((long)to.Y - from.Y);
            var candidate = new GridPosition(from.X + dx, from.Y + dy);
            if (nav == null)
                return candidate;

            var diagonal = dx != 0 && dy != 0;
            if (diagonal && nav.BlocksDiagonal(from, candidate))
                return AxialFallback(from, dx, dy, nav);
            if (nav.IsWalkable(candidate))
                return candidate;
            return AxialFallback(from, dx, dy, nav);
        }

        private static GridPosition AxialFallback(
            GridPosition from,
            int dx,
            int dy,
            IWorldNavigability nav)
        {
            if (dx != 0)
            {
                var xAxial = new GridPosition(from.X + dx, from.Y);
                if (nav.IsWalkable(xAxial))
                    return xAxial;
            }
            if (dy != 0)
            {
                var yAxial = new GridPosition(from.X, from.Y + dy);
                if (nav.IsWalkable(yAxial))
                    return yAxial;
            }
            return from;
        }

        private static bool CanEnter(
            GridPosition from,
            GridPosition to,
            IWorldNavigability nav)
        {
            if (!nav.IsWalkable(to))
                return false;
            var diagonal = from.X != to.X && from.Y != to.Y;
            return !diagonal || !nav.BlocksDiagonal(from, to);
        }

        private static MovementStepResult OpenGraphStep(GridPosition from, GridPosition to)
        {
            var dx = Math.Sign((long)to.X - from.X);
            var dy = Math.Sign((long)to.Y - from.Y);
            return new MovementStepResult(
                MovementStepOutcome.Moved,
                new GridPosition(from.X + dx, from.Y + dy));
        }

        private static bool IsGoal(GridPosition cell, GridPosition target, int goalRadius)
        {
            return Distance(cell, target) <= goalRadius;
        }

        private static long Heuristic(GridPosition cell, GridPosition target, int goalRadius)
        {
            return Math.Max(0L, Distance(cell, target) - goalRadius);
        }

        private static long Distance(GridPosition left, GridPosition right)
        {
            var dx = Math.Abs((long)left.X - right.X);
            var dy = Math.Abs((long)left.Y - right.Y);
            return Math.Max(dx, dy);
        }

        private static long ManhattanTieBreak(GridPosition left, GridPosition right)
        {
            return Math.Abs((long)left.X - right.X)
                + Math.Abs((long)left.Y - right.Y);
        }

        private static bool TryTranslate(
            GridPosition from,
            int dx,
            int dy,
            out GridPosition next)
        {
            var x = (long)from.X + dx;
            var y = (long)from.Y + dy;
            if (x < int.MinValue || x > int.MaxValue
                || y < int.MinValue || y > int.MaxValue)
            {
                next = from;
                return false;
            }
            next = new GridPosition((int)x, (int)y);
            return true;
        }

        private static MovementStepResult FirstStep(
            GridPosition from,
            GridPosition goal,
            Dictionary<GridPosition, GridPosition> parent)
        {
            var step = goal;
            while (parent.TryGetValue(step, out var previous) && !previous.Equals(from))
                step = previous;
            return step.Equals(from)
                ? new MovementStepResult(MovementStepOutcome.Arrived, from)
                : new MovementStepResult(MovementStepOutcome.Moved, step);
        }

        private readonly struct SearchNode
        {
            public SearchNode(
                GridPosition cell,
                int cost,
                long heuristic,
                long tieBreakDistance,
                int sequence)
            {
                Cell = cell;
                Cost = cost;
                Heuristic = heuristic;
                TieBreakDistance = tieBreakDistance;
                Sequence = sequence;
            }

            public GridPosition Cell { get; }
            public int Cost { get; }
            public long Heuristic { get; }
            public long TieBreakDistance { get; }
            public int Sequence { get; }
            public long Score => Cost + Heuristic;
        }

        private sealed class OpenNavigability : IWorldNavigability
        {
            public static readonly OpenNavigability Instance = new OpenNavigability();

            private OpenNavigability() { }

            public bool IsWalkable(GridPosition cell) => true;

            public bool BlocksDiagonal(GridPosition from, GridPosition to) => false;
        }

        /// <summary>Small allocation-local min-heap; ordering is fully explicit.</summary>
        private sealed class SearchHeap
        {
            private readonly List<SearchNode> _rows = new List<SearchNode>();

            public int Count => _rows.Count;

            public void Push(SearchNode node)
            {
                _rows.Add(node);
                var index = _rows.Count - 1;
                while (index > 0)
                {
                    var parent = (index - 1) / 2;
                    if (!ComesBefore(_rows[index], _rows[parent]))
                        break;
                    (_rows[index], _rows[parent]) = (_rows[parent], _rows[index]);
                    index = parent;
                }
            }

            public SearchNode Pop()
            {
                var result = _rows[0];
                var last = _rows[_rows.Count - 1];
                _rows.RemoveAt(_rows.Count - 1);
                if (_rows.Count == 0)
                    return result;

                _rows[0] = last;
                var index = 0;
                while (true)
                {
                    var left = (index * 2) + 1;
                    if (left >= _rows.Count)
                        break;
                    var right = left + 1;
                    var next = right < _rows.Count && ComesBefore(_rows[right], _rows[left])
                        ? right
                        : left;
                    if (!ComesBefore(_rows[next], _rows[index]))
                        break;
                    (_rows[index], _rows[next]) = (_rows[next], _rows[index]);
                    index = next;
                }
                return result;
            }

            private static bool ComesBefore(SearchNode left, SearchNode right)
            {
                if (left.Score != right.Score)
                    return left.Score < right.Score;
                if (left.Heuristic != right.Heuristic)
                    return left.Heuristic < right.Heuristic;
                if (left.TieBreakDistance != right.TieBreakDistance)
                    return left.TieBreakDistance < right.TieBreakDistance;
                if (left.Sequence != right.Sequence)
                    return left.Sequence < right.Sequence;
                if (left.Cell.X != right.Cell.X)
                    return left.Cell.X < right.Cell.X;
                return left.Cell.Y < right.Cell.Y;
            }
        }
    }
}
