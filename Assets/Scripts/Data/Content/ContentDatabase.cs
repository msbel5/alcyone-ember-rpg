using System;
using System.Collections.Generic;
using System.IO;

namespace EmberCrpg.Data.Content
{
    // Patterns: Repository + Loader + DTO Catalog. Additive data access only; no simulation wiring.
    public sealed partial class ContentDatabase
    {
        private ContentDatabase(
            IReadOnlyDictionary<string, ItemDto> items,
            IReadOnlyDictionary<string, RecipeDto> recipes,
            IReadOnlyDictionary<string, MaterialDto> materials,
            IReadOnlyDictionary<string, FactionDto> factions,
            IReadOnlyDictionary<string, ClassDto> classes,
            IReadOnlyDictionary<string, SpellDto> spells,
            IReadOnlyDictionary<string, MonsterDto> monsters,
            IReadOnlyDictionary<string, LocationEntryDto> locations,
            IReadOnlyDictionary<string, NpcTemplateDto> npcTemplates,
            FactionCatalogDto factionCatalog,
            LocationCatalogDto locationCatalog)
        {
            Items = items;
            Recipes = recipes;
            Materials = materials;
            Factions = factions;
            Classes = classes;
            Spells = spells;
            Monsters = monsters;
            Locations = locations;
            NpcTemplates = npcTemplates;
            FactionCatalog = factionCatalog;
            LocationCatalog = locationCatalog;
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

            var items = ContentJson.Deserialize<ItemsDocumentDto>(Read(contentRootPath, "items.json")).items;
            var recipes = ContentJson.Deserialize<RecipesDocumentDto>(Read(contentRootPath, "recipes.json")).recipes;
            var materials = ContentJson.Deserialize<MaterialListDocumentDto>(Read(contentRootPath, "materials.json"));
            var factions = ContentJson.Deserialize<FactionsDocumentDto>(Read(contentRootPath, "factions.json")).factions;
            var classes = ContentJson.Deserialize<ClassesDocumentDto>(Read(contentRootPath, "classes.json")).classes;
            var spells = ContentJson.Deserialize<SpellsDocumentDto>(Read(contentRootPath, "spells.json")).spells;
            var monsters = ContentJson.Deserialize<MonstersDocumentDto>(Read(contentRootPath, "monsters.json")).monsters;
            var locations = ContentJson.Deserialize<LocationsDocumentDto>(Read(contentRootPath, "locations.json")).locations;
            var npcs = ContentJson.Deserialize<NpcTemplatesDocumentDto>(Read(contentRootPath, "npc_templates.json")).npc_templates;

            return new ContentDatabase(
                Index(items, item => item.id),
                Index(recipes, recipe => recipe.id),
                IndexMaterials(materials),
                IndexFactionProfiles(factions),
                new Dictionary<string, ClassDto>(classes, StringComparer.Ordinal),
                Index(spells, spell => StableKey(spell.name)),
                Index(monsters, monster => monster.id),
                Index(locations.location_list, location => string.IsNullOrWhiteSpace(location.location_id) ? StableKey(location.name) : location.location_id),
                Index(npcs, npc => npc.id),
                factions,
                locations);
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
