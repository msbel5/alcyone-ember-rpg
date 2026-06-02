using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class InteractionRuleDto
    {
        public string target_type;
        public string interaction_type;
        public string skill;
        public List<int> dc_range = new List<int>();
        public int ap_cost;
        public List<string> requirements = new List<string>();
    }

    public sealed class ConsequenceRulesDocumentDto { public List<ConsequenceRuleDto> consequence_rules = new List<ConsequenceRuleDto>(); }

    public sealed class ConsequenceRuleDto
    {
        public string rule_id;
        public string trigger_type;
        public Dictionary<string, object> conditions = new Dictionary<string, object>();
        public List<ConsequenceEffectDto> effects = new List<ConsequenceEffectDto>();
        public int delay_hours;
        public float probability;
        public string description;
    }

    public sealed class ConsequenceEffectDto : ContentExtensionDto
    {
        public string type;
        public string target;
        public Dictionary<string, object> @params = new Dictionary<string, object>();
        public string description;
    }

    public sealed class LootTablesDocumentDto { public LootTablesDto loot_tables = new LootTablesDto(); }

    public sealed class LootTablesDto : ContentExtensionDto
    {
        public Dictionary<string, float> rarity_drop_chances = new Dictionary<string, float>();
        public List<string> rarity_order = new List<string>();
        public float base_drop_chance;
        public Dictionary<string, MonsterLootTableDto> monster_tables = new Dictionary<string, MonsterLootTableDto>();
    }

    public sealed class MonsterLootTableDto : ContentExtensionDto
    {
        public List<GuaranteedLootDto> guaranteed = new List<GuaranteedLootDto>();
        public List<LootDropDto> drops = new List<LootDropDto>();
        public List<int> gold_range = new List<int>();
    }

    public sealed class GuaranteedLootDto
    {
        public string item_id;
        public List<int> quantity_range = new List<int>();
    }

    public sealed class LootDropDto
    {
        public string item_id;
        public float drop_chance;
        public List<int> quantity_range = new List<int>();
        public string rarity;
    }
}
