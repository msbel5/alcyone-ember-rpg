using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class LocationsDocumentDto
    {
        public LocationCatalogDto locations = new LocationCatalogDto();
    }

    public sealed class LocationCatalogDto
    {
        public LocationSceneDto default_opening_scene = new LocationSceneDto();
        public Dictionary<string, int> location_stock_baseline = new Dictionary<string, int>();
        public List<LocationSceneDto> opening_scenes = new List<LocationSceneDto>();
        public Dictionary<string, int[]> scene_anchor_offsets = new Dictionary<string, int[]>();
        public Dictionary<string, List<LocationRoleDto>> scene_role_sets = new Dictionary<string, List<LocationRoleDto>>();
        public Dictionary<string, string> role_anchor_map = new Dictionary<string, string>();
        public Dictionary<string, List<string>> npc_visuals = new Dictionary<string, List<string>>();
        public Dictionary<string, WorkstationSpecDto> workstation_specs = new Dictionary<string, WorkstationSpecDto>();
        public Dictionary<string, string> workstation_anchors = new Dictionary<string, string>();
        public Dictionary<string, List<string>> role_production = new Dictionary<string, List<string>>();
        public Dictionary<string, Dictionary<string, int>> role_skill_profiles = new Dictionary<string, Dictionary<string, int>>();
        public Dictionary<string, Dictionary<string, object>> role_stats = new Dictionary<string, Dictionary<string, object>>();
        public List<LocationEntryDto> location_list = new List<LocationEntryDto>();
    }

    public sealed class LocationSceneDto
    {
        public string location;
        public string description;
    }

    public sealed class LocationRoleDto
    {
        public string role;
        public string faction;
    }

    public sealed class WorkstationSpecDto
    {
        public string name;
        public string glyph;
        public string color;
    }

    public sealed class LocationEntryDto
    {
        public string location_id;
        public string name;
        public string type;
        public string description;
        public List<string> scene_role_sets = new List<string>();
        public List<string> connected_locations = new List<string>();
        public int danger_level;
        public List<string> notable_features = new List<string>();
    }

    public sealed class NpcTemplatesDocumentDto
    {
        public List<NpcTemplateDto> npc_templates = new List<NpcTemplateDto>();
    }

    public sealed class NpcTemplateDto
    {
        public string id;
        public string name;
        public string role;
        public List<string> personality = new List<string>();
        public string speech_style;
        public Dictionary<string, List<string>> dialogue = new Dictionary<string, List<string>>();
        public string disposition;
        public string faction;
        public int[] level_range;
        public List<string> shop_inventory = new List<string>();
    }
}
