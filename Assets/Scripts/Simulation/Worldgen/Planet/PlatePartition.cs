using System;
using System.Collections.Generic;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Deterministic multi-source flood-fill plate assignment.</summary>
    public sealed class PlatePartition
    {
        public PlatePartitionResult Build(IcosphereGrid grid, int plateCount, double oceanicFraction, double driftScale, XorShiftRng rng)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            if (plateCount <= 0 || plateCount > grid.Count)
                throw new ArgumentOutOfRangeException(nameof(plateCount), plateCount, "Plate count must fit the grid.");
            if (oceanicFraction < 0d || oceanicFraction > 1d)
                throw new ArgumentOutOfRangeException(nameof(oceanicFraction), oceanicFraction, "Oceanic fraction must be between 0 and 1.");
            if (driftScale <= 0d)
                throw new ArgumentOutOfRangeException(nameof(driftScale), driftScale, "Drift scale must be positive.");

            int[] seedTiles = ChooseSeedTiles(grid.Count, plateCount, rng);
            var plates = BuildPlates(grid, seedTiles, oceanicFraction, driftScale, rng);
            int[] tilePlateIds = FloodFill(grid, seedTiles);
            return new PlatePartitionResult(tilePlateIds, plates);
        }

        private static int[] ChooseSeedTiles(int tileCount, int plateCount, XorShiftRng rng)
        {
            var used = new bool[tileCount];
            var seeds = new int[plateCount];

            for (int plateId = 0; plateId < plateCount; plateId++)
            {
                int nth = rng.NextInt(tileCount - plateId);
                int tileId = SelectNthUnused(used, nth);
                used[tileId] = true;
                seeds[plateId] = tileId;
            }

            return seeds;
        }

        private static int SelectNthUnused(bool[] used, int nth)
        {
            int seen = 0;
            for (int tileId = 0; tileId < used.Length; tileId++)
            {
                if (used[tileId])
                    continue;
                if (seen == nth)
                    return tileId;
                seen++;
            }

            throw new InvalidOperationException("Unable to select an unused plate seed tile.");
        }

        private static PlateMotion[] BuildPlates(IcosphereGrid grid, int[] seedTiles, double oceanicFraction, double driftScale, XorShiftRng rng)
        {
            bool[] oceanic = ChooseOceanicPlates(seedTiles.Length, oceanicFraction, rng);
            var plates = new PlateMotion[seedTiles.Length];
            for (int plateId = 0; plateId < seedTiles.Length; plateId++)
            {
                PlanetVector fallbackAxis = grid.TileAt(seedTiles[plateId]).Position;
                PlanetVector axis = RandomUnitVector(rng, fallbackAxis);
                double speed = driftScale * (0.35d + (PlanetRng.NextUnit(rng) * 0.65d));
                plates[plateId] = new PlateMotion(
                    plateId,
                    oceanic[plateId] ? PlateKind.Oceanic : PlateKind.Continental,
                    axis,
                    speed);
            }

            return plates;
        }

        private static bool[] ChooseOceanicPlates(int plateCount, double oceanicFraction, XorShiftRng rng)
        {
            int oceanicCount = (int)Math.Round(plateCount * oceanicFraction, MidpointRounding.AwayFromZero);
            if (plateCount > 1 && oceanicFraction > 0d && oceanicFraction < 1d)
            {
                if (oceanicCount < 1)
                    oceanicCount = 1;
                if (oceanicCount > plateCount - 1)
                    oceanicCount = plateCount - 1;
            }

            var scores = new PlateScore[plateCount];
            for (int plateId = 0; plateId < plateCount; plateId++)
                scores[plateId] = new PlateScore(plateId, rng.NextInt(int.MaxValue));

            Array.Sort(scores, ComparePlateScores);

            var oceanic = new bool[plateCount];
            for (int i = 0; i < oceanicCount; i++)
                oceanic[scores[i].PlateId] = true;
            return oceanic;
        }

        private static int ComparePlateScores(PlateScore left, PlateScore right)
        {
            int score = left.Score.CompareTo(right.Score);
            return score != 0 ? score : left.PlateId.CompareTo(right.PlateId);
        }

        private static PlanetVector RandomUnitVector(XorShiftRng rng, PlanetVector fallback)
        {
            var axis = new PlanetVector(
                PlanetRng.NextSignedUnit(rng),
                PlanetRng.NextSignedUnit(rng),
                PlanetRng.NextSignedUnit(rng));

            if (axis.Length < 0.000001d)
                return fallback.Normalize();
            return axis.Normalize();
        }

        private static int[] FloodFill(IcosphereGrid grid, int[] seedTiles)
        {
            var assignment = new int[grid.Count];
            for (int i = 0; i < assignment.Length; i++)
                assignment[i] = -1;

            var frontier = new List<int>(seedTiles.Length);
            for (int plateId = 0; plateId < seedTiles.Length; plateId++)
            {
                int tileId = seedTiles[plateId];
                assignment[tileId] = plateId;
                frontier.Add(tileId);
            }

            frontier.Sort();
            int assigned = frontier.Count;
            while (assigned < grid.Count)
            {
                var next = new List<int>();
                for (int frontierIndex = 0; frontierIndex < frontier.Count; frontierIndex++)
                {
                    int tileId = frontier[frontierIndex];
                    int plateId = assignment[tileId];
                    IReadOnlyList<int> neighbors = grid.TileAt(tileId).Neighbors;
                    for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                    {
                        int neighbor = neighbors[neighborIndex];
                        if (assignment[neighbor] >= 0)
                            continue;

                        assignment[neighbor] = plateId;
                        next.Add(neighbor);
                        assigned++;
                    }
                }

                if (next.Count == 0)
                    throw new InvalidOperationException("Icosphere grid is disconnected; plate flood-fill cannot complete.");

                next.Sort();
                frontier = next;
            }

            return assignment;
        }

        private struct PlateScore
        {
            public PlateScore(int plateId, int score)
            {
                PlateId = plateId;
                Score = score;
            }

            public int PlateId { get; }
            public int Score { get; }
        }
    }
}
