using EmberCrpg.Domain.Combat;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Rng;
using NUnit.Framework;

// Design note:
// These tests pin DFU-style weighted target selection with deterministic RNG.
// They do not validate the full combat flow.
namespace EmberCrpg.Tests.EditMode.Combat
{
    /// <summary>Verifies deterministic body-part selection.</summary>
    public sealed class BodyPartSelectorTests
    {
        [Test]
        public void Select_UsesWeightedArrayOrder()
        {
            var selector = new BodyPartSelector();
            var part = selector.Select(new SequenceRng(0, 8, 19));
            Assert.That(part, Is.EqualTo(BodyPart.Head));
            Assert.That(selector.Select(new SequenceRng(8)), Is.EqualTo(BodyPart.Chest));
            Assert.That(selector.Select(new SequenceRng(19)), Is.EqualTo(BodyPart.Feet));
        }

        private sealed class SequenceRng : IDeterministicRng
        {
            private readonly int[] _values;
            private int _index;
            public SequenceRng(params int[] values) { _values = values; }
            public int NextInt(int exclusiveMax) { return _values[_index++] % exclusiveMax; }
            public int RollPercent() { return 1; }
        }
    }
}
