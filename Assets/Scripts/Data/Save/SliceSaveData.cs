namespace EmberCrpg.Data.Save
{
    using System.Collections.Generic;

    // Top-level save packet for the slice. Kept intentionally small for Faz 4.
    public sealed class SliceSaveData
    {
        public SliceSaveData()
        {
            Actors = new List<ActorSaveData>();
        }

        public List<ActorSaveData> Actors { get; }

        // Deterministic game tick anchor used for replay alignment
        public long GameTick { get; set; }
    }

    public sealed class ActorSaveData
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }

        // Needs saved as compact integers (0..100)
        public int Hunger { get; set; }
        public int Fatigue { get; set; }
        public int Thirst { get; set; }

        // Mood scalar 0..100
        public int Mood { get; set; }
    }
}
