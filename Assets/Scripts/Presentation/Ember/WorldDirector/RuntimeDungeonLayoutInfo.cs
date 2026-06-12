using System.Collections.Generic;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>F18: the realize step records the multi-room delve's key world anchors so the proof
    /// driver and the dweller placement stop guessing local axes. One writer (RuntimeDungeonBuilder),
    /// readers: WorldSceneDirector (dweller spots) and EmberProofScreenshotDriver (camera anchors).</summary>
    public static class RuntimeDungeonLayoutInfo
    {
        public static int RoomCount { get; private set; }
        public static Vector3 EntryWorld { get; private set; }       // just outside the mouth
        public static Vector3 StartRoomWorld { get; private set; }   // room 0 centre
        public static Vector3 BossRoomWorld { get; private set; }    // last room centre
        public static Vector3 ChestWorld { get; private set; }
        public static Vector3 FootprintCenterWorld { get; private set; }
        public static float FootprintExtentMeters { get; private set; }
        public static readonly List<Vector3> DwellerSpots = new List<Vector3>();
        public static Vector3 BossSpot { get; private set; }

        public static void Record(
            int roomCount, Vector3 entry, Vector3 startRoom, Vector3 bossRoom, Vector3 chest,
            Vector3 footprintCenter, float footprintExtent, List<Vector3> dwellerSpots, Vector3 bossSpot)
        {
            RoomCount = roomCount;
            EntryWorld = entry;
            StartRoomWorld = startRoom;
            BossRoomWorld = bossRoom;
            ChestWorld = chest;
            FootprintCenterWorld = footprintCenter;
            FootprintExtentMeters = footprintExtent;
            DwellerSpots.Clear();
            if (dwellerSpots != null) DwellerSpots.AddRange(dwellerSpots);
            BossSpot = bossSpot;
        }
    }
}
