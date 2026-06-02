using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace EmberCrpg.Data.Content
{
    // Pattern: Extension Object. Preserves schema tail fields without coupling gameplay to raw JSON.
    public abstract class ContentExtensionDto
    {
#if UNITY_5_3_OR_NEWER
        [JsonExtensionData] public IDictionary<string, JToken> extension_data;
#else
        [JsonExtensionData] public IDictionary<string, JsonElement> extension_data;
#endif
    }
}
