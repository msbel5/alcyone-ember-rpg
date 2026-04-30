// Design note:
// ActorVitals groups the three DFU-style Sprint 1 resource pools.
// Inputs: health, fatigue, and mana snapshots.
// Outputs: immutable actor vital state with dead/alive convenience.
// Bible reference: MASTER_MECHANICS_BIBLE.md §3, PRD FR-01.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Immutable trio of health, fatigue, and mana pools.</summary>
    public readonly struct ActorVitals
    {
        public ActorVitals(VitalStat health, VitalStat fatigue, VitalStat mana)
        {
            Health = health;
            Fatigue = fatigue;
            Mana = mana;
        }

        public VitalStat Health { get; }
        public VitalStat Fatigue { get; }
        public VitalStat Mana { get; }
        public bool IsDead => Health.IsDepleted;

        public ActorVitals WithHealth(VitalStat health)
        {
            return new ActorVitals(health, Fatigue, Mana);
        }

        public ActorVitals WithFatigue(VitalStat fatigue)
        {
            return new ActorVitals(Health, fatigue, Mana);
        }

        public ActorVitals WithMana(VitalStat mana)
        {
            return new ActorVitals(Health, Fatigue, mana);
        }
    }
}
