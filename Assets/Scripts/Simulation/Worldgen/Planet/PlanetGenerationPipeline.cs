using System;
using System.Collections.Generic;
using System.Globalization;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    public interface IPlanetStage
    {
        string Name { get; }
        void Run(PlanetGenerationContext context);
    }

    public interface IPlanetGenerationObserver
    {
        void OnStageCompleted(PlanetStageReport report);
    }

    public sealed class PlanetStageReport
    {
        public PlanetStageReport(int stageIndex, int stageCount, string stageName, string summary, PlanetField partial)
        {
            if (stageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(stageIndex), stageIndex, "Stage index must be non-negative.");
            if (stageCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(stageCount), stageCount, "Stage count must be positive.");
            if (stageIndex >= stageCount)
                throw new ArgumentOutOfRangeException(nameof(stageIndex), stageIndex, "Stage index must be less than stage count.");

            StageIndex = stageIndex;
            StageCount = stageCount;
            StageName = stageName ?? throw new ArgumentNullException(nameof(stageName));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            Partial = partial;
        }

        public int StageIndex { get; }
        public int StageCount { get; }
        public string StageName { get; }
        public string Summary { get; }
        public PlanetField Partial { get; }
    }

    public sealed class PlanetGenerationContext
    {
        public const uint PlateStageSeed = 0x70514E45u;
        public const uint ElevationNoiseStageSeed = 0x454E4F49u;
        public const uint ClimateStageSeed = 0x434C494Du;
        public const uint HydrologyStageSeed = 0x48594452u;
        public const uint ErosionStageSeed = 0x45524F53u;
        public const uint ResourceStageSeed = 0x5245534Fu;
        public const uint SettlementStageSeed = 0x5345544Cu;

        private readonly XorShiftRng _rootRng;

        public PlanetGenerationContext(uint seed, PlanetParameters parameters)
        {
            Seed = seed;
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            _rootRng = new XorShiftRng(seed);
        }

        public uint Seed { get; }
        public PlanetParameters Parameters { get; }
        public IcosphereGrid Grid { get; internal set; }
        public PlatePartitionResult Plates { get; internal set; }
        public PlateBoundarySet Boundaries { get; internal set; }
        public PlanetField Field { get; internal set; }
        public IReadOnlyList<PlanetSettlement> Settlements => Field == null ? Array.Empty<PlanetSettlement>() : Field.Settlements;
        public IReadOnlyList<PlanetImpactSite> ResourceImpacts => Field == null ? Array.Empty<PlanetImpactSite>() : Field.ResourceImpacts;

        public XorShiftRng Fork(uint stageConstant)
        {
            return PlanetRng.Fork(_rootRng, stageConstant);
        }

        internal IcosphereGrid RequireGrid()
        {
            if (Grid == null)
                throw new InvalidOperationException("Planet grid has not been generated yet.");
            return Grid;
        }

        internal PlatePartitionResult RequirePlates()
        {
            if (Plates == null)
                throw new InvalidOperationException("Planet plates have not been generated yet.");
            return Plates;
        }

        internal PlateBoundarySet RequireBoundaries()
        {
            if (Boundaries == null)
                throw new InvalidOperationException("Planet boundaries have not been generated yet.");
            return Boundaries;
        }

        internal PlanetField RequireField()
        {
            if (Field == null)
                throw new InvalidOperationException("Planet field has not been generated yet.");
            return Field;
        }
    }

    public static class PlanetStageFactory
    {
        public static IReadOnlyList<IPlanetStage> CreateStages(PlanetParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            return Array.AsReadOnly(new IPlanetStage[]
            {
                new IcosphereStage(),
                new PlatesStage(),
                new BoundariesStage(),
                new TectonicElevationStage(),
                new ElevationNoiseStage(),
                new ClimateStage(),
                new HydrologyStage(),
                new ErosionStage(),
                new ResourceStage(),
                new SettlementStage(),
            });
        }
    }

    public sealed class PlanetGenerationManager
    {
        private readonly IReadOnlyList<IPlanetStage> _stages;

        public PlanetGenerationManager(IReadOnlyList<IPlanetStage> stages)
        {
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            if (_stages.Count == 0)
                throw new ArgumentException("At least one planet generation stage is required.", nameof(stages));
        }

        public PlanetField Generate(PlanetGenerationContext context, IPlanetGenerationObserver observer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            for (int stageIndex = 0; stageIndex < _stages.Count; stageIndex++)
            {
                IPlanetStage stage = _stages[stageIndex];
                stage.Run(context);
                observer?.OnStageCompleted(new PlanetStageReport(
                    stageIndex,
                    _stages.Count,
                    stage.Name,
                    PlanetStageSummaries.Create(stage.Name, context),
                    context.Field));
            }

            return context.RequireField();
        }
    }

    internal sealed class IcosphereStage : IPlanetStage
    {
        public string Name => "Icosphere";

        public void Run(PlanetGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Grid = IcosphereGrid.Build(context.Parameters.SubdivisionLevel);
        }
    }

    internal sealed class PlatesStage : IPlanetStage
    {
        public string Name => "Plates";

        public void Run(PlanetGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Plates = new PlatePartition().Build(
                context.RequireGrid(),
                context.Parameters.PlateCount,
                context.Parameters.OceanicFraction,
                context.Parameters.DriftScale,
                context.Fork(PlanetGenerationContext.PlateStageSeed));
        }
    }

    internal sealed class BoundariesStage : IPlanetStage
    {
        public string Name => "Boundaries";

        public void Run(PlanetGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Boundaries = new PlateBoundaries().Build(context.RequireGrid(), context.RequirePlates());
        }
    }

    internal sealed class TectonicElevationStage : IPlanetStage
    {
        public string Name => "TectonicElevation";

        public void Run(PlanetGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Field = new TectonicElevation().Build(
                context.Seed,
                context.Parameters,
                context.RequireGrid(),
                context.RequirePlates(),
                context.RequireBoundaries());
        }
    }

    internal sealed class ElevationNoiseStage : IPlanetStage
    {
        public string Name => "ElevationNoise";

        public void Run(PlanetGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Field = new ElevationNoise().Apply(
                context.RequireField(),
                context.Fork(PlanetGenerationContext.ElevationNoiseStageSeed));
        }
    }

    internal static class PlanetStageSummaries
    {
        public static string Create(string stageName, PlanetGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            switch (stageName)
            {
                case "Icosphere":
                    return string.Format(CultureInfo.InvariantCulture, "Icosphere: {0} tiles", context.RequireGrid().Count);
                case "Plates":
                    return Plates(context.RequirePlates());
                case "Boundaries":
                    return Boundaries(context.RequireBoundaries());
                case "Hydrology":
                    return Hydrology(context.RequireField());
                case "Resources":
                    return string.Format(CultureInfo.InvariantCulture, "Resources: {0} impacts", context.ResourceImpacts.Count);
                case "Settlements":
                    return string.Format(CultureInfo.InvariantCulture, "Settlements: {0} settlements", context.Settlements.Count);
                default:
                    return Field(stageName, context.RequireField());
            }
        }

        private static string Plates(PlatePartitionResult plates)
        {
            int oceanic = 0;
            for (int i = 0; i < plates.Plates.Count; i++)
            {
                if (plates.Plates[i].Kind == PlateKind.Oceanic)
                    oceanic++;
            }

            return string.Format(CultureInfo.InvariantCulture, "Plates: {0} plates, {1} oceanic", plates.PlateCount, oceanic);
        }

        private static string Boundaries(PlateBoundarySet boundaries)
        {
            int convergent = 0;
            int divergent = 0;
            int transform = 0;
            for (int i = 0; i < boundaries.Edges.Count; i++)
            {
                switch (boundaries.Edges[i].Kind)
                {
                    case PlateBoundaryKind.Convergent:
                        convergent++;
                        break;
                    case PlateBoundaryKind.Divergent:
                        divergent++;
                        break;
                    default:
                        transform++;
                        break;
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "Boundaries: {0} edges, {1} convergent, {2} divergent, {3} transform",
                boundaries.Edges.Count,
                convergent,
                divergent,
                transform);
        }

        private static string Hydrology(PlanetField field)
        {
            int rivers = 0;
            int lakes = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                if (tile.IsRiver)
                    rivers++;
                if (tile.IsLake)
                    lakes++;
            }

            return string.Format(CultureInfo.InvariantCulture, "Hydrology: {0} river tiles, {1} lake tiles", rivers, lakes);
        }

        private static string Field(string stageName, PlanetField field)
        {
            int land = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (field.TileAt(tileId).IsLand)
                    land++;
            }

            double landPercent = land * 100d / field.TileCount;
            return string.Format(CultureInfo.InvariantCulture, "{0}: {1} tiles, {2:0.#}% land", stageName, field.TileCount, landPercent);
        }
    }
}
