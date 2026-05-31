using System;
using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    public sealed class FactionReputationDecaySystemTests
    {
        private static readonly FactionId A = new FactionId(1UL);
        private static readonly FactionId B = new FactionId(2UL);

        [Test]
        public void Config_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FactionDecayConfig(ratePerStep: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FactionDecayConfig(deadBand: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FactionDecayConfig(daysPerDecayStep: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FactionDecayConfig(baseline: 5));
        }

        [Test]
        public void Apply_DriftsPositiveAndNegativeTowardNeutral()
        {
            var factions = new FactionStore();
            factions.WithReputation(A, B, new FactionReputation(12));
            factions.WithReputation(new FactionId(3UL), new FactionId(4UL), new FactionReputation(-8));
            var events = new WorldEventLog();
            var system = new FactionReputationDecaySystem();

            system.Apply(factions, FactionDecayConfig.Default, new GameTime(240), events);

            Assert.That(factions.GetReputation(A, B).Value, Is.EqualTo(11));
            Assert.That(factions.GetReputation(new FactionId(3UL), new FactionId(4UL)).Value, Is.EqualTo(-7));
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events.Events.Select(e => e.Reason), Is.All.Contains("reason:decay"));
        }

        [Test]
        public void Apply_HonorsDeadBandAndZeroRate()
        {
            var factions = new FactionStore();
            factions.WithReputation(A, B, new FactionReputation(3));
            var events = new WorldEventLog();
            var system = new FactionReputationDecaySystem();

            system.Apply(factions, new FactionDecayConfig(deadBand: 3), new GameTime(240), events);
            Assert.That(factions.GetReputation(A, B).Value, Is.EqualTo(3));
            Assert.That(events.Count, Is.EqualTo(0));

            system.Apply(factions, new FactionDecayConfig(ratePerStep: 0), new GameTime(480), events);
            Assert.That(factions.GetReputation(A, B).Value, Is.EqualTo(3));
            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void Apply_IsDeterministicAcrossRuns()
        {
            Assert.That(SnapshotAfterDecay(), Is.EqualTo(SnapshotAfterDecay()));
        }

        private static string SnapshotAfterDecay()
        {
            var factions = new FactionStore();
            factions.WithReputation(new FactionId(3UL), new FactionId(4UL), new FactionReputation(-8));
            factions.WithReputation(A, B, new FactionReputation(12));
            var events = new WorldEventLog();

            new FactionReputationDecaySystem().Apply(factions, FactionDecayConfig.Default, new GameTime(240), events);

            var rows = string.Join(",", factions.ReputationRows.Select(r => $"{r.A.Value}:{r.B.Value}:{r.Reputation.Value}"));
            var reasons = string.Join(",", events.Events.Select(e => e.Reason));
            return rows + "|" + reasons;
        }
    }
}
