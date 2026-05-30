using EmberCrpg.Domain.Actors;
using NUnit.Framework;

// Design note:
// These tests pin Phase 4's seed need categories before any ticking or recovery
// logic exists.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies the seed actor need categories.</summary>
    public sealed class NeedKindTests
    {
        [Test]
        public void None_IsZeroSentinel()
        {
            Assert.That((int)NeedKind.None, Is.EqualTo(0));
        }

        [Test]
        public void SeedKinds_AreStable()
        {
            Assert.That(NeedKind.Hunger, Is.EqualTo((NeedKind)1));
            Assert.That(NeedKind.Fatigue, Is.EqualTo((NeedKind)2));
            Assert.That(NeedKind.Thirst, Is.EqualTo((NeedKind)3));
        }
    }
}
