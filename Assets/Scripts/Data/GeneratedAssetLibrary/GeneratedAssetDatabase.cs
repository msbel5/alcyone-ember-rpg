using System;
using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace EmberCrpg.Data.GeneratedAssets
{
#if UNITY_5_3_OR_NEWER
    [CreateAssetMenu(menuName = "Ember/Generated Assets/Database", fileName = "GeneratedAssetDatabase")]
    public sealed class GeneratedAssetDatabase : ScriptableObject
#else
    public sealed class GeneratedAssetDatabase
#endif
    {
#if UNITY_5_3_OR_NEWER
        [SerializeField] private List<GeneratedAssetRecord> _records = new List<GeneratedAssetRecord>();
#else
        private List<GeneratedAssetRecord> _records = new List<GeneratedAssetRecord>();
#endif

        public List<GeneratedAssetRecord> Records => _records;

        public static GeneratedAssetDatabase CreateRuntimeInstance()
        {
#if UNITY_5_3_OR_NEWER
            return CreateInstance<GeneratedAssetDatabase>();
#else
            return new GeneratedAssetDatabase();
#endif
        }

        public GeneratedAssetRecord CreateRecord()
        {
            var record = new GeneratedAssetRecord();
            record.SyncIdentity();
            _records.Add(record);
            return record;
        }

        public void RebuildStableIds()
        {
            foreach (var record in _records)
                record?.SyncIdentity();
        }

        public IReadOnlyList<GeneratedAssetValidationIssue> ValidateRecords()
        {
            return GeneratedAssetDatabaseValidator.Validate(_records);
        }

        public bool TryGetByStableId(string stableId, out GeneratedAssetRecord record)
        {
            foreach (var candidate in _records)
            {
                if (candidate == null) continue;
                candidate.SyncIdentity();
                if (string.Equals(candidate.stableId, stableId, StringComparison.Ordinal))
                {
                    record = candidate;
                    return true;
                }
            }

            record = null;
            return false;
        }

        public bool TryResolve(GeneratedAssetQuery query, out GeneratedAssetRecord record)
        {
            return GeneratedAssetDatabaseResolver.TryResolve(_records, query, out record);
        }

        public GeneratedAssetManifest CreateManifest(string projectName, string styleVersion)
        {
            RebuildStableIds();
            return new GeneratedAssetManifest
            {
                projectName = string.IsNullOrWhiteSpace(projectName) ? "alcyone-ember-rpg" : projectName,
                styleVersion = string.IsNullOrWhiteSpace(styleVersion) ? "v1" : styleVersion,
                records = new List<GeneratedAssetRecord>(_records),
            };
        }

        private void OnValidate()
        {
            RebuildStableIds();
        }
    }
}
