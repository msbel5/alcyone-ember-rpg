using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;

// Design note:
// ActorSaveMapper isolates actor save/load field translation for Sprint 1 JSON persistence.
// Inputs: pure actor records or actor DTOs.
// Outputs: round-trippable actor save snapshots with no behavior.
// Bible reference: PRD FR-06.
namespace EmberCrpg.Data.Save
{
    /// <summary>Field mapper between actor records and actor DTOs.</summary>
    public static class ActorSaveMapper
    {
        public static ActorSaveData ToData(ActorRecord actor)
        {
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
                topicIds = actor.TopicIds.ToArray(),
                askedTopicIds = actor.AskedTopicIds.ToArray(),
                jobPreferences = actor.JobPreferences.Select(ToPreferenceData).ToArray(),
                currentJobId = actor.ScheduleState.CurrentJobId.Value,
                targetSiteId = actor.ScheduleState.TargetSiteId.Value,
                targetWorksitePositionX = actor.ScheduleState.TargetWorksitePosition.X,
                targetWorksitePositionY = actor.ScheduleState.TargetWorksitePosition.Y,
            };
        }

        public static ActorRecord ToActor(ActorSaveData actor)
        {
            var stats = new EmberStatBlock(actor.mig, actor.agi, actor.end, actor.mnd, actor.ins, actor.pre);
            var vitals = new ActorVitals(
                new VitalStat(actor.healthCurrent, actor.healthMax),
                new VitalStat(actor.fatigueCurrent, actor.fatigueMax),
                new VitalStat(actor.manaCurrent, actor.manaMax));
            var record = new ActorRecord(
                new ActorId(actor.id),
                actor.name,
                (ActorRole)actor.role,
                stats,
                vitals,
                new GridPosition(actor.positionX, actor.positionY),
                actor.accuracy,
                actor.dodge,
                actor.armor,
                actor.baseDamage,
                actor.topicIds,
                ToJobPreferences(actor.jobPreferences),
                ToScheduleState(actor));
            record.ReplaceAskedTopics(actor.askedTopicIds);
            return record;
        }

        private static ActorJobPreferenceSaveData ToPreferenceData(ActorJobPreference preference)
        {
            return new ActorJobPreferenceSaveData
            {
                kind = (int)preference.Kind,
                priority = preference.Priority.Value,
            };
        }

        private static ActorJobPreference[] ToJobPreferences(ActorJobPreferenceSaveData[] preferences)
        {
            return (preferences ?? System.Array.Empty<ActorJobPreferenceSaveData>())
                .Select(preference => new ActorJobPreference((JobKind)preference.kind, JobPriorityFromRaw(preference.priority)))
                .ToArray();
        }

        private static JobPriority JobPriorityFromRaw(int value)
        {
            return value > 0 ? JobPriority.Active(value) : JobPriority.Disabled;
        }

        private static ActorScheduleState ToScheduleState(ActorSaveData actor)
        {
            if (actor.currentJobId == 0UL)
                return ActorScheduleState.Idle;

            return ActorScheduleState.Assigned(
                new JobId(actor.currentJobId),
                new SiteId(actor.targetSiteId),
                new GridPosition(actor.targetWorksitePositionX, actor.targetWorksitePositionY));
        }
    }
}
