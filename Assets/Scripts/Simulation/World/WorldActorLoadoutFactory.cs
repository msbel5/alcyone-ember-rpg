using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

// Design note:
// WorldActorLoadoutFactory assigns role-differentiated deterministic stats, vitals, and combat fields.
// Inputs: actor identity, role, spawn position, and optional topic ids for narrative-capable roles.
// Outputs: ready-to-use pure actor records that feel distinct before any UI text lands.
// Bible reference: MASTER_MECHANICS_BIBLE.md §13/§14/§15, PRD Sprint 2 FR-05.
namespace EmberCrpg.Simulation.World
{
    /// <summary>Creates slice actors with role-appropriate starting loadouts.</summary>
    public sealed class WorldActorLoadoutFactory
    {
        public ActorRecord Create(ActorId id, string name, ActorRole role, GridPosition position, IEnumerable<string> topicIds = null)
        {
            return role switch
            {
                ActorRole.Player => Build(id, name, role, position, new EmberStatBlock(62, 56, 58, 44, 48, 46), new ActorVitals(new VitalStat(28, 28), new VitalStat(20, 20), new VitalStat(12, 12)), 18, 12, 2, 7, topicIds),
                ActorRole.Talker => Build(id, name, role, position, new EmberStatBlock(38, 42, 40, 60, 58, 64), new ActorVitals(new VitalStat(18, 18), new VitalStat(12, 12), new VitalStat(20, 20)), 6, 8, 0, 2, topicIds),
                ActorRole.Merchant => Build(id, name, role, position, new EmberStatBlock(42, 44, 46, 52, 48, 60), new ActorVitals(new VitalStat(20, 20), new VitalStat(14, 14), new VitalStat(10, 10)), 8, 9, 1, 3, topicIds),
                ActorRole.Guard => Build(id, name, role, position, new EmberStatBlock(58, 50, 62, 40, 44, 42), new ActorVitals(new VitalStat(30, 30), new VitalStat(18, 18), new VitalStat(8, 8)), 16, 11, 4, 6, topicIds),
                ActorRole.Enemy => Build(id, name, role, position, new EmberStatBlock(52, 58, 48, 22, 28, 18), new ActorVitals(new VitalStat(22, 22), new VitalStat(18, 18), new VitalStat(4, 4)), 14, 14, 1, 5, topicIds),
                _ => Build(id, name, role, position, new EmberStatBlock(50, 50, 50, 50, 50, 50), new ActorVitals(new VitalStat(20, 20), new VitalStat(20, 20), new VitalStat(10, 10)), 10, 10, 0, 3, topicIds),
            };
        }

        private static ActorRecord Build(ActorId id, string name, ActorRole role, GridPosition position, EmberStatBlock stats, ActorVitals vitals, int accuracy, int dodge, int armor, int baseDamage, IEnumerable<string> topicIds)
        {
            return new ActorRecord(id, name, role, stats, vitals, position, accuracy, dodge, armor, baseDamage, topicIds);
        }
    }
}
