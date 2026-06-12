using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Quest;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Quest
{
    /// <summary>
    /// F21 DoD: 20 seeds → 20 VALID quests (giver gives work, every reference filled per template,
    /// deadline in the future, reward positive) + determinism (same seed → same quest) + all four
    /// templates reachable over a seed sweep.
    /// </summary>
    public sealed class WorldQuestGeneratorTests
    {
        private static readonly SettlementId Here = new SettlementId(1UL);
        private static readonly SettlementId Yonder = new SettlementId(2UL);
        private static readonly SettlementId Delve = new SettlementId(3UL);

        private static List<NpcSeedRecord> Npcs()
        {
            var faction = new FactionId(1UL);
            return new List<NpcSeedRecord>
            {
                new NpcSeedRecord(new NpcId(10UL), Here, faction, "Maren the Trader", 950, NpcRole.Merchant),
                new NpcSeedRecord(new NpcId(11UL), Here, faction, "Father Olun", 940, NpcRole.Priest),
                new NpcSeedRecord(new NpcId(12UL), Here, faction, "Idle Farmhand", 955, NpcRole.Farmer),
                new NpcSeedRecord(new NpcId(20UL), Yonder, faction, "Sira of Yonder", 948, NpcRole.Innkeeper),
                new NpcSeedRecord(new NpcId(30UL), Delve, faction, "Gnash the Cutpurse", 952, NpcRole.Outlaw),
            };
        }

        private static List<OverlandSettlement> Settlements()
        {
            return new List<OverlandSettlement>
            {
                new OverlandSettlement(Here, SettlementKind.Town, new GridPosition(4, 4), "Hearthome", "town"),
                new OverlandSettlement(Yonder, SettlementKind.Village, new GridPosition(9, 4), "Yonderbrook", "village"),
                new OverlandSettlement(Delve, SettlementKind.Dungeon, new GridPosition(7, 9), "Gloomwarren", "dungeon"),
            };
        }

        [Test]
        public void Generate_TwentySeeds_TwentyValidQuests()
        {
            var npcs = Npcs();
            var settlements = Settlements();
            for (ulong seed = 100; seed < 120; seed++)
            {
                var quest = WorldQuestGenerator.Generate(npcs, settlements, Here, currentDay: 10, seed);
                Assert.That(quest, Is.Not.Null, $"seed {seed} must mint a quest");
                Assert.That(quest.GiverName, Is.Not.Empty, $"seed {seed}: giver");
                Assert.That(quest.RewardGold, Is.GreaterThan(0), $"seed {seed}: reward");
                Assert.That(quest.DeadlineDay, Is.GreaterThan(10), $"seed {seed}: deadline in the future");
                Assert.That(quest.Title, Is.Not.Empty, $"seed {seed}: title");
                switch (quest.Template)
                {
                    case WorldQuestTemplate.Fetch:
                        Assert.That(quest.ItemTemplateId, Is.Not.Empty);
                        Assert.That(quest.TargetSettlementId, Is.EqualTo(Here));
                        break;
                    case WorldQuestTemplate.Kill:
                        Assert.That(quest.TargetNpcName, Is.EqualTo("Gnash the Cutpurse"));
                        break;
                    case WorldQuestTemplate.Deliver:
                        Assert.That(quest.ItemTemplateId, Is.Not.Empty);
                        Assert.That(quest.TargetSettlementId, Is.Not.EqualTo(Here));
                        break;
                    case WorldQuestTemplate.Visit:
                        Assert.That(quest.TargetSettlementId, Is.Not.EqualTo(Here));
                        break;
                }
            }
        }

        [Test]
        public void Generate_SameSeed_SameQuest()
        {
            var a = WorldQuestGenerator.Generate(Npcs(), Settlements(), Here, 10, 4242UL);
            var b = WorldQuestGenerator.Generate(Npcs(), Settlements(), Here, 10, 4242UL);
            Assert.That(a.Title, Is.EqualTo(b.Title));
            Assert.That(a.Template, Is.EqualTo(b.Template));
            Assert.That(a.RewardGold, Is.EqualTo(b.RewardGold));
            Assert.That(a.DeadlineDay, Is.EqualTo(b.DeadlineDay));
        }

        [Test]
        public void Generate_SeedSweep_ReachesAllFourTemplates()
        {
            var seen = new HashSet<WorldQuestTemplate>();
            for (ulong seed = 0; seed < 40; seed++)
                seen.Add(WorldQuestGenerator.Generate(Npcs(), Settlements(), Here, 10, seed).Template);
            Assert.That(seen.Count, Is.EqualTo(4), "fetch/kill/deliver/visit must all be reachable");
        }

        [Test]
        public void Generate_ForcedTemplate_HonorsIt()
        {
            var quest = WorldQuestGenerator.Generate(
                Npcs(), Settlements(), Here, 10, 7UL, WorldQuestTemplate.Fetch);
            Assert.That(quest.Template, Is.EqualTo(WorldQuestTemplate.Fetch));
        }

        [Test]
        public void Generate_NoGiversInTown_ReturnsNull()
        {
            var onlyOutlaws = new List<NpcSeedRecord>
            {
                new NpcSeedRecord(new NpcId(30UL), Here, new FactionId(1UL), "Gnash", 952, NpcRole.Outlaw),
            };
            Assert.That(WorldQuestGenerator.Generate(onlyOutlaws, Settlements(), Here, 10, 1UL), Is.Null);
        }
    }
}
