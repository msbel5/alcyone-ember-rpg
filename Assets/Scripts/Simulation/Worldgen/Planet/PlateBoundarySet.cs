using System;
using System.Collections.Generic;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>All inter-plate edges derived from Euler-pole motion.</summary>
    public sealed class PlateBoundarySet
    {
        internal PlateBoundarySet(PlateBoundaryEdge[] edges)
        {
            Edges = Array.AsReadOnly(edges);
        }

        public IReadOnlyList<PlateBoundaryEdge> Edges { get; }
    }
}
