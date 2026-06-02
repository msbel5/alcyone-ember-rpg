using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class InstitutionsDto : ContentExtensionDto
    {
        public Dictionary<string, Dictionary<string, InstitutionRoleDto>> town_institutions = new Dictionary<string, Dictionary<string, InstitutionRoleDto>>();
        public Dictionary<string, List<InstitutionEventResponseDto>> event_response_rules = new Dictionary<string, List<InstitutionEventResponseDto>>();
        public List<string> severity_order = new List<string>();
        public Dictionary<string, InstitutionVacuumEffectDto> vacuum_effects = new Dictionary<string, InstitutionVacuumEffectDto>();
    }

    public sealed class InstitutionRoleDto : ContentExtensionDto
    {
        public string role_id;
        public string title;
        public string faction_affiliation;
        public int authority_level;
        public List<string> responsibilities = new List<string>();
        public List<string> can_issue = new List<string>();
        public string reports_to;
        public string holder_name;
    }

    public sealed class InstitutionEventResponseDto : ContentExtensionDto { public string role; public string response; public string desc; }
    public sealed class InstitutionVacuumEffectDto : ContentExtensionDto { public List<string> effects = new List<string>(); public int chaos_level; public List<string> potential_successors = new List<string>(); }

    public sealed class SchedulesDocumentDto { public SchedulesCatalogDto schedules = new SchedulesCatalogDto(); }
    public sealed class SchedulesCatalogDto { public Dictionary<string, Dictionary<string, string>> default_schedules = new Dictionary<string, Dictionary<string, string>>(); }

    public sealed class SocialRulesDocumentDto { public SocialRulesDto social_rules = new SocialRulesDto(); }

    public sealed class SocialRulesDto : ContentExtensionDto
    {
        public Dictionary<string, int> interaction_hold_turns = new Dictionary<string, int>();
        public Dictionary<string, object> attitude_dcs = new Dictionary<string, object>();
        public Dictionary<string, string> default_npc_attitude = new Dictionary<string, string>();
        public Dictionary<string, string> default_npc_alignment = new Dictionary<string, string>();
        public Dictionary<string, List<string>> think_topic_skills = new Dictionary<string, List<string>>();
        public List<string> hostile_keywords = new List<string>();
    }
}
