using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class QualityTierDto { public string label; public float multiplier; }

    public sealed class InventoryLayoutsDto : ContentExtensionDto
    {
        public Dictionary<string, InventoryShapeDto> shapes = new Dictionary<string, InventoryShapeDto>();
        public Dictionary<string, string> item_type_shapes = new Dictionary<string, string>();
        public List<string> default_equipment_slots = new List<string>();
        public Dictionary<string, object> default_containers = new Dictionary<string, object>();
    }

    public sealed class InventoryShapeDto : ContentExtensionDto
    {
        public List<List<int>> cells = new List<List<int>>();
        public bool rigid;
    }

    public sealed class ProgressionDocumentDto { public ProgressionDto progression = new ProgressionDto(); }

    public sealed class ProgressionDto : ContentExtensionDto
    {
        public List<int> xp_thresholds = new List<int>();
        public Dictionary<string, int> hp_per_level = new Dictionary<string, int>();
        public Dictionary<string, int> sp_per_level = new Dictionary<string, int>();
        public Dictionary<string, string> stat_bonus_by_class = new Dictionary<string, string>();
        public Dictionary<string, int> xp_rewards = new Dictionary<string, int>();
        public Dictionary<string, List<ClassAbilityDto>> class_abilities = new Dictionary<string, List<ClassAbilityDto>>();
        public List<int> skill_xp_thresholds = new List<int>();
        public Dictionary<string, string> skill_level_names = new Dictionary<string, string>();
    }

    public sealed class ClassAbilityDto : ContentExtensionDto
    {
        public string name;
        public string description;
        public bool passive;
        public int required_level;
        public string class_name;
        public int cost;
    }

    public sealed class CharacterCreationDocumentDto { public CharacterCreationConfigDto character_creation = new CharacterCreationConfigDto(); }

    public sealed class CharacterCreationConfigDto : ContentExtensionDto
    {
        public string default_class;
        public string default_adapter;
        public string default_profile;
        public List<string> ability_order = new List<string>();
        public Dictionary<string, string> skill_stat_map = new Dictionary<string, string>();
        public List<CharacterCreationQuestionGroupDto> question_groups = new List<CharacterCreationQuestionGroupDto>();
    }

    public sealed class CharacterCreationQuestionGroupDto : ContentExtensionDto
    {
        public string id;
        public string title;
        public List<CharacterCreationQuestionDto> questions = new List<CharacterCreationQuestionDto>();
    }

    public sealed class CharacterCreationQuestionDto : ContentExtensionDto
    {
        public string id;
        public string prompt;
        public List<CharacterCreationAnswerDto> answers = new List<CharacterCreationAnswerDto>();
    }

    public sealed class CharacterCreationAnswerDto : ContentExtensionDto { public string id; public string text; }

    public sealed class NameBanksDocumentDto { public Dictionary<string, NameBankDto> name_banks = new Dictionary<string, NameBankDto>(); }

    public sealed class NameBankDto : ContentExtensionDto
    {
        public List<string> male_first = new List<string>();
        public List<string> female_first = new List<string>();
        public List<string> surnames = new List<string>();
        public List<string> clan_names = new List<string>();
        public List<string> house_names = new List<string>();
    }

    public sealed class QuestConfigDocumentDto { public QuestConfigDto quest_config = new QuestConfigDto(); }

    public sealed class QuestConfigDto : ContentExtensionDto
    {
        public Dictionary<string, RewardScaleDto> reward_scales = new Dictionary<string, RewardScaleDto>();
        public float severity_multiplier;
        public List<int> rng_gold_range = new List<int>();
        public List<int> rng_xp_range = new List<int>();
        public Dictionary<string, QuestGenerationWeightDto> generation_weights = new Dictionary<string, QuestGenerationWeightDto>();
        public List<ShortageQuestDto> emergent_shortages = new List<ShortageQuestDto>();
        public Dictionary<string, ThinkDcKeywordGroupDto> think_dc_keywords = new Dictionary<string, ThinkDcKeywordGroupDto>();
    }

    public sealed class RewardScaleDto { public int gold; public int xp; }
    public sealed class QuestGenerationWeightDto : ContentExtensionDto { public float base_weight; public string boost_tag; public string boost_condition; public string boost_metric; public int boost_threshold; public float boosted_weight; }
    public sealed class ThinkDcKeywordGroupDto { public int dc; public List<string> keywords = new List<string>(); }
}
