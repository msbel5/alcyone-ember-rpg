using System;
using System.Collections.Generic;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Classifies inter-plate adjacency by relative Euler velocity.</summary>
    public sealed class PlateBoundaries
    {
        private const double TransformNormalRatio = 0.35d;
        private const double MinimumMagnitude = 0.000000001d;

        public PlateBoundarySet Build(IcosphereGrid grid, PlatePartitionResult partition)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));
            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            var edges = new List<PlateBoundaryEdge>();
            for (int tileId = 0; tileId < grid.Count; tileId++)
            {
                IcosphereTile tile = grid.TileAt(tileId);
                int plateId = partition.PlateIdForTile(tileId);
                for (int neighborIndex = 0; neighborIndex < tile.Neighbors.Count; neighborIndex++)
                {
                    int neighborId = tile.Neighbors[neighborIndex];
                    if (neighborId <= tileId)
                        continue;

                    int neighborPlateId = partition.PlateIdForTile(neighborId);
                    if (neighborPlateId == plateId)
                        continue;

                    edges.Add(ClassifyEdge(grid, partition, tileId, neighborId, plateId, neighborPlateId));
                }
            }

            return new PlateBoundarySet(edges.ToArray());
        }

        private static PlateBoundaryEdge ClassifyEdge(
            IcosphereGrid grid,
            PlatePartitionResult partition,
            int tileA,
            int tileB,
            int plateA,
            int plateB)
        {
            PlanetVector positionA = grid.TileAt(tileA).Position;
            PlanetVector positionB = grid.TileAt(tileB).Position;
            PlanetVector midpoint = PlanetVector.UnitMidpoint(positionA, positionB);
            PlanetVector edgeDirection = positionB.Subtract(positionA).Normalize();

            PlateMotion motionA = partition.Plates[plateA];
            PlateMotion motionB = partition.Plates[plateB];
            PlanetVector relativeVelocity = motionB.VelocityAt(midpoint).Subtract(motionA.VelocityAt(midpoint));
            double magnitude = relativeVelocity.Length;
            double normal = PlanetVector.Dot(relativeVelocity, edgeDirection);
            PlateBoundaryKind kind = Classify(magnitude, normal);
            return new PlateBoundaryEdge(tileA, tileB, plateA, plateB, kind, magnitude);
        }

        private static PlateBoundaryKind Classify(double magnitude, double normal)
        {
            if (magnitude <= MinimumMagnitude)
                return PlateBoundaryKind.Transform;

            if (Math.Abs(normal) < magnitude * TransformNormalRatio)
                return PlateBoundaryKind.Transform;

            return normal < 0d ? PlateBoundaryKind.Convergent : PlateBoundaryKind.Divergent;
        }
    }
}
