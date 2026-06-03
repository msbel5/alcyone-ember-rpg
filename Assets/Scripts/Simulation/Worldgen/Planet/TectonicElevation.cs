using System;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Gainey-style boundary-distance elevation field for the spherical substrate.</summary>
    public sealed class TectonicElevation
    {
        private const int PropagationRadius = 5;
        private const double PropagationDecay = 0.58d;
        private const double OceanicBase = -0.34d;
        private const double ContinentalBase = 0.20d;

        public PlanetField Build(
            uint seed,
            PlanetParameters parameters,
            IcosphereGrid grid,
            PlatePartitionResult partition,
            PlateBoundarySet boundaries)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));
            if (partition == null)
                throw new ArgumentNullException(nameof(partition));
            if (boundaries == null)
                throw new ArgumentNullException(nameof(boundaries));

            var sourceEffects = new double[grid.Count];
            for (int i = 0; i < boundaries.Edges.Count; i++)
            {
                PlateBoundaryEdge edge = boundaries.Edges[i];
                PlateKind kindA = partition.Plates[edge.PlateA].Kind;
                PlateKind kindB = partition.Plates[edge.PlateB].Kind;
                sourceEffects[edge.TileA] += EndpointEffect(edge, kindA, kindB, parameters);
                sourceEffects[edge.TileB] += EndpointEffect(edge, kindB, kindA, parameters);
            }

            var tectonic = PropagateBoundaryEffects(grid, sourceEffects);
            var tiles = new PlanetTileField[grid.Count];
            for (int tileId = 0; tileId < grid.Count; tileId++)
            {
                int plateId = partition.PlateIdForTile(tileId);
                PlateKind plateKind = partition.Plates[plateId].Kind;
                double elevation = BaseElevation(plateKind) + tectonic[tileId];
                tiles[tileId] = new PlanetTileField(tileId, plateId, elevation, elevation >= parameters.SeaLevelThreshold);
            }

            return new PlanetField(seed, parameters, grid, partition, boundaries, tiles);
        }

        private static double BaseElevation(PlateKind plateKind)
        {
            return plateKind == PlateKind.Oceanic ? OceanicBase : ContinentalBase;
        }

        private static double EndpointEffect(PlateBoundaryEdge edge, PlateKind ownKind, PlateKind otherKind, PlanetParameters parameters)
        {
            double activity = 0.75d + (Math.Min(1d, edge.Magnitude / parameters.DriftScale) * 0.25d);
            switch (edge.Kind)
            {
                case PlateBoundaryKind.Convergent:
                    if (ownKind == PlateKind.Continental && otherKind == PlateKind.Continental)
                        return 0.80d * activity;
                    if (ownKind == PlateKind.Continental || otherKind == PlateKind.Continental)
                        return (ownKind == PlateKind.Continental ? 0.55d : 0.30d) * activity;
                    return 0.24d * activity;
                case PlateBoundaryKind.Divergent:
                    return (ownKind == PlateKind.Oceanic ? 0.18d : -0.16d) * activity;
                default:
                    return (ownKind == PlateKind.Continental ? 0.05d : 0.02d) * activity;
            }
        }

        private static double[] PropagateBoundaryEffects(IcosphereGrid grid, double[] sourceEffects)
        {
            var effects = new double[grid.Count];
            var distance = new int[grid.Count];
            for (int i = 0; i < distance.Length; i++)
                distance[i] = int.MaxValue;

            var queue = new DeterministicPriorityQueue();
            for (int tileId = 0; tileId < sourceEffects.Length; tileId++)
            {
                if (Math.Abs(sourceEffects[tileId]) <= 0.000000001d)
                    continue;

                distance[tileId] = 0;
                effects[tileId] = Clamp(sourceEffects[tileId], -0.40d, 1.10d);
                queue.Push(new QueueNode(tileId, 0));
            }

            while (queue.Count > 0)
            {
                QueueNode node = queue.Pop();
                if (node.Distance != distance[node.TileId] || node.Distance >= PropagationRadius)
                    continue;

                var neighbors = grid.TileAt(node.TileId).Neighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int neighbor = neighbors[i];
                    int nextDistance = node.Distance + 1;
                    if (nextDistance >= distance[neighbor])
                        continue;

                    distance[neighbor] = nextDistance;
                    effects[neighbor] = effects[node.TileId] * PropagationDecay;
                    queue.Push(new QueueNode(neighbor, nextDistance));
                }
            }

            return effects;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
                return minimum;
            if (value > maximum)
                return maximum;
            return value;
        }

        private struct QueueNode
        {
            public QueueNode(int tileId, int distance)
            {
                TileId = tileId;
                Distance = distance;
            }

            public int TileId { get; }
            public int Distance { get; }
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
                int distance = left.Distance.CompareTo(right.Distance);
                return distance != 0 ? distance : left.TileId.CompareTo(right.TileId);
            }
        }
    }
}
