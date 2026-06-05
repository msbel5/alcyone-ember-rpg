using System;
using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public static class IgMockData
    {
        public static readonly PlayerData DefaultPlayer = new(
            "Cinder Vey",
            "Scholar",
            4,
            3420,
            5000,
            38,
            50,
            55,
            80,
            72,
            100,
            240,
            "The Ember",
            "Neutral Good",
            new[]
            {
                new StatData("MIG", 10),
                new StatData("AGI", 14),
                new StatData("END", 11),
                new StatData("MND", 18),
                new StatData("INS", 16),
                new StatData("PRE", 13),
            },
            new[] { "investigation", "arcana", "perception", "insight", "history" });

        // Live character: InGameUiController.RefreshLivePlayer overwrites this with the real created character's
        // name / six attributes / vitals when a screen opens; falls back to DefaultPlayer when there is no bound
        // world (proof + EditMode contexts). Level/XP/class/birthsign/skills/gold stay mock until the domain
        // tracks them.
        public static PlayerData Player = DefaultPlayer;

        public static readonly EquipmentSlotData[] DefaultEquipSlots =
        {
            new("head", "Head", "⬡", false, null),
            new("chest", "Chest", "▣", true, "Leather Jerkin"),
            new("legs", "Legs", "▬", true, "Wool Trousers"),
            new("feet", "Feet", "▭", false, null),
            new("mainhand", "Main Hand", "⚔", true, "Iron Shortsword"),
            new("offhand", "Off Hand", "◈", false, null),
            new("ring", "Ring", "◯", false, null),
            new("neck", "Neck", "◇", false, null),
        };
        public static EquipmentSlotData[] EquipSlots = DefaultEquipSlots;

        public static readonly InventoryItemData[] DefaultInventory =
        {
            new(1, "Iron Shortsword", "Weapon", 2.0f, 40, 1, true),
            new(2, "Leather Jerkin", "Armor", 3.5f, 25, 1, true),
            new(3, "Wool Trousers", "Armor", 1.5f, 10, 1, true),
            new(4, "Healing Potion", "Potion", 0.5f, 30, 3, false),
            new(5, "Scroll of Flame", "Scroll", 0.1f, 55, 2, false),
            new(6, "Lockpick", "Tool", 0.1f, 5, 6, false),
            new(7, "Torch", "Tool", 0.5f, 2, 4, false),
            new(8, "Rations (1 day)", "Food", 1.0f, 3, 7, false),
            new(9, "Silver Coin (×20)", "Currency", 0.2f, 20, 1, false),
            new(10, "Bandit's Dagger", "Weapon", 0.8f, 22, 1, false),
            new(11, "Antidote", "Potion", 0.3f, 18, 1, false),
            new(12, "Old Map Fragment", "Quest", 0.1f, 0, 1, false),
        };
        public static InventoryItemData[] Inventory = DefaultInventory;

        public static readonly SpellSchoolData[] DefaultSpellSchools =
        {
            new("Destruction", new[]
            {
                new SpellData("Flame Bolt", 12, "8–14 fire", "Medium", "Instant"),
                new SpellData("Frost Ray", 14, "6–12 frost", "Short", "Instant"),
                new SpellData("Shock Burst", 16, "10–18 shock", "Medium", "Instant"),
            }),
            new("Restoration", new[]
            {
                new SpellData("Minor Heal", 8, "Restore 12 HP", "Self", "Instant"),
                new SpellData("Stamina Tap", 6, "Restore 15 FAT", "Self", "Instant"),
                new SpellData("Cure Poison", 10, "Remove 1 poison", "Self", "Instant"),
            }),
            new("Illusion", new[]
            {
                new SpellData("Shadow Veil", 20, "Invisible 8s", "Self", "8s"),
                new SpellData("Fear", 18, "-MIG on target", "Short", "6s"),
                new SpellData("Charm", 22, "+PRE vs target", "Medium", "12s"),
            }),
            new("Conjuration", new[]
            {
                new SpellData("Summon Shade", 30, "Ally 20HP/10s", "Short", "10s"),
                new SpellData("Soul Bind", 25, "Trap soul", "Short", "Instant"),
            }),
            new("Mysticism", new[]
            {
                new SpellData("Detect Life", 10, "Sense NPCs 30m", "Self", "15s"),
                new SpellData("Telekinesis", 15, "Move item 8m", "Long", "Instant"),
            }),
            new("Alteration", new[]
            {
                new SpellData("Water Breath", 18, "Breathe water", "Self", "60s"),
                new SpellData("Feather", 12, "-50% carry wt", "Self", "30s"),
            }),
        };
        public static SpellSchoolData[] SpellSchools = DefaultSpellSchools;

        public static readonly SpellBarSlotData[] DefaultSpellBar =
        {
            new(1, "Flame Bolt", true),
            new(2, "Minor Heal"),
            new(3, null),
            new(4, null),
            new(5, null),
        };
        public static SpellBarSlotData[] SpellBar = DefaultSpellBar;

        public static readonly ColonyNpcData[] DefaultColonyNpcs =
        {
            new("Gareth the Smith", "Blacksmith", 80, 80,
                new[] { new NeedData("Hunger", 62), new NeedData("Fatigue", 45), new NeedData("Thirst", 70) },
                "Content", "Forging iron nails"),
            new("Mira Coldwell", "Herbalist", 55, 55,
                new[] { new NeedData("Hunger", 88), new NeedData("Fatigue", 60), new NeedData("Thirst", 40) },
                "Anxious", "Gathering fireweed"),
            new("Old Rook", "Guard", 65, 90,
                new[] { new NeedData("Hunger", 30), new NeedData("Fatigue", 90), new NeedData("Thirst", 55) },
                "Tired", "Patrolling east wall"),
            new("Sable", "Scout", 70, 70,
                new[] { new NeedData("Hunger", 50), new NeedData("Fatigue", 35), new NeedData("Thirst", 60) },
                "Restless", "Idle"),
        };
        public static ColonyNpcData[] ColonyNpcs = DefaultColonyNpcs;

        public static readonly OracleData Oracle = new("The Oracle", "Keeper of the World's Memory", null, "⌖");

        public static readonly string[] OraclePrompts =
        {
            "What stirs in the dark…",
            "What fate lies ahead…",
            "What does the world hide from me…",
            "Who moves against me…",
        };

        public static SpellSchoolData GetSpellSchool(string name)
        {
            for (int i = 0; i < SpellSchools.Length; i++)
            {
                if (string.Equals(SpellSchools[i].Name, name, StringComparison.Ordinal))
                    return SpellSchools[i];
            }

            return SpellSchools[0];
        }

        public static List<SpellData> GetAllSpells()
        {
            var list = new List<SpellData>();
            for (int i = 0; i < SpellSchools.Length; i++)
            {
                list.AddRange(SpellSchools[i].Spells);
            }

            return list;
        }
    }

    public sealed record PlayerData(
        string Name,
        string ClassName,
        int Level,
        int Xp,
        int XpNext,
        int Hp,
        int HpMax,
        int Fatigue,
        int FatigueMax,
        int Mana,
        int ManaMax,
        int Gold,
        string Birthsign,
        string Alignment,
        StatData[] Stats,
        string[] Skills);

    public sealed record StatData(string Abbr, int Value);
    public sealed record EquipmentSlotData(string Id, string Label, string Icon, bool Filled, string Item);
    public sealed record InventoryItemData(int Id, string Name, string Type, float Weight, int Value, int Quantity, bool Equipped);
    public sealed record SpellSchoolData(string Name, SpellData[] Spells);
    public sealed record SpellData(string Name, int ManaCost, string Effect, string Range, string Duration);
    public sealed record SpellBarSlotData(int Slot, string Spell, bool Selected = false);
    public sealed record ColonyNpcData(string Name, string Role, int Hp, int HpMax, NeedData[] Needs, string Mood, string Task);
    public sealed record NeedData(string Name, int Value);
    public sealed record OracleData(string Name, string Subtitle, string PortraitPath, string Sigil);
}
