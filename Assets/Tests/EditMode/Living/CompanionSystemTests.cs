using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Living.Actions;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// V3 YOLDAŞ — TDD first. Companions are STATE, not a new role: recruited civilians keep
    /// their identity (and their memories — the dialogue pipeline already recalls them), but
    /// follow the player, stand with them in danger, and leave when dismissed.
    /// </summary>
    public sealed class CompanionSystemTests
    {
        private static ActorRecord Actor(
            ulong id,
            string name,
            ActorRole role,
            GridPosition position,
            int health = 30,
            int accuracy = 60,
            int baseDamage = 3)
        {
            return new ActorRecord(
                new ActorId(id), name, role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(health, health), new VitalStat(10, 10), new VitalStat(10, 10)),
                position, accuracy: accuracy, dodge: 10, armor: 0, baseDamage: baseDamage);
        }

        private static WorldState World(out ActorRecord player, out ActorRecord friend)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(60);
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(10, 10)));
            player = Actor(1, "Warden", ActorRole.Player, new GridPosition(5, 5));
            friend = Actor(2, "Fenn", ActorRole.Talker, new GridPosition(6, 5),
                accuracy: 100, baseDamage: 20);
            world.Actors.Add(player);
            world.Actors.Add(friend);
            // The canonical world mapper has legacy role slots in addition to the actor array.
            // Keep them far away so they satisfy the save contract without entering these stories.
            world.Actors.Add(Actor(3, "Trader", ActorRole.Merchant, new GridPosition(100, 100)));
            world.Actors.Add(Actor(4, "Watch", ActorRole.Guard, new GridPosition(200, 200)));
            world.Actors.Add(Actor(5, "Distant foe", ActorRole.Enemy, new GridPosition(300, 300)));
            return world;
        }

        private static ActionLifecycleSystem Lifecycle()
            => new ActionLifecycleSystem(new ActionLogManager(), enableGuardAndCombat: true);

        private static void Tick(
            WorldState world,
            ActionLifecycleSystem lifecycle,
            long minute)
        {
            world.Time = new GameTime(minute);
            lifecycle.Decide(world, world.Time);
            lifecycle.Advance(world, world.Time);
        }

        [Test]
        public void TryRecruit_NearbyCivilian_JoinsAndEmitsEvent()
        {
            var world = World(out _, out var friend);

            bool joined = CompanionService.TryRecruit(world, friend.Id);

            Assert.That(joined, Is.True);
            Assert.That(world.CompanionIds, Does.Contain(friend.Id));
            Assert.That(world.Events.Events.Any(e =>
                e.Kind == WorldEventKind.ActorTalked && e.Reason.StartsWith("companion_joined")), Is.True,
                "recruitment is a story beat — it must be logged");
        }

        [Test]
        public void TryRecruit_BeyondReachOrOverCap_IsRefused()
        {
            var world = World(out _, out var friend);
            friend.MoveTo(new GridPosition(20, 20)); // out of recruiting reach
            Assert.That(CompanionService.TryRecruit(world, friend.Id), Is.False, "too far to ask");

            friend.MoveTo(new GridPosition(6, 5));
            for (ulong extra = 10; extra < 10 + CompanionService.MaxCompanions; extra++)
            {
                var filler = Actor(extra, $"F{extra}", ActorRole.Talker, new GridPosition(5, 6));
                world.Actors.Add(filler);
                Assert.That(CompanionService.TryRecruit(world, filler.Id), Is.True);
            }
            Assert.That(CompanionService.TryRecruit(world, friend.Id), Is.False, "the party is full");
        }

        [Test]
        public void FollowAction_CompanionLagsBehind_PersistsAndMovesOncePerTick()
        {
            var world = World(out var player, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            player.MoveTo(new GridPosition(15, 5)); // player walked off
            var lifecycle = Lifecycle();

            Tick(world, lifecycle, 60);

            global::EmberCrpg.Tests.EditMode.TestAssert.Multiple(() =>
            {
                Assert.That(friend.Position, Is.EqualTo(new GridPosition(7, 5)),
                    "the canonical advancer owns exactly one autonomous movement step");
                Assert.That(friend.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.FollowPlayer));
                Assert.That(friend.ActionState.CurrentAction, Is.EqualTo(ActorActionType.FollowPlayer));
                Assert.That(friend.ActionState.Phase, Is.EqualTo(ActionPhase.Running));
                Assert.That(friend.ActionState.ProgressTicks, Is.EqualTo(1));
                Assert.That(ActionVerbTable.Verb(friend.ActionState.CurrentAction), Is.EqualTo("following"));
            });

            Tick(world, lifecycle, 61);
            Assert.That(friend.Position, Is.EqualTo(new GridPosition(8, 5)),
                "follow remains persistent instead of being recomputed by a second manager");
        }

        [Test]
        public void FollowDecision_CompanionAtHeel_HoldsPositionAndStaysIdle()
        {
            var world = World(out _, out var friend);
            CompanionService.TryRecruit(world, friend.Id); // adjacent (Chebyshev 1)

            Tick(world, Lifecycle(), 60);

            Assert.That(friend.Position, Is.EqualTo(new GridPosition(6, 5)), "no jitter at heel range");
            Assert.That(friend.ActionState.IsIdle, Is.True, "at heel opens no completion-spam action");
        }

        [Test]
        public void GuardAction_EnemyBesideThePlayer_UsesStrikeResolverAndCompletes()
        {
            var world = World(out _, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            var wolf = Actor(9, "Wolf", ActorRole.Enemy, new GridPosition(5, 5), health: 1);
            world.Actors.Add(wolf);
            var lifecycle = Lifecycle();

            Tick(world, lifecycle, 60);  // decide guard; Hunt reaches adjacency
            Tick(world, lifecycle, 61);  // hand over to StrikeQuarry
            Tick(world, lifecycle, 120); // canonical hourly strike
            Tick(world, lifecycle, 121); // consume successful terminal handover

            global::EmberCrpg.Tests.EditMode.TestAssert.Multiple(() =>
            {
                Assert.That(world.Events.Events.Any(e =>
                    e.Kind == WorldEventKind.CombatResolved && e.ActorId.Equals(friend.Id)), Is.True,
                    "guard combat crosses StrikeQuarry -> CombatOperations -> resolver");
                Assert.That(wolf.IsAlive, Is.False);
                Assert.That(world.HuntTargets.Any(row => row.HunterId == friend.Id.Value), Is.False);
                Assert.That(friend.ActionState.IsIdle, Is.True,
                    "a resolved guard target completes rather than leaving a dangling relationship");
            });
        }

        [Test]
        public void FollowAction_DismissedCompanion_FailsInterruptedWithoutMovingAgain()
        {
            var world = World(out var player, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            player.MoveTo(new GridPosition(15, 5));
            var lifecycle = Lifecycle();
            Tick(world, lifecycle, 60);
            var beforeDismissedAdvance = friend.Position;

            Assert.That(CompanionService.TryDismiss(world, friend.Id), Is.True);
            Tick(world, lifecycle, 61);

            global::EmberCrpg.Tests.EditMode.TestAssert.Multiple(() =>
            {
                Assert.That(friend.Position, Is.EqualTo(beforeDismissedAdvance));
                Assert.That(friend.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
                Assert.That(friend.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Interrupted));
            });
        }

        [Test]
        public void GuardAction_DismissedCompanion_CleansTargetAndFailsInterrupted()
        {
            var world = World(out _, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            world.Actors.Add(Actor(9, "Wolf", ActorRole.Enemy, new GridPosition(8, 5)));
            var lifecycle = Lifecycle();
            Tick(world, lifecycle, 61);
            Assert.That(friend.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.GuardCompanion));
            Assert.That(world.HuntTargets.Count(row => row.HunterId == friend.Id.Value), Is.EqualTo(1));

            CompanionService.TryDismiss(world, friend.Id);
            Tick(world, lifecycle, 62);

            global::EmberCrpg.Tests.EditMode.TestAssert.Multiple(() =>
            {
                Assert.That(friend.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
                Assert.That(friend.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Interrupted));
                Assert.That(world.HuntTargets.Any(row => row.HunterId == friend.Id.Value), Is.False);
            });
        }

        [Test]
        public void ActiveEatAction_BlocksCompanionFollowMovementInTheSameTick()
        {
            var world = World(out var player, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            player.MoveTo(new GridPosition(20, 5));
            var pile = new StockpileComponent(new SiteId(1));
            pile.Add("wheat", 1);
            world.Stockpiles.Add(pile);
            Assert.That(world.Reservations.TryReserve(
                1UL, "wheat", friend.Id.Value, 999L, 1, out var reservationId), Is.True);
            friend.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.MoveToFood, new SiteId(1), ItemId.Empty,
                new ReservationId(reservationId), 60L, ActionInterruptPolicy.Interruptible));
            var before = friend.Position;

            Tick(world, Lifecycle(), 60);

            global::EmberCrpg.Tests.EditMode.TestAssert.Multiple(() =>
            {
                Assert.That(friend.Position.ChebyshevDistanceTo(before), Is.EqualTo(1),
                    "the real Eat advancer owns the only autonomous movement step");
                Assert.That(friend.Position, Is.Not.EqualTo(new GridPosition(before.X + 1, before.Y)),
                    "the removed follow path cannot add its playerward step");
                Assert.That(friend.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.Eat));
                Assert.That(friend.ActionState.CurrentAction, Is.EqualTo(ActorActionType.MoveToFood));
            });
        }

        [Test]
        public void DecisionSweep_CompanionDied_LeavesThePartyWithAFallenEvent()
        {
            // M2: death is a story beat, not a silent list entry — the party shrinks and the
            // log carries the loss.
            var world = World(out _, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = friend.Id.Value,
                TargetId = 5UL,
                UntilMinutes = 999L,
            });
            friend.ApplyVitals(new ActorVitals(
                new VitalStat(0, friend.Vitals.Health.Max), friend.Vitals.Fatigue, friend.Vitals.Mana));

            Lifecycle().Decide(world, new GameTime(60));

            Assert.That(world.CompanionIds, Is.Empty, "the fallen leave the roster");
            Assert.That(world.HuntTargets.Any(row => row.HunterId == friend.Id.Value), Is.False,
                "a dead companion cannot leave a guard target claim behind");
            Assert.That(world.Events.Events.Any(e => e.Reason.StartsWith("companion_fell")), Is.True);
        }

        [Test]
        public void TryDismiss_Companion_LeavesAndEmitsEvent()
        {
            var world = World(out _, out var friend);
            CompanionService.TryRecruit(world, friend.Id);

            Assert.That(CompanionService.TryDismiss(world, friend.Id), Is.True);
            Assert.That(world.CompanionIds, Is.Empty);
            Assert.That(world.Events.Events.Any(e => e.Reason.StartsWith("companion_left")), Is.True);
        }

        [Test]
        public void MidFollow_SaveLoad_PreservesPersistentActionAndProjectionVerb()
        {
            var world = World(out var player, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            player.MoveTo(new GridPosition(15, 5));
            Tick(world, Lifecycle(), 60);

            var loaded = WorldSaveMapper.ToWorld(
                WorldSaveMapper.ToData(world),
                SeedWorld());
            Assert.That(loaded.Actors.TryGet(friend.Id, out var restored), Is.True);

            global::EmberCrpg.Tests.EditMode.TestAssert.Multiple(() =>
            {
                Assert.That(loaded.CompanionIds, Does.Contain(friend.Id));
                Assert.That(restored.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.FollowPlayer));
                Assert.That(restored.ActionState.CurrentAction, Is.EqualTo(ActorActionType.FollowPlayer));
                Assert.That(restored.ActionState.Phase, Is.EqualTo(ActionPhase.Running));
                Assert.That(restored.ActionState.ProgressTicks, Is.EqualTo(friend.ActionState.ProgressTicks));
                Assert.That(ActionVerbTable.Verb(restored.ActionState.CurrentAction), Is.EqualTo("following"));
            });
        }

        [Test]
        public void MidGuard_SaveLoad_PreservesIntentTargetAndDeterministicContinuation()
        {
            var world = World(out _, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            var wolf = Actor(9, "Wolf", ActorRole.Enemy, new GridPosition(3, 5));
            world.Actors.Add(wolf);
            var lifecycle = Lifecycle();
            Tick(world, lifecycle, 61); // off-cadence: running Hunt, no movement/progress

            var loaded = WorldSaveMapper.ToWorld(
                WorldSaveMapper.ToData(world),
                SeedWorld());
            Assert.That(loaded.Actors.TryGet(friend.Id, out var restored), Is.True);

            global::EmberCrpg.Tests.EditMode.TestAssert.Multiple(() =>
            {
                Assert.That(restored.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.GuardCompanion));
                Assert.That(restored.ActionState.CurrentAction, Is.EqualTo(ActorActionType.Hunt));
                Assert.That(restored.ActionState.Phase, Is.EqualTo(ActionPhase.Running));
                Assert.That(loaded.HuntTargets.Count(row => row.HunterId == friend.Id.Value), Is.EqualTo(1));
                Assert.That(loaded.HuntTargets.Single(row => row.HunterId == friend.Id.Value).TargetId,
                    Is.EqualTo(wolf.Id.Value));
            });

            lifecycle.Advance(world, new GameTime(120));
            Lifecycle().Advance(loaded, new GameTime(120));
            Assert.That(restored.Position, Is.EqualTo(friend.Position),
                "the first restored guard movement is deterministic");
            Assert.That(restored.ActionState.ProgressTicks, Is.EqualTo(friend.ActionState.ProgressTicks));
        }

        private static WorldState SeedWorld()
        {
            var seed = new WorldState();
            seed.EnsureInvariants();
            return seed;
        }
    }
}
