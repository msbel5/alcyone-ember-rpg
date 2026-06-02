using System.Collections.Generic;
using System.IO;

namespace EmberCrpg.Data.Content
{
    public sealed partial class ContentDatabase
    {
        private static ContentDatabaseState LoadState(string root)
        {
            var factions = ContentJson.Deserialize<FactionsDocumentDto>(Read(root, "factions.json")).factions;
            var locations = ContentJson.Deserialize<LocationsDocumentDto>(Read(root, "locations.json")).locations;

            return new ContentDatabaseState
            {
                Items = Index(ContentJson.Deserialize<ItemsDocumentDto>(Read(root, "items.json")).items, row => row.id),
                Recipes = Index(ContentJson.Deserialize<RecipesDocumentDto>(Read(root, "recipes.json")).recipes, row => row.id),
                Materials = IndexMaterials(ContentJson.Deserialize<MaterialListDocumentDto>(Read(root, "materials.json"))),
                Factions = IndexFactionProfiles(factions),
                Classes = CopyMap(ContentJson.Deserialize<ClassesDocumentDto>(Read(root, "classes.json")).classes),
                Spells = Index(ContentJson.Deserialize<SpellsDocumentDto>(Read(root, "spells.json")).spells, row => StableKey(row.name)),
                Monsters = Index(ContentJson.Deserialize<MonstersDocumentDto>(Read(root, "monsters.json")).monsters, row => row.id),
                Locations = Index(locations.location_list, row => string.IsNullOrWhiteSpace(row.location_id) ? StableKey(row.name) : row.location_id),
                NpcTemplates = Index(ContentJson.Deserialize<NpcTemplatesDocumentDto>(Read(root, "npc_templates.json")).npc_templates, row => row.id),
                FactionCatalog = factions,
                LocationCatalog = locations,
                Worldgen = ContentJson.Deserialize<WorldgenDocumentDto>(Read(root, "worldgen.json")).worldgen,
                Biomes = CopyMap(ContentJson.Deserialize<BiomesDocumentDto>(Read(root, Path.Combine("world", "biomes.json"))).biomes),
                Cultures = CopyMap(ContentJson.Deserialize<CulturesDocumentDto>(Read(root, Path.Combine("world", "cultures.json"))).cultures),
                WorldBuildingTemplates = CopyMap(ContentJson.Deserialize<BuildingTemplatesDocumentDto>(Read(root, Path.Combine("world", "building_templates.json"))).building_templates),
                SpeciesTemplates = CopyMap(ContentJson.Deserialize<SpeciesTemplatesDocumentDto>(Read(root, Path.Combine("world", "species_templates.json"))).species_templates),
                WorldQuestTemplates = CopyMap(ContentJson.Deserialize<WorldQuestTemplatesDocumentDto>(Read(root, Path.Combine("world", "quest_templates.json"))).quest_templates),
                WorldFurniture = CopyMap(ContentJson.Deserialize<WorldFurnitureDocumentDto>(Read(root, Path.Combine("world", "furniture.json"))).furniture),
                WorldProfiles = CopyMap(ContentJson.Deserialize<WorldProfilesDocumentDto>(Read(root, Path.Combine("world", "profiles.json"))).profiles),
                EconomyConfig = ContentJson.Deserialize<EconomyConfigDocumentDto>(Read(root, "economy_config.json")).economy_config,
                ColonyConfig = ContentJson.Deserialize<ColonyConfigDocumentDto>(Read(root, "colony_config.json")).colony_config,
                Caravans = CopyMap(ContentJson.Deserialize<CaravansDocumentDto>(Read(root, "caravans.json")).caravans),
                DialogDefinitions = Index(ContentJson.Deserialize<DialogDefsDocumentDto>(Read(root, "dialog_defs.json")).dialog_defs, row => row.dialog_id),
                HistoryTables = ContentJson.Deserialize<HistoryTablesDocumentDto>(Read(root, "history_tables.json")).history_tables,
                Institutions = ContentJson.Deserialize<InstitutionsDto>(Read(root, "institutions.json")),
                Schedules = ContentJson.Deserialize<SchedulesDocumentDto>(Read(root, "schedules.json")).schedules,
                SocialRules = ContentJson.Deserialize<SocialRulesDocumentDto>(Read(root, "social_rules.json")).social_rules,
                InteractionRules = ContentJson.Deserialize<List<InteractionRuleDto>>(Read(root, "interaction_rules.json")),
                ConsequenceRules = Index(ContentJson.Deserialize<ConsequenceRulesDocumentDto>(Read(root, "consequence_rules.json")).consequence_rules, row => row.rule_id),
                LootTables = ContentJson.Deserialize<LootTablesDocumentDto>(Read(root, "loot_tables.json")).loot_tables,
                QualityTiers = CopyMap(ContentJson.Deserialize<Dictionary<string, QualityTierDto>>(Read(root, "quality_tiers.json"))),
                InventoryLayouts = ContentJson.Deserialize<InventoryLayoutsDto>(Read(root, "inventory_layouts.json")),
                Progression = ContentJson.Deserialize<ProgressionDocumentDto>(Read(root, "progression.json")).progression,
                CharacterCreation = ContentJson.Deserialize<CharacterCreationDocumentDto>(Read(root, "character_creation.json")).character_creation,
                NameBanks = CopyMap(ContentJson.Deserialize<NameBanksDocumentDto>(Read(root, "name_banks.json")).name_banks),
                QuestConfig = ContentJson.Deserialize<QuestConfigDocumentDto>(Read(root, "quest_config.json")).quest_config,
                CampaignTemplates = Index(ContentJson.Deserialize<CampaignTemplatesDocumentDto>(Read(root, "campaign_templates.json")).campaigns, row => row.id),
                CampaignRuntime = ContentJson.Deserialize<CampaignRuntimeDocumentDto>(Read(root, "campaign_runtime.json")).campaign_runtime,
                AdapterPrompts = CopyMap(ContentJson.Deserialize<Dictionary<string, AdapterPromptDto>>(Read(root, "adapter_prompts.json"))),
                RuntimeConfig = ContentJson.Deserialize<RuntimeConfigDocumentDto>(Read(root, "runtime_config.json")).runtime_config,
                UiPlan = ContentJson.Deserialize<UiPlanDto>(Read(root, "ui_plan.json")),
                CampaignArcs = LoadDirectory<CampaignArcDto>(root, "campaigns", dto => dto.id),
                LegacyNpcs = Index(ContentJson.Deserialize<LegacyNpcsDocumentDto>(Read(root, Path.Combine("npcs", "npcs.json"))).npcs, row => row.id),
                WorldNpcTemplates = CopyMap(ContentJson.Deserialize<WorldNpcTemplatesDocumentDto>(Read(root, Path.Combine("world", "npc_templates.json"))).npc_templates),
                WorldAdapters = LoadDirectory<WorldAdapterDto>(root, Path.Combine("world", "adapters"), dto => dto.id),
            };
        }

        private static IReadOnlyDictionary<string, T> CopyMap<T>(Dictionary<string, T> source)
        {
            return source == null
                ? new Dictionary<string, T>(System.StringComparer.Ordinal)
                : new Dictionary<string, T>(source, System.StringComparer.Ordinal);
        }

        private static IReadOnlyDictionary<string, T> LoadDirectory<T>(string root, string relativeDirectory, System.Func<T, string> keySelector)
        {
            var result = new Dictionary<string, T>(System.StringComparer.Ordinal);
            var path = Path.Combine(root, relativeDirectory);
            if (!Directory.Exists(path)) return result;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                var value = ContentJson.Deserialize<T>(File.ReadAllText(file));
                var key = keySelector(value);
                if (!string.IsNullOrWhiteSpace(key)) result[key] = value;
            }

            return result;
        }
    }
}
