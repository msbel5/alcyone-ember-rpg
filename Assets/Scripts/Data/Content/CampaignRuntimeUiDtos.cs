using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class CampaignTemplatesDocumentDto { public List<CampaignTemplateDto> campaigns = new List<CampaignTemplateDto>(); }

    public sealed class CampaignTemplateDto : ContentExtensionDto
    {
        public string id;
        public string name;
        public string description;
        public string difficulty;
        public int estimated_sessions;
        public List<CampaignTemplateQuestDto> quests = new List<CampaignTemplateQuestDto>();
    }

    public sealed class CampaignTemplateQuestDto : ContentExtensionDto
    {
        public string id;
        public string title;
        public string description;
        public List<string> objectives = new List<string>();
        public int recommended_level;
        public List<string> enemy_ids = new List<string>();
        public Dictionary<string, object> rewards = new Dictionary<string, object>();
        public string next_quest;
    }

    public sealed class CampaignRuntimeDocumentDto { public CampaignRuntimeDto campaign_runtime = new CampaignRuntimeDto(); }

    public sealed class CampaignRuntimeDto : ContentExtensionDto
    {
        public List<string> arc_titles = new List<string>();
        public List<string> arc_premises = new List<string>();
        public List<CampaignRuntimeQuestDto> kill_quests = new List<CampaignRuntimeQuestDto>();
        public List<CampaignRuntimeQuestDto> fetch_quests = new List<CampaignRuntimeQuestDto>();
        public CampaignRuntimeQuestDto explore_quest = new CampaignRuntimeQuestDto();
        public CampaignRuntimeQuestDto dialogue_quest = new CampaignRuntimeQuestDto();
        public List<CampaignRuntimeEventDto> world_events = new List<CampaignRuntimeEventDto>();
    }

    public sealed class CampaignRuntimeQuestDto : ContentExtensionDto
    {
        public string title;
        public string description;
        public string target;
        public int count;
        public string item;
        public string giver;
        public string objective;
    }

    public sealed class CampaignRuntimeEventDto : ContentExtensionDto
    {
        public string event_type;
        public string title;
        public string description;
        public List<string> options = new List<string>();
        public Dictionary<string, object> outcomes = new Dictionary<string, object>();
    }

    public sealed class AdapterPromptDto : ContentExtensionDto
    {
        public string prompt_prefix;
        public string negative_prompt;
        public int seed_offset;
        public Dictionary<string, float> lora_weight_overrides = new Dictionary<string, float>();
    }

    public sealed class RuntimeConfigDocumentDto { public RuntimeConfigDto runtime_config = new RuntimeConfigDto(); }

    public sealed class RuntimeConfigDto : ContentExtensionDto
    {
        public RuntimeLlmConfigDto llm = new RuntimeLlmConfigDto();
        public RuntimeGodotClientConfigDto godot_client = new RuntimeGodotClientConfigDto();
    }

    public sealed class RuntimeLlmConfigDto : ContentExtensionDto
    {
        public string narration_mode_default;
        public RuntimeCopilotConfigDto copilot = new RuntimeCopilotConfigDto();
        public Dictionary<string, string> models = new Dictionary<string, string>();
    }

    public sealed class RuntimeCopilotConfigDto : ContentExtensionDto { public string base_url; public string token_path_default; public string cli_command_default; public Dictionary<string, string> default_headers = new Dictionary<string, string>(); }
    public sealed class RuntimeGodotClientConfigDto : ContentExtensionDto { public string backend_url_default; }

    public sealed class UiPlanDto
    {
        public string kind;
        public string generated_at;
        public int count;
        public List<UiPlanJobDto> jobs = new List<UiPlanJobDto>();
    }

    public sealed class UiPlanJobDto : ContentExtensionDto
    {
        public string id;
        public string category;
        public List<int> size = new List<int>();
        public string prompt_hint;
        public int variants;
    }
}
