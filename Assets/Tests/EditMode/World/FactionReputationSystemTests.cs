using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Pins Phase 6 Atom 4 FactionReputationSystem: delta apply, event emission, no-op guards.</summary>
    public sealed class FactionReputationSystemTests
    {
        private static readonly FactionId A = new FactionId(1UL);
        private static readonly FactionId B = new FactionId(2UL);

        [Test]
        public void ApplyDelta_RaisesReputation_AndEmitsEvent()
        {
            var factions = new FactionStore();
            var events = new WorldEventLog();
            var system = new FactionReputationSystem();

            system.ApplyDelta(factions, A, B, +30, "trade_completed", default, events);

            Assert.That(factions.GetReputation(A, B).Value, Is.EqualTo(30));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(WorldEventKind.FactionReputationChanged));
            Assert.That(events.Events[0].Reason, Does.Contain("trade_completed"));
            Assert.That(events.Events[0].Reason, Does.Contain("from:0"));
            Assert.That(events.Events[0].Reason, Does.Contain("to:30"));
        }

        [Test]
        public void ApplyDelta_LowersReputation_AndClampsAtMin()
        {
            var factions = new FactionStore();
            factions.WithReputation(A, B, new FactionReputation(-90));
            var events = new WorldEventLog();
            var system = new FactionReputationSystem();

            system.ApplyDelta(factions, A, B, -50, "crime_observed", default, events);

            Assert.That(factions.GetReputation(A, B).Value, Is.EqualTo(-100));
            Assert.That(events.Count, Is.EqualTo(1));
        }

        [Test]
        public void ApplyDelta_ZeroDelta_NoEvent()
        {
            var factions = new FactionStore();
            var events = new WorldEventLog();
            var system = new FactionReputationSystem();

            system.ApplyDelta(factions, A, B, 0, "noop", default, events);

            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(factions.GetReputation(A, B), Is.EqualTo(FactionReputation.Neutral));
        }

        [Test]
        public void ApplyDelta_EmptyOrSelfPair_NoOp()
        {
            var factions = new FactionStore();
            var events = new WorldEventLog();
            var system = new FactionReputationSystem();

            system.ApplyDelta(factions, default, B, 10, "x", default, events);
            system.ApplyDelta(factions, A, default, 10, "x", default, events);
            system.ApplyDelta(factions, A, A, 10, "x", default, events);

            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyDelta_AlreadyAtCap_NoEvent()
        {
            var factions = new FactionStore();
            factions.WithReputation(A, B, new FactionReputation(100));
            var events = new WorldEventLog();
            var system = new FactionReputationSystem();

            system.ApplyDelta(factions, A, B, +20, "ignored", default, events);

            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(factions.GetReputation(A, B).Value, Is.EqualTo(100));
        }
    }
}
