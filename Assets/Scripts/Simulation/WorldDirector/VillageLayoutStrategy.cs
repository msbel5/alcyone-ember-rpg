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

        public VillageLayoutStrategy(int minBuildings = 14, int maxBuildings = 28, float ringRadius = 8f)
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

            // Distribute the buildings across CONCENTRIC RINGS (~5m spacing per ring) around an open central
            // plaza, so a populous settlement reads as a real town with depth instead of one thin ring of
            // shells. Each ring is pushed further out and packs as many houses as its circumference allows;
            // all positions/sizes stay seed-derived, so determinism is preserved.
            var buildings = new List<BuildingPlacement>(count);
            int placed = 0;
            int ring = 0;
            float lastRingRadius = _ringRadius;
            while (placed < count && ring < 6)
            {
                float ringRadius = _ringRadius + (ring * _ringRadius * 0.9f);
                lastRingRadius = ringRadius;
                int capacity = Math.Max(6, (int)((2.0 * Math.PI * ringRadius) / 5.0));
                int thisRing = Math.Min(capacity, count - placed);
                for (int i = 0; i < thisRing; i++)
                {
                    double baseAngle = (2.0 * Math.PI * i) / thisRing;
                    double jitter = ((rng.NextInt(1000) / 1000.0) - 0.5) * (Math.PI / thisRing);
                    double angle = baseAngle + jitter;

                    float radius = ringRadius + (rng.NextInt(300) / 100f);
                    float x = (float)(Math.Cos(angle) * radius);
                    float z = (float)(Math.Sin(angle) * radius);
                    float sizeX = 3f + (rng.NextInt(400) / 100f);             // 3..7m
                    float sizeZ = 3f + (rng.NextInt(400) / 100f);
                    float height = 2.5f + (rng.NextInt(400) / 100f);          // 2.5..6.5m
                    int material = rng.NextInt(4);

                    buildings.Add(new BuildingPlacement(x, z, sizeX, sizeZ, height, material));
                    placed++;
                }
                ring++;
            }

            float groundRadius = lastRingRadius + 16f;
            // Spawn at the plaza centre, facing +Z (into the town).
            return new SettlementLayout(buildings, groundRadius, 0f, 0f, 0f);
        }
    }
}
