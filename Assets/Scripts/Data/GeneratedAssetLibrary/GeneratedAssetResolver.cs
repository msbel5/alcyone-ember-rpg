using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    public sealed class GeneratedAssetResolver
    {
        private readonly GeneratedAssetDatabase _database;

        public GeneratedAssetResolver(GeneratedAssetDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public bool TryResolve(GeneratedAssetQuery query, out GeneratedAssetRecord record)
        {
            return _database.TryResolve(query, out record);
        }
    }
}
