using System;
using EmberCrpg.Presentation.Ember.Adapters;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    /// <summary>
    /// Owns the host's adapter-to-role binding so EmberWorldHost does not manually
    /// cast the aggregate adapter into every narrow presentation surface.
    /// </summary>
    public sealed class EmberWorldHostAdapterBinding
    {
        private EmberWorldHostAdapterBinding(IDomainSimulationAdapter adapter)
        {
            Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            Clock = adapter;
            Hud = adapter;
            WorldView = adapter;
            Commands = adapter;
            Oracle = adapter;
        }

        public IDomainSimulationAdapter Adapter { get; }
        public IEmberSimulationClock Clock { get; }
        public IEmberHudReadModel Hud { get; }
        public IWorldViewReadModel WorldView { get; }
        public IPlayerCommandSink Commands { get; }
        public IConsultFateOracle Oracle { get; }

        public static EmberWorldHostAdapterBinding From(IDomainSimulationAdapter adapter)
        {
            return new EmberWorldHostAdapterBinding(adapter);
        }

        public static EmberWorldHostAdapterBinding Create(
            IDomainSimulationAdapter candidate,
            Func<IDomainSimulationAdapter> fallbackFactory)
        {
            var adapter = candidate;
            if (adapter == null)
            {
                if (fallbackFactory == null) throw new ArgumentNullException(nameof(fallbackFactory));
                adapter = fallbackFactory();
            }

            return From(adapter);
        }
    }
}
