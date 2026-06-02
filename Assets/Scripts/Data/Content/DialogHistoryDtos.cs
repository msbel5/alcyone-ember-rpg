using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class DialogDefsDocumentDto { public List<DialogDefDto> dialog_defs = new List<DialogDefDto>(); }

    public sealed class DialogDefDto
    {
        public string dialog_id;
        public string npc_id;
        public string role;
        public List<DialogStateDto> states = new List<DialogStateDto>();
    }

    public sealed class DialogStateDto
    {
        public string state_id;
        public string text;
        public List<DialogTransitionDto> transitions = new List<DialogTransitionDto>();
    }

    public sealed class DialogTransitionDto : ContentExtensionDto
    {
        public string transition_id;
        public string text;
        public DialogConditionDto condition = new DialogConditionDto();
        public string next_state_id;
        public List<DialogActionDto> actions = new List<DialogActionDto>();
        public bool terminates;
    }

    public sealed class DialogConditionDto : ContentExtensionDto
    {
        public string condition_type;
        public Dictionary<string, object> @params = new Dictionary<string, object>();
        public List<DialogConditionDto> children = new List<DialogConditionDto>();
    }

    public sealed class DialogActionDto : ContentExtensionDto
    {
        public string action_type;
        public Dictionary<string, object> @params = new Dictionary<string, object>();
        public string description;
    }

    public sealed class HistoryTablesDocumentDto
    {
        public HistoryTablesDto history_tables = new HistoryTablesDto();
    }

    public sealed class HistoryTablesDto : ContentExtensionDto
    {
        public int present_year;
        public List<string> all_factions = new List<string>();
        public List<string> scholarly_roles = new List<string>();
        public List<string> severity_levels = new List<string>();
        public List<string> war_names = new List<string>();
        public List<string> kingdom_names = new List<string>();
        public List<string> catastrophe_names = new List<string>();
    }
}
