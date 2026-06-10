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
        private const float DefaultStreetClearance = 4.5f;
        private const float CentralPlazaRadius = 7.0f;
        private const float RingSpacingMeters = 13.0f;
        private const float MinimumArcSpacingMeters = 12.0f;
        private const int MaxRings = 8;

        private readonly int _minBuildings;
        private readonly int _maxBuildings;
        private readonly float _ringRadius;

        public VillageLayoutStrategy(int minBuildings = 8, int maxBuildings = 18, float ringRadius = 14f)
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

            // Distribute buildings across wide rings around a real plaza. Candidates that would block streets
            // are rejected deterministically; the settlement may contain fewer buildings than requested, but it
            // remains navigable and reproducible.
            var buildings = new List<BuildingPlacement>(count);
            int placed = 0;
            int ring = 0;
            float lastRingRadius = _ringRadius;
            while (placed < count && ring < MaxRings)
            {
                float ringRadius = _ringRadius + (ring * RingSpacingMeters);
                lastRingRadius = ringRadius;
                int capacity = Math.Max(6, (int)((2.0 * Math.PI * ringRadius) / MinimumArcSpacingMeters));
                for (int i = 0; i < capacity && placed < count; i++)
                {
                    double baseAngle = (2.0 * Math.PI * i) / capacity;
                    double jitter = ((rng.NextInt(1000) / 1000.0) - 0.5) * (Math.PI / capacity);
                    double angle = baseAngle + jitter;

                    float radius = ringRadius + (rng.NextInt(260) / 100f);
                    float x = (float)(Math.Cos(angle) * radius);
                    float z = (float)(Math.Sin(angle) * radius);
                    float sizeX = 3.5f + (rng.NextInt(220) / 100f);           // 3.5..5.7m
                    float sizeZ = 3.5f + (rng.NextInt(220) / 100f);
                    float height = 2.8f + (rng.NextInt(320) / 100f);          // 2.8..6.0m
                    int material = rng.NextInt(4);

                    var candidate = new BuildingPlacement(x, z, sizeX, sizeZ, height, material);
                    if (BlocksPlaza(candidate) || OverlapsAny(candidate, buildings))
                        continue;

                    buildings.Add(candidate);
                    placed++;
                }
                ring++;
            }

            float groundRadius = lastRingRadius + 16f;
            // Spawn at the plaza centre, facing +Z (into the town).
            return new SettlementLayout(buildings, groundRadius, 0f, 0f, 0f);
        }

        private static bool BlocksPlaza(BuildingPlacement candidate)
        {
            double halfDiagonal = Math.Sqrt((candidate.SizeX * candidate.SizeX) + (candidate.SizeZ * candidate.SizeZ)) * 0.5d;
            double distance = Math.Sqrt((candidate.OriginX * candidate.OriginX) + (candidate.OriginZ * candidate.OriginZ));
            return distance - halfDiagonal < CentralPlazaRadius;
        }

        private static bool OverlapsAny(BuildingPlacement candidate, IReadOnlyList<BuildingPlacement> buildings)
        {
            for (int i = 0; i < buildings.Count; i++)
            {
                if (Overlaps(candidate, buildings[i], DefaultStreetClearance))
                    return true;
            }

            return false;
        }

        private static bool Overlaps(BuildingPlacement a, BuildingPlacement b, float clearance)
        {
            float ax = (a.SizeX * 0.5f) + clearance;
            float az = (a.SizeZ * 0.5f) + clearance;
            float bx = b.SizeX * 0.5f;
            float bz = b.SizeZ * 0.5f;
            return Math.Abs(a.OriginX - b.OriginX) < ax + bx
                && Math.Abs(a.OriginZ - b.OriginZ) < az + bz;
        }
    }
}
