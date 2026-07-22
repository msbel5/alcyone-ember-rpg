using System;
using System.Collections.Generic;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// SettlementLayoutGraph v1 for populous kinds (City/Town): deterministic RADIAL STREETS from the plaza
    /// with buildings parcelled along BOTH sides of each avenue, leaving a clear walkable lane — towns read
    /// as streets, not concentric orbits. The runtime entrance rule (door faces the settlement centre) opens
    /// each shell toward the avenue it stands on, so every building stays reachable. Deterministic per seed.
    /// v2 (open, honest): cross-streets, parcels/doors as first-class graph nodes, interiors.
    /// </summary>
    public sealed class StreetLayoutStrategy : ISettlementLayoutStrategy
    {
        private const float PlazaRadius = 8f;
        private const float StreetHalfWidth = 3.5f; // clear lane each side of the avenue axis
        private const float ParcelStep = 9f;        // spacing between consecutive parcels along an avenue
        private const float Clearance = 2.5f;

        public SettlementLayout Plan(in SettlementContext context)
        {
            var rng = new XorShiftRng(context.Seed == 0u ? 2u : context.Seed);
            bool city = context.Kind == EmberCrpg.Domain.Overland.SettlementKind.City;
            int avenues = city ? 4 + rng.NextInt(2) : 3 + rng.NextInt(2);        // city 4-5, town 3-4
            int parcelsPerSide = city ? 4 + rng.NextInt(3) : 3 + rng.NextInt(2); // depth along each avenue
            float heightBoost = city ? 3.2f : 1.0f; // cities read TALLER from the gate
            int dominantMaterial = rng.NextInt(4); // town material identity (timber town vs stone town)

            var buildings = new List<BuildingPlacement>(avenues * parcelsPerSide * 2);
            double angleOffset = (rng.NextInt(1000) / 1000.0) * Math.PI * 2.0;
            float maxReach = PlazaRadius;

            for (int a = 0; a < avenues; a++)
            {
                double angle = angleOffset + ((Math.PI * 2.0 * a) / avenues);
                double dirX = Math.Cos(angle), dirZ = Math.Sin(angle);
                double perpX = -dirZ, perpZ = dirX;

                for (int p = 0; p < parcelsPerSide; p++)
                {
                    float along = PlazaRadius + 6f + (p * ParcelStep) + (rng.NextInt(200) / 100f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float sizeX = 3.5f + (rng.NextInt(220) / 100f);
                        float sizeZ = 3.5f + (rng.NextInt(220) / 100f);
                        float height = 2.8f + heightBoost + (rng.NextInt(320) / 100f);
                        int material = rng.NextInt(100) < 60 ? dominantMaterial : rng.NextInt(4);

                        // Offset past the lane so the avenue stays walkable shoulder-to-shoulder.
                        float offset = StreetHalfWidth + (Math.Max(sizeX, sizeZ) * 0.5f) + (rng.NextInt(120) / 100f);
                        float x = (float)((dirX * along) + (perpX * offset * side));
                        float z = (float)((dirZ * along) + (perpZ * offset * side));

                        var candidate = new BuildingPlacement(x, z, sizeX, sizeZ, height, material);
                        if (OverlapsAny(candidate, buildings)) continue;

                        buildings.Add(candidate);
                        float reach = along + (ParcelStep * 0.5f);
                        if (reach > maxReach) maxReach = reach;
                    }
                }
            }

            // Spawn at the plaza centre, facing +Z (down a street when one aligns; into town otherwise).
            return new SettlementLayout(buildings, maxReach + 16f, 0f, 0f, 0f);
        }

        private static bool OverlapsAny(BuildingPlacement candidate, IReadOnlyList<BuildingPlacement> buildings)
        {
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                float ax = (candidate.SizeX * 0.5f) + Clearance + (b.SizeX * 0.5f);
                float az = (candidate.SizeZ * 0.5f) + Clearance + (b.SizeZ * 0.5f);
                if (Math.Abs(candidate.OriginX - b.OriginX) < ax && Math.Abs(candidate.OriginZ - b.OriginZ) < az)
                    return true;
            }

            return false;
        }
    }
}
