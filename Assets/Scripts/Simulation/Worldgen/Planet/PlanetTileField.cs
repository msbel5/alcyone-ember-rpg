namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Per-tile output from the planetary substrate pipeline.</summary>
    public sealed class PlanetTileField
    {
        public PlanetTileField(int tileId, int plateId, double elevation, bool isLand)
            : this(
                tileId,
                plateId,
                elevation,
                isLand,
                0d,
                0d,
                isLand ? PlanetBiome.Grassland : PlanetBiome.Ocean,
                0d,
                false,
                false)
        {
        }

        public PlanetTileField(
            int tileId,
            int plateId,
            double elevation,
            bool isLand,
            double temperature,
            double moisture,
            PlanetBiome biome,
            double flow,
            bool isRiver,
            bool isLake)
        {
            TileId = tileId;
            PlateId = plateId;
            Elevation = elevation;
            IsLand = isLand;
            Temperature = Clamp01(temperature);
            Moisture = Clamp01(moisture);
            Biome = biome;
            Flow = flow < 0d ? 0d : flow;
            IsRiver = isRiver;
            IsLake = isLake;
        }

        public int TileId { get; }
        public int PlateId { get; }
        public double Elevation { get; }
        public bool IsLand { get; }
        public double Temperature { get; }
        public double Moisture { get; }
        public PlanetBiome Biome { get; }
        public double Flow { get; }
        public bool IsRiver { get; }
        public bool IsLake { get; }

        public PlanetTileField CopyWith(
            double? elevation = null,
            bool? isLand = null,
            double? temperature = null,
            double? moisture = null,
            PlanetBiome? biome = null,
            double? flow = null,
            bool? isRiver = null,
            bool? isLake = null)
        {
            return new PlanetTileField(
                TileId,
                PlateId,
                elevation ?? Elevation,
                isLand ?? IsLand,
                temperature ?? Temperature,
                moisture ?? Moisture,
                biome ?? Biome,
                flow ?? Flow,
                isRiver ?? IsRiver,
                isLake ?? IsLake);
        }

        private static double Clamp01(double value)
        {
            if (value < 0d)
                return 0d;
            if (value > 1d)
                return 1d;
            return value;
        }
    }
}
