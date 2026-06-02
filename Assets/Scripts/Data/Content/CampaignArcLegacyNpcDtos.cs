using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class CampaignArcDto : ContentExtensionDto
    {
        public string id;
        public string title;
        public string description;
        public int recommended_level;
        public double max_cr;
        public double min_cr;
        public List<CampaignActDto> acts = new List<CampaignActDto>();
    }

    public sealed class CampaignActDto : ContentExtensionDto
    {
        public string id;
        public string name;
        public string description;
        public List<CampaignEncounterDto> encounters = new List<CampaignEncounterDto>();
        public List<CampaignObjectiveDto> objectives = new List<CampaignObjectiveDto>();
        public List<CampaignRewardDto> rewards = new List<CampaignRewardDto>();
        public string quest_id;
    }

    public sealed class CampaignObjectiveDto : ContentExtensionDto
    {
        public string id;
        public string description;
        public string type;
        public string target;
        public int required_count;
    }

    public sealed class CampaignRewardDto : ContentExtensionDto
    {
        public string item_id;
        public int quantity;
    }

    public sealed class CampaignEncounterDto : ContentExtensionDto
    {
        public string id;
        public string name;
        public string description;
        public List<string> monsters = new List<string>();
        public string difficulty;
        public string exploration_note;
        public string tutorial_hint;
    }

    public sealed class LegacyNpcsDocumentDto { public List<LegacyNpcDto> npcs = new List<LegacyNpcDto>(); }

    public sealed class LegacyNpcDto : ContentExtensionDto
    {
        public string id;
        public string name;
        public string race;
        public string role;
        public string faction_alignment;
        public LegacyNpcPersonalityDto personality = new LegacyNpcPersonalityDto();
        public Dictionary<string, List<string>> dialogue_snippets = new Dictionary<string, List<string>>();
        public Dictionary<string, int> relationship_modifiers = new Dictionary<string, int>();
    }

    public sealed class LegacyNpcPersonalityDto : ContentExtensionDto
    {
        public List<string> traits = new List<string>();
        public List<string> motivations = new List<string>();
        public List<string> fears = new List<string>();
    }
}
