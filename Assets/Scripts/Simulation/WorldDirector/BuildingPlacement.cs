namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// One building shell to realize: a box footprint centred at (OriginX, OriginZ) on the ground plane,
    /// in metres of world XZ. Engine-free on purpose — the deterministic plan stays unit-testable; the
    /// presentation builder turns this into actual geometry. MaterialIndex is an abstract palette slot the
    /// presentation layer maps to a wall colour (keeps UnityEngine.Color out of the deterministic layer).
    /// </summary>
    public readonly struct BuildingPlacement
    {
        public BuildingPlacement(float originX, float originZ, float sizeX, float sizeZ, float height, int materialIndex)
        {
            OriginX = originX;
            OriginZ = originZ;
            SizeX = sizeX;
            SizeZ = sizeZ;
            Height = height;
            MaterialIndex = materialIndex;
        }

        public float OriginX { get; }
        public float OriginZ { get; }
        public float SizeX { get; }
        public float SizeZ { get; }
        public float Height { get; }
        public int MaterialIndex { get; }
    }
}
