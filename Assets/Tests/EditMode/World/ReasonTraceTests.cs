using System;
using EmberCrpg.Domain.World;
using NUnit.Framework;

// Design note:
// These tests pin the ReasonTrace constructor contract and chain accessors
// before WorldEventLog consumers exist. Coverage stays scoped to the pure
// record; allocation, persistence, and Unity concerns belong elsewhere.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies the pure-Domain invariants required of ReasonTrace.</summary>
    public sealed class ReasonTraceTests
    {
        private static ReasonTrace MakeTrace()
        {
            return new ReasonTrace(new[] { "player-action", "actor-spawned" });
        }

        /// <summary>Constructor stores causes in root-first order.</summary>
        [Test]
        public void Constructor_StoresCausesInOrder()
        {
            var trace = MakeTrace();

            Assert.That(trace.Causes, Is.EqualTo(new[] { "player-action", "actor-spawned" }));
            Assert.That(trace.Depth, Is.EqualTo(2));
            Assert.That(trace.RootCause, Is.EqualTo("player-action"));
            Assert.That(trace.LeafCause, Is.EqualTo("actor-spawned"));
        }

        /// <summary>A single-cause trace is the minimum valid chain.</summary>
        [Test]
        public void Constructor_AcceptsSingleCause()
        {
            var trace = new ReasonTrace(new[] { "lone-cause" });

            Assert.That(trace.Depth, Is.EqualTo(1));
            Assert.That(trace.RootCause, Is.EqualTo("lone-cause"));
            Assert.That(trace.LeafCause, Is.EqualTo("lone-cause"));
        }

        /// <summary>A null causes enumerable is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsNullCauses()
        {
            Assert.Throws<ArgumentNullException>(() => new ReasonTrace(null));
        }

        /// <summary>An empty causes enumerable is rejected — every trace needs a root cause.</summary>
        [Test]
        public void Constructor_RejectsEmptyCauses()
        {
            Assert.Throws<ArgumentException>(() => new ReasonTrace(Array.Empty<string>()));
        }

        /// <summary>A blank or whitespace cause entry is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsBlankCauseEntry()
        {
            Assert.Throws<ArgumentException>(() => new ReasonTrace(new[] { "valid", "   " }));
        }

        /// <summary>Causes snapshot is detached from the source enumerable — later mutation must not leak in.</summary>
        [Test]
        public void Constructor_TakesDefensiveCopyOfCauses()
        {
            var source = new System.Collections.Generic.List<string> { "first", "second" };
            var trace = new ReasonTrace(source);

            source.Add("after-the-fact");

            Assert.That(trace.Depth, Is.EqualTo(2));
            Assert.That(trace.Causes, Is.EqualTo(new[] { "first", "second" }));
        }

        /// <summary>The Causes view cannot be downcast back to a mutable array.</summary>
        [Test]
        public void Causes_ViewIsReadOnly()
        {
            var trace = MakeTrace();

            Assert.That(trace.Causes, Is.Not.InstanceOf<string[]>());
        }

        /// <summary>HasCause does a case-sensitive match against the stored chain.</summary>
        [Test]
        public void HasCause_MatchesCaseSensitively()
        {
            var trace = MakeTrace();

            Assert.That(trace.HasCause("player-action"), Is.True);
            Assert.That(trace.HasCause("actor-spawned"), Is.True);
            Assert.That(trace.HasCause("Player-Action"), Is.False);
            Assert.That(trace.HasCause("missing"), Is.False);
        }

        /// <summary>HasCause rejects blank queries rather than matching by accident.</summary>
        [Test]
        public void HasCause_RejectsBlankQuery()
        {
            var trace = MakeTrace();

            Assert.That(trace.HasCause(null), Is.False);
            Assert.That(trace.HasCause(""), Is.False);
            Assert.That(trace.HasCause("   "), Is.False);
        }
    }
}
