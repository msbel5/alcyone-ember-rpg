using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies the Faz 5 world component id primitive.</summary>
    public sealed class WorldComponentIdTests
    {
        [Test]
        public void DefaultId_IsEmptySentinel()
        {
            Assert.That(default(WorldComponentId).IsEmpty, Is.True);
            Assert.That(new WorldComponentId(5).IsEmpty, Is.False);
        }

        [Test]
        public void Equality_UsesRawValue()
        {
            Assert.That(new WorldComponentId(5), Is.EqualTo(new WorldComponentId(5)));
            Assert.That(new WorldComponentId(5), Is.Not.EqualTo(new WorldComponentId(6)));
        }
    }
}
