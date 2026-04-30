using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Rng;
using NUnit.Framework;

// Design note:
// These tests pin Sprint 1's bounded turn loop compromise explicitly.
// They verify alternating turns and encounter completion without touching Unity presentation.
namespace EmberCrpg.Tests.EditMode.Combat
{
    /// <summary>Verifies slice-only encounter progression.</summary>
    public sealed class EncounterTurnServiceTests
    {
        [Test]
        public void Advance_FirstCallUsesPlayerTurnThenFlipsToEnemy()
        {
            var service = new EncounterTurnService();
            var player = CreateActor(1, 90, 5, 0, 8);
            var enemy = CreateActor(2, 0, 0, 0, 1);
            var encounter = new EncounterState(player.Id, enemy.Id);
            service.Advance(encounter, player, enemy, new FixedRng(1, 8));
            Assert.That(encounter.PlayerActsNext, Is.False);
        }

        [Test]
        public void Advance_KillingStrike_FinishesEncounter()
        {
            var service = new EncounterTurnService();
            var player = CreateActor(1, 90, 5, 0, 20);
            var enemy = CreateActor(2, 0, 0, 0, 1);
            enemy.ApplyVitals(new ActorVitals(new VitalStat(3, 24), new VitalStat(18, 18), new VitalStat(12, 12)));
            var encounter = new EncounterState(player.Id, enemy.Id);
            service.Advance(encounter, player, enemy, new FixedRng(1, 8));
            Assert.That(encounter.IsFinished && encounter.WinnerName == player.Name, Is.True);
        }

        private static ActorRecord CreateActor(ulong id, int accuracy, int dodge, int armor, int baseDamage)
        {
            return new ActorRecord(
                new ActorId(id),
                id == 1 ? "Player" : "Enemy",
                id == 1 ? ActorRole.Player : ActorRole.Enemy,
                new EmberStatBlock(60, 50, 40, 30, 20, 10),
                new ActorVitals(new VitalStat(24, 24), new VitalStat(18, 18), new VitalStat(12, 12)),
                new GridPosition(1, 1),
                accuracy,
                dodge,
                armor,
                baseDamage);
        }

        private sealed class FixedRng : IDeterministicRng
        {
            private readonly int[] _values;
            private int _index;
            public FixedRng(params int[] values) { _values = values; }
            public int NextInt(int exclusiveMax) { return _values[_index++] % exclusiveMax; }
            public int RollPercent() { return _values[_index++]; }
        }
    }
}
