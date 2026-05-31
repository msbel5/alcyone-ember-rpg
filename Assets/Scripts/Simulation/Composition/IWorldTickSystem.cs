namespace EmberCrpg.Simulation.Composition
{
    public interface IWorldTickSystem
    {
        string Id { get; }
        TickCadence Cadence { get; }
        int Order { get; }
        void Run(in TickContext context);
    }
}
