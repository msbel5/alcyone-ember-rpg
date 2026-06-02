using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Overland
{
    /// <summary>Director for DF-style continent fields: elevation, temperature, moisture, then biome classification.</summary>
    public sealed class WorldGenerationManager
    {
        private readonly IFieldStrategy _elevation = new PlateElevationFieldStrategy();
        private readonly IFieldStrategy _temperature = new TemperatureFieldStrategy();
        private readonly IFieldStrategy _moisture = new MoistureFieldStrategy();
        private readonly BiomeClassifier _classifier = new BiomeClassifier();

        public OverlandWorldFields Generate(uint seed, int width, int height)
        {
            var context = new WorldFieldContext(seed, width, height);
            var elevation = _elevation.Build(context, null);
            var temperature = _temperature.Build(context, elevation);
            var moisture = _moisture.Build(context, elevation);
            return _classifier.Classify(context, elevation, temperature, moisture);
        }

        private interface IFieldStrategy
        {
            double[] Build(WorldFieldContext context, double[] elevation);
        }

        private sealed class PlateElevationFieldStrategy : IFieldStrategy
        {
            private const int MinPlateCount = 12;
            private const int MaxPlateCount = 28;
            private const int LloydRelaxationPasses = 1;
            private const int DefaultDriftSteps = 2;
            private const double ConvergentThreshold = 0.14d;
            private const double TransformThreshold = 0.62d;

            private readonly int _driftSteps;

            public PlateElevationFieldStrategy()
                : this(DefaultDriftSteps)
            {
            }

            private PlateElevationFieldStrategy(int driftSteps)
            {
                if (driftSteps < 0)
                    throw new ArgumentOutOfRangeException(nameof(driftSteps), driftSteps, "Drift steps cannot be negative.");
                _driftSteps = driftSteps;
            }

            public double[] Build(WorldFieldContext context, double[] elevation)
            {
                var rng = new XorShiftRng(context.ElevationSeed ^ 0x5F3759DFu);
                int plateCount = ChoosePlateCount(context, rng);
                var plates = BuildPlates(context, rng, plateCount);
                RelaxPlateCenters(context, plates);

                var finalAssignment = AssignPlates(context, plates, _driftSteps);
                AssignContinentalCrust(context, rng, plates, finalAssignment);
                var tectonic = new double[context.TileCount];
                var ridges = new double[context.TileCount];

                double stepWeight = 1d / (_driftSteps + 1d);
                for (int step = 0; step <= _driftSteps; step++)
                {
                    var assignment = step == _driftSteps ? finalAssignment : AssignPlates(context, plates, step);
                    AccumulateBoundaryEffects(context, plates, assignment, step, stepWeight, tectonic, ridges);
                }

                var raw = ComposeRawElevation(context, plates, finalAssignment, tectonic, ridges);
                return CalibrateSeaLevel(raw, context);
            }

            private static int ChoosePlateCount(WorldFieldContext context, XorShiftRng rng)
            {
                int maximum = MaxPlateCount < context.TileCount ? MaxPlateCount : context.TileCount;
                int scaledMaximum = MinPlateCount + ((context.Width + context.Height) / 8);
                if (scaledMaximum < maximum)
                    maximum = scaledMaximum;
                int minimum = MinPlateCount < maximum ? MinPlateCount : maximum;
                if (maximum <= minimum)
                    return minimum;
                return minimum + rng.NextInt((maximum - minimum) + 1);
            }

            private static Plate[] BuildPlates(WorldFieldContext context, XorShiftRng rng, int plateCount)
            {
                var plates = new Plate[plateCount];
                double maxX = context.Width - 1d;
                double maxY = context.Height - 1d;

                for (int i = 0; i < plateCount; i++)
                {
                    double centerX = maxX <= 0d ? 0d : NextUnit(rng) * maxX;
                    double centerY = maxY <= 0d ? 0d : NextUnit(rng) * maxY;
                    double driftX = (rng.NextInt(2001) - 1000) / 1000d;
                    double driftY = (rng.NextInt(2001) - 1000) / 1000d;
                    double length = Math.Sqrt((driftX * driftX) + (driftY * driftY));
                    if (length < 0.001d)
                    {
                        driftX = 1d;
                        driftY = 0d;
                        length = 1d;
                    }

                    plates[i] = new Plate(
                        i,
                        centerX,
                        centerY,
                        driftX / length,
                        driftY / length,
                        NextUnit(rng));
                }

                return plates;
            }

            private static void RelaxPlateCenters(WorldFieldContext context, Plate[] plates)
            {
                for (int pass = 0; pass < LloydRelaxationPasses; pass++)
                {
                    var assignment = AssignPlates(context, plates, 0);
                    var sumX = new double[plates.Length];
                    var sumY = new double[plates.Length];
                    var counts = new int[plates.Length];

                    for (int y = 0; y < context.Height; y++)
                    {
                        for (int x = 0; x < context.Width; x++)
                        {
                            int plateId = assignment[context.Index(x, y)];
                            sumX[plateId] += x;
                            sumY[plateId] += y;
                            counts[plateId]++;
                        }
                    }

                    for (int i = 0; i < plates.Length; i++)
                    {
                        if (counts[i] == 0)
                            continue;
                        plates[i].CenterX = (plates[i].CenterX + (sumX[i] / counts[i])) * 0.5d;
                        plates[i].CenterY = (plates[i].CenterY + (sumY[i] / counts[i])) * 0.5d;
                    }
                }
            }

            private static void AssignContinentalCrust(WorldFieldContext context, XorShiftRng rng, Plate[] plates, int[] finalAssignment)
            {
                int cratonCount = 2 + rng.NextInt(2);
                var cratons = new Craton[cratonCount];
                double maxX = context.Width - 1d;
                double maxY = context.Height - 1d;
                double radius = Math.Max(context.Width, context.Height) * 0.62d;
                var plateAreas = new int[plates.Length];

                for (int i = 0; i < cratons.Length; i++)
                {
                    cratons[i] = new Craton(
                        maxX <= 0d ? 0d : NextUnit(rng) * maxX,
                        maxY <= 0d ? 0d : NextUnit(rng) * maxY,
                        radius * (0.78d + (NextUnit(rng) * 0.34d)));
                }

                for (int i = 0; i < finalAssignment.Length; i++)
                    plateAreas[finalAssignment[i]]++;

                var scores = new PlateScore[plates.Length];
                for (int i = 0; i < plates.Length; i++)
                {
                    double bestCluster = 0d;
                    for (int c = 0; c < cratons.Length; c++)
                    {
                        double dx = plates[i].CenterX - cratons[c].X;
                        double dy = plates[i].CenterY - cratons[c].Y;
                        double distance = Math.Sqrt((dx * dx) + (dy * dy));
                        double cluster = Clamp01(1d - (distance / cratons[c].Radius));
                        if (cluster > bestCluster)
                            bestCluster = cluster;
                    }

                    scores[i] = new PlateScore(i, (bestCluster * 2.1d) + (plates[i].ContinentalScore * 0.45d));
                }

                Array.Sort(scores, ComparePlateScores);
                int targetContinentalCells = (int)Math.Round(context.TileCount * (0.52d + (NextUnit(rng) * 0.08d)), MidpointRounding.AwayFromZero);
                int continentalCells = 0;

                for (int rank = 0; rank < scores.Length; rank++)
                {
                    var plate = plates[scores[rank].PlateId];
                    plate.IsContinental = rank == 0 || continentalCells < targetContinentalCells;
                    if (plate.IsContinental)
                        continentalCells += plateAreas[plate.Id];

                    double jitter = plate.ContinentalScore - 0.5d;
                    plate.BaseElevation = plate.IsContinental
                        ? 0.58d + (jitter * 0.07d)
                        : 0.20d + (jitter * 0.06d);
                }
            }

            private static int ComparePlateScores(PlateScore left, PlateScore right)
            {
                int score = right.Score.CompareTo(left.Score);
                return score != 0 ? score : left.PlateId.CompareTo(right.PlateId);
            }

            private static int[] AssignPlates(WorldFieldContext context, Plate[] plates, int driftStep)
            {
                var result = new int[context.TileCount];
                double warpRange = Math.Max(0.35d, Math.Min(context.Width, context.Height) * 0.075d);

                for (int y = 0; y < context.Height; y++)
                {
                    for (int x = 0; x < context.Width; x++)
                    {
                        double wx = x + ((FractalNoise(context.RidgeSeed, context.X01(x) + 101d, context.Y01(y) + 109d, 2, 2.7d) - 0.5d) * warpRange);
                        double wy = y + ((FractalNoise(context.RidgeSeed, context.X01(x) + 149d, context.Y01(y) + 157d, 2, 2.9d) - 0.5d) * warpRange);
                        int bestPlate = 0;
                        double bestDistance = double.MaxValue;

                        for (int i = 0; i < plates.Length; i++)
                        {
                            double dx = wx - DriftedX(context, plates[i], driftStep);
                            double dy = wy - DriftedY(context, plates[i], driftStep);
                            double distance = (dx * dx) + (dy * dy);
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestPlate = i;
                            }
                        }

                        result[context.Index(x, y)] = bestPlate;
                    }
                }

                return result;
            }

            private static void AccumulateBoundaryEffects(
                WorldFieldContext context,
                Plate[] plates,
                int[] assignment,
                int driftStep,
                double stepWeight,
                double[] tectonic,
                double[] ridges)
            {
                for (int y = 0; y < context.Height; y++)
                {
                    for (int x = 0; x < context.Width; x++)
                    {
                        int index = context.Index(x, y);
                        int plateId = assignment[index];

                        if (x + 1 < context.Width)
                            AccumulateBoundaryEdge(context, plates, assignment, x, y, plateId, x + 1, y, assignment[context.Index(x + 1, y)], driftStep, stepWeight, tectonic, ridges);
                        if (y + 1 < context.Height)
                            AccumulateBoundaryEdge(context, plates, assignment, x, y, plateId, x, y + 1, assignment[context.Index(x, y + 1)], driftStep, stepWeight, tectonic, ridges);
                    }
                }
            }

            private static void AccumulateBoundaryEdge(
                WorldFieldContext context,
                Plate[] plates,
                int[] assignment,
                int ax,
                int ay,
                int aId,
                int bx,
                int by,
                int bId,
                int driftStep,
                double stepWeight,
                double[] tectonic,
                double[] ridges)
            {
                if (aId == bId)
                    return;

                var a = plates[aId];
                var b = plates[bId];
                double normalX = DriftedX(context, b, driftStep) - DriftedX(context, a, driftStep);
                double normalY = DriftedY(context, b, driftStep) - DriftedY(context, a, driftStep);
                double normalLength = Math.Sqrt((normalX * normalX) + (normalY * normalY));
                if (normalLength < 0.001d)
                {
                    normalX = bx - ax;
                    normalY = by - ay;
                    normalLength = Math.Sqrt((normalX * normalX) + (normalY * normalY));
                }

                normalX /= normalLength;
                normalY /= normalLength;

                double relativeX = a.DriftX - b.DriftX;
                double relativeY = a.DriftY - b.DriftY;
                double pressure = (relativeX * normalX) + (relativeY * normalY);
                double tangential = Math.Abs((relativeX * -normalY) + (relativeY * normalX));

                if (pressure > ConvergentThreshold)
                {
                    if (a.IsContinental && b.IsContinental)
                    {
                        double uplift = (0.22d + (pressure * 0.14d)) * stepWeight;
                        double ridge = (0.32d + (pressure * 0.16d)) * stepWeight;
                        AddPlateInfluence(context, assignment, ax, ay, aId, uplift, ridge, tectonic, ridges);
                        AddPlateInfluence(context, assignment, bx, by, bId, uplift, ridge, tectonic, ridges);
                    }
                    else if (a.IsContinental || b.IsContinental)
                    {
                        double uplift = (0.16d + (pressure * 0.12d)) * stepWeight;
                        double ridge = (0.24d + (pressure * 0.14d)) * stepWeight;
                        double trench = (-0.11d - (pressure * 0.04d)) * stepWeight;
                        if (a.IsContinental)
                        {
                            AddPlateInfluence(context, assignment, ax, ay, aId, uplift, ridge, tectonic, ridges);
                            AddPlateInfluence(context, assignment, bx, by, bId, trench, 0.08d * stepWeight, tectonic, ridges);
                        }
                        else
                        {
                            AddPlateInfluence(context, assignment, bx, by, bId, uplift, ridge, tectonic, ridges);
                            AddPlateInfluence(context, assignment, ax, ay, aId, trench, 0.08d * stepWeight, tectonic, ridges);
                        }
                    }
                    else
                    {
                        double islandArc = (0.10d + (pressure * 0.08d)) * stepWeight;
                        double ridge = (0.18d + (pressure * 0.10d)) * stepWeight;
                        AddPlateInfluence(context, assignment, ax, ay, aId, islandArc, ridge, tectonic, ridges);
                        AddPlateInfluence(context, assignment, bx, by, bId, islandArc, ridge, tectonic, ridges);
                    }
                }
                else if (pressure < -ConvergentThreshold)
                {
                    double rift = (-0.15d + (pressure * 0.04d)) * stepWeight;
                    double ridge = 0.06d * stepWeight;
                    AddPlateInfluence(context, assignment, ax, ay, aId, rift, ridge, tectonic, ridges);
                    AddPlateInfluence(context, assignment, bx, by, bId, rift, ridge, tectonic, ridges);
                }
                else if (tangential > TransformThreshold)
                {
                    double faulting = (0.045d + ((tangential - TransformThreshold) * 0.035d)) * stepWeight;
                    AddPlateInfluence(context, assignment, ax, ay, aId, faulting, 0.08d * stepWeight, tectonic, ridges);
                    AddPlateInfluence(context, assignment, bx, by, bId, faulting, 0.08d * stepWeight, tectonic, ridges);
                }
            }

            private static void AddPlateInfluence(
                WorldFieldContext context,
                int[] assignment,
                int sourceX,
                int sourceY,
                int plateId,
                double uplift,
                double ridge,
                double[] tectonic,
                double[] ridges)
            {
                int radius = InfluenceRadius(context);
                for (int y = sourceY - radius; y <= sourceY + radius; y++)
                {
                    if (y < 0 || y >= context.Height)
                        continue;

                    for (int x = sourceX - radius; x <= sourceX + radius; x++)
                    {
                        if (x < 0 || x >= context.Width)
                            continue;

                        int index = context.Index(x, y);
                        if (assignment[index] != plateId)
                            continue;

                        double dx = x - sourceX;
                        double dy = y - sourceY;
                        double distance = Math.Sqrt((dx * dx) + (dy * dy));
                        if (distance > radius)
                            continue;

                        double falloff = SmoothStep(1d - (distance / (radius + 0.001d)));
                        tectonic[index] += uplift * falloff;
                        ridges[index] += ridge * falloff;
                    }
                }
            }

            private static double[] ComposeRawElevation(
                WorldFieldContext context,
                Plate[] plates,
                int[] assignment,
                double[] tectonic,
                double[] ridges)
            {
                var result = new double[context.TileCount];
                for (int y = 0; y < context.Height; y++)
                {
                    for (int x = 0; x < context.Width; x++)
                    {
                        int index = context.Index(x, y);
                        var plate = plates[assignment[index]];
                        double detail = FractalNoise(context.ElevationSeed, context.X01(x) + 11d, context.Y01(y) + 17d, 4, 2.15d) - 0.5d;
                        double ridgeNoise = Math.Abs((FractalNoise(context.RidgeSeed, context.X01(x) + 31d, context.Y01(y) + 43d, 3, 3.6d) * 2d) - 1d);
                        double ridgeIntensity = Clamp01(ridges[index]);
                        double crustDetail = detail * (plate.IsContinental ? 0.14d : 0.08d);
                        double ridgeLift = ridgeIntensity * (0.06d + (ridgeNoise * 0.18d));
                        result[index] = plate.BaseElevation + tectonic[index] + crustDetail + ridgeLift;
                    }
                }

                return result;
            }

            private static double[] CalibrateSeaLevel(double[] raw, WorldFieldContext context)
            {
                var sorted = (double[])raw.Clone();
                Array.Sort(sorted);

                int desiredLand = (int)Math.Round(context.TileCount * 0.50d, MidpointRounding.AwayFromZero);
                if (desiredLand < 1)
                    desiredLand = 1;
                if (desiredLand > context.TileCount - 1 && context.TileCount > 1)
                    desiredLand = context.TileCount - 1;

                int waterCutoff = context.TileCount - desiredLand;
                if (waterCutoff < 0)
                    waterCutoff = 0;
                if (waterCutoff >= sorted.Length)
                    waterCutoff = sorted.Length - 1;

                double threshold = sorted[waterCutoff];
                int highIndex = waterCutoff + ((desiredLand * 9) / 10);
                if (highIndex >= sorted.Length)
                    highIndex = sorted.Length - 1;
                int lowIndex = waterCutoff / 5;
                if (lowIndex < 0)
                    lowIndex = 0;

                double landSpan = sorted[highIndex] - threshold;
                if (landSpan < 0.000001d)
                    landSpan = 1d;
                double waterSpan = threshold - sorted[lowIndex];
                if (waterSpan < 0.000001d)
                    waterSpan = 1d;

                var result = new double[raw.Length];
                for (int i = 0; i < raw.Length; i++)
                {
                    if (raw[i] >= threshold)
                    {
                        double normalized = (raw[i] - threshold) / landSpan;
                        double lowland = Clamp01(normalized);
                        double aboveHigh = normalized > 1d ? (normalized - 1d) * 0.10d : 0d;
                        result[i] = Clamp01(WorldFieldContext.SeaLevel + (lowland * lowland * 0.42d) + aboveHigh);
                    }
                    else
                    {
                        double normalized = (threshold - raw[i]) / waterSpan;
                        double basin = SmoothStep(Clamp01(normalized));
                        double deepBasin = normalized > 1d ? (normalized - 1d) * 0.05d : 0d;
                        result[i] = Clamp01(WorldFieldContext.SeaLevel - (basin * 0.34d) - deepBasin);
                    }
                }

                return result;
            }

            private static double DriftedX(WorldFieldContext context, Plate plate, int driftStep)
            {
                return Clamp(plate.CenterX + (plate.DriftX * driftStep * DriftDistance(context)), 0d, context.Width - 1d);
            }

            private static double DriftedY(WorldFieldContext context, Plate plate, int driftStep)
            {
                return Clamp(plate.CenterY + (plate.DriftY * driftStep * DriftDistance(context)), 0d, context.Height - 1d);
            }

            private static double DriftDistance(WorldFieldContext context)
            {
                return Math.Max(context.Width, context.Height) * 0.075d;
            }

            private static int InfluenceRadius(WorldFieldContext context)
            {
                int radius = Math.Min(context.Width, context.Height) / 6;
                if (radius < 2)
                    return 2;
                return radius > 4 ? 4 : radius;
            }

            private static double NextUnit(XorShiftRng rng)
            {
                return rng.NextInt(1_000_000) / 999_999d;
            }

            private static double Clamp(double value, double min, double max)
            {
                if (value < min) return min;
                return value > max ? max : value;
            }

            private sealed class Plate
            {
                public Plate(int id, double centerX, double centerY, double driftX, double driftY, double continentalScore)
                {
                    Id = id;
                    CenterX = centerX;
                    CenterY = centerY;
                    DriftX = driftX;
                    DriftY = driftY;
                    ContinentalScore = continentalScore;
                }

                public int Id { get; }
                public double CenterX { get; set; }
                public double CenterY { get; set; }
                public double DriftX { get; }
                public double DriftY { get; }
                public double ContinentalScore { get; }
                public bool IsContinental { get; set; }
                public double BaseElevation { get; set; }
            }

            private readonly struct Craton
            {
                public Craton(double x, double y, double radius)
                {
                    X = x;
                    Y = y;
                    Radius = radius;
                }

                public double X { get; }
                public double Y { get; }
                public double Radius { get; }
            }

            private readonly struct PlateScore
            {
                public PlateScore(int plateId, double score)
                {
                    PlateId = plateId;
                    Score = score;
                }

                public int PlateId { get; }
                public double Score { get; }
            }
        }

        private sealed class TemperatureFieldStrategy : IFieldStrategy
        {
            public double[] Build(WorldFieldContext context, double[] elevation)
            {
                var result = new double[context.TileCount];
                for (int y = 0; y < context.Height; y++)
                {
                    double latitude = Math.Abs((context.Y01(y) * 2d) - 1d);
                    for (int x = 0; x < context.Width; x++)
                    {
                        int index = context.Index(x, y);
                        double weather = (FractalNoise(context.TemperatureSeed, context.X01(x) + 71d, context.Y01(y) + 29d, 2, 2.4d) - 0.5d) * 0.12d;
                        double lapse = Math.Max(0d, elevation[index] - WorldFieldContext.SeaLevel) * 0.45d;
                        // Steeper latitude band (warm equator -> frozen poles) so the now-rounder continent's
                        // higher-latitude land actually gets cold enough to read as tundra.
                        result[index] = Clamp01(1.02d - (latitude * 1.06d) + weather - lapse);
                    }
                }

                return result;
            }
        }

        private sealed class MoistureFieldStrategy : IFieldStrategy
        {
            private const int WaterRange = 3;

            public double[] Build(WorldFieldContext context, double[] elevation)
            {
                var result = new double[context.TileCount];
                for (int y = 0; y < context.Height; y++)
                {
                    for (int x = 0; x < context.Width; x++)
                    {
                        int index = context.Index(x, y);
                        if (elevation[index] < WorldFieldContext.SeaLevel)
                        {
                            result[index] = 1d;
                            continue;
                        }

                        double rainfall = FractalNoise(context.MoistureSeed, context.X01(x) + 47d, context.Y01(y) + 83d, 3, 2.9d);
                        result[index] = Clamp01((rainfall * 0.82d) + WaterBoost(context, elevation, x, y));
                    }
                }

                return result;
            }

            private static double WaterBoost(WorldFieldContext context, double[] elevation, int x, int y)
            {
                int best = WaterRange + 1;
                for (int ny = y - WaterRange; ny <= y + WaterRange; ny++)
                {
                    for (int nx = x - WaterRange; nx <= x + WaterRange; nx++)
                    {
                        if (nx < 0 || ny < 0 || nx >= context.Width || ny >= context.Height)
                            continue;
                        if (elevation[context.Index(nx, ny)] >= WorldFieldContext.SeaLevel)
                            continue;
                        int distance = Math.Abs(nx - x) + Math.Abs(ny - y);
                        if (distance < best)
                            best = distance;
                    }
                }

                return best > WaterRange ? 0d : ((WaterRange + 1 - best) / (double)(WaterRange + 1)) * 0.42d;
            }
        }

        private sealed class BiomeClassifier
        {
            public OverlandWorldFields Classify(WorldFieldContext context, double[] elevation, double[] temperature, double[] moisture)
            {
                var land = new bool[context.TileCount];
                var biomes = new BiomeKind[context.TileCount];

                for (int i = 0; i < elevation.Length; i++)
                    land[i] = elevation[i] >= WorldFieldContext.SeaLevel;

                for (int y = 0; y < context.Height; y++)
                {
                    for (int x = 0; x < context.Width; x++)
                    {
                        int index = context.Index(x, y);
                        biomes[index] = ClassifyTile(context, elevation, temperature, moisture, land, x, y, index);
                    }
                }

                return new OverlandWorldFields(context.Width, context.Height, elevation, temperature, moisture, land, biomes);
            }

            private static BiomeKind ClassifyTile(
                WorldFieldContext context,
                double[] elevation,
                double[] temperature,
                double[] moisture,
                bool[] land,
                int x,
                int y,
                int index)
            {
                if (!land[index] || IsCoastline(context, land, x, y))
                    return BiomeKind.Coast;
                if (elevation[index] >= 0.82d && temperature[index] >= 0.58d)
                    return BiomeKind.Ash;
                if (elevation[index] >= 0.74d)
                    return BiomeKind.Mountain;
                if (temperature[index] <= 0.30d)
                    return BiomeKind.Tundra;
                if (temperature[index] >= 0.70d && moisture[index] <= 0.34d)
                    return BiomeKind.Desert;
                if (elevation[index] <= WorldFieldContext.SeaLevel + 0.10d && moisture[index] >= 0.64d)
                    return BiomeKind.Swamp;
                if (temperature[index] >= 0.30d && moisture[index] >= 0.54d)
                    return BiomeKind.Forest;
                return BiomeKind.Plains;
            }
        }

        private static bool IsCoastline(WorldFieldContext context, bool[] land, int x, int y)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= context.Width || ny >= context.Height)
                        continue;
                    if (!land[context.Index(nx, ny)])
                        return true;
                }
            }

            return false;
        }

        private static double FractalNoise(uint seed, double x, double y, int octaves, double scale)
        {
            double value = 0d;
            double amplitude = 1d;
            double total = 0d;
            double frequency = scale;

            for (int octave = 0; octave < octaves; octave++)
            {
                value += ValueNoise(seed + (uint)(octave * 0x9E37), x * frequency, y * frequency) * amplitude;
                total += amplitude;
                amplitude *= 0.5d;
                frequency *= 2d;
            }

            return value / total;
        }

        private static double ValueNoise(uint seed, double x, double y)
        {
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            double tx = SmoothStep(x - x0);
            double ty = SmoothStep(y - y0);

            double a = Noise01(seed, x0, y0);
            double b = Noise01(seed, x0 + 1, y0);
            double c = Noise01(seed, x0, y0 + 1);
            double d = Noise01(seed, x0 + 1, y0 + 1);
            return Lerp(Lerp(a, b, tx), Lerp(c, d, tx), ty);
        }

        private static double Noise01(uint seed, int x, int y)
        {
            unchecked
            {
                uint mixed = seed ^ ((uint)x * 374761393u) ^ ((uint)y * 668265263u);
                mixed ^= mixed >> 13;
                mixed *= 1274126177u;
                var rng = new XorShiftRng(mixed);
                return rng.NextInt(1_000_000) / 999_999d;
            }
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + ((b - a) * t);
        }

        private static double SmoothStep(double value)
        {
            value = Clamp01(value);
            return value * value * (3d - (2d * value));
        }

        private static double Clamp01(double value)
        {
            if (value < 0d) return 0d;
            if (value > 1d) return 1d;
            return value;
        }

        private readonly struct WorldFieldContext
        {
            public const double SeaLevel = 0.43d;

            public WorldFieldContext(uint seed, int width, int height)
            {
                if (width <= 0)
                    throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
                if (height <= 0)
                    throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");

                Width = width;
                Height = height;
                TileCount = width * height;

                var rng = new XorShiftRng(seed ^ 0xC01171E7u);
                ElevationSeed = NextSeed(rng);
                RidgeSeed = NextSeed(rng);
                TemperatureSeed = NextSeed(rng);
                MoistureSeed = NextSeed(rng);
            }

            public int Width { get; }
            public int Height { get; }
            public int TileCount { get; }
            public uint ElevationSeed { get; }
            public uint RidgeSeed { get; }
            public uint TemperatureSeed { get; }
            public uint MoistureSeed { get; }

            public int Index(int x, int y)
            {
                return (y * Width) + x;
            }

            public double X01(int x)
            {
                return Width == 1 ? 0.5d : x / (double)(Width - 1);
            }

            public double Y01(int y)
            {
                return Height == 1 ? 0.5d : y / (double)(Height - 1);
            }

            private static uint NextSeed(XorShiftRng rng)
            {
                return (uint)rng.NextInt(int.MaxValue) + 1u;
            }
        }
    }

    public sealed class OverlandWorldFields
    {
        private readonly double[] _elevation;
        private readonly double[] _temperature;
        private readonly double[] _moisture;
        private readonly bool[] _land;
        private readonly BiomeKind[] _biomes;

        public OverlandWorldFields(
            int width,
            int height,
            double[] elevation,
            double[] temperature,
            double[] moisture,
            bool[] land,
            BiomeKind[] biomes)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            int expected = width * height;
            if (elevation == null || temperature == null || moisture == null || land == null || biomes == null)
                throw new ArgumentNullException(nameof(elevation), "World field arrays cannot be null.");
            if (elevation.Length != expected || temperature.Length != expected || moisture.Length != expected || land.Length != expected || biomes.Length != expected)
                throw new ArgumentException("World field arrays must match width * height.");

            Width = width;
            Height = height;
            _elevation = (double[])elevation.Clone();
            _temperature = (double[])temperature.Clone();
            _moisture = (double[])moisture.Clone();
            _land = (bool[])land.Clone();
            _biomes = (BiomeKind[])biomes.Clone();
            Elevation = new ReadOnlyCollection<double>(_elevation);
            Temperature = new ReadOnlyCollection<double>(_temperature);
            Moisture = new ReadOnlyCollection<double>(_moisture);
            LandMask = new ReadOnlyCollection<bool>(_land);
            Biomes = new ReadOnlyCollection<BiomeKind>(_biomes);
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<double> Elevation { get; }
        public IReadOnlyList<double> Temperature { get; }
        public IReadOnlyList<double> Moisture { get; }
        public IReadOnlyList<bool> LandMask { get; }
        public IReadOnlyList<BiomeKind> Biomes { get; }

        public bool IsLandAt(int x, int y)
        {
            return _land[Index(x, y)];
        }

        public BiomeKind BiomeAt(int x, int y)
        {
            return _biomes[Index(x, y)];
        }

        public BiomeKind[] CopyBiomes()
        {
            return (BiomeKind[])_biomes.Clone();
        }

        public bool[] CopyLandMask()
        {
            return (bool[])_land.Clone();
        }

        private int Index(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(x), $"Tile ({x},{y}) is outside the field bounds.");
            return (y * Width) + x;
        }
    }

    public static partial class OverlandWorldgen
    {
        private static void SmoothSingleTileIslands(int width, int height, BiomeKind[] biomes, bool[] land)
        {
            var scratch = new BiomeKind[biomes.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < biomes.Length; i++)
                    scratch[i] = biomes[i];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = ToIndex(x, y, width);
                        if (!land[index] || biomes[index] == BiomeKind.Coast || CountMatchingNeighbors(width, height, biomes, land, x, y, biomes[index]) > 0)
                            continue;

                        scratch[index] = DominantLandNeighbor(width, height, biomes, land, x, y, biomes[index]);
                    }
                }

                for (int i = 0; i < biomes.Length; i++)
                    biomes[i] = scratch[i];
            }
        }

        private static BiomeKind DominantLandNeighbor(int width, int height, BiomeKind[] biomes, bool[] land, int x, int y, BiomeKind fallback)
        {
            var dominant = fallback;
            int bestCount = -1;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    int index = ToIndex(nx, ny, width);
                    if (!land[index] || biomes[index] == BiomeKind.Coast)
                        continue;

                    int count = CountMatchingNeighbors(width, height, biomes, land, x, y, biomes[index]);
                    if (count > bestCount)
                    {
                        dominant = biomes[index];
                        bestCount = count;
                    }
                }
            }

            return bestCount < 0 ? BiomeKind.Coast : dominant;
        }

        private static int CountMatchingNeighbors(int width, int height, BiomeKind[] biomes, bool[] land, int x, int y, BiomeKind biome)
        {
            int count = 0;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    int index = ToIndex(nx, ny, width);
                    if (land[index] && biomes[index] == biome)
                        count++;
                }
            }

            return count;
        }
    }
}
