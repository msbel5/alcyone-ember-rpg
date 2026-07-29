using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Living.Actions;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// P0 pin (ARCHITECTURE_GAPS #2): the watch CHASES. The witness report arms a pursuit,
    /// the PerTick schedule runs it at full speed, and expiry hands the guard back to its post.
    /// </summary>
    public sealed class GuardPursuitTests
    {
        private static ActorRecord Actor(
            ulong id,
            string name,
            ActorRole role,
            GridPosition position,
            int health = 30,
            int accuracy = 50,
            int baseDamage = 2)
        {
            return new ActorRecord(
                new ActorId(id), name, role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(health, health), new VitalStat(10, 10), new VitalStat(10, 10)),
                position, accuracy: accuracy, dodge: 10, armor: 0, baseDamage: baseDamage);
        }

        private static WorldState World()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(30, 30)));
            return world;
        }

        private static ActionLifecycleSystem Lifecycle()
            => new ActionLifecycleSystem(new ActionLogManager(), enableGuardAndCombat: true);

        private static void Tick(WorldState world, ActionLifecycleSystem lifecycle, long minute)
        {
            var stamp = new GameTime(minute);
            world.Time = stamp;
            lifecycle.Decide(world, stamp);
            lifecycle.Advance(world, stamp);
        }

        [Test]
        public void WitnessReport_ArmsAPursuit_ForGuardsInEarshot()
        {
            var world = World();
            var attacker = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 5));
            world.Actors.Add(attacker);
            world.Actors.Add(Actor(2, "Witness", ActorRole.Talker, new GridPosition(6, 5)));
            world.Actors.Add(Actor(3, "Watch", ActorRole.Guard, new GridPosition(7, 5)));

            var hour = new GameTime(60);
            world.Events.Append(new WorldEvent(hour, WorldEventKind.CombatResolved, attacker.Id, new SiteId(1), "maul hits"));
            new WitnessResponseSystem().Tick(world, hour);
            Tick(world, Lifecycle(), 60);

            Assert.That(world.GuardPursuits.Any(p => p.GuardId == 3UL && p.TargetId == 1UL), Is.True,
                "a guard within earshot must arm a chase, not just nudge one tile an hour");
        }

        [Test]
        public void PursueAction_ClosesAtMostOneStepPerTick_WithoutRubberBanding()
        {
            var world = World();
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(0, 0))
                .WithHomeAndAnchor(new GridPosition(0, 0), new GridPosition(0, 0));
            var quarry = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(8, 0))
                .WithHomeAndAnchor(new GridPosition(8, 0), new GridPosition(20, 0));
            world.Actors.Add(guard);
            world.Actors.Add(quarry);
            world.GuardPursuits.Add(new PursuitRecord { GuardId = 3UL, TargetId = 1UL, UntilMinutes = 600 });

            var lifecycle = Lifecycle();
            var previous = guard.Position;
            for (int tick = 1; tick <= 7; tick++)
            {
                Tick(world, lifecycle, 60 + tick);
                Assert.That(guard.Position.ChebyshevDistanceTo(previous), Is.LessThanOrEqualTo(1),
                    "event volume cannot multiply autonomous movement within one tick");
                previous = guard.Position;
            }

            int dist = System.Math.Max(
                System.Math.Abs(world.Actors.Get(new ActorId(3)).Position.X - world.Actors.Get(new ActorId(1)).Position.X),
                System.Math.Abs(world.Actors.Get(new ActorId(3)).Position.Y - world.Actors.Get(new ActorId(1)).Position.Y));
            Assert.That(dist, Is.LessThanOrEqualTo(2),
                "the persistent Pursue action owns the chase instead of the schedule router");
            Assert.That(guard.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.Pursue));
        }

        [Test]
        public void Advance_ExpiredPursuit_IsPruned_AndTheWatchGoesHome()
        {
            var world = World();
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(5, 5))
                .WithHomeAndAnchor(new GridPosition(0, 0), new GridPosition(0, 0));
            var quarry = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(20, 5))
                .WithHomeAndAnchor(new GridPosition(20, 5), new GridPosition(20, 0));
            world.Actors.Add(guard);
            world.Actors.Add(quarry);
            world.GuardPursuits.Add(new PursuitRecord { GuardId = 3UL, TargetId = 1UL, UntilMinutes = 100 });

            var stamp = new GameTime(23 * 60);
            Lifecycle().Decide(world, stamp);
            new ScheduleSystem().Advance(world.Actors, stamp, world);

            Assert.That(world.GuardPursuits, Is.Empty, "an expired chase is pruned");
            Assert.That(world.Actors.Get(new ActorId(3)).Position, Is.EqualTo(new GridPosition(4, 4)),
                "off-shift after the chase, the guard steps toward home");
        }

        [Test]
        public void PursueAction_TargetDies_FailsWithSemanticTargetGoneAndCleansClaims()
        {
            var world = World();
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(0, 0));
            var quarry = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(8, 0));
            world.Actors.Add(guard);
            world.Actors.Add(quarry);
            world.GuardPursuits.Add(new PursuitRecord
                { GuardId = guard.Id.Value, TargetId = quarry.Id.Value, UntilMinutes = 600 });
            var lifecycle = Lifecycle();
            Tick(world, lifecycle, 60);
            quarry.ApplyVitals(new ActorVitals(
                new VitalStat(0, quarry.Vitals.Health.Max),
                quarry.Vitals.Fatigue, quarry.Vitals.Mana));

            lifecycle.Advance(world, new GameTime(61));

            Assert.Multiple(() =>
            {
                Assert.That(guard.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
                Assert.That(guard.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.TargetGone));
                Assert.That(world.GuardPursuits.Any(row => row.GuardId == guard.Id.Value), Is.False);
                Assert.That(world.HuntTargets.Any(row => row.HunterId == guard.Id.Value), Is.False);
            });
        }

        [Test]
        public void PursueAction_TargetEscapesBeyondBound_FailsAndCleansClaims()
        {
            var world = World();
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(0, 0));
            var quarry = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 0));
            world.Actors.Add(guard);
            world.Actors.Add(quarry);
            world.GuardPursuits.Add(new PursuitRecord
                { GuardId = guard.Id.Value, TargetId = quarry.Id.Value, UntilMinutes = 600 });
            Tick(world, Lifecycle(), 60);
            quarry.MoveTo(new GridPosition(PursueAdvancer.MaxDistance + 2, 0));

            new PursueAdvancer(new ActionLogManager()).Advance(world, guard, new GameTime(61));

            Assert.Multiple(() =>
            {
                Assert.That(guard.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.TargetGone));
                Assert.That(world.GuardPursuits.Any(row => row.GuardId == guard.Id.Value), Is.False);
                Assert.That(world.HuntTargets.Any(row => row.HunterId == guard.Id.Value), Is.False);
            });
        }

        [Test]
        public void NewReport_RetargetsRunningPursuitWithoutDeletingNewestLedger()
        {
            var world = World();
            var oldTarget = Actor(1, "OldHound", ActorRole.Enemy, new GridPosition(8, 0));
            var newTarget = Actor(2, "NewHound", ActorRole.Enemy, new GridPosition(9, 0));
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(0, 0));
            var witness = Actor(4, "Witness", ActorRole.Talker, new GridPosition(1, 0));
            world.Actors.Add(oldTarget);
            world.Actors.Add(newTarget);
            world.Actors.Add(guard);
            world.Actors.Add(witness);
            world.GuardPursuits.Add(new PursuitRecord
                { GuardId = guard.Id.Value, TargetId = oldTarget.Id.Value, UntilMinutes = 600 });
            world.HuntTargets.Add(new HuntTargetRecord
                { HunterId = guard.Id.Value, TargetId = oldTarget.Id.Value, UntilMinutes = 600 });
            guard.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Pursue).Start(
                ActorActionType.Pursue, default, ItemId.Empty, ReservationId.Empty,
                60L, ActionInterruptPolicy.Interruptible, oldTarget.Id));
            witness.ApplyActionState(ActorActionState.ForIntent(ActorIntent.ReportCrime).Start(
                ActorActionType.ReportCrime, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                61L, ActionInterruptPolicy.Interruptible, newTarget.Id));

            new ReportCrimeAdvancer(new ActionLogManager())
                .Advance(world, witness, new GameTime(61));
            new PursueAdvancer(new ActionLogManager())
                .Advance(world, guard, new GameTime(62));

            Assert.Multiple(() =>
            {
                Assert.That(guard.ActionState.CurrentAction, Is.EqualTo(ActorActionType.Pursue));
                Assert.That(guard.ActionState.TargetActorId, Is.EqualTo(newTarget.Id));
                Assert.That(world.GuardPursuits.Single(row => row.GuardId == guard.Id.Value).TargetId,
                    Is.EqualTo(newTarget.Id.Value));
                Assert.That(world.HuntTargets.Single(row => row.HunterId == guard.Id.Value).TargetId,
                    Is.EqualTo(newTarget.Id.Value));
            });
        }

        [Test]
        public void ReportCrime_DeadTargetClosesFactWithoutArmingPursuitOrRetry()
        {
            var world = World();
            var attacker = Actor(1, "DeadHound", ActorRole.Enemy, new GridPosition(2, 0), health: 1);
            var witness = Actor(2, "Witness", ActorRole.Talker, new GridPosition(0, 0));
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(1, 0));
            attacker.ApplyVitals(attacker.Vitals.WithHealth(new VitalStat(0, 1)));
            world.Actors.Add(attacker);
            world.Actors.Add(witness);
            world.Actors.Add(guard);
            world.NpcMemory.GetOrCreate(witness.Id).RecordEvent(new EmberCrpg.Domain.Memory.InteractionEvent(
                new GameTime(60), "witnessed_attack", attacker.Id,
                "predation", string.Empty, 0, witness.Position));
            witness.ApplyActionState(ActorActionState.ForIntent(ActorIntent.ReportCrime).Start(
                ActorActionType.ReportCrime, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                60L, ActionInterruptPolicy.Interruptible, attacker.Id));
            var lifecycle = Lifecycle();

            lifecycle.Advance(world, new GameTime(60));
            lifecycle.Advance(world, new GameTime(61));
            lifecycle.Decide(world, new GameTime(62));

            Assert.Multiple(() =>
            {
                Assert.That(witness.ActionState.IsIdle, Is.True);
                Assert.That(world.GuardPursuits, Is.Empty);
                Assert.That(world.NpcMemory.GetOrCreate(witness.Id).Events.Any(e =>
                    e.EventType == "report_closed" && e.ActorSeen.Equals(attacker.Id)), Is.True);
            });
        }

        [Test]
        public void ReportCrime_UnreachableClosesFactWithoutArmingPursuitOrRetry()
        {
            var world = World();
            var attacker = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(10, 10));
            var witness = Actor(2, "Witness", ActorRole.Talker, new GridPosition(0, 0));
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(10, 0));
            world.Actors.Add(attacker);
            world.Actors.Add(witness);
            world.Actors.Add(guard);
            world.NpcMemory.GetOrCreate(witness.Id).RecordEvent(
                new EmberCrpg.Domain.Memory.InteractionEvent(
                    new GameTime(60), "witnessed_attack", attacker.Id,
                    "predation", string.Empty, 0, witness.Position));
            witness.ApplyActionState(ActorActionState.ForIntent(ActorIntent.ReportCrime).Start(
                ActorActionType.ReportCrime, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                60L, ActionInterruptPolicy.Interruptible, attacker.Id));
            for (var x = -1; x <= 1; x++)
                for (var y = -1; y <= 1; y++)
                    if (x != 0 || y != 0)
                        world.Blocked.Add(new GridPosition(x, y));

            new ReportCrimeAdvancer(new ActionLogManager())
                .Advance(world, witness, new GameTime(60));
            var lifecycle = Lifecycle();
            lifecycle.Advance(world, new GameTime(61));
            lifecycle.Decide(world, new GameTime(62));

            Assert.Multiple(() =>
            {
                Assert.That(witness.ActionState.IsIdle, Is.True);
                Assert.That(world.GuardPursuits, Is.Empty);
                Assert.That(world.NpcMemory.GetOrCreate(witness.Id).Events.Any(e =>
                    e.EventType == "report_closed"
                    && e.SubjectId == "unreachable"
                    && e.ActorSeen.Equals(attacker.Id)), Is.True);
            });
        }

        [Test]
        public void StrikePhase_ExpiredPursuitFailsTimedOut()
        {
            var world = World();
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(0, 0));
            var quarry = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(1, 0));
            world.Actors.Add(guard);
            world.Actors.Add(quarry);
            world.GuardPursuits.Add(new PursuitRecord
                { GuardId = guard.Id.Value, TargetId = quarry.Id.Value, UntilMinutes = 59 });
            world.HuntTargets.Add(new HuntTargetRecord
                { HunterId = guard.Id.Value, TargetId = quarry.Id.Value, UntilMinutes = 59 });
            guard.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Pursue).Start(
                ActorActionType.StrikeQuarry, default, ItemId.Empty, ReservationId.Empty,
                1L, ActionInterruptPolicy.Interruptible, quarry.Id));

            new StrikeQuarryAdvancer(new ActionLogManager())
                .Advance(world, guard, new GameTime(60));

            Assert.That(guard.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.TimedOut));
        }

        [Test]
        public void StrikePhase_CooldownTickRetainsExactStateWithoutFakeProgress()
        {
            var world = World();
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(0, 0));
            var quarry = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(1, 0));
            world.Actors.Add(guard);
            world.Actors.Add(quarry);
            world.GuardPursuits.Add(new PursuitRecord
                { GuardId = guard.Id.Value, TargetId = quarry.Id.Value, UntilMinutes = 600 });
            world.HuntTargets.Add(new HuntTargetRecord
                { HunterId = guard.Id.Value, TargetId = quarry.Id.Value, UntilMinutes = 600 });
            guard.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Pursue).Start(
                ActorActionType.StrikeQuarry, default, ItemId.Empty, ReservationId.Empty,
                1L, ActionInterruptPolicy.Interruptible, quarry.Id).Advanced());
            var before = guard.ActionState;

            new StrikeQuarryAdvancer(new ActionLogManager())
                .Advance(world, guard, new GameTime(61));

            Assert.Multiple(() =>
            {
                Assert.That(guard.ActionState, Is.EqualTo(before));
                Assert.That(world.ActionLog.Count, Is.Zero);
                Assert.That(quarry.Vitals.Health.Current, Is.EqualTo(quarry.Vitals.Health.Max));
            });
        }

        [Test]
        public void IdleGuard_AdjacentEnemyStartsCanonicalPursuitWithoutWitness()
        {
            var world = World();
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(0, 0));
            var enemy = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(1, 0));
            world.Actors.Add(guard);
            world.Actors.Add(enemy);

            Lifecycle().Decide(world, new GameTime(60));

            Assert.Multiple(() =>
            {
                Assert.That(guard.ActionState.CurrentAction, Is.EqualTo(ActorActionType.Pursue));
                Assert.That(guard.ActionState.TargetActorId, Is.EqualTo(enemy.Id));
                Assert.That(world.GuardPursuits.Any(row =>
                    row.GuardId == guard.Id.Value && row.TargetId == enemy.Id.Value), Is.True);
            });
        }

        [Test]
        public void PursueStrike_EmitsOneCombatResolved_ThenCompletesAndProjectsTruth()
        {
            var world = World();
            var guard = Actor(
                3, "Watch", ActorRole.Guard, new GridPosition(0, 0),
                accuracy: 100, baseDamage: 20);
            var quarry = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(1, 0), health: 1);
            world.Actors.Add(guard);
            world.Actors.Add(quarry);
            world.GuardPursuits.Add(new PursuitRecord
                { GuardId = guard.Id.Value, TargetId = quarry.Id.Value, UntilMinutes = 600 });
            var lifecycle = Lifecycle();

            Tick(world, lifecycle, 60);
            Tick(world, lifecycle, 61);
            Tick(world, lifecycle, 120);
            Tick(world, lifecycle, 121);

            Assert.Multiple(() =>
            {
                Assert.That(world.Events.Events.Count(e =>
                        e.Kind == WorldEventKind.CombatResolved
                        && e.ActorId.Equals(guard.Id)), Is.EqualTo(1));
                Assert.That(world.Events.Events.Count(e =>
                        e.Kind == WorldEventKind.GuardResponded
                        && e.ActorId.Equals(guard.Id)), Is.EqualTo(1));
                Assert.That(guard.ActionState.IsIdle, Is.True);
                Assert.That(ActionVerbTable.Verb(ActorActionType.ReportCrime), Is.EqualTo("reporting crime"));
                Assert.That(ActionVerbTable.Verb(ActorActionType.Pursue), Is.EqualTo("pursuing"));
                Assert.That(ActionVerbTable.Verb(ActorActionType.StrikeQuarry), Is.EqualTo("striking"));
            });
        }

        [Test]
        public void PursueStrike_KilledRunningHunterRetiresItsActionAndTargetLedger()
        {
            var world = World();
            var guard = Actor(
                3, "Watch", ActorRole.Guard, new GridPosition(0, 0),
                accuracy: 100, baseDamage: 20);
            var hunter = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(1, 0), health: 1);
            var civilian = Actor(2, "Villager", ActorRole.Talker, new GridPosition(2, 0));
            world.Actors.Add(guard);
            world.Actors.Add(hunter);
            world.Actors.Add(civilian);
            world.GuardPursuits.Add(new PursuitRecord
                { GuardId = guard.Id.Value, TargetId = hunter.Id.Value, UntilMinutes = 600 });
            world.HuntTargets.Add(new HuntTargetRecord
                { HunterId = guard.Id.Value, TargetId = hunter.Id.Value, UntilMinutes = 600 });
            world.HuntTargets.Add(new HuntTargetRecord
                { HunterId = hunter.Id.Value, TargetId = civilian.Id.Value, UntilMinutes = 600 });
            guard.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Pursue).Start(
                ActorActionType.StrikeQuarry, default, ItemId.Empty, ReservationId.Empty,
                1L, ActionInterruptPolicy.Interruptible, hunter.Id));
            hunter.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Hunt).Start(
                ActorActionType.Hunt, default, ItemId.Empty, ReservationId.Empty,
                1L, ActionInterruptPolicy.Interruptible, civilian.Id));

            new StrikeQuarryAdvancer(new ActionLogManager())
                .Advance(world, guard, new GameTime(60));

            Assert.Multiple(() =>
            {
                Assert.That(hunter.IsAlive, Is.False);
                Assert.That(hunter.ActionState.IsIdle, Is.True);
                Assert.That(world.HuntTargets.Any(row => row.HunterId == hunter.Id.Value), Is.False);
            });
        }

        [Test]
        public void MidPursuit_SaveLoad_PreservesDurableActorTargetAndLedgers()
        {
            var world = new WorldFactory().Create(1337);
            var guard = world.Actors.FirstByRole(ActorRole.Guard);
            var quarry = world.Actors.FirstByRole(ActorRole.Enemy);
            world.GuardPursuits.Clear();
            world.HuntTargets.Clear();
            world.GuardPursuits.Add(new PursuitRecord
                { GuardId = guard.Id.Value, TargetId = quarry.Id.Value, UntilMinutes = 999 });
            world.HuntTargets.Add(new HuntTargetRecord
                { HunterId = guard.Id.Value, TargetId = quarry.Id.Value, UntilMinutes = 999 });
            guard.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Pursue).Start(
                ActorActionType.Pursue, default, ItemId.Empty, ReservationId.Empty,
                60L, ActionInterruptPolicy.Interruptible, quarry.Id).Advanced());

            var loaded = WorldSaveMapper.ToWorld(
                WorldSaveMapper.ToData(world),
                new WorldFactory().Create(1337));
            var restored = loaded.Actors.Get(guard.Id);

            Assert.Multiple(() =>
            {
                Assert.That(restored.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.Pursue));
                Assert.That(restored.ActionState.CurrentAction, Is.EqualTo(ActorActionType.Pursue));
                Assert.That(restored.ActionState.TargetActorId, Is.EqualTo(quarry.Id));
                Assert.That(restored.ActionState.ProgressTicks, Is.EqualTo(1));
                Assert.That(loaded.GuardPursuits.Any(row =>
                    row.GuardId == guard.Id.Value && row.TargetId == quarry.Id.Value), Is.True);
                Assert.That(loaded.HuntTargets.Any(row =>
                    row.HunterId == guard.Id.Value && row.TargetId == quarry.Id.Value), Is.True);
            });
        }

        [Test]
        public void Digest_DistinguishesDifferentPersistedPursuitTargets()
        {
            var first = new WorldFactory().Create(1337);
            var second = new WorldFactory().Create(1337);
            var firstGuard = first.Actors.FirstByRole(ActorRole.Guard);
            var secondGuard = second.Actors.FirstByRole(ActorRole.Guard);
            var enemy = first.Actors.FirstByRole(ActorRole.Enemy);
            var talker = second.Actors.FirstByRole(ActorRole.Talker);

            firstGuard.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Pursue).Start(
                ActorActionType.Pursue, default, ItemId.Empty, ReservationId.Empty,
                60L, ActionInterruptPolicy.Interruptible, enemy.Id));
            secondGuard.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Pursue).Start(
                ActorActionType.Pursue, default, ItemId.Empty, ReservationId.Empty,
                60L, ActionInterruptPolicy.Interruptible, talker.Id));

            Assert.That(WorldStateDigest.Compute(first),
                Is.Not.EqualTo(WorldStateDigest.Compute(second)));
        }

        [Test]
        public void NonInterruptiblePolicy_IsHonoredByReportAction()
        {
            var world = World();
            var attacker = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 5));
            var witness = Actor(2, "Witness", ActorRole.Talker, new GridPosition(6, 5));
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(6, 6));
            world.Actors.Add(attacker);
            world.Actors.Add(witness);
            world.Actors.Add(guard);
            world.GuardPursuits.Add(new PursuitRecord
                { GuardId = guard.Id.Value, TargetId = witness.Id.Value, UntilMinutes = 999 });
            witness.ApplyActionState(ActorActionState.ForIntent(ActorIntent.ReportCrime).Start(
                ActorActionType.ReportCrime, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                60L, ActionInterruptPolicy.NonInterruptible, attacker.Id));

            new ReportCrimeAdvancer(new ActionLogManager())
                .Advance(world, witness, new GameTime(60));

            Assert.That(witness.ActionState.Phase, Is.EqualTo(ActionPhase.Succeeded),
                "NonInterruptible must be enforced, not a persisted decorative enum value");
        }
    }
}
