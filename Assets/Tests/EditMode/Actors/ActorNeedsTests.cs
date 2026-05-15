using System;
using EmberCrpg.Domain.Actors;
using NUnit.Framework;

// Design note:
// These tests pin the actor-local needs component before ticking, recovery, or
// mood derivation systems consume it.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies immutable actor needs snapshots.</summary>
    public sealed class ActorNeedsTests
    {
        [Test]
        public void Comfortable_DefaultsToZeroPressure()
        {
            Assert.That(ActorNeeds.Comfortable.Hunger, Is.EqualTo(NeedValue.Comfortable));
            Assert.That(ActorNeeds.Comfortable.Fatigue, Is.EqualTo(NeedValue.Comfortable));
            Assert.That(ActorNeeds.Comfortable.Thirst, Is.EqualTo(NeedValue.Comfortable));
        }

        [Test]
        public void Constructor_StoresNeedPressures()
        {
            var needs = new ActorNeeds(new NeedValue(10), new NeedValue(20), new NeedValue(30));

            Assert.That(needs.Hunger.Value, Is.EqualTo(10));
            Assert.That(needs.Fatigue.Value, Is.EqualTo(20));
            Assert.That(needs.Thirst.Value, Is.EqualTo(30));
        }

        [Test]
        public void With_ReplacesOneNeedOnly()
        {
            var needs = ActorNeeds.Comfortable
                .With(NeedKind.Hunger, new NeedValue(40))
                .With(NeedKind.Fatigue, new NeedValue(50))
                .With(NeedKind.Thirst, new NeedValue(60));

            Assert.That(needs.Get(NeedKind.Hunger), Is.EqualTo(new NeedValue(40)));
            Assert.That(needs.Get(NeedKind.Fatigue), Is.EqualTo(new NeedValue(50)));
            Assert.That(needs.Get(NeedKind.Thirst), Is.EqualTo(new NeedValue(60)));
        }

        [Test]
        public void With_RejectsNoneKind()
        {
            Assert.Throws<ArgumentException>(() => ActorNeeds.Comfortable.Get(NeedKind.None));
            Assert.Throws<ArgumentException>(() => ActorNeeds.Comfortable.With(NeedKind.None, NeedValue.Critical));
        }

        [Test]
        public void Equality_UsesAllNeedPressures()
        {
            var left = new ActorNeeds(new NeedValue(1), new NeedValue(2), new NeedValue(3));
            var right = new ActorNeeds(new NeedValue(1), new NeedValue(2), new NeedValue(3));

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsDebugLabel()
        {
            Assert.That(
                new ActorNeeds(new NeedValue(1), new NeedValue(2), new NeedValue(3)).ToString(),
                Is.EqualTo("ActorNeeds(hunger=1, fatigue=2, thirst=3)"));
        }
    }
}
