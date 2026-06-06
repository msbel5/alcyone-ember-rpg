using System;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Simulation.Forge
{
    public sealed class NullImageMatteService : IImageMatteService
    {
        public MatteResult Matte(ReadOnlySpan<byte> rgba, int width, int height)
        {
            return MatteResult.Opaque(width, height);
        }
    }
}
