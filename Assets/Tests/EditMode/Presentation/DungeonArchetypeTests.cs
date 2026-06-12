#if UNITY_EDITOR
using EmberCrpg.Presentation.Ember.WorldDirector;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    /// <summary>
    /// F19: the delve archetype must be deterministic in the dungeon seed and all three archetypes
    /// must be reachable (seed % 3). Worlds that roll fewer than three dungeons still prove variety
    /// through this mapping — the lookaround variety leg captures whatever the world actually has.
    /// </summary>
    public sealed class DungeonArchetypeTests
    {
        [Test]
        public void For_SameSeed_IsDeterministic()
        {
            var a = RuntimeDungeonArchetype.For(328528998);
            var b = RuntimeDungeonArchetype.For(328528998);
            Assert.That(a.Name, Is.EqualTo(b.Name));
            Assert.That(a.Rock, Is.EqualTo(b.Rock));
            Assert.That(a.Torch, Is.EqualTo(b.Torch));
        }

        [Test]
        public void For_CoversAllThreeArchetypes()
        {
            // The seed is HASHED before the pick (realize seeds proved structurally ≡ 0 mod 3),
            // so reachability is asserted over a seed sweep instead of pinned single values.
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int seed = 0; seed < 30; seed++)
                seen.Add(RuntimeDungeonArchetype.For(seed).Name);
            Assert.That(seen, Is.EquivalentTo(new[] { "Mağara", "Kripta", "Harabe" }));
        }

        [Test]
        public void For_ProofWorldSeeds_AreNotAllTheSameArchetype()
        {
            // The three delve seeds of the proof world — all divisible by 3 — must not collapse
            // onto one archetype anymore.
            var seen = new System.Collections.Generic.HashSet<string>
            {
                RuntimeDungeonArchetype.For(886870881).Name,
                RuntimeDungeonArchetype.For(502681080).Name,
                RuntimeDungeonArchetype.For(328528998).Name,
            };
            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void For_NegativeSeed_IsStableAndInSet()
        {
            var archetype = RuntimeDungeonArchetype.For(-7);
            Assert.That(archetype.Name, Is.EqualTo(RuntimeDungeonArchetype.For(-7).Name));
            Assert.That(archetype.Name,
                Is.EqualTo("Mağara").Or.EqualTo("Kripta").Or.EqualTo("Harabe"));
        }
    }
}
#endif
