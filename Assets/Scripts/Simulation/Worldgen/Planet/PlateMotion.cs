namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Euler-pole plate rotation on the unit sphere.</summary>
    public sealed class PlateMotion
    {
        public PlateMotion(int id, PlateKind kind, PlanetVector axis, double angularSpeed)
        {
            Id = id;
            Kind = kind;
            Axis = axis.Normalize();
            AngularSpeed = angularSpeed;
        }

        public int Id { get; }
        public PlateKind Kind { get; }
        public PlanetVector Axis { get; }
        public double AngularSpeed { get; }

        public PlanetVector VelocityAt(PlanetVector surfacePoint)
        {
            return PlanetVector.Cross(Axis.Scale(AngularSpeed), surfacePoint);
        }
    }
}
