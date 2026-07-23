namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// M6 ("npcler birbiriyle etkilesiyor mu bilmiyorum"): transient echoes of REAL sim events,
    /// keyed by actor id - a witness gets an eye, a report an alert, a guard a sword, a picker
    /// a sheaf, a talker a chat mark. Ring buffer, stamp-not-consume (the combat feed rule):
    /// one cascade tick can burst MANY events and a single slot would drop them.
    /// </summary>
    public static class NpcEventEchoFeed
    {
        public const int KindWitness = 0;
        public const int KindReport = 1;
        public const int KindGuard = 2;
        public const int KindHarvest = 3;
        public const int KindTalk = 4;

        public struct Echo
        {
            public ulong ActorId;
            public int Kind;
            public int StampAt;
        }

        private static readonly Echo[] Ring = new Echo[128];
        private static int _writeIndex;

        public static int Stamp { get; private set; }

        public static void Raise(ulong actorId, int kind)
        {
            Ring[_writeIndex % Ring.Length] = new Echo { ActorId = actorId, Kind = kind, StampAt = ++Stamp };
            _writeIndex++;
        }

        /// <summary>Newest echo for this actor newer than sinceStamp; -1 = nothing new.</summary>
        public static int LatestKindFor(ulong actorId, int sinceStamp)
        {
            int newest = -1, newestStamp = sinceStamp;
            for (int i = 0; i < Ring.Length; i++)
            {
                var echo = Ring[i];
                if (echo.StampAt > newestStamp && echo.ActorId == actorId)
                {
                    newest = echo.Kind;
                    newestStamp = echo.StampAt;
                }
            }
            return newest;
        }
    }
}
