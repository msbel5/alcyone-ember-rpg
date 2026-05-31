using System;
using System.Collections.Generic;

namespace EmberCrpg.Simulation.Composition
{
    public sealed class WorldTickRegistry
    {
        private readonly IWorldTickSystem[] _ordered;
        private readonly IWorldTickSystem[] _perTick;
        private readonly IWorldTickSystem[] _hourly;
        private readonly IWorldTickSystem[] _daily;

        public WorldTickRegistry(IEnumerable<IWorldTickSystem> systems)
        {
            if (systems == null) throw new ArgumentNullException(nameof(systems));

            var rows = new List<IWorldTickSystem>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var system in systems)
            {
                if (system == null) throw new ArgumentException("Tick system list cannot contain null.", nameof(systems));
                if (string.IsNullOrWhiteSpace(system.Id))
                    throw new ArgumentException("Tick system id cannot be empty.", nameof(systems));
                if (!ids.Add(system.Id))
                    throw new InvalidOperationException("Duplicate world tick system id: " + system.Id);
                rows.Add(system);
            }

            rows.Sort(Compare);
            _ordered = rows.ToArray();
            _perTick = Filter(TickCadence.PerTick);
            _hourly = Filter(TickCadence.Hourly);
            _daily = Filter(TickCadence.Daily);
        }

        public IReadOnlyList<IWorldTickSystem> Ordered => _ordered;
        public IReadOnlyList<IWorldTickSystem> PerTick => _perTick;
        public IReadOnlyList<IWorldTickSystem> Hourly => _hourly;
        public IReadOnlyList<IWorldTickSystem> Daily => _daily;

        private static int Compare(IWorldTickSystem left, IWorldTickSystem right)
        {
            var byCadence = left.Cadence.CompareTo(right.Cadence);
            if (byCadence != 0) return byCadence;

            var byOrder = left.Order.CompareTo(right.Order);
            return byOrder != 0
                ? byOrder
                : StringComparer.Ordinal.Compare(left.Id, right.Id);
        }

        private IWorldTickSystem[] Filter(TickCadence cadence)
        {
            var rows = new List<IWorldTickSystem>();
            for (var i = 0; i < _ordered.Length; i++)
            {
                if (_ordered[i].Cadence == cadence)
                    rows.Add(_ordered[i]);
            }

            return rows.ToArray();
        }
    }
}
