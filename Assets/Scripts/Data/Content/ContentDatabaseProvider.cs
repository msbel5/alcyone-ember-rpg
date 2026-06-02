using System;

namespace EmberCrpg.Data.Content
{
    public static class ContentDatabaseProvider
    {
        private static IContentPathProvider _pathProvider;
        private static ContentDatabase _current;

        public static ContentDatabase Current
        {
            get
            {
                if (_current != null) return _current;
                if (_pathProvider == null)
                {
#if UNITY_5_3_OR_NEWER
                    _pathProvider = new UnityStreamingAssetsContentPathProvider();
#else
                    throw new InvalidOperationException("ContentDatabaseProvider needs an IContentPathProvider outside Unity runtime.");
#endif
                }

                _current = ContentDatabase.Load(_pathProvider);
                return _current;
            }
        }

        public static void Configure(IContentPathProvider pathProvider)
        {
            _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            _current = null;
        }

        public static void Clear()
        {
            _pathProvider = null;
            _current = null;
        }
    }
}
