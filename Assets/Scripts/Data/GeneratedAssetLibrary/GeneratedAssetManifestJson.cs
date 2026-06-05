using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedAssetManifestJson
    {
        public static string ToJson(GeneratedAssetManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            return JsonConvert.SerializeObject(manifest, Formatting.Indented);
        }

        public static GeneratedAssetManifest FromJson(string json)
        {
            var manifest = JsonConvert.DeserializeObject<GeneratedAssetManifest>(json);
            if (manifest == null) throw new InvalidOperationException("Generated asset manifest deserialized to null.");
            manifest.records ??= new List<GeneratedAssetRecord>();
            return manifest;
        }
    }
}
