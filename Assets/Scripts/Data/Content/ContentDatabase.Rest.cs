using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed partial class ContentDatabase
    {
        public WorldgenConfigDto Worldgen { get; }
        public IReadOnlyDictionary<string, BiomeDto> Biomes { get; }
        public IReadOnlyDictionary<string, CultureDto> Cultures { get; }
        public IReadOnlyDictionary<string, WorldBuildingTemplateDto> WorldBuildingTemplates { get; }
        public IReadOnlyDictionary<string, SpeciesTemplateDto> SpeciesTemplates { get; }
        public IReadOnlyDictionary<string, WorldQuestTemplateDto> WorldQuestTemplates { get; }
        public IReadOnlyDictionary<string, WorldFurnitureDto> WorldFurniture { get; }
        public IReadOnlyDictionary<string, WorldProfileDto> WorldProfiles { get; }
        public EconomyConfigDto EconomyConfig { get; }
        public ColonyConfigDto ColonyConfig { get; }
        public IReadOnlyDictionary<string, CaravanDto> Caravans { get; }
        public IReadOnlyDictionary<string, DialogDefDto> DialogDefinitions { get; }
        public HistoryTablesDto HistoryTables { get; }
        public InstitutionsDto Institutions { get; }
        public SchedulesCatalogDto Schedules { get; }
        public SocialRulesDto SocialRules { get; }
        public IReadOnlyList<InteractionRuleDto> InteractionRules { get; }
        public IReadOnlyDictionary<string, ConsequenceRuleDto> ConsequenceRules { get; }
        public LootTablesDto LootTables { get; }
        public IReadOnlyDictionary<string, QualityTierDto> QualityTiers { get; }
        public InventoryLayoutsDto InventoryLayouts { get; }
        public ProgressionDto Progression { get; }
        public CharacterCreationConfigDto CharacterCreation { get; }
        public IReadOnlyDictionary<string, NameBankDto> NameBanks { get; }
        public QuestConfigDto QuestConfig { get; }
        public IReadOnlyDictionary<string, CampaignTemplateDto> CampaignTemplates { get; }
        public CampaignRuntimeDto CampaignRuntime { get; }
        public IReadOnlyDictionary<string, AdapterPromptDto> AdapterPrompts { get; }
        public RuntimeConfigDto RuntimeConfig { get; }
        public UiPlanDto UiPlan { get; }
        public IReadOnlyDictionary<string, CampaignArcDto> CampaignArcs { get; }
        public IReadOnlyDictionary<string, LegacyNpcDto> LegacyNpcs { get; }
        public IReadOnlyDictionary<string, WorldNpcTemplateDto> WorldNpcTemplates { get; }
        public IReadOnlyDictionary<string, WorldAdapterDto> WorldAdapters { get; }
    }
}
