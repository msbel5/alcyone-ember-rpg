using EmberCrpg.Domain.Actors;
using NUnit.Framework;

// Design note:
// These tests pin Faz 4's bounded need pressure value. Higher pressure means
// a worse need, but policies that act on thresholds are later atoms.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies bounded need pressure behavior.</summary>
    public sealed class NeedValueTests
    {
        [Test]
        public void Constructor_ClampsToZeroToOneHundred()
        {
            Assert.That(new NeedValue(-5).Value, Is.EqualTo(0));
            Assert.That(new NeedValue(105).Value, Is.EqualTo(100));
        }

        [Test]
        public void IncreaseAndDecrease_ClampAtBounds()
        {
            Assert.That(new NeedValue(95).Increase(20), Is.EqualTo(NeedValue.Critical));
            Assert.That(new NeedValue(5).Decrease(20), Is.EqualTo(NeedValue.Comfortable));
        }

        [Test]
        public void Increase_SaturatesLargePositiveDeltas()
        {
            Assert.That(new NeedValue(95).Increase(int.MaxValue), Is.EqualTo(NeedValue.Critical));
        }

        [Test]
        public void IsAtLeast_PinsThresholdSemantics()
        {
            Assert.That(new NeedValue(70).IsAtLeast(new NeedValue(70)), Is.True);
            Assert.That(new NeedValue(69).IsAtLeast(new NeedValue(70)), Is.False);
        }

        [Test]
        public void Comparison_UsesRawPressure()
        {
            Assert.That(new NeedValue(80) > new NeedValue(40), Is.True);
            Assert.That(new NeedValue(20) < new NeedValue(40), Is.True);
            Assert.That(new NeedValue(40).CompareTo(new NeedValue(40)), Is.EqualTo(0));
        }

        [Test]
        public void ToString_ReturnsDebugLabel()
        {
            Assert.That(new NeedValue(12).ToString(), Is.EqualTo("NeedValue(12)"));
        }
    }
}
