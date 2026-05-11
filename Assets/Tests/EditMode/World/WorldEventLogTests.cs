using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using NUnit.Framework;

// Design note:
// These tests pin the WorldEventLog append-and-enumerate contract before any
// runtime consumer (save/load mapper, replay HUD) exists. Coverage stays
// scoped to the pure log: null rejection, deterministic insertion order,
// snapshot stability across further appends, and the immutability of the
// public Events view, plus carrying ReasonTrace through the append path.
// Save/load round-trip remains scoped to the TIME-box follow-up PR.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies the pure-Domain invariants required of WorldEventLog.</summary>
    public sealed class WorldEventLogTests
    {
        private static readonly ActorId SampleActor = new ActorId(7UL);
        private static readonly SiteId SampleSite = new SiteId(3UL);

        private static WorldEvent MakeEvent(long tick, WorldEventKind kind, string reason)
        {
            return new WorldEvent(
                new GameTime(tick),
                kind,
                SampleActor,
                SampleSite,
                reason);
        }

        /// <summary>A fresh log reports zero count and an empty enumeration.</summary>
        [Test]
        public void NewLog_IsEmpty()
        {
            var log = new WorldEventLog();

            Assert.That(log.Count, Is.EqualTo(0));
            Assert.That(log.IsEmpty, Is.True);
            Assert.That(log.Events, Is.Empty);
        }

        /// <summary>Append stores the event and grows the count.</summary>
        [Test]
        public void Append_StoresEvent()
        {
            var log = new WorldEventLog();
            var evt = MakeEvent(10L, WorldEventKind.ActorSpawned, "player_command");

            log.Append(evt);

            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log.IsEmpty, Is.False);
            Assert.That(log.Events, Has.Count.EqualTo(1));
            Assert.That(log.Events[0], Is.SameAs(evt));
        }

        /// <summary>Multiple appends are exposed in deterministic insertion order.</summary>
        [Test]
        public void Append_PreservesInsertionOrder()
        {
            var log = new WorldEventLog();
            var first = MakeEvent(10L, WorldEventKind.ActorSpawned, "player_command");
            var second = MakeEvent(11L, WorldEventKind.ActorTalked, "player_command");
            var third = MakeEvent(12L, WorldEventKind.SiteEntered, "player_command");

            log.Append(first);
            log.Append(second);
            log.Append(third);

            Assert.That(log.Count, Is.EqualTo(3));
            Assert.That(log.Events, Is.EqualTo(new[] { first, second, third }));
        }

        /// <summary>Out-of-order ticks are still appended in insertion order — the log is a chronicle, not a sorter.</summary>
        [Test]
        public void Append_PreservesInsertionOrderEvenWhenTicksDecrease()
        {
            var log = new WorldEventLog();
            var later = MakeEvent(50L, WorldEventKind.ActorSpawned, "player_command");
            var earlier = MakeEvent(20L, WorldEventKind.SiteEntered, "player_command");

            log.Append(later);
            log.Append(earlier);

            Assert.That(log.Events, Is.EqualTo(new[] { later, earlier }));
        }


        /// <summary>The log preserves a WorldEvent causal trace reference through append/enumeration.</summary>
        [Test]
        public void Append_PreservesReasonTraceOnEvent()
        {
            var log = new WorldEventLog();
            var trace = new ReasonTrace(new[] { "player_command", "guard_talked" });
            var evt = new WorldEvent(
                new GameTime(13L),
                WorldEventKind.ActorTalked,
                SampleActor,
                SampleSite,
                "player_command",
                trace);

            log.Append(evt);

            Assert.That(log.Events[0].ReasonTrace, Is.SameAs(trace));
            Assert.That(log.Events[0].ReasonTrace.LeafCause, Is.EqualTo("guard_talked"));
        }

        /// <summary>A null event is rejected at append so the log never contains gaps.</summary>
        [Test]
        public void Append_RejectsNullEvent()
        {
            var log = new WorldEventLog();

            Assert.Throws<ArgumentNullException>(() => log.Append(null));
            Assert.That(log.Count, Is.EqualTo(0));
        }

        /// <summary>The Events view tracks subsequent appends after being captured.</summary>
        [Test]
        public void Events_ViewReflectsLaterAppends()
        {
            var log = new WorldEventLog();
            var view = log.Events;
            var evt = MakeEvent(10L, WorldEventKind.ActorSpawned, "player_command");

            log.Append(evt);

            Assert.That(view, Has.Count.EqualTo(1));
            Assert.That(view[0], Is.SameAs(evt));
        }

        /// <summary>The Events view cannot be downcast to a mutable list.</summary>
        [Test]
        public void Events_ViewIsReadOnly()
        {
            var log = new WorldEventLog();

            Assert.That(log.Events, Is.Not.InstanceOf<System.Collections.Generic.List<WorldEvent>>());
            Assert.That(log.Events, Is.Not.InstanceOf<WorldEvent[]>());
        }
    }
}
