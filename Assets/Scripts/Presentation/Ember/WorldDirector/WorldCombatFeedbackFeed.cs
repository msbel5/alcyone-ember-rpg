namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F10 hit-feel mirror ("savaşamadım" follow-up): the adapter raises a stamp when the player's strike
    /// lands or fells a world actor; ActorCombatFeedbackView instances poll the stamp and react (red flash,
    /// fall flat). Stamp pattern instead of a consume-flag because MANY views read the same feed — each
    /// remembers the last stamp it saw, so one event fans out without any consumer eating it.
    /// </summary>
    public static class WorldCombatFeedbackFeed
    {
        public static int HitStamp { get; private set; }
        public static ulong HitTargetId { get; private set; }
        public static int FelledStamp { get; private set; }
        public static ulong FelledTargetId { get; private set; }

        public static void RaiseHit(ulong targetActorId)
        {
            HitTargetId = targetActorId;
            HitStamp++;
        }

        public static void RaiseFelled(ulong targetActorId)
        {
            FelledTargetId = targetActorId;
            FelledStamp++;
        }
    }
}
