using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Routes precipitation over a priority-flooded terrain graph and marks rivers/lakes.</summary>
    public sealed class HydrologyStage
    {
        private const double LakeEpsilon = 0.018d;

        public PlanetField Apply(PlanetField field, XorShiftRng rng)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            Flood(field, out double[] filledElevation, out int[] target, out bool[] lake, out int[] visitOrder, out int visitCount);
            double[] flow = AccumulateFlow(field, target, lake, visitOrder, visitCount);
            double riverThreshold = RiverFlowThreshold(field.TileCount);

            var tiles = new PlanetTileField[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField source = field.TileAt(tileId);
                bool isLand = source.IsLand && filledElevation[tileId] >= field.Parameters.SeaLevelThreshold;
                bool isLake = isLand && lake[tileId];
                bool isRiver = isLand && !isLake && target[tileId] >= 0 && flow[tileId] >= riverThreshold;
                tiles[tileId] = source.CopyWith(
                    elevation: isLand ? filledElevation[tileId] : source.Elevation,
                    isLand: isLand,
                    flow: isLand ? flow[tileId] : 0d,
                    isRiver: isRiver,
                    isLake: isLake);
            }

            return new PlanetField(field.Seed, field.Parameters, field.Grid, field.Plates, field.Boundaries, tiles);
        }

        public static double RiverFlowThreshold(int tileCount)
        {
            return Math.Max(6d, Math.Sqrt(tileCount) * 0.42d);
        }

        private static void Flood(
            PlanetField field,
            out double[] filledElevation,
            out int[] target,
            out bool[] lake,
            out int[] visitOrder,
            out int visitCount)
        {
            int count = field.TileCount;
            double seaLevel = field.Parameters.SeaLevelThreshold;
            filledElevation = new double[count];
            target = new int[count];
            lake = new bool[count];
            visitOrder = new int[count];
            visitCount = 0;

            var visited = new bool[count];
            for (int tileId = 0; tileId < count; tileId++)
                target[tileId] = -1;

            var queue = new DeterministicPriorityQueue();
            for (int tileId = 0; tileId < count; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                filledElevation[tileId] = tile.Elevation;
                if (tile.IsLand)
                    continue;

                visited[tileId] = true;
                filledElevation[tileId] = Math.Min(tile.Elevation, seaLevel);
                queue.Push(new QueueNode(tileId, filledElevation[tileId]));
            }

            if (queue.Count == 0)
            {
                int lowest = LowestTile(field);
                visited[lowest] = true;
                lake[lowest] = true;
                queue.Push(new QueueNode(lowest, field.TileAt(lowest).Elevation));
                visitOrder[visitCount++] = lowest;
            }

            while (queue.Count > 0)
            {
                QueueNode node = queue.Pop();
                var neighbors = field.Grid.TileAt(node.TileId).Neighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int neighbor = neighbors[i];
                    if (visited[neighbor])
                        continue;

                    PlanetTileField neighborTile = field.TileAt(neighbor);
                    visited[neighbor] = true;
                    if (!neighborTile.IsLand)
                    {
                        filledElevation[neighbor] = Math.Min(neighborTile.Elevation, seaLevel);
                        queue.Push(new QueueNode(neighbor, filledElevation[neighbor]));
                        continue;
                    }

                    double filled = Math.Max(neighborTile.Elevation, node.Priority);
                    filledElevation[neighbor] = filled;
                    bool isLake = filled > neighborTile.Elevation + LakeEpsilon;
                    lake[neighbor] = isLake;
                    target[neighbor] = isLake ? -1 : node.TileId;
                    visitOrder[visitCount++] = neighbor;
                    queue.Push(new QueueNode(neighbor, filled));
                }
            }
        }

        private static double[] AccumulateFlow(PlanetField field, int[] target, bool[] lake, int[] visitOrder, int visitCount)
        {
            var flow = new double[field.TileCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                flow[tileId] = tile.IsLand ? 0.35d + (tile.Moisture * 1.85d) : 0d;
            }

            for (int index = visitCount - 1; index >= 0; index--)
            {
                int tileId = visitOrder[index];
                if (!field.TileAt(tileId).IsLand || lake[tileId])
                    continue;

                int downstream = target[tileId];
                if (downstream >= 0 && field.TileAt(downstream).IsLand)
                    flow[downstream] += flow[tileId];
            }

            return flow;
        }

        private static int LowestTile(PlanetField field)
        {
            int lowest = 0;
            double elevation = field.TileAt(0).Elevation;
            for (int tileId = 1; tileId < field.TileCount; tileId++)
            {
                double candidate = field.TileAt(tileId).Elevation;
                if (candidate < elevation)
                {
                    lowest = tileId;
                    elevation = candidate;
                }
            }

            return lowest;
        }

        private struct QueueNode
        {
            public QueueNode(int tileId, double priority)
            {
                TileId = tileId;
                Priority = priority;
            }

            public int TileId { get; }
            public double Priority { get; }
        }

        private sealed class DeterministicPriorityQueue
        {
            private readonly System.Collections.Generic.List<QueueNode> _items = new System.Collections.Generic.List<QueueNode>();

            public int Count => _items.Count;

            public void Push(QueueNode node)
            {
                _items.Add(node);
                SiftUp(_items.Count - 1);
            }

            public QueueNode Pop()
            {
                QueueNode root = _items[0];
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _items.RemoveAt(last);
                if (_items.Count > 0)
                    SiftDown(0);
                return root;
            }

            private void SiftUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (Compare(_items[parent], _items[index]) <= 0)
                        return;

                    Swap(parent, index);
                    index = parent;
                }
            }

            private void SiftDown(int index)
            {
                while (true)
                {
                    int left = (index * 2) + 1;
                    int right = left + 1;
                    int smallest = index;

                    if (left < _items.Count && Compare(_items[left], _items[smallest]) < 0)
                        smallest = left;
                    if (right < _items.Count && Compare(_items[right], _items[smallest]) < 0)
                        smallest = right;
                    if (smallest == index)
                        return;

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int left, int right)
            {
                QueueNode temp = _items[left];
                _items[left] = _items[right];
                _items[right] = temp;
            }

            private static int Compare(QueueNode left, QueueNode right)
            {
                int priority = left.Priority.CompareTo(right.Priority);
                return priority != 0 ? priority : left.TileId.CompareTo(right.TileId);
            }
        }
    }
}
