using System;

#if UNITY_5_3_OR_NEWER
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
#endif

namespace EmberCrpg.Data.Content
{
    // Patterns: Data Mapper + Parser Adapter. Keeps DTO loading pure and parser choice isolated.
    internal static class ContentJson
    {
#if UNITY_5_3_OR_NEWER
        public static T Deserialize<T>(string json)
        {
            var value = JsonConvert.DeserializeObject<T>(json);
            if (value == null) throw new InvalidOperationException("Content JSON deserialized to null.");
            return value;
        }

        public static T ToObject<T>(object token)
        {
            if (token is JToken jToken)
            {
                var value = jToken.ToObject<T>();
                if (value == null) throw new InvalidOperationException("Content JSON token deserialized to null.");
                return value;
            }

            throw new InvalidOperationException("Unsupported content JSON token: " + (token == null ? "<null>" : token.GetType().Name));
        }
#else
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = false,
        };

        public static T Deserialize<T>(string json)
        {
            var value = JsonSerializer.Deserialize<T>(json, Options);
            if (value == null) throw new InvalidOperationException("Content JSON deserialized to null.");
            return value;
        }

        public static T ToObject<T>(object token)
        {
            if (token is JsonElement element)
            {
                var value = JsonSerializer.Deserialize<T>(element.GetRawText(), Options);
                if (value == null) throw new InvalidOperationException("Content JSON token deserialized to null.");
                return value;
            }

            throw new InvalidOperationException("Unsupported content JSON token: " + (token == null ? "<null>" : token.GetType().Name));
        }
#endif
    }
}
