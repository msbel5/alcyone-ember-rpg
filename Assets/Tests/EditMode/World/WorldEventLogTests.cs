using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using NUnit.Framework;

// Design note:
// These tests pin the WorldEventLog append-and-enumerate contract before any
// runtime consumer (save/load mapper, replay HUD) exists. Coverage stays
// scoped to the pure log: null rejection, deterministic insertion order,
// live Events-view reflection across further appends, the immutability of the
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
            Assert.That(evt.Sequence, Is.EqualTo(0L));
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
            Assert.That(log.Events[0].Sequence, Is.EqualTo(0L));
            Assert.That(log.Events[1].Sequence, Is.EqualTo(1L));
            Assert.That(log.Events[2].Sequence, Is.EqualTo(2L));
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

        // B21 (W32-04 §6): trim contract - matter-conservation for the seq baseline.

        /// <summary>Under-cap trim is a no-op: no drops, FirstRetainedSeq stays at 0.</summary>
        [Test]
        public void TrimOldest_UnderCap_IsNoop()
        {
            var log = new WorldEventLog();
            for (int i = 0; i < 5; i++) log.Append(MakeEvent(i, WorldEventKind.ActorSpawned, "e" + i));

            Assert.That(log.TrimOldest(maxRetained: 8), Is.EqualTo(0));
            Assert.That(log.Count, Is.EqualTo(5));
            Assert.That(log.FirstRetainedSeq, Is.EqualTo(0L));
            Assert.That(log.TotalAppended, Is.EqualTo(5L));
        }

        /// <summary>Over-cap trim drops the oldest N; FirstRetainedSeq += N; TotalAppended unchanged; invariant holds.</summary>
        [Test]
        public void TrimOldest_OverCap_DropsOldestAndAdvancesSeqBaseline()
        {
            var log = new WorldEventLog();
            for (int i = 0; i < 10; i++) log.Append(MakeEvent(i, WorldEventKind.ActorSpawned, "e" + i));

            int dropped = log.TrimOldest(maxRetained: 4);

            Assert.That(dropped, Is.EqualTo(6), "10-4=6 rows dropped");
            Assert.That(log.Count, Is.EqualTo(4));
            Assert.That(log.FirstRetainedSeq, Is.EqualTo(6L), "seq baseline advances by drop count");
            Assert.That(log.TotalAppended, Is.EqualTo(10L), "TotalAppended is monotone across trim");
            Assert.That(log.TotalAppended, Is.EqualTo(log.FirstRetainedSeq + log.Count),
                "matter-conservation invariant: TotalAppended == FirstRetainedSeq + Count");
            // The oldest surviving event is the one appended at index 6 (reason "e6").
            Assert.That(log.Events[0].Reason, Is.EqualTo("e6"));
            Assert.That(log.Events[0].Sequence, Is.EqualTo(6L));
            Assert.That(log.Events[3].Sequence, Is.EqualTo(9L));
        }

        /// <summary>TryIndexForSeq maps seq→index correctly across a trim (path avoids the dropped band).</summary>
        [Test]
        public void TryIndexForSeq_MapsCorrectlyAcrossTrim()
        {
            var log = new WorldEventLog();
            for (int i = 0; i < 10; i++) log.Append(MakeEvent(i, WorldEventKind.ActorSpawned, "e" + i));

            // Before trim: seq==index.
            Assert.That(log.TryIndexForSeq(3L, out int preIdx), Is.True);
            Assert.That(preIdx, Is.EqualTo(3));

            log.TrimOldest(maxRetained: 4);

            // After trim (FirstRetainedSeq==6): a live seq maps to index (seq-6).
            Assert.That(log.TryIndexForSeq(7L, out int liveIdx), Is.True);
            Assert.That(liveIdx, Is.EqualTo(1));
            // A pre-trim seq clamps to 0 (start of retained window) - never re-mills a dropped row.
            Assert.That(log.TryIndexForSeq(2L, out int oldIdx), Is.True);
            Assert.That(oldIdx, Is.EqualTo(0));
            // A future seq clamps to Count (nothing to consume yet).
            Assert.That(log.TryIndexForSeq(999L, out int futureIdx), Is.True);
            Assert.That(futureIdx, Is.EqualTo(log.Count));
        }

        [Test]
        public void ComposerFinalization_IsTheBoundedProductionOwner()
        {
            var world = new WorldState();
            for (var i = 0; i < WorldTickComposer.MaxRetainedWorldEvents + 25; i++)
                world.Events.Append(MakeEvent(i, WorldEventKind.ActorSpawned, "e" + i));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            composer.Advance(world, 1);

            Assert.That(world.Events.Count, Is.EqualTo(WorldTickComposer.MaxRetainedWorldEvents));
            Assert.That(world.Events.FirstRetainedSeq,
                Is.EqualTo(world.Events.TotalAppended - world.Events.Count));
            Assert.That(world.Events.Events[0].Sequence, Is.EqualTo(world.Events.FirstRetainedSeq));
            Assert.That(world.Events.Events[world.Events.Count - 1].Sequence,
                Is.EqualTo(world.Events.TotalAppended - 1));
        }
    }
}
