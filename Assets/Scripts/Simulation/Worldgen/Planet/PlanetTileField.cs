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
            : this(
                tileId,
                plateId,
                elevation,
                isLand,
                temperature,
                moisture,
                biome,
                flow,
                isRiver,
                isLake,
                0d,
                0d,
                0d,
                0d,
                0d,
                0d,
                0d,
                0d,
                0d)
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
            bool isLake,
            double ironOre,
            double preciousMetal,
            double coal,
            double oilGas,
            double stone,
            double clay,
            double wood,
            double soilFertility,
            double freshWater)
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
            IronOre = Clamp01(ironOre);
            PreciousMetal = Clamp01(preciousMetal);
            Coal = Clamp01(coal);
            OilGas = Clamp01(oilGas);
            Stone = Clamp01(stone);
            Clay = Clamp01(clay);
            Wood = Clamp01(wood);
            SoilFertility = Clamp01(soilFertility);
            FreshWater = Clamp01(freshWater);
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
        public double IronOre { get; }
        public double PreciousMetal { get; }
        public double Coal { get; }
        public double OilGas { get; }
        public double Stone { get; }
        public double Clay { get; }
        public double Wood { get; }
        public double SoilFertility { get; }
        public double FreshWater { get; }

        public PlanetTileField CopyWith(
            double? elevation = null,
            bool? isLand = null,
            double? temperature = null,
            double? moisture = null,
            PlanetBiome? biome = null,
            double? flow = null,
            bool? isRiver = null,
            bool? isLake = null,
            double? ironOre = null,
            double? preciousMetal = null,
            double? coal = null,
            double? oilGas = null,
            double? stone = null,
            double? clay = null,
            double? wood = null,
            double? soilFertility = null,
            double? freshWater = null)
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
                isLake ?? IsLake,
                ironOre ?? IronOre,
                preciousMetal ?? PreciousMetal,
                coal ?? Coal,
                oilGas ?? OilGas,
                stone ?? Stone,
                clay ?? Clay,
                wood ?? Wood,
                soilFertility ?? SoilFertility,
                freshWater ?? FreshWater);
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
