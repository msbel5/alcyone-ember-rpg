using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class WorldQuestTemplatesDocumentDto
    {
        public Dictionary<string, WorldQuestTemplateDto> quest_templates = new Dictionary<string, WorldQuestTemplateDto>();
    }

    public sealed class WorldQuestTemplateDto : ContentExtensionDto
    {
        public string id;
        public string title;
        public string description;
        public List<string> preferred_roles = new List<string>();
    }

    public sealed class WorldFurnitureDocumentDto
    {
        public Dictionary<string, WorldFurnitureDto> furniture = new Dictionary<string, WorldFurnitureDto>();
    }

    public sealed class WorldFurnitureDto : ContentExtensionDto
    {
        public string id;
        public bool blocking;
        public string interaction_type;
        public List<int> tile_size = new List<int>();
    }

    public sealed class WorldProfilesDocumentDto
    {
        public Dictionary<string, WorldProfileDto> profiles = new Dictionary<string, WorldProfileDto>();
    }

    public sealed class WorldProfileDto : ContentExtensionDto
    {
        public string id;
        public string title;
        public int world_width;
        public int world_height;
        public int plate_count;
        public int climate_bands;
        public int region_size;
        public int history_end_year;
    }

    public sealed class WorldNpcTemplatesDocumentDto
    {
        public Dictionary<string, WorldNpcTemplateDto> npc_templates = new Dictionary<string, WorldNpcTemplateDto>();
    }

    public sealed class WorldNpcTemplateDto : ContentExtensionDto
    {
        public string id;
        public string sprite_template;
        public string activity;
        public List<string> inventory = new List<string>();
        public List<string> traits = new List<string>();
        public List<string> context_actions = new List<string>();
        public List<string> first_names = new List<string>();
        public List<string> surnames = new List<string>();
    }

    public sealed class WorldAdapterDto : ContentExtensionDto
    {
        public string id;
        public string title;
        public List<string> allowed_species = new List<string>();
        public Dictionary<string, string> species_labels = new Dictionary<string, string>();
        public WorldAdapterStarterContentDto starter_content = new WorldAdapterStarterContentDto();
    }

    public sealed class WorldAdapterStarterContentDto : ContentExtensionDto
    {
        public List<string> player_classes = new List<string>();
        public string default_player_class;
        public string starting_focus;
    }
}
