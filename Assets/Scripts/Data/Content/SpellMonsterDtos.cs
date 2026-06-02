using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class SpellsDocumentDto
    {
        public List<SpellDto> spells = new List<SpellDto>();
    }

    public sealed class SpellDto
    {
        public string name;
        public int cost;
        public int range;
        public string target_type;
        public string school;
        public int level;
        public string description;
        public List<SpellEffectDto> effects = new List<SpellEffectDto>();
    }

    public sealed class SpellEffectDto
    {
        public string type;
        public object amount;
        public string damage_type;
        public string stat;
        public int duration;
        public string save;
        public int save_dc;
        public string status;
    }

    public sealed class MonstersDocumentDto
    {
        public List<MonsterDto> monsters = new List<MonsterDto>();
    }

    public sealed class MonsterDto
    {
        public string id;
        public string name;
        public string type;
        public float cr;
        public int hp;
        public int armor_class;
        public int speed;
        public Dictionary<string, int> stats = new Dictionary<string, int>();
        public List<MonsterAttackDto> attacks = new List<MonsterAttackDto>();
        public Dictionary<string, object> abilities = new Dictionary<string, object>();
        public int xp_reward;
        public List<MonsterLootDto> loot_table = new List<MonsterLootDto>();
    }

    public sealed class MonsterAttackDto
    {
        public string name;
        public string damage_dice;
        public string damage_type;
        public int attack_bonus;
    }

    public sealed class MonsterLootDto
    {
        public string id;
        public string rarity;
    }
}
