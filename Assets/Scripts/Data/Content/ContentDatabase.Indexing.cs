using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed partial class ContentDatabase
    {
        private static IReadOnlyDictionary<string, MaterialDto> IndexMaterials(MaterialListDocumentDto document)
        {
            var result = new Dictionary<string, MaterialDto>(System.StringComparer.Ordinal);
            if (document == null) return result;

            AddMaterials(result, document.materials);
            if (document.extension_data == null) return result;

            foreach (var pair in document.extension_data)
            {
                if (pair.Key == "materials") continue;
                var material = ContentJson.ToObject<MaterialDto>(pair.Value);
                if (!string.IsNullOrWhiteSpace(material.material_id))
                    result[material.material_id] = material;
            }

            return result;
        }

        private static IReadOnlyDictionary<string, FactionDto> IndexFactionProfiles(FactionCatalogDto catalog)
        {
            var result = new Dictionary<string, FactionDto>(System.StringComparer.Ordinal);
            if (catalog == null) return result;

            AddFactionMap(result, catalog.ethics, catalog.values);
            if (catalog.extension_data == null) return result;

            foreach (var pair in catalog.extension_data)
            {
                var faction = ContentJson.ToObject<FactionDto>(pair.Value);
                if ((faction.ethics != null && faction.ethics.Count > 0)
                    || (faction.values != null && faction.values.Count > 0))
                {
                    result[pair.Key] = faction;
                }
            }

            return result;
        }

        private static void AddMaterials(Dictionary<string, MaterialDto> result, IEnumerable<MaterialDto> rows)
        {
            if (rows == null) return;
            foreach (var material in rows)
            {
                if (material != null && !string.IsNullOrWhiteSpace(material.material_id))
                    result[material.material_id] = material;
            }
        }

        private static void AddFactionMap(
            Dictionary<string, FactionDto> result,
            Dictionary<string, Dictionary<string, string>> ethics,
            Dictionary<string, Dictionary<string, int>> values)
        {
            if (ethics == null) return;
            foreach (var pair in ethics)
            {
                Dictionary<string, int> factionValues = null;
                values?.TryGetValue(pair.Key, out factionValues);
                result[pair.Key] = new FactionDto
                {
                    ethics = pair.Value ?? new Dictionary<string, string>(),
                    values = factionValues ?? new Dictionary<string, int>(),
                };
            }
        }
    }
}
