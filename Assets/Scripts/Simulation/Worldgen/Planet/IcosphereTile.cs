using System.Collections.Generic;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>A geodesic grid vertex used as a spherical world tile.</summary>
    public sealed class IcosphereTile
    {
        internal IcosphereTile(int id, PlanetVector position, IReadOnlyList<int> neighbors)
        {
            Id = id;
            Position = position;
            Neighbors = neighbors;
        }

        public int Id { get; }
        public PlanetVector Position { get; }
        public IReadOnlyList<int> Neighbors { get; }
    }
}
