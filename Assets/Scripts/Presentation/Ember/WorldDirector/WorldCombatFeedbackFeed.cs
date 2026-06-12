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
        // F29: what the strike LANDED ON — the bestiary's hit material ("flesh"/"bone"/"hide"/
        // "chitin"/"wail") keys the audio director's impact variant.
        public static string HitMaterial { get; private set; } = "flesh";
        public static int FelledStamp { get; private set; }
        public static ulong FelledTargetId { get; private set; }

        public static void RaiseHit(ulong targetActorId, string hitMaterial = "flesh")
        {
            HitTargetId = targetActorId;
            HitMaterial = string.IsNullOrEmpty(hitMaterial) ? "flesh" : hitMaterial;
            HitStamp++;
        }

        public static void RaiseFelled(ulong targetActorId)
        {
            FelledTargetId = targetActorId;
            FelledStamp++;
        }

        // F14: the ENEMY's own swing — its billboard lunges for 0.2s (attack tell without animation).
        public static int EnemyStrikeStamp { get; private set; }
        public static ulong EnemyStrikeId { get; private set; }

        public static void RaiseEnemyStrike(ulong attackerActorId)
        {
            EnemyStrikeId = attackerActorId;
            EnemyStrikeStamp++;
        }
    }
}
