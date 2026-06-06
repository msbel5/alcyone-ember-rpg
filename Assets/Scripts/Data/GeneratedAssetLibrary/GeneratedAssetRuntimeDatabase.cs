using UnityEngine;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedAssetRuntimeDatabase
    {
        private static GeneratedAssetDatabase _cached;
        private const string ResourcePath = "GeneratedAssets/GeneratedAssetDatabase";

        public static GeneratedAssetDatabase Current
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                if (_cached == null)
                    _cached = Resources.Load<GeneratedAssetDatabase>(ResourcePath);
#endif
                return _cached;
            }
        }

        public static void ResetCache()
        {
            _cached = null;
        }
    }
}
