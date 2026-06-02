using System;
using System.Collections.Generic;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// Default layout for populous settlements (City / Town / Village): a deterministic ring of building
    /// shells around an open central plaza where the player spawns. Building count, ring radius, footprint
    /// sizes, heights, and a small angular jitter are all derived from the settlement seed, so the same
    /// world always rebuilds the identical town (verified by SettlementLayoutDeterminismTests).
    /// </summary>
    public sealed class VillageLayoutStrategy : ISettlementLayoutStrategy
    {
        private readonly int _minBuildings;
        private readonly int _maxBuildings;
        private readonly float _ringRadius;

        public VillageLayoutStrategy(int minBuildings = 5, int maxBuildings = 8, float ringRadius = 9f)
        {
            _minBuildings = minBuildings < 1 ? 1 : minBuildings;
            _maxBuildings = maxBuildings < _minBuildings ? _minBuildings : maxBuildings;
            _ringRadius = ringRadius < 2f ? 2f : ringRadius;
        }

        public SettlementLayout Plan(in SettlementContext context)
        {
            var rng = new XorShiftRng(context.Seed == 0u ? 1u : context.Seed);
            int span = (_maxBuildings - _minBuildings) + 1;
            int count = _minBuildings + rng.NextInt(span);

            var buildings = new List<BuildingPlacement>(count);
            for (int i = 0; i < count; i++)
            {
                // Even angular slot + small seed-driven jitter so the ring reads organic, not mechanical.
                double baseAngle = (2.0 * Math.PI * i) / count;
                double jitter = ((rng.NextInt(1000) / 1000.0) - 0.5) * (Math.PI / count);
                double angle = baseAngle + jitter;

                float radius = _ringRadius + (rng.NextInt(400) / 100f);   // up to +4m outward variation
                float x = (float)(Math.Cos(angle) * radius);
                float z = (float)(Math.Sin(angle) * radius);
                float sizeX = 3f + (rng.NextInt(300) / 100f);             // 3..6m
                float sizeZ = 3f + (rng.NextInt(300) / 100f);
                float height = 2.5f + (rng.NextInt(250) / 100f);          // 2.5..5m
                int material = rng.NextInt(4);

                buildings.Add(new BuildingPlacement(x, z, sizeX, sizeZ, height, material));
            }

            float groundRadius = _ringRadius + 14f;
            // Spawn at the plaza centre, facing +Z (into the ring).
            return new SettlementLayout(buildings, groundRadius, 0f, 0f, 0f);
        }
    }
}
