#if UNITY_5_3_OR_NEWER
using System.IO;
using UnityEngine;

namespace EmberCrpg.Data.Content
{
    public sealed class UnityStreamingAssetsContentPathProvider : IContentPathProvider
    {
        public string ContentRootPath => Path.Combine(Application.streamingAssetsPath, "Content");
    }
}
#endif
