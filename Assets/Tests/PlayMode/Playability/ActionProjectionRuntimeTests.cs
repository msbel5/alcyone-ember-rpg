using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Simulation.Diagnostics;
using NUnit.Framework;

namespace EmberCrpg.Tests.PlayMode.Playability
{
    /// <summary>PRD-09 runtime projection and main-thread apply authority stories.</summary>
    public sealed class ActionProjectionRuntimeTests
    {
        [TestCase(ActorRole.Guard)]
        [TestCase(ActorRole.Enemy)]
        public void ActionlessActorAtHomeAtNight_ProjectsNoGuessedActivity(ActorRole role)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(23 * GameTime.MinutesPerHour);
            var home = new GridPosition(4, 4);
            var actor = Actor(7, role, home);
            world.Actors.Add(actor);
            var adapter = new DomainSimulationAdapter(world);

            Assert.That(adapter.TryReadActor(actor.Id, out var state), Is.True);
            Assert.That(state.Activity, Is.Null);
            Assert.That(state.ActionKind, Is.Null);
            Assert.That(state.Sleeping, Is.False);
        }

        [Test]
        public void RealCurrentAction_ProjectsVerbKindAndSleepPoseVerbatim()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            var actor = Actor(7, ActorRole.Talker, new GridPosition(4, 4));
            world.Actors.Add(actor);
            actor.ApplyActionState(
                ActorActionState.ForIntent(ActorIntent.Rest).Start(
                    ActorActionType.Sleep,
                    default,
                    ItemId.Empty,
                    ReservationId.Empty,
                    startedAtMinutes: 0,
                    ActionInterruptPolicy.Interruptible));
            var adapter = new DomainSimulationAdapter(world);

            Assert.That(adapter.TryReadActor(actor.Id, out var state), Is.True);
            Assert.That(state.Activity, Is.EqualTo(ActionVerbTable.Verb(ActorActionType.Sleep)));
            Assert.That(state.ActionKind, Is.EqualTo(ActionVerbTable.KindName(ActorActionType.Sleep)));
            Assert.That(state.Sleeping, Is.True);
        }

        [Test]
        public void MainThreadApplyFailure_IsCountedLoggedAndDoesNotStopTheQueue()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            var adapter = new DomainSimulationAdapter(world);
            var queueField = typeof(DomainSimulationAdapter).GetField(
                "_mainThreadApply", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(queueField, Is.Not.Null);
            var queue = (ConcurrentQueue<Action>)queueField.GetValue(adapter);
            var continued = false;
            queue.Enqueue(() => throw new InvalidOperationException("apply failed\nwith detail"));
            queue.Enqueue(() => continued = true);

            var lines = new List<string>();
            var priorSink = EmberLog.Sink;
            EmberLog.Sink = lines.Add;
            try
            {
                adapter.AdvanceTick(0);
            }
            finally
            {
                EmberLog.Sink = priorSink;
            }

            Assert.That(continued, Is.True, "one bad apply must not starve later queued work");
            Assert.That(adapter.MainThreadApplyFailureCount, Is.EqualTo(1));
            Assert.That(lines.Single(line => line.Contains("event=apply_failed")),
                Does.Contain("severity=error")
                    .And.Contain("count=1")
                    .And.Contain("exception=System.InvalidOperationException"));
            Assert.That(lines.Any(line => line.Contains('\n') || line.Contains('\r')), Is.False,
                "structured apply failures stay one physical log line");
        }

        private static ActorRecord Actor(ulong id, ActorRole role, GridPosition position)
        {
            return new ActorRecord(
                new ActorId(id), role + id.ToString(), role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(10, 10),
                    new VitalStat(10, 10),
                    new VitalStat(10, 10)),
                position,
                accuracy: 10,
                dodge: 5,
                armor: 0,
                baseDamage: 1,
                home: position,
                dayAnchor: position);
        }
    }
}
