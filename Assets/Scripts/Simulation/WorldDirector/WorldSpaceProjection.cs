using EmberCrpg.Domain.Overland;

namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// THE canonical mapping between walkable world metres and overland tile space. One overland tile is a
    /// real area whose edge length comes from <see cref="OverlandParameters.DefaultRegionEdgeKm"/> (40 km),
    /// so every atlas pixel corresponds to a real x/z range. The 3D terrain, the map image and travel math
    /// must all go through here — inventing a second projection is how the map and the world drift apart.
    /// World origin (0,0) is the CENTRE of the player's home tile; +X is east (tile X grows), +Z is north
    /// (tile Y SHRINKS — atlas row 0 is the northern edge).
    /// </summary>
    public static class WorldSpaceProjection
    {
        public static double MetersPerTile => OverlandParameters.DefaultRegionEdgeKm * 1000d;

        public static double TileFracX(int homeTileX, double worldXMeters)
            => homeTileX + 0.5d + (worldXMeters / MetersPerTile);

        public static double TileFracY(int homeTileY, double worldZMeters)
            => homeTileY + 0.5d - (worldZMeters / MetersPerTile);
    }
}
