using System;
using System.Runtime.CompilerServices;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Worldgen;

namespace EmberCrpg.Simulation.Overland
{
    /// <summary>
    /// Attaches immutable world-geography samples to an overland map without changing the domain map contract.
    /// Pattern: sidecar metadata store; the map remains pure data, while renderers can use real planet fields.
    /// </summary>
    internal static class OverlandMapGeographyStore
    {
        private static readonly ConditionalWeakTable<OverlandMap, OverlandMapGeographySnapshot> Snapshots = new ConditionalWeakTable<OverlandMap, OverlandMapGeographySnapshot>();

        public static void Register(OverlandMap map, WorldGeography geography)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (geography == null)
                throw new ArgumentNullException(nameof(geography));
            if (geography.Width != map.Width || geography.Height != map.Height)
                throw new ArgumentException("Geography dimensions must match the overland map.", nameof(geography));

            Snapshots.Remove(map);
            Snapshots.Add(map, new OverlandMapGeographySnapshot(geography));
        }

        public static bool TryGet(OverlandMap map, out OverlandMapGeographySnapshot snapshot)
        {
            if (map == null)
            {
                snapshot = null;
                return false;
            }

            return Snapshots.TryGetValue(map, out snapshot);
        }
    }

    internal sealed class OverlandMapGeographySnapshot
    {
        private readonly bool[] _land;
        private readonly double[] _elevation;
        private readonly double[] _temperature;
        private readonly double[] _moisture;

        public OverlandMapGeographySnapshot(WorldGeography geography)
        {
            Width = geography.Width;
            Height = geography.Height;
            _land = geography.CopyLandMask();
            _elevation = Copy(geography.Elevation);
            _temperature = Copy(geography.Temperature);
            _moisture = Copy(geography.Moisture);
        }

        public int Width { get; }
        public int Height { get; }

        public bool IsLand(int x, int y) => _land[ToIndex(WrapX(x), ClampY(y))];
        public double Elevation(int x, int y) => _elevation[ToIndex(WrapX(x), ClampY(y))];
        public double Temperature(int x, int y) => _temperature[ToIndex(WrapX(x), ClampY(y))];
        public double Moisture(int x, int y) => _moisture[ToIndex(WrapX(x), ClampY(y))];

        private int ToIndex(int x, int y) => (y * Width) + x;

        private static double[] Copy(System.Collections.Generic.IReadOnlyList<double> source)
        {
            var copy = new double[source.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = source[i];
            return copy;
        }

        private int WrapX(int x)
        {
            int wrapped = x % Width;
            return wrapped < 0 ? wrapped + Width : wrapped;
        }

        private int ClampY(int y)
        {
            if (y < 0)
                return 0;
            return y >= Height ? Height - 1 : y;
        }
    }
}
