using EmberCrpg.Simulation.WorldDirector;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.WorldDirector
{
    /// <summary>
    /// Direction-truth lock ("compass mı harita mı yanlış?"): the compass shows camera yaw and Unity yaw 0
    /// faces +Z, so the compass is correct IFF +Z heads toward atlas row 0 (map-up = north) and +X toward
    /// growing tileX (map-right = east). This pins that chain so neither side can silently flip again.
    /// </summary>
    public sealed class WorldSpaceProjectionDirectionTests
    {
        [Test]
        public void WalkingPlusZ_HeadsNorth_AndPlusX_HeadsEast()
        {
            double ty0 = WorldSpaceProjection.TileFracY(10, 0d);
            double tyAfterNorthWalk = WorldSpaceProjection.TileFracY(10, 1000d); // +Z, 1 km
            Assert.That(tyAfterNorthWalk, Is.LessThan(ty0),
                "+Z must DECREASE tileY (atlas row 0 = north = map-up) — compass N is map-up");

            double tx0 = WorldSpaceProjection.TileFracX(10, 0d);
            double txAfterEastWalk = WorldSpaceProjection.TileFracX(10, 1000d); // +X, 1 km
            Assert.That(txAfterEastWalk, Is.GreaterThan(tx0),
                "+X must INCREASE tileX (east = map-right) — compass E is map-right");
        }
    }
}
