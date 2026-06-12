using System.Collections.Generic;

// Design note:
// WorldBestiaryCatalog is the deterministic F29 monster table: five base types + the boss rule.
// Inputs: a dungeon archetype name (the presentation's Mağara/Kripta/Harabe palette pick) and a
// dweller slot index. Outputs: a full BestiaryEntry — display prefix, combat stats, billboard
// sprite role, hit-impact material, and the SDXL forge prompt that generates the real sprite when
// the forge is on. Pure Simulation: no Unity dependency; the same archetype + slot always yields
// the same type, so worldgen goldens and saves stay deterministic.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3; MASTER_MECHANICS_BIBLE.md §12 combat dice.
namespace EmberCrpg.Simulation.Bestiary
{
    /// <summary>One bestiary row: identity, dice, presentation keys, forge prompt.</summary>
    public sealed class BestiaryEntry
    {
        public BestiaryEntry(
            string key, string displayPrefix, string spriteRole, string hitMaterial,
            string forgePrompt, int accuracy, int dodge, int armor, int baseDamage, int healthMax)
        {
            Key = key;
            DisplayPrefix = displayPrefix;
            SpriteRole = spriteRole;
            HitMaterial = hitMaterial;
            ForgePrompt = forgePrompt;
            Accuracy = accuracy;
            Dodge = dodge;
            Armor = armor;
            BaseDamage = baseDamage;
            HealthMax = healthMax;
        }

        public string Key { get; }
        /// <summary>Names are "&lt;prefix&gt;&lt;settlement&gt;" — the prefix both names the monster
        /// and (ending in " of ") lets the presentation resolve the type back from an actor name.</summary>
        public string DisplayPrefix { get; }
        public string SpriteRole { get; }
        public string HitMaterial { get; }
        public string ForgePrompt { get; }
        public int Accuracy { get; }
        public int Dodge { get; }
        public int Armor { get; }
        public int BaseDamage { get; }
        public int HealthMax { get; }
    }

    /// <summary>F29: the six-strong bestiary — bandit/skeleton/wolf/spider/ghost + boss variants
    /// (the boss takes the archetype's APEX type with 2× health / 1.5× damage).</summary>
    public static class WorldBestiaryCatalog
    {
        public const string BanditKey = "bandit";
        public const string SkeletonKey = "skeleton";
        public const string WolfKey = "wolf";
        public const string SpiderKey = "spider";
        public const string GhostKey = "ghost";
        public const string DefaultHitMaterial = "flesh";

        private static readonly BestiaryEntry[] _all =
        {
            // Balance anchor: the v0.3 outlaw baseline was acc 30 / dodge 20 / armor 4 / dmg 10 / hp 22.
            // Prompt bodies follow the HOUSE STYLE: descriptor-only, no style/count/backdrop tokens
            // (the forge wraps them in StaticPromptCatalog.EmberNpcSpriteHeader, which carries
            // "single subject centered" etc.; "sprite/sheet" vocabulary is banned — it correlates
            // with multi-figure sheets on guidance-0 pipelines).
            new BestiaryEntry(
                BanditKey, "Bandit of ", "monster_bandit", "flesh",
                "a ragged bandit raider in dark leather hood and cloak, rusted shortsword low at the hip, wary crouch",
                accuracy: 30, dodge: 20, armor: 4, baseDamage: 10, healthMax: 22),
            new BestiaryEntry(
                SkeletonKey, "Bone Walker of ", "monster_skeleton", "bone",
                "an undead skeleton warrior of bare yellowed bone, one broken pauldron, slow reaching stance",
                accuracy: 28, dodge: 12, armor: 8, baseDamage: 9, healthMax: 18),
            new BestiaryEntry(
                WolfKey, "Fen Wolf of ", "monster_wolf", "hide",
                "a feral grey fen wolf, hackles raised, bared teeth, lean flanks, low stalking side profile",
                accuracy: 34, dodge: 28, armor: 1, baseDamage: 8, healthMax: 16),
            new BestiaryEntry(
                SpiderKey, "Pit Spider of ", "monster_spider", "chitin",
                "a giant pit spider of dark violet chitin, eight bristled legs spread wide, clustered eyes",
                accuracy: 32, dodge: 24, armor: 2, baseDamage: 7, healthMax: 12),
            new BestiaryEntry(
                GhostKey, "Grave Wisp of ", "monster_ghost", "wail",
                "a spectral grave wisp, translucent pale cyan shroud trailing into mist, hollow dark face",
                accuracy: 26, dodge: 32, armor: 0, baseDamage: 11, healthMax: 14),
        };

        public static IReadOnlyList<BestiaryEntry> All => _all;

        public static BestiaryEntry Find(string key)
        {
            for (var i = 0; i < _all.Length; i++)
                if (_all[i].Key == key) return _all[i];
            return null;
        }

        /// <summary>Archetype → ordered type rotation. Cave runs beasts, crypt runs the dead,
        /// ruin runs squatters; unknown archetypes fall back to the cave mix.</summary>
        public static IReadOnlyList<string> TypesFor(string archetypeName)
        {
            switch (archetypeName)
            {
                // Every rotation carries THREE types so any delve can stage the trio frame.
                case "Kripta": return new[] { SkeletonKey, GhostKey, SpiderKey };
                case "Harabe": return new[] { BanditKey, SkeletonKey, SpiderKey };
                default: return new[] { WolfKey, SpiderKey, BanditKey }; // Mağara + fallback
            }
        }

        /// <summary>The boss ("Warden of X") wears the archetype's apex type.</summary>
        public static string ApexKeyFor(string archetypeName)
        {
            switch (archetypeName)
            {
                case "Kripta": return GhostKey;
                case "Harabe": return BanditKey;
                default: return WolfKey;
            }
        }

        /// <summary>Deterministic per-slot pick: the same archetype + slot is always the same type.</summary>
        public static BestiaryEntry EntryForSlot(string archetypeName, int slotIndex)
        {
            var rotation = TypesFor(archetypeName);
            var index = slotIndex < 0 ? 0 : slotIndex % rotation.Count;
            return Find(rotation[index]);
        }

        /// <summary>Resolve the type back from an actor display name ("Fen Wolf of X" → wolf).</summary>
        public static BestiaryEntry FromActorName(string actorName)
        {
            if (string.IsNullOrEmpty(actorName)) return null;
            for (var i = 0; i < _all.Length; i++)
                if (actorName.StartsWith(_all[i].DisplayPrefix, System.StringComparison.Ordinal))
                    return _all[i];
            return null;
        }

        public static bool IsBestiaryName(string actorName) => FromActorName(actorName) != null;

        /// <summary>Hit-impact material for a struck actor; non-bestiary targets thud as flesh.</summary>
        public static string HitMaterialForName(string actorName)
        {
            var entry = FromActorName(actorName);
            return entry == null ? DefaultHitMaterial : entry.HitMaterial;
        }
    }
}
