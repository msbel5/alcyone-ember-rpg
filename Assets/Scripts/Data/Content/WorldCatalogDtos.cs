using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class BiomesDocumentDto
    {
        public Dictionary<string, BiomeDto> biomes = new Dictionary<string, BiomeDto>();
    }

    public sealed class BiomeDto : ContentExtensionDto
    {
        public string id;
        public List<float> temperature_range = new List<float>();
        public List<float> moisture_range = new List<float>();
        public List<float> elevation_range = new List<float>();
        public Dictionary<string, float> terrain_weights = new Dictionary<string, float>();
        public List<string> resources = new List<string>();
        public List<string> fauna = new List<string>();
        public float settlement_weight;
    }

    public sealed class CulturesDocumentDto
    {
        public Dictionary<string, CultureDto> cultures = new Dictionary<string, CultureDto>();
    }

    public sealed class CultureDto : ContentExtensionDto
    {
        public string id;
        public Dictionary<string, int> values = new Dictionary<string, int>();
        public Dictionary<string, string> ethics = new Dictionary<string, string>();
        public string governance_bias;
        public Dictionary<string, float> institution_bias = new Dictionary<string, float>();
    }

    public sealed class BuildingTemplatesDocumentDto
    {
        public Dictionary<string, WorldBuildingTemplateDto> building_templates = new Dictionary<string, WorldBuildingTemplateDto>();
    }

    public sealed class WorldBuildingTemplateDto : ContentExtensionDto
    {
        public string id;
        public List<int> footprint = new List<int>();
        public string wall_material;
        public string floor_kind;
        public List<string> npc_roles = new List<string>();
        public List<RequiredFurnitureDto> required_furniture = new List<RequiredFurnitureDto>();
    }

    public sealed class RequiredFurnitureDto : ContentExtensionDto
    {
        public string kind;
        public List<int> anchor = new List<int>();
    }

    public sealed class SpeciesTemplatesDocumentDto
    {
        public Dictionary<string, SpeciesTemplateDto> species_templates = new Dictionary<string, SpeciesTemplateDto>();
    }

    public sealed class SpeciesTemplateDto : ContentExtensionDto
    {
        public string id;
        public string name;
        public bool sapient;
        public List<string> habitats = new List<string>();
        public List<float> temperature_range = new List<float>();
        public List<float> moisture_range = new List<float>();
        public List<string> physiology_tags = new List<string>();
        public List<string> cognition_tags = new List<string>();
        public string social_structure;
        public List<string> domestication_roles = new List<string>();
        public List<string> supernatural_tags = new List<string>();
        public string culture_hint;
    }
}
