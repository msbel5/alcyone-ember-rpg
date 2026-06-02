using System;
using System.Collections.Generic;
using System.IO;

namespace EmberCrpg.Data.Content
{
    // Patterns: Repository + Loader + DTO Catalog. Additive data access only; no simulation wiring.
    public sealed partial class ContentDatabase
    {
        private ContentDatabase(ContentDatabaseState state)
        {
            Items = state.Items;
            Recipes = state.Recipes;
            Materials = state.Materials;
            Factions = state.Factions;
            Classes = state.Classes;
            Spells = state.Spells;
            Monsters = state.Monsters;
            Locations = state.Locations;
            NpcTemplates = state.NpcTemplates;
            FactionCatalog = state.FactionCatalog;
            LocationCatalog = state.LocationCatalog;
            Worldgen = state.Worldgen;
            Biomes = state.Biomes;
            Cultures = state.Cultures;
            WorldBuildingTemplates = state.WorldBuildingTemplates;
            SpeciesTemplates = state.SpeciesTemplates;
            WorldQuestTemplates = state.WorldQuestTemplates;
            WorldFurniture = state.WorldFurniture;
            WorldProfiles = state.WorldProfiles;
            EconomyConfig = state.EconomyConfig;
            ColonyConfig = state.ColonyConfig;
            Caravans = state.Caravans;
            DialogDefinitions = state.DialogDefinitions;
            HistoryTables = state.HistoryTables;
            Institutions = state.Institutions;
            Schedules = state.Schedules;
            SocialRules = state.SocialRules;
            InteractionRules = state.InteractionRules;
            ConsequenceRules = state.ConsequenceRules;
            LootTables = state.LootTables;
            QualityTiers = state.QualityTiers;
            InventoryLayouts = state.InventoryLayouts;
            Progression = state.Progression;
            CharacterCreation = state.CharacterCreation;
            NameBanks = state.NameBanks;
            QuestConfig = state.QuestConfig;
            CampaignTemplates = state.CampaignTemplates;
            CampaignRuntime = state.CampaignRuntime;
            AdapterPrompts = state.AdapterPrompts;
            RuntimeConfig = state.RuntimeConfig;
            UiPlan = state.UiPlan;
            CampaignArcs = state.CampaignArcs;
            LegacyNpcs = state.LegacyNpcs;
            WorldNpcTemplates = state.WorldNpcTemplates;
            WorldAdapters = state.WorldAdapters;
        }

        public IReadOnlyDictionary<string, ItemDto> Items { get; }
        public IReadOnlyDictionary<string, RecipeDto> Recipes { get; }
        public IReadOnlyDictionary<string, MaterialDto> Materials { get; }
        public IReadOnlyDictionary<string, FactionDto> Factions { get; }
        public IReadOnlyDictionary<string, ClassDto> Classes { get; }
        public IReadOnlyDictionary<string, SpellDto> Spells { get; }
        public IReadOnlyDictionary<string, MonsterDto> Monsters { get; }
        public IReadOnlyDictionary<string, LocationEntryDto> Locations { get; }
        public IReadOnlyDictionary<string, NpcTemplateDto> NpcTemplates { get; }
        public FactionCatalogDto FactionCatalog { get; }
        public LocationCatalogDto LocationCatalog { get; }

        public static ContentDatabase Load(IContentPathProvider pathProvider)
        {
            if (pathProvider == null) throw new ArgumentNullException(nameof(pathProvider));
            return LoadFromRoot(pathProvider.ContentRootPath);
        }

        public static ContentDatabase LoadFromRoot(string contentRootPath)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
                throw new ArgumentException("Content root path is required.", nameof(contentRootPath));

            return new ContentDatabase(LoadState(contentRootPath));
        }

        private static string Read(string root, string fileName)
        {
            return File.ReadAllText(Path.Combine(root, fileName));
        }

        private static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> rows, Func<T, string> keySelector)
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            if (rows == null) return result;
            foreach (var row in rows)
            {
                var key = keySelector(row);
                if (string.IsNullOrWhiteSpace(key)) continue;
                result[key] = row;
            }
            return result;
        }

        private static string StableKey(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }
}
