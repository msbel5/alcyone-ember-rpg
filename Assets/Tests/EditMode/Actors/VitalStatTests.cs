using System;
using EmberCrpg.Domain.Actors;
using NUnit.Framework;

// Design note:
// These tests pin health/fatigue/mana pool behavior for the deterministic slice.
// They avoid combat orchestration and focus on pure bounded transitions.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies bounded vital-pool transitions.</summary>
    public sealed class VitalStatTests
    {
        [Test]
        public void Constructor_CurrentAboveMax_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VitalStat(6, 5));
        }

        [Test]
        public void Damage_ClampsAtZero()
        {
            Assert.That(new VitalStat(4, 10).Damage(10).Current, Is.EqualTo(0));
        }

        [Test]
        public void Restore_ClampsAtMax()
        {
            Assert.That(new VitalStat(4, 10).Restore(20).Current, Is.EqualTo(10));
        }
    }
}
