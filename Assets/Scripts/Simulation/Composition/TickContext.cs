using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Composition
{
    public readonly struct TickContext
    {
        public TickContext(WorldState world, GameTime stamp, int delta)
        {
            World = world;
            Stamp = stamp;
            Delta = delta;
        }

        public WorldState World { get; }
        public GameTime Stamp { get; }
        public int Delta { get; }
    }
}
