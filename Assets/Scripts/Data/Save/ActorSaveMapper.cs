using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Data.Save
{
    // Mapper between domain ActorRecord and Unity-serializable ActorSaveData.
    // Keeps mapping minimal and defensive: missing fields use sane defaults so
    // older save formats remain loadable.
    public static class ActorSaveMapper
    {
        public static ActorSaveData ToData(ActorRecord actor) => ToSave(actor);

        public static ActorRecord ToActor(ActorSaveData data) => FromSave(data);

        public static ActorSaveData ToSave(ActorRecord actor)
        {
            if (actor == null)
                return null;

            var schedule = actor.ScheduleState;
            return new ActorSaveData
            {
                id = actor.Id.Value,
                name = actor.Name,
                role = (int)actor.Role,
                positionX = actor.Position.X,
                positionY = actor.Position.Y,
                mig = actor.Stats.Mig,
                agi = actor.Stats.Agi,
                end = actor.Stats.End,
                mnd = actor.Stats.Mnd,
                ins = actor.Stats.Ins,
                pre = actor.Stats.Pre,
                healthCurrent = actor.Vitals.Health.Current,
                healthMax = actor.Vitals.Health.Max,
                fatigueCurrent = actor.Vitals.Fatigue.Current,
                fatigueMax = actor.Vitals.Fatigue.Max,
                manaCurrent = actor.Vitals.Mana.Current,
                manaMax = actor.Vitals.Mana.Max,
                accuracy = actor.Accuracy,
                dodge = actor.Dodge,
                armor = actor.Armor,
                baseDamage = actor.BaseDamage,
                topicIds = actor.TopicIds?.ToArray(),
                askedTopicIds = actor.AskedTopicIds?.ToArray(),
                jobPreferences = actor.JobPreferences?.Select(p => new ActorJobPreferenceSaveData { kind = (int)p.Kind, priority = p.Priority.Value }).ToArray(),
                currentJobId = schedule.IsIdle ? 0UL : schedule.CurrentJobId.Value,
                targetSiteId = schedule.IsIdle ? 0UL : schedule.TargetSiteId.Value,
                targetWorksitePositionX = schedule.IsIdle ? 0 : schedule.TargetWorksitePosition.X,
                targetWorksitePositionY = schedule.IsIdle ? 0 : schedule.TargetWorksitePosition.Y,
            };
        }

        public static ActorRecord FromSave(ActorSaveData save)
        {
            if (save == null)
                return null;

            var id = new ActorId(save.id);
            var name = string.IsNullOrEmpty(save.name) ? "restored" : save.name;
            var role = (ActorRole)save.role;
            var stats = new EmberStatBlock(save.mig, save.agi, save.end, save.mnd, save.ins, save.pre);
            var vitals = new ActorVitals(
                new VitalStat(save.healthCurrent, Math.Max(1, save.healthMax)),
                new VitalStat(save.fatigueCurrent, Math.Max(1, save.fatigueMax)),
                new VitalStat(save.manaCurrent, Math.Max(1, save.manaMax)));
            var position = new GridPosition(save.positionX, save.positionY);

            // Save format does not currently carry needs/mood; use comfortable defaults.
            var needs = ActorNeeds.Comfortable;
            var mood = default(ActorMood);

            var topicIds = save.topicIds ?? Array.Empty<string>();
            var asked = save.askedTopicIds ?? Array.Empty<string>();
            var jobPrefs = (save.jobPreferences ?? Array.Empty<ActorJobPreferenceSaveData>())
                .Select(p => new ActorJobPreference((JobKind)p.kind, JobPriority.Active(p.priority)))
                .ToArray();

            var record = new ActorRecord(
                id,
                name,
                role,
                stats,
                vitals,
                position,
                accuracy: save.accuracy,
                dodge: save.dodge,
                armor: save.armor,
                baseDamage: save.baseDamage,
                topicIds: topicIds,
                jobPreferences: jobPrefs,
                scheduleState: default,
                needs: needs,
                mood: mood);

            record.ReplaceAskedTopics(asked);

            return record;
        }
    }
}
