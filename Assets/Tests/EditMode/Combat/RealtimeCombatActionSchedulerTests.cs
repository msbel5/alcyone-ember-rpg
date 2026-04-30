using System.Linq;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Combat;
using NUnit.Framework;

// Design note:
// These tests pin the Sprint 4 real-time-with-pause action scheduler without Unity input or animation.
// They cover actor-local ordering, pause behavior, and queue editability while paused.
namespace EmberCrpg.Tests.EditMode.Combat
{
    /// <summary>Verifies deterministic RTWP action queue timing.</summary>
    public sealed class RealtimeCombatActionSchedulerTests
    {
        [Test]
        public void Tick_QueuesActorActionsInOrderAndEmitsActivationBeforeCompletion()
        {
            var state = new RealtimeCombatState();
            var scheduler = new RealtimeCombatActionScheduler();
            var actor = new ActorId(1UL);

            var swing = scheduler.QueueAction(state, actor, CombatActionKind.MeleeSwing, new ActorId(2UL));
            var block = scheduler.QueueAction(state, actor, CombatActionKind.Block, new ActorId(0UL));

            Assert.That(block.StartAtSeconds, Is.EqualTo(swing.CompleteAtSeconds).Within(0.0001d));

            var result = scheduler.Tick(state, 2.0d);
            var events = result.Events.Select(combatEvent => combatEvent.Action.Sequence + ":" + combatEvent.EventKind).ToArray();

            Assert.That(events, Is.EqualTo(new[]
            {
                swing.Sequence + ":Activated",
                swing.Sequence + ":Completed",
                block.Sequence + ":Activated",
                block.Sequence + ":Completed",
            }));
        }

        [Test]
        public void Tick_WhenPaused_DoesNotAdvanceTimeOrActivateActions()
        {
            var state = new RealtimeCombatState();
            var scheduler = new RealtimeCombatActionScheduler();
            scheduler.QueueAction(state, new ActorId(1UL), CombatActionKind.MeleeSwing, new ActorId(2UL));

            state.SetPaused(true);
            var result = scheduler.Tick(state, 10.0d);

            Assert.That(state.ElapsedSeconds, Is.EqualTo(0d));
            Assert.That(result.Events.Count, Is.EqualTo(0));
            Assert.That(state.Queue[0].IsActivated, Is.False);
        }

        [Test]
        public void Queue_CanBeEditedWhilePausedAndResumesDeterministically()
        {
            var state = new RealtimeCombatState();
            var scheduler = new RealtimeCombatActionScheduler();
            var actor = new ActorId(1UL);
            var enemy = new ActorId(2UL);
            var swing = scheduler.QueueAction(state, actor, CombatActionKind.MeleeSwing, enemy);
            state.SetPaused(true);

            Assert.That(scheduler.CancelAction(state, swing.Sequence), Is.True);
            var cast = scheduler.QueueAction(state, actor, CombatActionKind.Cast, enemy);
            state.SetPaused(false);

            var beforeActive = scheduler.Tick(state, cast.WindupSeconds - 0.01d);
            var afterActive = scheduler.Tick(state, 0.02d);

            Assert.That(state.Queue.Count, Is.EqualTo(1));
            Assert.That(state.Queue[0].Kind, Is.EqualTo(CombatActionKind.Cast));
            Assert.That(beforeActive.Events.Count, Is.EqualTo(0));
            Assert.That(afterActive.Events[0].Action.Sequence, Is.EqualTo(cast.Sequence));
            Assert.That(afterActive.Events[0].EventKind, Is.EqualTo(CombatActionEventKind.Activated));
        }

        [Test]
        public void GetActiveDefenseIntent_PrefersDodgeOverBlock()
        {
            var state = new RealtimeCombatState();
            var scheduler = new RealtimeCombatActionScheduler();
            var actor = new ActorId(1UL);
            scheduler.QueueAction(state, actor, CombatActionKind.Block, new ActorId(0UL));
            scheduler.QueueAction(state, actor, CombatActionKind.Dodge, new ActorId(0UL));

            scheduler.Tick(state, 0.06d);
            Assert.That(scheduler.GetActiveDefenseIntent(state, actor), Is.EqualTo(CombatDefenseIntent.Blocking));

            scheduler.Tick(state, 0.70d);
            Assert.That(scheduler.GetActiveDefenseIntent(state, actor), Is.EqualTo(CombatDefenseIntent.Dodging));
        }
    }
}
