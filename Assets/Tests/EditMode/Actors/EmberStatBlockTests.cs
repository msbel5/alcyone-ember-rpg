using System;
using EmberCrpg.Domain.Actors;
using NUnit.Framework;

// Design note:
// These tests pin the six-stat deterministic actor kernel.
// They cover only pure stat validation and lookup behavior.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies the Sprint 1 stat block contract.</summary>
    public sealed class EmberStatBlockTests
    {
        [Test]
        public void Constructor_StoresAllSixStats()
        {
            var stats = new EmberStatBlock(10, 11, 12, 13, 14, 15);
            Assert.That(new[] { stats.Mig, stats.Agi, stats.End, stats.Mnd, stats.Ins, stats.Pre }, Is.EqualTo(new[] { 10, 11, 12, 13, 14, 15 }));
        }

        [Test]
        public void Constructor_BelowMinimum_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EmberStatBlock(0, 10, 10, 10, 10, 10));
        }

        [Test]
        public void Get_ReturnsSelectedAttribute()
        {
            var stats = new EmberStatBlock(10, 11, 12, 13, 14, 15);
            Assert.That(stats.Get(EmberAttribute.Ins), Is.EqualTo(14));
        }
    }
}
