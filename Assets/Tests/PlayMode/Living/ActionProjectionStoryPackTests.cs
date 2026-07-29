using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EmberCrpg.Tests.PlayMode.Living
{
    /// <summary>
    /// Small runtime lane for the recovered action spine. Each story advances through the
    /// production composer, then crosses the real adapter projection boundary.
    /// </summary>
    public sealed class ActionProjectionStoryPackTests
    {
        private static readonly SiteId Site = new SiteId(1UL);

        [UnityTest]
        public IEnumerator EatAndFarm_ProjectTheirRunningActionsVerbatim()
        {
            var eatWorld = World(SiteKind.Settlement, 60);
            var larder = new StockpileComponent(Site);
            larder.Add("wheat", 10);
            eatWorld.Stockpiles.Add(larder);
            var diner = Actor(7, "Diner", ActorRole.Talker, new GridPosition(20, 20));
            diner.ApplyNeeds(diner.Needs.WithHunger(new NeedValue(80)));
            eatWorld.Actors.Add(diner);

            var idle = Actor(8, "Idle", ActorRole.Talker, new GridPosition(6, 6));
            eatWorld.Actors.Add(idle);
            AssertActionlessProjection(eatWorld, idle);

            var eatComposer = new WorldTickComposer();
            int eatTick = RunUntil(eatWorld, eatComposer, diner, ActorActionType.ConsumeFood, 200);
            AssertProjected(eatWorld, diner, ActorActionType.ConsumeFood);
            for (int tick = eatTick + 1; tick <= eatTick + 5; tick++)
                eatComposer.Advance(eatWorld, tick);
            Assert.That(eatWorld.Events.Events.Any(e =>
                e.Kind == WorldEventKind.ActionCompleted && e.ActorId.Equals(diner.Id)), Is.True);

            var farmWorld = World(SiteKind.Region, 6 * GameTime.MinutesPerHour);
            farmWorld.Stockpiles.Add(new StockpileComponent(Site));
            var soilId = new WorldComponentId(101UL);
            var plantId = new WorldComponentId(500_101UL);
            farmWorld.Soils.Add(soilId, new SoilComponent(
                soilId, Site, new GridPosition(0, 0), 50, 50, plantId));
            farmWorld.Plants.Add(plantId, new PlantComponent(
                plantId, Site, new GridPosition(0, 0), "wheat", new PlantStageId("ripe"), 0));
            farmWorld.Worksites.Add(new WorksiteRecord(
                Site, new GridPosition(0, 0), WorksiteKind.Field, true));
            var farmer = Actor(
                9, "Farmer", ActorRole.Talker, new GridPosition(9, 9),
                new[] { new ActorJobPreference(JobKind.Farmer, JobPriority.Active(1)) });
            farmWorld.Actors.Add(farmer);
            farmWorld.Jobs.Add(FarmingJobRequestFactory.CreateHarvestJob(
                new JobId(9001UL), Site, new GridPosition(0, 0),
                new ActorId(999UL), JobPriority.Active(1)));

            RunUntil(farmWorld, new WorldTickComposer(), farmer, ActorActionType.HarvestCrop, 200);
            AssertProjected(farmWorld, farmer, ActorActionType.HarvestCrop);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SleepAndWork_ProjectTheirRunningActionsVerbatim()
        {
            var sleepWorld = World(SiteKind.Settlement, 21 * GameTime.MinutesPerHour);
            sleepWorld.Stockpiles.Add(new StockpileComponent(Site));
            var sleeper = Actor(
                11, "Sleeper", ActorRole.Talker, new GridPosition(9, 9),
                home: new GridPosition(2, 2));
            sleeper.ApplyNeeds(sleeper.Needs.WithFatigue(new NeedValue(80)));
            sleepWorld.Actors.Add(sleeper);

            RunUntil(sleepWorld, new WorldTickComposer(), sleeper, ActorActionType.Sleep, 180);
            AssertProjected(sleepWorld, sleeper, ActorActionType.Sleep);

            var workWorld = World(SiteKind.Region, 6 * GameTime.MinutesPerHour);
            var pile = new StockpileComponent(Site);
            pile.Add("iron_ore", 2);
            pile.Add("fuel", 1);
            workWorld.Stockpiles.Add(pile);
            var bench = new GridPosition(4, 5);
            workWorld.Worksites.Add(new WorksiteRecord(Site, bench, WorksiteKind.Furnace, true));
            var smith = Actor(
                12, "Smith", ActorRole.Talker, new GridPosition(9, 9),
                new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });
            workWorld.Actors.Add(smith);
            workWorld.Jobs.Add(new JobRequest(
                new JobId(9101UL), new RecipeId(1001UL), Site, bench,
                WorksiteKind.Furnace, JobKind.Smith, JobPriority.Active(1),
                1, new ActorId(999UL)));

            RunUntil(workWorld, new WorldTickComposer(), smith, ActorActionType.PerformWork, 200);
            AssertProjected(workWorld, smith, ActorActionType.PerformWork);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompanionAndReport_ProjectTheirRunningActionsVerbatim()
        {
            var companionWorld = World(SiteKind.Settlement, 60, 30);
            var player = Actor(1, "Warden", ActorRole.Player, new GridPosition(5, 5));
            var friend = Actor(2, "Fenn", ActorRole.Talker, new GridPosition(6, 5));
            companionWorld.Actors.Add(player);
            companionWorld.Actors.Add(friend);
            AddDistantRoleSlots(companionWorld, firstId: 20);
            Assert.That(CompanionService.TryRecruit(companionWorld, friend.Id), Is.True);
            player.MoveTo(new GridPosition(15, 5));

            RunUntil(companionWorld, new WorldTickComposer(), friend, ActorActionType.FollowPlayer, 5);
            AssertProjected(companionWorld, friend, ActorActionType.FollowPlayer);

            var reportWorld = World(SiteKind.Settlement, 0, 30);
            var attacker = Actor(31, "Hound", ActorRole.Enemy, new GridPosition(0, 0));
            var witness = Actor(32, "Witness", ActorRole.Talker, new GridPosition(8, 0));
            reportWorld.Actors.Add(attacker);
            reportWorld.Actors.Add(witness);
            reportWorld.Actors.Add(Actor(33, "Watch", ActorRole.Guard, new GridPosition(10, 0)));
            reportWorld.Actors.Add(Actor(34, "Player", ActorRole.Player, new GridPosition(100, 100)));
            reportWorld.Actors.Add(Actor(35, "Trader", ActorRole.Merchant, new GridPosition(100, 101)));

            var reportComposer = new WorldTickComposer();
            reportComposer.Advance(reportWorld, 0);
            reportComposer.Advance(reportWorld, 1);
            reportWorld.Events.Append(new WorldEvent(
                reportWorld.Time, WorldEventKind.CombatResolved, attacker.Id, Site, "maul hits"));
            reportComposer.Advance(reportWorld, 60);
            reportComposer.Advance(reportWorld, 61);

            AssertProjected(reportWorld, witness, ActorActionType.ReportCrime);
            Assert.That(reportWorld.GuardPursuits.Any(p => p.TargetId == attacker.Id.Value), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PursuitAndHunt_ProjectTheirRunningActionsVerbatim()
        {
            var world = World(SiteKind.Settlement, 60, 30);
            var guard = Actor(41, "Watch", ActorRole.Guard, new GridPosition(0, 0));
            var hunter = Actor(42, "Hound", ActorRole.Enemy, new GridPosition(8, 0));
            var prey = Actor(43, "Villager", ActorRole.Talker, new GridPosition(14, 0));
            world.Actors.Add(guard);
            world.Actors.Add(hunter);
            world.Actors.Add(prey);
            world.Actors.Add(Actor(44, "Player", ActorRole.Player, new GridPosition(100, 100)));
            world.Actors.Add(Actor(45, "Trader", ActorRole.Merchant, new GridPosition(100, 101)));
            world.GuardPursuits.Add(new PursuitRecord
            {
                GuardId = guard.Id.Value,
                TargetId = hunter.Id.Value,
                UntilMinutes = 600,
            });

            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            composer.Advance(world, 1);

            var adapter = new DomainSimulationAdapter(world);
            AssertProjected(adapter, guard, ActorActionType.Pursue);
            AssertProjected(adapter, hunter, ActorActionType.Hunt);
            yield return null;
        }

        private static WorldState World(SiteKind kind, long minutes, int max = 10)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(minutes);
            world.Sites.Add(new SiteRecord(
                Site, kind, "Story Site", new GridPosition(0, 0), new GridPosition(max, max)));
            return world;
        }

        private static ActorRecord Actor(
            ulong id,
            string name,
            ActorRole role,
            GridPosition position,
            IEnumerable<ActorJobPreference> jobs = null,
            GridPosition? home = null)
        {
            return new ActorRecord(
                new ActorId(id), name, role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(10, 10), new VitalStat(10, 10)),
                position, accuracy: 50, dodge: 10, armor: 0, baseDamage: 2,
                jobPreferences: jobs, home: home);
        }

        private static void AddDistantRoleSlots(WorldState world, ulong firstId)
        {
            world.Actors.Add(Actor(firstId, "Trader", ActorRole.Merchant, new GridPosition(100, 100)));
            world.Actors.Add(Actor(firstId + 1, "Watch", ActorRole.Guard, new GridPosition(200, 200)));
            world.Actors.Add(Actor(firstId + 2, "Foe", ActorRole.Enemy, new GridPosition(300, 300)));
        }

        private static int RunUntil(
            WorldState world,
            WorldTickComposer composer,
            ActorRecord actor,
            ActorActionType expected,
            int maxTicks)
        {
            composer.Advance(world, 0);
            for (int tick = 1; tick <= maxTicks; tick++)
            {
                composer.Advance(world, tick);
                if (actor.ActionState.CurrentAction == expected)
                    return tick;
            }
            Assert.Fail($"Actor {actor.Id.Value} never reached {expected} within {maxTicks} ticks.");
            return -1;
        }

        private static void AssertProjected(
            WorldState world,
            ActorRecord actor,
            ActorActionType expected)
            => AssertProjected(new DomainSimulationAdapter(world), actor, expected);

        private static void AssertProjected(
            DomainSimulationAdapter adapter,
            ActorRecord actor,
            ActorActionType expected)
        {
            Assert.That(actor.ActionState.CurrentAction, Is.EqualTo(expected));
            Assert.That(adapter.TryReadActor(actor.Id, out var projected), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(projected.ActionKind, Is.EqualTo(ActionVerbTable.KindName(expected)));
                Assert.That(projected.Activity, Is.EqualTo(ActionVerbTable.Verb(expected)));
            });
        }

        private static void AssertActionlessProjection(WorldState world, ActorRecord actor)
        {
            Assert.That(actor.ActionState.CurrentAction, Is.EqualTo(ActorActionType.None));
            var adapter = new DomainSimulationAdapter(world);
            Assert.That(adapter.TryReadActor(actor.Id, out var projected), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(projected.ActionKind, Is.Null);
                Assert.That(projected.Activity, Is.Null);
            });
        }
    }
}
