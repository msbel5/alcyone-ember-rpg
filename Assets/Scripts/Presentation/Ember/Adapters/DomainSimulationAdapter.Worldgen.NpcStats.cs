using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using EmberCrpg.Presentation.Ember.Worldgen;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        private static ActorRole ToActorRole(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Merchant: return ActorRole.Merchant;
                case NpcRole.Guard: return ActorRole.Guard;
                case NpcRole.Outlaw: return ActorRole.Enemy;
                default: return ActorRole.Talker;
            }
        }

        private static EmberStatBlock StatsFor(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Guard: return new EmberStatBlock(55, 45, 55, 30, 35, 35);
                case NpcRole.Merchant: return new EmberStatBlock(35, 40, 35, 45, 45, 60);
                case NpcRole.Scholar: return new EmberStatBlock(25, 35, 35, 70, 60, 45);
                case NpcRole.Outlaw: return new EmberStatBlock(45, 60, 40, 35, 55, 40);
                default: return new EmberStatBlock(35, 35, 40, 35, 40, 45);
            }
        }

        private static ActorVitals VitalsFor(NpcRole role)
        {
            var stats = StatsFor(role);
            return new ActorVitals(
                new VitalStat(20 + stats.End / 2, 20 + stats.End / 2),
                new VitalStat(20 + stats.Mig / 2, 20 + stats.Mig / 2),
                new VitalStat(10 + stats.Mnd / 2, 10 + stats.Mnd / 2));
        }

        private static WorldEventKind ToRuntimeEventKind(WorldHistoryKind kind)
        {
            switch (kind)
            {
                case WorldHistoryKind.FactionWar:
                case WorldHistoryKind.FactionAlliance:
                    return WorldEventKind.FactionReputationChanged;
                case WorldHistoryKind.TradeRouteOpened:
                    return WorldEventKind.TradeCompleted;
                case WorldHistoryKind.Calamity:
                    return WorldEventKind.ShortageDetected;
                default:
                    return WorldEventKind.StorytellerCheckpoint;
            }
        }

    }
}
