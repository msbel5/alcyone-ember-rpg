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

        public static readonly EquipmentSlotData[] EquipSlots =
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

        public static readonly InventoryItemData[] Inventory =
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

        public static readonly SpellSchoolData[] SpellSchools =
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

        public static readonly SpellBarSlotData[] SpellBar =
        {
            new(1, "Flame Bolt"),
            new(2, "Minor Heal"),
            new(3, null),
            new(4, null),
            new(5, null),
        };

        public static readonly QuestData[] Quests =
        {
            new(1, "The Missing Caravan", "active",
                "Three wagons went east past the dead oak and never came back.",
                new[]
                {
                    new QuestTaskData("Investigate the road east of Ashton", true),
                    new QuestTaskData("Find evidence at the ambush site", true),
                    new QuestTaskData("Identify the raider camp", false),
                    new QuestTaskData("Rescue or confirm fate of survivors", false),
                }),
            new(2, "The Sealed Shrine", "active",
                "A forbidden shrine lies beneath old stone. The carvings warn.",
                new[]
                {
                    new QuestTaskData("Locate the shrine entrance", false),
                    new QuestTaskData("Decipher the carvings", false),
                }),
            new(3, "A Beggar's Debt", "completed",
                "You gave the coin. The beggar remembered.",
                new[] { new QuestTaskData("Deliver the coin", true) }),
        };

        public static readonly ColonyNpcData[] ColonyNpcs =
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

        public static readonly OracleData Oracle = new("The Oracle", "Keeper of the World's Memory", null, "⌖");

        public static readonly string[] DmNarrations =
        {
            "The road narrows. A raven follows you at a fixed distance — it has been following you since the oak.",
            "The forge-fire spits ash. Something in the iron remembers older shapes.",
            "Three guards at the gate. They are watching the horizon, not the door. They are expecting someone.",
            "A cold wind blows from the North. The stars align in silence.",
        };

        public static readonly WorldLocationData[] WorldLocations =
        {
            new("ashton", "Ashton", 38, 58, "Town", true),
            new("oakroad", "Dead Oak Road", 52, 52, "Road", true),
            new("shrine", "Sealed Shrine", 64, 44, "Dungeon", false),
            new("camp", "Raider Camp", 74, 50, "Camp", false),
            new("city", "Crestfall", 22, 34, "City", true),
            new("ruin", "Old Watchtower", 48, 28, "Ruin", false),
        };

        public static readonly DialogNpcData DialogNpc = new(
            "Gareth the Smith",
            "../assets/characters/beggar.png",
            "Aye, I heard the noise on the east road. Three nights running now. If the caravan doesn't come through soon, we'll be short iron before winter sets in.",
            new[]
            {
                new TopicData("the missing caravan", "Three wagons. Two guards. The merchant’s boy was with them — young Edric. I told the captain before they left: the east road’s not safe after the moon changes."),
                new TopicData("iron supplies", "I can smelt what the colony brings in, but I can't smelt rumor. Get me ore, or get me that caravan."),
                new TopicData("work", "You want work? Find out what happened on that road. Coin's in it for you — more than the captain will offer."),
                new TopicData("the east road", "There's an old waystone two hours east. Something's been moving near it. I've seen the smoke."),
            });

        public static readonly string[] OraclePrompts =
        {
            "What stirs in the dark…",
            "What fate lies ahead…",
            "What does the world hide from me…",
            "Who moves against me…",
        };

        public static readonly TradeItemData[] LootItems =
        {
            new("l1", "Iron Dagger", "Weapon", 18, 1),
            new("l2", "Dried Meat", "Food", 4, 1),
            new("l3", "Silver Ring", "Misc", 60, 1),
            new("l4", "Lockpick", "Tool", 5, 1),
            new("l5", "Pouch (32 gp)", "Currency", 32, 1),
        };

        public static readonly TradeItemData[] MerchantItems =
        {
            new("m1", "Iron Sword", "Weapon", 65, 2),
            new("m2", "Chain Shirt", "Armor", 120, 1),
            new("m3", "Healing Potion", "Potion", 30, 5),
            new("m4", "Torch ×5", "Tool", 8, 10),
            new("m5", "Rations", "Food", 4, 20),
            new("m6", "Rope (10m)", "Tool", 12, 4),
        };

        public static readonly SaveSlotData[] SaveSlots =
        {
            new(1, "Autosave", "Ashton Crossroads", "3d 2h", "4", "2026-06-04 18:42"),
            new(2, "Slot 2", "Ashton Crossroads", "3d 1h", "4", "2026-06-04 17:10"),
            new(3, "Before Shrine", "Dead Oak Road", "2d 6h", "3", "2026-06-03 14:22"),
            new(4, "Slot 4", "Empty", "—", "—", "—"),
            new(5, "Slot 5", "Empty", "—", "—", "—"),
        };

        public static readonly CraftingRecipeData[] CraftingRecipes =
        {
            new("campfire_tonic", "Campfire Tonic", "Alchemy", 18,
                "A bitter restorative brewed from field herbs and clean water.",
                new[]
                {
                    new IngredientData("Fireweed", 2, true),
                    new IngredientData("Spring Water", 1, true),
                    new IngredientData("Ash Salt", 1, false),
                }),
            new("iron_bolts", "Iron Bolts", "Smithing", 12,
                "Simple ammunition bundled for field repairs and light ranged use.",
                new[]
                {
                    new IngredientData("Iron Ingot", 1, true),
                    new IngredientData("Feather Fletching", 2, true),
                    new IngredientData("Binding Wire", 1, true),
                }),
            new("ember_charm", "Ember Charm", "Trinket", 45,
                "A rune-tied charm that glows faintly near old wards.",
                new[]
                {
                    new IngredientData("Bronze Wire", 2, true),
                    new IngredientData("Old Sigil Fragment", 1, false),
                    new IngredientData("Resin", 1, true),
                }),
            new("traveler_rations", "Traveler Rations", "Cooking", 9,
                "A dry-packed meal meant to last one hard day on the road.",
                new[]
                {
                    new IngredientData("Dried Meat", 1, true),
                    new IngredientData("Hard Bread", 2, true),
                    new IngredientData("Salt Pouch", 1, true),
                }),
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
    public sealed record SpellBarSlotData(int Slot, string Spell);
    public sealed record QuestData(int Id, string Title, string Status, string Description, QuestTaskData[] Tasks);
    public sealed record QuestTaskData(string Text, bool Done);
    public sealed record ColonyNpcData(string Name, string Role, int Hp, int HpMax, NeedData[] Needs, string Mood, string Task);
    public sealed record NeedData(string Name, int Value);
    public sealed record OracleData(string Name, string Subtitle, string PortraitPath, string Sigil);
    public sealed record WorldLocationData(string Id, string Name, float XPercent, float YPercent, string Type, bool Visited);
    public sealed record DialogNpcData(string Name, string PortraitPath, string Greeting, TopicData[] Topics);
    public sealed record TopicData(string Topic, string Response);
    public sealed record TradeItemData(string Id, string Name, string Type, int Value, int Quantity);
    public sealed record SaveSlotData(int Number, string Name, string Location, string PlayedTime, string Level, string Date);
    public sealed record CraftingRecipeData(string Id, string Name, string Category, int OutputValue, string Description, IngredientData[] Ingredients);
    public sealed record IngredientData(string Name, int Quantity, bool Available);
}
