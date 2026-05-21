using System.Text.Json;

// This file is compiled only by the repo-local pure-C# fallback harness.
// It gives JsonSliceSaveService a tiny stand-in for UnityEngine.JsonUtility so
// save/load tests can run under .NET when a local Unity editor is unavailable.
// It is intentionally not a claim of Unity serialization parity.
namespace UnityEngine
{
    /// <summary>
    /// Codex audit (fifth pass): minimal Vector3 stub so the fallback harness
    /// can compile SaveData (which now lives in its own file and carries
    /// playerPosition). Unity provides the real Vector3 at editor/runtime;
    /// this struct has no behavior beyond holding three floats.
    /// </summary>
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }

    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true,
            PropertyNameCaseInsensitive = false,
        };

        public static string ToJson(object obj, bool prettyPrint)
        {
            var options = new JsonSerializerOptions(Options) { WriteIndented = prettyPrint };
            return JsonSerializer.Serialize(obj, obj.GetType(), options);
        }

        public static T FromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
    }
}
