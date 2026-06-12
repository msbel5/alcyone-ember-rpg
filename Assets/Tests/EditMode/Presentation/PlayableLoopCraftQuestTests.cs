#if UNITY_EDITOR
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Quest;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    public sealed class PlayableLoopCraftQuestTests
    {
        [Test]
        public void ForgeQuest_StartsFromNpc_CraftsThenCompletesOnDelivery()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            var smith = new NpcSeedRecord(
                new NpcId(7UL),
                new SettlementId(1UL),
                new FactionId(1UL),
                "Ada the Smith",
                980,
                NpcRole.Blacksmith);
            world.NpcSeeds.Add(smith);
            var adapter = new DomainSimulationAdapter(world);
            var source = adapter.GetDialogSource("Ada the Smith");

            Assert.That(world.Quests.Contains(QuestCatalog.ForgeIronIngotId), Is.False);
            Assert.That(world.PlayerInventory.Contains("iron_ore"), Is.False);
            Assert.That(source.GetTopics(), Does.Contain(QuestInteractionService.ForgeIronIngotTopicId));

            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);

            Assert.That(world.Quests.Contains(QuestCatalog.ForgeIronIngotId), Is.True);
            Assert.That(world.PlayerInventory.Contains("iron_ore"), Is.True);
            Assert.That(world.PlayerInventory.Contains("fuel"), Is.True);

            var result = ((ICraftingCommandSink)adapter).ExecuteCraft("1001");
            var chapters = ((IJournalSource)adapter).GetChapters();

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(chapters.Count, Is.EqualTo(1));
            Assert.That(chapters[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Active));
            Assert.That(chapters[0].Entries[0].Body, Does.Contain("Return"));

            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);
            chapters = ((IJournalSource)adapter).GetChapters();

            Assert.That(chapters[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Completed));
            Assert.That(chapters[0].Entries[0].Body, Does.Contain("delivered"));
            Assert.That(world.PlayerInventory.Contains("iron_ingot"), Is.False);
            Assert.That(((IQuestGuidanceSource)adapter).ReadQuestGuidance().HasTarget, Is.False);
        }

        [Test]
        public void ForgeQuest_PreexistingIngot_DoesNotCompleteWithoutPostQuestCraft()
        {
            var world = new WorldFactory().Create(roomSeed: 21);
            var smith = new NpcSeedRecord(
                new NpcId(11UL),
                new SettlementId(1UL),
                new FactionId(1UL),
                "Toma the Smith",
                984,
                NpcRole.Blacksmith);
            world.NpcSeeds.Add(smith);
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(900UL), "iron_ingot", "Iron Ingot", 1));
            var adapter = new DomainSimulationAdapter(world);
            var source = adapter.GetDialogSource("Toma the Smith");

            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);
            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);

            var chapters = ((IJournalSource)adapter).GetChapters();
            Assert.That(chapters[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Active));
            Assert.That(Count(world.PlayerInventory, "iron_ingot"), Is.EqualTo(1));

            Assert.That(((ICraftingCommandSink)adapter).ExecuteCraft("1001").Success, Is.True);
            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);

            Assert.That(Count(world.PlayerInventory, "iron_ingot"), Is.EqualTo(1));
            Assert.That(((IJournalSource)adapter).GetChapters()[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Completed));
        }

        // F9 ("zindanı bulamadım"): the delve pointer must exist on a FRESH seeded world and must stay
        // visible while the forge quest is still active — v0.2 hid it behind quest completion.
        [Test]
        public void DelveGuidance_AvailableOnFreshWorld_AndIndependentOfQuestState()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            var adapter = new DomainSimulationAdapter(world);
            adapter.SeedWorld("grim", "survival", "crossroads", 7u);

            var fresh = ((IQuestGuidanceSource)adapter).ReadDelveGuidance();
            Assert.That(fresh.HasTarget, Is.True, "fresh world must already point at a delve");
            Assert.That(fresh.Title, Is.EqualTo("Delve Lead"));
            Assert.That(fresh.TargetName, Is.Not.Empty);

            world.Quests.Add(QuestCatalog.ForgeIronIngotId, new QuestState(1, world.Time));
            var during = ((IQuestGuidanceSource)adapter).ReadDelveGuidance();
            Assert.That(during.HasTarget, Is.True, "active forge quest must not hide the delve pointer");
            Assert.That(during.TargetName, Is.EqualTo(fresh.TargetName));
        }

        // F9 root-cause guard: the EXACT world the proof harness (and a default New Game) seeds — answer
        // tuple (grim, wanderer, crossroads), derived seed — must contain a Dungeon settlement. Dungeon
        // kind only rolls from small Mountain/Ash/Swamp placements, so a temperate planet shipped ZERO
        // delves until the EnsureAtLeastOneDungeon worldgen invariant; this pins that invariant.
        [Test]
        public void DelveGuidance_DefaultNewGameWorld_AlwaysHasADungeon()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            var adapter = new DomainSimulationAdapter(world);
            adapter.SeedWorld("grim", "wanderer", "crossroads", null);

            var row = ((IQuestGuidanceSource)adapter).ReadDelveGuidance();
            Assert.That(row.HasTarget, Is.True, "every generated world must contain at least one delve");
        }

        // F14 ("düşman kovalasın"): a hostile that SEES the player closes one grid cell per AI cadence
        // and AUTO-BINDS the encounter at aggro range — combat starts because you got close, not E.
        [Test]
        public void HostileAi_ChasesPlayer_AndAutoBindsInAggroRange()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            var adapter = new DomainSimulationAdapter(world);
            adapter.SeedWorld("grim", "survival", "crossroads", 7u);
            var delve = ((IQuestGuidanceSource)adapter).ReadDelveGuidance();
            Assert.That(delve.HasTarget, Is.True);
            Assert.That(adapter.TryTravelToSettlement(delve.TargetName, out _), Is.True);
            // Headless travel may leave the billboard origin un-hydrated (no realize), so the origin and
            // the site centre can disagree — place the haunters RELATIVE to the player via the public
            // origin hook: grid = origin + round(spot) must land exactly player + (8,0).
            var player = world.Actors.FirstByRole(ActorRole.Player);
            var origin = adapter.BillboardOriginCell();
            Assert.That(adapter.EnsureDungeonHaunters(
                new UnityEngine.Vector3(player.Position.X - origin.X + 8, 0f, player.Position.Y - origin.Y),
                new UnityEngine.Vector3(player.Position.X - origin.X + 9, 0f, player.Position.Y - origin.Y + 2)),
                Is.EqualTo(2));
            ActorRecord haunter = null;
            foreach (var a in world.Actors.Records)
                if (a != null && a.Name.StartsWith("Haunter of")) { haunter = a; break; }
            Assert.That(haunter, Is.Not.Null);
            int Cheb() => System.Math.Max(
                System.Math.Abs(haunter.Position.X - player.Position.X),
                System.Math.Abs(haunter.Position.Y - player.Position.Y));

            int d0 = Cheb();
            Assert.That(d0, Is.GreaterThan(3).And.LessThanOrEqualTo(12), "setup: haunter starts in sight");

            adapter.TickHostileAi(10f);
            Assert.That(Cheb(), Is.EqualTo(d0 - 1), "one AI cadence closes exactly one cell");

            float t = 10f;
            for (int i = 0; i < 16 && Cheb() > 2; i++)
            {
                t += 0.5f;
                adapter.TickHostileAi(t);
            }
            Assert.That(Cheb(), Is.LessThanOrEqualTo(3), "the chase must reach melee reach");
            var combat = ((ICombatScreenSource)adapter).ReadCombatScreenState();
            Assert.That(combat.HasEncounter, Is.True, "aggro range must auto-bind the encounter");
            Assert.That(combat.EnemyName, Does.StartWith("Haunter of").Or.StartWith("Stalker of"));
        }

        // F15 ("ölüm bir duvar değil, bedel"): respawn 20% altın keser, canları doldurur, saati tam
        // 8 saat yürütür (saat saat — tek tick-sıçraması cadence atlatır) ve gövdeyi plazaya taşır.
        [Test]
        public void RespawnAfterDeath_TollsGold_RefillsVitals_AdvancesEightHours()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            var adapter = new DomainSimulationAdapter(world);
            adapter.SeedWorld("grim", "survival", "crossroads", 7u);

            var player = world.Actors.FirstByRole(ActorRole.Player);
            world.PlayerGold = 240;
            long minutesBefore = world.Time.TotalMinutes;
            player.ApplyVitals(new ActorVitals(
                new VitalStat(0, player.Vitals.Health.Max),
                new VitalStat(3, player.Vitals.Fatigue.Max),
                new VitalStat(1, player.Vitals.Mana.Max)));
            Assert.That(player.IsAlive, Is.False, "setup: the player must be dead");

            var line = adapter.RespawnAfterDeath();

            Assert.That(world.PlayerGold, Is.EqualTo(192), "20% of 240 = 48 gold toll");
            Assert.That(player.Vitals.Health.Current, Is.EqualTo(player.Vitals.Health.Max));
            Assert.That(player.Vitals.Fatigue.Current, Is.EqualTo(player.Vitals.Fatigue.Max));
            Assert.That(player.Vitals.Mana.Current, Is.EqualTo(player.Vitals.Mana.Max));
            // ≥8h, possibly one cadence-step over (the stepper lands on tick boundaries; never under).
            Assert.That(world.Time.TotalMinutes - minutesBefore, Is.InRange(8 * 60, 8 * 60 + 59), "8 hours pass");
            Assert.That(line, Does.Contain("awaken").IgnoreCase);
            // Dying twice must work too: the gate re-arms and the toll applies again.
            player.ApplyVitals(new ActorVitals(
                new VitalStat(0, player.Vitals.Health.Max), player.Vitals.Fatigue, player.Vitals.Mana));
            Assert.That(adapter.RespawnAfterDeath(), Does.Contain("awaken").IgnoreCase);
            Assert.That(world.PlayerGold, Is.EqualTo(154), "second toll: 192 - 38");
        }

        // F16 ("ekipman zara girsin"): same seed, same dice — the equipped weapon's bonuses must change
        // the outcome. The starting blade now ships EQUIPPED; the chest sword tier-ups and auto-equips.
        [Test]
        public void Equipment_BonusesEnterTheDice_AndChestSwordTiersUp()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            var adapter = new DomainSimulationAdapter(world);
            adapter.SeedWorld("grim", "survival", "crossroads", 7u);
            var player = world.Actors.FirstByRole(ActorRole.Player);

            // Starting kit equipped by default (the blade sat inert in the backpack since Sprint 1).
            var weaponId = world.PlayerEquipment.GetEquippedItemId(EquipmentSlot.Weapon);
            Assert.That(weaponId.IsEmpty, Is.False, "the starting blade must come equipped");
            Assert.That(world.PlayerInventory.FindById(weaponId).TemplateId,
                Is.EqualTo(WorldItemCatalog.AshTrainingBladeTemplateId));

            // Same seed, same target: armed damage must exceed bare damage (acc+5/dmg+2 vs 0/0).
            var resolver = new EmberCrpg.Simulation.Combat.CombatActionResolver(
                new EmberCrpg.Simulation.Combat.CombatHitRollService(),
                new EmberCrpg.Simulation.Combat.CombatDamageService());
            var action = new CombatActionDef(new CombatActionId("melee_swing"), 0, "h", "d", "a");
            var loadout = new WorldActorLoadoutFactory();
            var dummyA = loadout.Create(new ActorId(9001), "Dummy A", ActorRole.Enemy, new GridPosition(1, 1));
            var dummyB = loadout.Create(new ActorId(9002), "Dummy B", ActorRole.Enemy, new GridPosition(1, 2));
            int bare = 0, armed = 0;
            for (uint seed = 1; seed <= 60; seed++)
            {
                int beforeA = dummyA.Vitals.Health.Current;
                resolver.Resolve(action, player, dummyA, 3, new XorShiftRng(seed), world.Time,
                    new SiteId(1UL), world.Events);
                bare += beforeA - dummyA.Vitals.Health.Current;
                int beforeB = dummyB.Vitals.Health.Current;
                resolver.Resolve(action, player, dummyB, 3, new XorShiftRng(seed), world.Time,
                    new SiteId(1UL), world.Events, attackerAccuracyBonus: 5, attackerDamageBonus: 2);
                armed += beforeB - dummyB.Vitals.Health.Current;
                // keep the dummies alive so every seed contributes
                dummyA.ApplyVitals(new ActorVitals(dummyA.Vitals.Health.Refill(), dummyA.Vitals.Fatigue, dummyA.Vitals.Mana));
                dummyB.ApplyVitals(new ActorVitals(dummyB.Vitals.Health.Refill(), dummyB.Vitals.Fatigue, dummyB.Vitals.Mana));
            }
            Assert.That(armed, Is.GreaterThan(bare), "weapon bonuses must raise total damage over 60 seeded swings");

            // Chest loot: grants the tier-up sword once, auto-equips it, and refuses a second grab.
            var line = adapter.LootDungeonChest();
            Assert.That(line, Does.Contain("Worn Iron Sword").And.Contain("equip"));
            var newWeaponId = world.PlayerEquipment.GetEquippedItemId(EquipmentSlot.Weapon);
            Assert.That(world.PlayerInventory.FindById(newWeaponId).TemplateId,
                Is.EqualTo(WorldItemCatalog.WornIronSwordTemplateId), "the better sword auto-equips");
            Assert.That(adapter.LootDungeonChest(), Does.Contain("empty"), "the chest yields exactly one sword");
        }

        private static int Count(EmberCrpg.Domain.Inventory.InventoryState inventory, string templateId)
        {
            var total = 0;
            foreach (var item in inventory.Items)
                if (item.TemplateId == templateId)
                    total += item.Quantity;
            return total;
        }

        [Test]
        public void ForgeQuest_IsOfferedBySmithJobWorkerEvenWhenNpcRoleIsGeneric()
        {
            var world = new WorldFactory().Create(roomSeed: 18);
            var npcId = new NpcId(8UL);
            var actorId = new ActorId(10_000UL + npcId.Value);
            world.NpcSeeds.Add(new NpcSeedRecord(
                npcId,
                new SettlementId(1UL),
                new FactionId(1UL),
                "Mara the Worker",
                981,
                NpcRole.Farmer));
            world.Actors.Add(new ActorRecord(
                actorId,
                "Mara the Worker",
                ActorRole.Talker,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(30, 30), new VitalStat(20, 20)),
                new GridPosition(1, 1),
                accuracy: 35,
                dodge: 30,
                armor: 4,
                baseDamage: 4,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) }));
            var adapter = new DomainSimulationAdapter(world);

            var source = adapter.GetDialogSource(actorId);

            Assert.That(source.GetTopics(), Does.Contain(QuestInteractionService.ForgeIronIngotTopicId));
        }

        [Test]
        public void QuestGuidance_PointsToForgeQuestGiverBeforeJournalExists()
        {
            var world = new WorldFactory().Create(roomSeed: 19);
            var npcId = new NpcId(9UL);
            var actorId = new ActorId(10_000UL + npcId.Value);
            world.NpcSeeds.Add(new NpcSeedRecord(
                npcId,
                new SettlementId(1UL),
                new FactionId(1UL),
                "Bera the Smith",
                982,
                NpcRole.Farmer));
            world.Actors.Add(new ActorRecord(
                actorId,
                "Bera the Smith",
                ActorRole.Talker,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(30, 30), new VitalStat(20, 20)),
                new GridPosition(3, 1),
                accuracy: 35,
                dodge: 30,
                armor: 4,
                baseDamage: 4,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) }));
            var adapter = new DomainSimulationAdapter(world);

            var guidance = ((IQuestGuidanceSource)adapter).ReadQuestGuidance();

            Assert.That(world.Quests.Contains(QuestCatalog.ForgeIronIngotId), Is.False);
            Assert.That(guidance.HasTarget, Is.True);
            Assert.That(guidance.TargetName, Is.EqualTo("Bera the Smith"));
            Assert.That(guidance.Title, Is.EqualTo("Quest Lead"));
            Assert.That(guidance.Line, Does.Contain("forge work"));
        }

        [Test]
        public void QuestGuidance_TracksLivePlayerLocalPosition()
        {
            var world = new WorldFactory().Create(roomSeed: 20);
            var npcId = new NpcId(10UL);
            var actorId = new ActorId(10_000UL + npcId.Value);
            var targetPosition = new GridPosition(6, 4);
            world.NpcSeeds.Add(new NpcSeedRecord(
                npcId,
                new SettlementId(1UL),
                new FactionId(1UL),
                "Corin the Smith",
                983,
                NpcRole.Farmer));
            world.Actors.Add(new ActorRecord(
                actorId,
                "Corin the Smith",
                ActorRole.Talker,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(30, 30), new VitalStat(20, 20)),
                targetPosition,
                accuracy: 35,
                dodge: 30,
                armor: 4,
                baseDamage: 4,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) }));
            var adapter = new DomainSimulationAdapter(world);
            var origin = adapter.BillboardOriginCell();
            var tracker = (IQuestGuidanceTracker)adapter;

            tracker.UpdateQuestGuidancePlayerLocalPosition(new GridPosition(1 - origin.X, 4 - origin.Y));
            var far = ((IQuestGuidanceSource)adapter).ReadQuestGuidance();

            tracker.UpdateQuestGuidancePlayerLocalPosition(new GridPosition(6 - origin.X, 4 - origin.Y));
            var near = ((IQuestGuidanceSource)adapter).ReadQuestGuidance();

            Assert.That(far.HasTarget, Is.True);
            Assert.That(far.DistanceTiles, Is.EqualTo(5));
            Assert.That(far.Direction, Is.EqualTo("east"));
            Assert.That(near.HasTarget, Is.True);
            Assert.That(near.DistanceTiles, Is.EqualTo(0));
            Assert.That(near.Direction, Is.EqualTo("nearby"));
        }
    }
}
#endif
