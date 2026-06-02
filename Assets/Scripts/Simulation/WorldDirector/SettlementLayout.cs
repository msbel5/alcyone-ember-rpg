using System.Collections.Generic;

namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// The deterministic plan for one realized location: the building shells, how big the ground plane must
    /// be to hold them, and where the player spawns (centre) facing which way. Pure data — the presentation
    /// World Scene Director consumes it to build geometry; no Unity types so it is unit-testable.
    /// </summary>
    public sealed class SettlementLayout
    {
        public SettlementLayout(
            IReadOnlyList<BuildingPlacement> buildings,
            float groundRadius,
            float playerSpawnX,
            float playerSpawnZ,
            float playerFacingDeg)
        {
            Buildings = buildings;
            GroundRadius = groundRadius;
            PlayerSpawnX = playerSpawnX;
            PlayerSpawnZ = playerSpawnZ;
            PlayerFacingDeg = playerFacingDeg;
        }

        public IReadOnlyList<BuildingPlacement> Buildings { get; }

        /// <summary>Half-extent (metres) of the square ground plane needed to contain the layout.</summary>
        public float GroundRadius { get; }

        public float PlayerSpawnX { get; }
        public float PlayerSpawnZ { get; }
        public float PlayerFacingDeg { get; }
    }
}
