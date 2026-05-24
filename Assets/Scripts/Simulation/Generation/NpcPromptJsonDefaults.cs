using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Simulation.Generation
{
    public static class NpcPromptJsonDefaults
    {
        private static readonly string[] Moods = { "wary", "soot-stained", "patient", "restless", "watchful", "tired" };
        private static readonly string[] Features = { "scar", "iron earring", "ash marks", "braided hair", "old burn", "copper beads" };
        private static readonly string[] Clothing = { "leather jerkin", "wool cloak", "patched robe", "travel coat", "linen tunic" };
        private static readonly string[] Accessories = { "talisman", "brass ring", "bone charm", "ledger", "iron key" };

        public static NpcPromptJson FromSeed(uint seed, GenericNpcBaseManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            var rng = new DeterministicPicker(seed == 0u ? 1u : seed);
            var archetype = manifest.Archetypes[rng.Next(manifest.Archetypes.Count)].ArchetypeId;
            return new NpcPromptJson(
                archetype,
                rng.Next(360),
                rng.Next(360),
                new[] { Moods[rng.Next(Moods.Length)], Moods[rng.Next(Moods.Length)] },
                new[] { Features[rng.Next(Features.Length)], Features[rng.Next(Features.Length)] },
                Clothing[rng.Next(Clothing.Length)],
                Accessories[rng.Next(Accessories.Length)],
                "ember-warm");
        }

        private struct DeterministicPicker
        {
            private uint _state;
            public DeterministicPicker(uint seed) { _state = seed; }
            public int Next(int maxExclusive)
            {
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
                return (int)(_state % (uint)maxExclusive);
            }
        }
    }
}
