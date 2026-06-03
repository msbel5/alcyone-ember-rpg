namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Per-tile output from the phase-1a planetary substrate.</summary>
    public sealed class PlanetTileField
    {
        public PlanetTileField(int tileId, int plateId, double elevation, bool isLand)
        {
            TileId = tileId;
            PlateId = plateId;
            Elevation = elevation;
            IsLand = isLand;
        }

        public int TileId { get; }
        public int PlateId { get; }
        public double Elevation { get; }
        public bool IsLand { get; }
    }
}
