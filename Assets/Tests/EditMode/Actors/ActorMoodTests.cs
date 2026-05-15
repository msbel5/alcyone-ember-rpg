using EmberCrpg.Domain.Actors;
using NUnit.Framework;

// Design note:
// These tests pin Faz 4's mood scalar before refusal or recovery systems
// consume it.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies bounded actor mood behavior.</summary>
    public sealed class ActorMoodTests
    {
        [Test]
        public void Default_IsNeutralMood()
        {
            Assert.That(ActorMood.Neutral.Value, Is.EqualTo(ActorMood.NeutralValue));
            Assert.That(default(ActorMood).Value, Is.EqualTo(ActorMood.NeutralValue));
        }

        [Test]
        public void Constructor_ClampsToZeroToOneHundred()
        {
            Assert.That(new ActorMood(-5).Value, Is.EqualTo(0));
            Assert.That(new ActorMood(105).Value, Is.EqualTo(100));
        }

        [Test]
        public void IsLow_UsesLowMoodThreshold()
        {
            Assert.That(new ActorMood(ActorMood.LowMoodThreshold).IsLow, Is.True);
            Assert.That(new ActorMood(ActorMood.LowMoodThreshold + 1).IsLow, Is.False);
        }

        [Test]
        public void EqualityAndComparison_UseMoodValue()
        {
            var left = new ActorMood(40);
            var right = new ActorMood(40);

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(new ActorMood(20) < new ActorMood(40), Is.True);
            Assert.That(new ActorMood(70) > new ActorMood(40), Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsDebugLabel()
        {
            Assert.That(new ActorMood(12).ToString(), Is.EqualTo("ActorMood(12)"));
        }
    }
}
