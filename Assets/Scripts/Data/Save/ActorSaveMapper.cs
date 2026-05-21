using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
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
                memoryFacts = actor.Memory?.Facts.Select(ToMemoryFactData).ToArray(),
                currentJobId = schedule.IsIdle ? 0UL : schedule.CurrentJobId.Value,
                targetSiteId = schedule.IsIdle ? 0UL : schedule.TargetSiteId.Value,
                targetWorksitePositionX = schedule.IsIdle ? 0 : schedule.TargetWorksitePosition.X,
                targetWorksitePositionY = schedule.IsIdle ? 0 : schedule.TargetWorksitePosition.Y,
                // Persist needs and mood as 0-100 ints. hasMood lets the load
                // path tell "actor saved at Lowest (Value=0)" apart from
                // "pre-Faz-4 save without a mood field" (Codex A/P3).
                hunger = actor.Needs.Hunger.Value,
                fatigue = actor.Needs.Fatigue.Value,
                thirst = actor.Needs.Thirst.Value,
                mood = actor.Mood.Value,
                hasMood = true,
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

            // Save format carries needs/mood if present; default to comfortable/null-neutral.
            // Codex audit (A/P3): the original "mood <= 0 → Neutral" heuristic
            // erased legitimately Lowest (Value=0) actors on every reload. Use
            // the new `hasMood` presence flag instead: when true, accept any
            // 0..100 mood value verbatim; only fall back to Neutral when the
            // save predates the flag (pre-Faz-4 saves where hasMood reads
            // false by default-deserialization).
            var needs = new ActorNeeds(new NeedValue(save.hunger), new NeedValue(save.fatigue), new NeedValue(save.thirst));
            var mood = save.hasMood ? new ActorMood(save.mood)
                      : save.mood > 0 ? new ActorMood(save.mood)
                      : ActorMood.Neutral;

            var topicIds = save.topicIds ?? Array.Empty<string>();
            var asked = save.askedTopicIds ?? Array.Empty<string>();
            // PR#129 bot review fix: JobPriority.Disabled.Value is 0, so saved
            // priority==0 entries used to disappear when re-loaded as
            // JobPriority.Active(0) (Active requires a positive int and would
            // either throw or be silently coerced). Detect the sentinel and
            // restore Disabled explicitly so disabled actor preferences survive
            // a save/load roundtrip.
            var jobPrefs = (save.jobPreferences ?? Array.Empty<ActorJobPreferenceSaveData>())
                .Select(p => new ActorJobPreference(
                    (JobKind)p.kind,
                    p.priority <= 0 ? JobPriority.Disabled : JobPriority.Active(p.priority)))
                .ToArray();
            var memory = id.IsEmpty ? null : new MemoryComponent(id);
            foreach (var fact in save.memoryFacts ?? Array.Empty<MemoryFactSaveData>())
            {
                if (fact == null || string.IsNullOrWhiteSpace(fact.topicCode))
                    continue;
                memory.Add(new MemoryFact(
                    new ActorId(fact.remembererId == 0UL ? id.Value : fact.remembererId),
                    new TopicId(fact.topicCode),
                    new ActorId(fact.aboutActorId),
                    new GameTime(fact.recordedAtMinutes < 0 ? 0 : fact.recordedAtMinutes),
                    fact.detail));
            }

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
                scheduleState: (save.currentJobId == 0UL
                    ? default(ActorScheduleState)
                    : ActorScheduleState.Assigned(new JobId(save.currentJobId), new SiteId(save.targetSiteId), new GridPosition(save.targetWorksitePositionX, save.targetWorksitePositionY))),
                needs: needs,
                mood: mood,
                memory: memory);

            record.ReplaceAskedTopics(asked);

            return record;
        }

        private static MemoryFactSaveData ToMemoryFactData(MemoryFact fact)
        {
            return new MemoryFactSaveData
            {
                remembererId = fact.Rememberer.Value,
                topicCode = fact.Topic.Code,
                aboutActorId = fact.AboutActor.Value,
                recordedAtMinutes = fact.RecordedAt.TotalMinutes,
                detail = fact.Detail,
            };
        }
    }
}
