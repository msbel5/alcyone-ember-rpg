using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F19 DUNGEON VARIETY: one delve no longer looks like every other delve. Three archetypes —
    /// Mağara (cave), Kripta (crypt), Harabe (ruin) — picked DETERMINISTICALLY from the settlement's
    /// dungeon seed (seed % 3), so the same world always realizes the same delve the same way.
    /// Purely presentational (material palette + torch light); the room graph, dwellers and loot
    /// are archetype-agnostic, so worldgen goldens and the domain stay untouched.
    /// </summary>
    public readonly struct RuntimeDungeonArchetype
    {
        public readonly string Name;
        public readonly Color Rock;
        public readonly Color Floor;
        public readonly Color Torch;
        public readonly float TorchIntensity;
        public readonly Color BossTorch;

        private RuntimeDungeonArchetype(
            string name, Color rock, Color floor, Color torch, float torchIntensity, Color bossTorch)
        {
            Name = name;
            Rock = rock;
            Floor = floor;
            Torch = torch;
            TorchIntensity = torchIntensity;
            BossTorch = bossTorch;
        }

        public static RuntimeDungeonArchetype For(int dungeonSeed)
        {
            // PROOF-CAUGHT BIAS: raw seed % 3 made every delve a Mağara — the realize seeds are
            // structurally divisible by 3 (886870881 / 502681080 / 328528998 all ≡ 0 mod 3).
            // Mix the bits first (murmur-style finalizer), then pick.
            uint h = (uint)dungeonSeed;
            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            h *= 0x846ca68bu;
            h ^= h >> 16;
            int pick = (int)(h % 3u);
            switch (pick)
            {
                case 1: // KRIPTA — pale dressed stone, cold blue-green grave-light.
                    return new RuntimeDungeonArchetype(
                        "Kripta",
                        new Color(0.32f, 0.34f, 0.38f),
                        new Color(0.24f, 0.25f, 0.28f),
                        new Color(0.45f, 0.75f, 0.95f),
                        2.8f,
                        new Color(0.30f, 0.55f, 1.00f));
                case 2: // HARABE — sun-bleached sandstone gone mossy, sickly green-gold light.
                    return new RuntimeDungeonArchetype(
                        "Harabe",
                        new Color(0.38f, 0.34f, 0.24f),
                        new Color(0.30f, 0.30f, 0.20f),
                        new Color(0.80f, 0.95f, 0.45f),
                        3.0f,
                        new Color(0.55f, 1.00f, 0.40f));
                default: // MAĞARA — the original dark wet rock and warm firelight.
                    return new RuntimeDungeonArchetype(
                        "Mağara",
                        new Color(0.20f, 0.18f, 0.17f),
                        new Color(0.14f, 0.13f, 0.12f),
                        new Color(1.00f, 0.62f, 0.30f),
                        3.4f,
                        new Color(1.00f, 0.42f, 0.22f));
            }
        }
    }
}
