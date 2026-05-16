namespace EmberCrpg.Data.Save
{
    using EmberCrpg.Domain.Actors;
    using EmberCrpg.Domain.Core;
    using EmberCrpg.Domain.World;

    // Simple mapper for Actor <-> ActorSaveData. This mapper intentionally
    // uses safe defaults for stats/vitals when restoring since Faz 4's
    // acceptance criteria only requires needs/mood preservation. Later fazs
    // will expand the save packet and mapper.
    public static class ActorSaveMapper
    {
        // Backwards-compatible method names used by the existing SliceSaveMapper
        // and test harness. Keep ToSave/FromSave as explicit names and expose
        // ToData/ToActor adapters so older callers continue to compile.
        public static ActorSaveData ToData(ActorRecord actor) => ToSave(actor);

        public static ActorRecord ToActor(ActorSaveData save) => FromSave(save);

        public static ActorSaveData ToSave(ActorRecord actor)
        {
            if (actor == null)
                return null;

            return new ActorSaveData
            {
                Id = actor.Id.Value,
                Name = actor.Name,
                PosX = actor.Position.X,
                PosY = actor.Position.Y,
                Hunger = actor.Needs.Hunger.Value,
                Fatigue = actor.Needs.Fatigue.Value,
                Thirst = actor.Needs.Thirst.Value,
                Mood = actor.Mood.Value,
            };
        }

        public static ActorRecord FromSave(ActorSaveData save)
        {
            if (save == null)
                return null;

            // Defaults chosen to match test helpers and existing actor seeds.
            var id = new ActorId(save.Id);
            var name = string.IsNullOrEmpty(save.Name) ? "restored" : save.Name;
            var role = ActorRole.Guard;
            var stats = new EmberStatBlock(10, 10, 10, 10, 10, 10);
            var vitals = new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6));
            var position = new GridPosition(save.PosX, save.PosY);

            var needs = new ActorNeeds(new NeedValue(save.Hunger), new NeedValue(save.Fatigue), new NeedValue(save.Thirst));
            var mood = new ActorMood(save.Mood);

            // Accuracy/dodge/armor/baseDamage chosen to mirror test actor constructors
            return new ActorRecord(
                id,
                name,
                role,
                stats,
                vitals,
                position,
                accuracy: 10,
                dodge: 5,
                armor: 1,
                baseDamage: 3,
                topicIds: null,
                jobPreferences: null,
                scheduleState: default,
                needs: needs,
                mood: mood);
        }
    }
}
