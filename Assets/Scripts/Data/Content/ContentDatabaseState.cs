using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    internal sealed class ContentDatabaseState
    {
        public IReadOnlyDictionary<string, ItemDto> Items;
        public IReadOnlyDictionary<string, RecipeDto> Recipes;
        public IReadOnlyDictionary<string, MaterialDto> Materials;
        public IReadOnlyDictionary<string, FactionDto> Factions;
        public IReadOnlyDictionary<string, ClassDto> Classes;
        public IReadOnlyDictionary<string, SpellDto> Spells;
        public IReadOnlyDictionary<string, MonsterDto> Monsters;
        public IReadOnlyDictionary<string, LocationEntryDto> Locations;
        public IReadOnlyDictionary<string, NpcTemplateDto> NpcTemplates;
        public FactionCatalogDto FactionCatalog;
        public LocationCatalogDto LocationCatalog;
        public WorldgenConfigDto Worldgen;
        public IReadOnlyDictionary<string, BiomeDto> Biomes;
        public IReadOnlyDictionary<string, CultureDto> Cultures;
        public IReadOnlyDictionary<string, WorldBuildingTemplateDto> WorldBuildingTemplates;
        public IReadOnlyDictionary<string, SpeciesTemplateDto> SpeciesTemplates;
        public IReadOnlyDictionary<string, WorldQuestTemplateDto> WorldQuestTemplates;
        public IReadOnlyDictionary<string, WorldFurnitureDto> WorldFurniture;
        public IReadOnlyDictionary<string, WorldProfileDto> WorldProfiles;
        public EconomyConfigDto EconomyConfig;
        public ColonyConfigDto ColonyConfig;
        public IReadOnlyDictionary<string, CaravanDto> Caravans;
        public IReadOnlyDictionary<string, DialogDefDto> DialogDefinitions;
        public HistoryTablesDto HistoryTables;
        public InstitutionsDto Institutions;
        public SchedulesCatalogDto Schedules;
        public SocialRulesDto SocialRules;
        public IReadOnlyList<InteractionRuleDto> InteractionRules;
        public IReadOnlyDictionary<string, ConsequenceRuleDto> ConsequenceRules;
        public LootTablesDto LootTables;
        public IReadOnlyDictionary<string, QualityTierDto> QualityTiers;
        public InventoryLayoutsDto InventoryLayouts;
        public ProgressionDto Progression;
        public CharacterCreationConfigDto CharacterCreation;
        public IReadOnlyDictionary<string, NameBankDto> NameBanks;
        public QuestConfigDto QuestConfig;
        public IReadOnlyDictionary<string, CampaignTemplateDto> CampaignTemplates;
        public CampaignRuntimeDto CampaignRuntime;
        public IReadOnlyDictionary<string, AdapterPromptDto> AdapterPrompts;
        public RuntimeConfigDto RuntimeConfig;
        public UiPlanDto UiPlan;
        public IReadOnlyDictionary<string, CampaignArcDto> CampaignArcs;
        public IReadOnlyDictionary<string, LegacyNpcDto> LegacyNpcs;
        public IReadOnlyDictionary<string, WorldNpcTemplateDto> WorldNpcTemplates;
        public IReadOnlyDictionary<string, WorldAdapterDto> WorldAdapters;
    }
}
