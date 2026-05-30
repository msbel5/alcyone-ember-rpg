using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin Phase 3's actor-local job preference row before assignment logic
// exists. Matching to JobBoard entries is deliberately out of scope here.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies actor job preference normalization and disabled rows.</summary>
    public sealed class ActorJobPreferenceTests
    {
        [Test]
        public void Constructor_StoresKindAndPriority()
        {
            var preference = new ActorJobPreference(JobKind.Smith, JobPriority.Active(1));

            Assert.That(preference.Kind, Is.EqualTo(JobKind.Smith));
            Assert.That(preference.Priority, Is.EqualTo(JobPriority.Active(1)));
            Assert.That(preference.IsEnabled, Is.True);
        }

        [Test]
        public void Constructor_RejectsNoneKind()
        {
            Assert.Throws<ArgumentException>(() => new ActorJobPreference(JobKind.None, JobPriority.Active(1)));
            Assert.Throws<ArgumentException>(() => ActorJobPreference.Disabled(JobKind.None));
        }

        [Test]
        public void Disabled_CreatesOptOutRowForConcreteKind()
        {
            var preference = ActorJobPreference.Disabled(JobKind.Smith);

            Assert.That(preference.Kind, Is.EqualTo(JobKind.Smith));
            Assert.That(preference.Priority, Is.EqualTo(JobPriority.Disabled));
            Assert.That(preference.IsEnabled, Is.False);
        }

        [Test]
        public void SameKindAndPriority_AreEqual()
        {
            var left = new ActorJobPreference(JobKind.Smith, JobPriority.Active(2));
            var right = new ActorJobPreference(JobKind.Smith, JobPriority.Active(2));

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsDebugLabel()
        {
            Assert.That(
                new ActorJobPreference(JobKind.Smith, JobPriority.Active(3)).ToString(),
                Is.EqualTo("ActorJobPreference(Smith, 3)"));
            Assert.That(
                ActorJobPreference.Disabled(JobKind.Smith).ToString(),
                Is.EqualTo("ActorJobPreference(Smith, disabled)"));
        }
    }
}
