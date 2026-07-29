using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Simulation.Living.Actions;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W36 GUARD+COMBAT story test #2: enemy Hunt → StrikeQuarry loop.
    /// PINS: an enemy with a civilian in HuntRadius opens a HuntTargets row, closes the
    /// distance via HuntAdvancer (hourly cadence — matches retired PredationSystem), and once
    /// adjacent StrikeQuarry resolves damage through CombatOperations.ResolveStrike (also
    /// hourly cadence). Matter conservation: a civilian target dropping to 0 is clamped to
    /// 1 HP (mauled_survives event) AND the HuntTargets row clears the same tick — the
    /// cyclic NextLink terminates because HuntAdvancer.TryResolvePrey fails TargetGone
    /// → Idle on the next Advance.
    /// Direct-lifecycle idiom: composer wiring is dark (docs/atlas/systems/04-cascades-crime.md
    /// debt #1); enableGuardAndCombat is passed explicitly here so the machinery is proven
    /// without perturbing the pre-W36 tick-composer soak gates.
    /// </summary>
    public sealed class EnemyHuntStoryTests
    {
        private static ActionLifecycleSystem Lifecycle()
            => new ActionLifecycleSystem(new ActionLogManager(), enableGuardAndCombat: true);

        private static void Pump(WorldState world, ActionLifecycleSystem lifecycle, int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                world.Time = world.Time.AddMinutes(1);
                lifecycle.Decide(world, world.Time);
                lifecycle.Advance(world, world.Time);
            }
        }

        [Test]
        public void EnemyClosesInAndStrikesUntilTargetIsMauled()
        {
            var world = HuntSliceWorld.Build();
            var enemy = HuntSliceWorld.Enemy(id: 100, x: 0, y: 0);
            var prey = HuntSliceWorld.Prey(id: 200, x: 3, y: 0, hp: 10);
            world.Actors.Add(enemy);
            world.Actors.Add(prey);
            var lifecycle = Lifecycle();

            Pump(world, lifecycle); // Decide opens Hunt; Advance runs one Hunt.Step
            Assert.That(enemy.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.Hunt),
                "TryDecideHunt opened a Hunt intent on the empty-action Enemy");
            Assert.That(world.HuntTargets.Count, Is.EqualTo(1),
                "the row IS the claim — RegisterHunt wrote it on Decide");
            Assert.That(world.HuntTargets[0].TargetId, Is.EqualTo(prey.Id.Value));

            // Advance until prey is mauled (clamped to 1 HP), capped at 400 game-minutes.
            var strikeCount = 0;
            for (var tick = 0; tick < 400; tick++)
            {
                Pump(world, lifecycle);
                strikeCount = world.Events.Events.Count(e => e.Kind == WorldEventKind.CombatResolved
                    && e.ActorId.Equals(enemy.Id));
                // An expired relationship now cleans up immediately and may be re-armed on the
                // next idle decision. Stop on the story outcome, not on a transient empty ledger.
                if (prey.Vitals.Health.Current <= 1) break;
            }

            Assert.That(strikeCount, Is.GreaterThan(0),
                "StrikeQuarryAdvancer resolved at least one CombatResolved through CombatOperations");
            Assert.That(prey.IsAlive, Is.True, "matter conservation: the civilian is MAULED, not killed");
            Assert.That(prey.Vitals.Health.Current, Is.EqualTo(1),
                "the clamp fired: predation preserves town population (PLAYTEST parity)");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.MaulSurvived
                    && e.Reason != null
                    && e.Reason.Contains("policy:civilian_maul_survival")), Is.True,
                "the mauled_survives event was appended by MaybeMaulClamp");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.NeedChanged
                    && e.Reason != null && e.Reason.StartsWith("mauled_survives")), Is.False);
            Assert.That(world.HuntTargets.Count, Is.EqualTo(0),
                "the HuntTargets row cleared on the clamp — the cyclic NextLink terminated");
        }

        [Test]
        public void EnemyWithNoPreyInRangeStaysIdleAndOpensNoRow()
        {
            var world = HuntSliceWorld.Build();
            var enemy = HuntSliceWorld.Enemy(id: 100, x: 0, y: 0);
            // No prey — the enemy scan returns null; TryDecideHunt returns early with no ledger
            // write and no log line.
            world.Actors.Add(enemy);
            var lifecycle = Lifecycle();

            Pump(world, lifecycle, ticks: 5);

            Assert.That(enemy.ActionState.CurrentAction, Is.EqualTo(ActorActionType.None),
                "no prey in range = no decision — cheap silent return");
            Assert.That(world.HuntTargets.Count, Is.EqualTo(0),
                "matter conservation: an empty scan opens NO row (a starving hunter's soundtrack is silence)");
        }

        [Test]
        public void HuntCadenceWait_DoesNotMoveOrIncrementProgress()
        {
            var world = HuntSliceWorld.Build();
            var enemy = HuntSliceWorld.Enemy(id: 100, x: 0, y: 0);
            var prey = HuntSliceWorld.Prey(id: 200, x: 5, y: 0, hp: 10);
            world.Actors.Add(enemy);
            world.Actors.Add(prey);
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = enemy.Id.Value,
                TargetId = prey.Id.Value,
                UntilMinutes = 999,
            });
            enemy.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Hunt).Start(
                ActorActionType.Hunt, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                startedAtMinutes: 60, ActionInterruptPolicy.Interruptible));

            new HuntAdvancer(new ActionLogManager()).Advance(world, enemy, new GameTime(61));

            Assert.That(enemy.Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(enemy.ActionState.Phase, Is.EqualTo(ActionPhase.Running));
            Assert.That(enemy.ActionState.ProgressTicks, Is.EqualTo(0),
                "cadence waiting is not movement progress");
        }

        [TestCase("expired")]
        [TestCase("dead")]
        [TestCase("missing")]
        public void HuntTerminalTargetLoss_FailsAndRemovesRelationship(string loss)
        {
            var world = HuntSliceWorld.Build();
            var enemy = HuntSliceWorld.Enemy(id: 100, x: 0, y: 0);
            var prey = HuntSliceWorld.Prey(id: 200, x: 5, y: 0, hp: 10);
            world.Actors.Add(enemy);
            if (loss != "missing")
                world.Actors.Add(prey);
            if (loss == "dead")
                prey.ApplyVitals(prey.Vitals.WithHealth(prey.Vitals.Health.Damage(999)));
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = enemy.Id.Value,
                TargetId = prey.Id.Value,
                UntilMinutes = loss == "expired" ? 59L : 999L,
            });
            enemy.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Hunt).Start(
                ActorActionType.Hunt, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                startedAtMinutes: 60, ActionInterruptPolicy.Interruptible));

            new HuntAdvancer(new ActionLogManager()).Advance(world, enemy, new GameTime(60));

            Assert.That((enemy.ActionState.Phase, enemy.ActionState.FailureReason),
                Is.EqualTo((ActionPhase.Failed, ActionFailureReason.TargetGone)));
            Assert.That(world.HuntTargets, Is.Empty,
                "expired/dead/missing quarry cannot leave a stale target relationship");
        }

        [Test]
        public void HuntInterruptedByPursuit_RemovesRelationship()
        {
            var world = HuntSliceWorld.Build();
            var enemy = HuntSliceWorld.Enemy(id: 100, x: 0, y: 0);
            var prey = HuntSliceWorld.Prey(id: 200, x: 5, y: 0, hp: 10);
            world.Actors.Add(enemy);
            world.Actors.Add(prey);
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = enemy.Id.Value,
                TargetId = prey.Id.Value,
                UntilMinutes = 999L,
            });
            world.GuardPursuits.Add(new PursuitRecord
            {
                GuardId = 300UL,
                TargetId = enemy.Id.Value,
                UntilMinutes = 999L,
            });
            enemy.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Hunt).Start(
                ActorActionType.Hunt, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                startedAtMinutes: 60, ActionInterruptPolicy.Interruptible));

            new HuntAdvancer(new ActionLogManager()).Advance(world, enemy, new GameTime(61));

            Assert.That((enemy.ActionState.Phase, enemy.ActionState.FailureReason),
                Is.EqualTo((ActionPhase.Failed, ActionFailureReason.Interrupted)));
            Assert.That(world.HuntTargets, Is.Empty,
                "the generic interruption gate must retire the Hunt target identity");
        }

        [Test]
        public void StrikeQuarryMissingTarget_FailsAndRemovesRelationship()
        {
            var world = HuntSliceWorld.Build();
            var enemy = HuntSliceWorld.Enemy(id: 100, x: 0, y: 0);
            world.Actors.Add(enemy);
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = enemy.Id.Value,
                TargetId = 200UL,
                UntilMinutes = 999L,
            });
            enemy.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Hunt).Start(
                ActorActionType.StrikeQuarry, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                startedAtMinutes: 60, ActionInterruptPolicy.Interruptible));

            new StrikeQuarryAdvancer(new ActionLogManager()).Advance(world, enemy, new GameTime(60));

            Assert.That((enemy.ActionState.Phase, enemy.ActionState.FailureReason),
                Is.EqualTo((ActionPhase.Failed, ActionFailureReason.TargetGone)));
            Assert.That(world.HuntTargets, Is.Empty,
                "StrikeQuarry shares the same terminal relationship cleanup");
        }

        [Test]
        public void HuntNoRoute_FailsWithoutProgress_AndRemovesTargetClaim()
        {
            var world = HuntSliceWorld.Build();
            var enemy = HuntSliceWorld.Enemy(id: 100, x: 0, y: 0);
            var prey = HuntSliceWorld.Prey(id: 200, x: 5, y: 0, hp: 10);
            world.Actors.Add(enemy);
            world.Actors.Add(prey);
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = enemy.Id.Value,
                TargetId = prey.Id.Value,
                UntilMinutes = 999,
            });
            for (var x = -1; x <= 1; x++)
                for (var y = -1; y <= 1; y++)
                    if (x != 0 || y != 0)
                        world.Blocked.Add(new GridPosition(x, y));
            enemy.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Hunt).Start(
                ActorActionType.Hunt, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                startedAtMinutes: 60, ActionInterruptPolicy.Interruptible));

            new HuntAdvancer(new ActionLogManager()).Advance(world, enemy, new GameTime(120));

            Assert.That(enemy.Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(enemy.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
            Assert.That(enemy.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Unreachable));
            Assert.That(enemy.ActionState.ProgressTicks, Is.EqualTo(0));
            Assert.That(world.HuntTargets, Is.Empty,
                "terminal no-route removes the hunt claim instead of retrying forever");
        }

        [Test]
        public void HuntOppositeIntegerBoundaries_DoesNotFalseArriveOrThrow()
        {
            var world = HuntSliceWorld.Build();
            var edge = new GridPosition(int.MaxValue, 0);
            var enemy = HuntSliceWorld.Enemy(id: 100, x: edge.X, y: edge.Y);
            var prey = HuntSliceWorld.Prey(id: 200, x: int.MinValue, y: 0, hp: 10);
            world.Actors.Add(enemy);
            world.Actors.Add(prey);
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = enemy.Id.Value,
                TargetId = prey.Id.Value,
                UntilMinutes = 999,
            });
            enemy.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Hunt).Start(
                ActorActionType.Hunt, new SiteId(1), ItemId.Empty, ReservationId.Empty,
                startedAtMinutes: 60, ActionInterruptPolicy.Interruptible));

            Assert.DoesNotThrow(() =>
                new HuntAdvancer(new ActionLogManager())
                    .Advance(world, enemy, new GameTime(120)));

            Assert.That(enemy.Position, Is.EqualTo(edge));
            Assert.That(enemy.ActionState.Phase, Is.EqualTo(ActionPhase.Failed),
                "opposite int boundaries are maximally far, never adjacent");
            Assert.That(enemy.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Unreachable));
            Assert.That(enemy.ActionState.ProgressTicks, Is.EqualTo(0));
        }

        [Test]
        public void EnemyHuntProjectsAsHuntNotAsGuess()
        {
            // W36: the projection reads verb from ActionVerbTable — the "hunting" GUESS row
            // in DescribeScheduleWord (Enemy branch) is DEAD. Hunt/StrikeQuarry both live.
            Assert.That(ActionVerbTable.Verb(ActorActionType.Hunt), Is.EqualTo("hunting"),
                "the verb table row IS the label; the retired GUESS branch's string moved here");
            Assert.That(ActionVerbTable.Verb(ActorActionType.StrikeQuarry), Is.EqualTo("striking"),
                "StrikeQuarry gets its own verb (the swing is distinct from the approach)");
            Assert.That(ActionVerbTable.KindName(ActorActionType.Hunt), Is.EqualTo("Hunt"));
            Assert.That(ActionVerbTable.KindName(ActorActionType.StrikeQuarry), Is.EqualTo("StrikeQuarry"));
        }
    }

    /// <summary>W36 GUARD+COMBAT: HuntSliceWorld is EatSliceWorld's mirror for the enemy
    /// hunt path — a big site with no pile (no eat rule fires) so the direct-lifecycle
    /// Decide only opens Hunt for hostiles.</summary>
    internal static class HuntSliceWorld
    {
        public static WorldState Build()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(60);
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Wilds",
                new GridPosition(-20, -20), new GridPosition(20, 20)));
            return world;
        }

        public static ActorRecord Enemy(ulong id, int x, int y)
        {
            // Overpowered stats guarantee the maul completes inside one strike — the story
            // test pins the LOOP, not the balance curve (that lives in Combat/*Tests). At
            // damage 20 ± 10 the deterministic dice cannot leave the prey standing.
            return new ActorRecord(
                new ActorId(id), "Hunter" + id, ActorRole.Enemy,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y),
                accuracy: 100, dodge: 5, armor: 0, baseDamage: 20);
        }

        public static ActorRecord Prey(ulong id, int x, int y, int hp)
        {
            return new ActorRecord(
                new ActorId(id), "Prey" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(hp, hp), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y),
                accuracy: 5, dodge: 5, armor: 0, baseDamage: 1);
        }
    }
}
