namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>An adjacency edge where two different plates meet.</summary>
    public sealed class PlateBoundaryEdge
    {
        public PlateBoundaryEdge(int tileA, int tileB, int plateA, int plateB, PlateBoundaryKind kind, double magnitude)
        {
            TileA = tileA;
            TileB = tileB;
            PlateA = plateA;
            PlateB = plateB;
            Kind = kind;
            Magnitude = magnitude;
        }

        public int TileA { get; }
        public int TileB { get; }
        public int PlateA { get; }
        public int PlateB { get; }
        public PlateBoundaryKind Kind { get; }
        public double Magnitude { get; }
    }
}
