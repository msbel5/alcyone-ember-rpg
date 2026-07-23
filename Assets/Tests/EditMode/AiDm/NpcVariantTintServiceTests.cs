using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>Paper-doll pin: tints are stable, bounded, and actually vary.</summary>
    public sealed class NpcVariantTintServiceTests
    {
        [Test]
        public void TintFor_IsStable_AndStaysReadable()
        {
            for (ulong id = 1; id < 120; id++)
            {
                var a = NpcVariantTintService.TintFor(id);
                var b = NpcVariantTintService.TintFor(id);
                Assert.That(a, Is.EqualTo(b), "same actor, same cloth - every session");
                Assert.That(a.R, Is.InRange(NpcVariantTintService.MinChannel, 1f));
                Assert.That(a.G, Is.InRange(NpcVariantTintService.MinChannel, 1f));
                Assert.That(a.B, Is.InRange(NpcVariantTintService.MinChannel, 1f));
            }
        }

        [Test]
        public void TintFor_VariesAcrossTheTown()
        {
            var seen = new System.Collections.Generic.HashSet<(float, float, float)>();
            for (ulong id = 1; id < 40; id++) seen.Add(NpcVariantTintService.TintFor(id));
            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(30), "40 villagers must not share a handful of tints");
        }
    }
}
