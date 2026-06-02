using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class WorldgenDocumentDto
    {
        public WorldgenConfigDto worldgen = new WorldgenConfigDto();
    }

    public sealed class WorldgenConfigDto : ContentExtensionDto
    {
        public List<string> town_building_types = new List<string>();
        public Dictionary<string, ZoneTilePaletteDto> zone_tile_palettes = new Dictionary<string, ZoneTilePaletteDto>();
        public Dictionary<string, WorldgenBuildingTemplateDto> building_templates = new Dictionary<string, WorldgenBuildingTemplateDto>();
        public Dictionary<string, WorldgenTileSetDto> map_generator_tile_sets = new Dictionary<string, WorldgenTileSetDto>();
        public Dictionary<string, List<WorldgenRoomTemplateDto>> map_generator_room_templates = new Dictionary<string, List<WorldgenRoomTemplateDto>>();
        public Dictionary<string, object> entity_templates_by_location = new Dictionary<string, object>();
        public Dictionary<string, WorldgenZoneRuleDto> zone_entity_rules = new Dictionary<string, WorldgenZoneRuleDto>();
        public Dictionary<string, List<WorldgenZoneLayoutDto>> zone_layouts = new Dictionary<string, List<WorldgenZoneLayoutDto>>();
        public Dictionary<string, object> scene_narration = new Dictionary<string, object>();
    }

    public sealed class ZoneTilePaletteDto : ContentExtensionDto
    {
        public List<List<object>> ground = new List<List<object>>();
        public List<string> accent = new List<string>();
    }

    public sealed class WorldgenBuildingTemplateDto : ContentExtensionDto
    {
        public List<int> size = new List<int>();
        public List<List<string>> tiles = new List<List<string>>();
        public List<WorldgenEntitySlotDto> entity_slots = new List<WorldgenEntitySlotDto>();
        public List<string> zone_affinity = new List<string>();
        public bool is_indoor;
    }

    public sealed class WorldgenEntitySlotDto : ContentExtensionDto
    {
        public List<int> offset = new List<int>();
        public string role;
        public string type;
        public bool required;
    }

    public sealed class WorldgenTileSetDto : ContentExtensionDto { }

    public sealed class WorldgenRoomTemplateDto : ContentExtensionDto
    {
        public string id;
        public string name;
        public string type;
        public List<float> bounds_rel = new List<float>();
    }

    public sealed class WorldgenZoneRuleDto : ContentExtensionDto { }

    public sealed class WorldgenZoneLayoutDto : ContentExtensionDto
    {
        public string id;
        public string zone_type;
        public string name;
        public List<int> bounds = new List<int>();
        public bool is_indoor;
        public int danger_level;
    }
}
