using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

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
                actor.topicIds);
            record.ReplaceAskedTopics(actor.askedTopicIds);
            return record;
        }
    }
}
