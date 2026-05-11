using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using NUnit.Framework;

// Design note:
// These tests pin the WorldEvent constructor contract before WorldEventLog
// consumers exist. Coverage stays scoped to the pure record; append-only log
// ordering, save/load round-trip, and reason-trace chaining belong elsewhere.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies the pure-Domain invariants required of WorldEvent.</summary>
    public sealed class WorldEventTests
    {
        private static readonly GameTime SampleTick = new GameTime(120L);
        private static readonly ActorId SampleActor = new ActorId(7UL);
        private static readonly SiteId SampleSite = new SiteId(3UL);

        private static WorldEvent MakeEvent()
        {
            return new WorldEvent(
                SampleTick,
                WorldEventKind.ActorSpawned,
                SampleActor,
                SampleSite,
                "player_command");
        }

        /// <summary>Constructor stores every field exactly as supplied.</summary>
        [Test]
        public void Constructor_StoresFields()
        {
            var evt = MakeEvent();

            Assert.That(evt.Tick, Is.EqualTo(SampleTick));
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.ActorSpawned));
            Assert.That(evt.ActorId, Is.EqualTo(SampleActor));
            Assert.That(evt.SiteId, Is.EqualTo(SampleSite));
            Assert.That(evt.Reason, Is.EqualTo("player_command"));
            Assert.That(evt.ReasonTrace, Is.Null);
        }


        /// <summary>An optional ReasonTrace is stored without copying so the event can carry a causal chain.</summary>
        [Test]
        public void Constructor_StoresReasonTraceWhenProvided()
        {
            var trace = new ReasonTrace(new[] { "player_command", "guard_spawned" });

            var evt = new WorldEvent(
                SampleTick,
                WorldEventKind.ActorSpawned,
                SampleActor,
                SampleSite,
                "player_command",
                trace);

            Assert.That(evt.ReasonTrace, Is.SameAs(trace));
            Assert.That(evt.ReasonTrace.RootCause, Is.EqualTo("player_command"));
            Assert.That(evt.ReasonTrace.LeafCause, Is.EqualTo("guard_spawned"));
        }

        /// <summary>A null ReasonTrace remains valid because early Faz 1 events may not yet have a causal chain.</summary>
        [Test]
        public void Constructor_AcceptsNullReasonTrace()
        {
            var evt = new WorldEvent(
                SampleTick,
                WorldEventKind.ActorSpawned,
                SampleActor,
                SampleSite,
                "player_command",
                null);

            Assert.That(evt.ReasonTrace, Is.Null);
        }

        /// <summary>The empty WorldEventKind sentinel cannot back an event.</summary>
        [Test]
        public void Constructor_RejectsNoneKind()
        {
            Assert.Throws<ArgumentException>(() => new WorldEvent(
                SampleTick,
                WorldEventKind.None,
                SampleActor,
                SampleSite,
                "player_command"));
        }

        /// <summary>An event with neither an actor subject nor a site locus is rejected.</summary>
        [Test]
        public void Constructor_RejectsEmptyActorAndSite()
        {
            Assert.Throws<ArgumentException>(() => new WorldEvent(
                SampleTick,
                WorldEventKind.ActorSpawned,
                default,
                default,
                "player_command"));
        }

        /// <summary>An actor-only event (empty site) is accepted; e.g. ActorSpawned before placement.</summary>
        [Test]
        public void Constructor_AcceptsEmptySiteWhenActorPresent()
        {
            var evt = new WorldEvent(
                SampleTick,
                WorldEventKind.ActorSpawned,
                SampleActor,
                default,
                "player_command");

            Assert.That(evt.ActorId, Is.EqualTo(SampleActor));
            Assert.That(evt.SiteId.IsEmpty, Is.True);
        }

        /// <summary>A site-only event (empty actor) is accepted; e.g. SiteEntered by the party.</summary>
        [Test]
        public void Constructor_AcceptsEmptyActorWhenSitePresent()
        {
            var evt = new WorldEvent(
                SampleTick,
                WorldEventKind.SiteEntered,
                default,
                SampleSite,
                "player_command");

            Assert.That(evt.ActorId.IsEmpty, Is.True);
            Assert.That(evt.SiteId, Is.EqualTo(SampleSite));
        }

        /// <summary>A blank or whitespace reason is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsBlankReason()
        {
            Assert.Throws<ArgumentException>(() => new WorldEvent(
                SampleTick,
                WorldEventKind.ActorSpawned,
                SampleActor,
                SampleSite,
                "   "));
        }

        /// <summary>A null reason is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsNullReason()
        {
            Assert.Throws<ArgumentException>(() => new WorldEvent(
                SampleTick,
                WorldEventKind.ActorSpawned,
                SampleActor,
                SampleSite,
                null));
        }
    }
}
