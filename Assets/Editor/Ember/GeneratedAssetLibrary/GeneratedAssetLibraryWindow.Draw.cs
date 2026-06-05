using System;
using System.Linq;
using EmberCrpg.Data.GeneratedAssets;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public sealed partial class GeneratedAssetLibraryWindow
    {
        private void DrawRecordList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.35f)))
            {
                EditorGUILayout.LabelField("Records", EditorStyles.boldLabel);
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                for (var i = 0; i < _database.Records.Count; i++)
                {
                    var record = _database.Records[i];
                    if (record == null) continue;
                    record.SyncIdentity();
                    var label = string.IsNullOrWhiteSpace(record.displayName) ? record.stableId : record.displayName;
                    if (GUILayout.Toggle(_selectedIndex == i, label, "Button")) _selectedIndex = i;
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSelectedRecord()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_selectedIndex < 0 || _selectedIndex >= _database.Records.Count)
                {
                    EditorGUILayout.HelpBox("Select a record to edit.", MessageType.Info);
                    return;
                }

                var record = _database.Records[_selectedIndex];
                if (record == null) return;
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                Undo.RecordObject(_database, "Edit Generated Asset Record");
                record.displayName = EditorGUILayout.TextField("Display Name", record.displayName);
                record.kind = (GeneratedAssetKind)EditorGUILayout.EnumPopup("Kind", record.kind);
                record.key.archetype = EditorGUILayout.TextField("Archetype", record.key.archetype);
                record.key.biome = EditorGUILayout.TextField("Biome", record.key.biome);
                record.key.culture = EditorGUILayout.TextField("Culture", record.key.culture);
                record.key.faction = EditorGUILayout.TextField("Faction", record.key.faction);
                record.key.role = EditorGUILayout.TextField("Role", record.key.role);
                record.key.material = EditorGUILayout.TextField("Material", record.key.material);
                record.key.tier = EditorGUILayout.TextField("Tier", record.key.tier);
                record.key.styleVersion = EditorGUILayout.TextField("Style Version", record.key.styleVersion);
                record.key.variantIndex = EditorGUILayout.IntField("Variant Index", record.key.variantIndex);
                record.seed = EditorGUILayout.IntField("Seed", record.seed);
                record.key.promptHash = EditorGUILayout.TextField("Prompt Hash", record.key.promptHash);
                record.tags = CsvField("Tags", record.tags);
                record.relativeAssetPath = EditorGUILayout.TextField("Relative Asset Path", record.relativeAssetPath);
                record.previewPath = EditorGUILayout.TextField("Preview Path", record.previewPath);
                record.spritePath = EditorGUILayout.TextField("Sprite Path", record.spritePath);
                record.materialPath = EditorGUILayout.TextField("Material Path", record.materialPath);
                record.prefabPath = EditorGUILayout.TextField("Prefab Path", record.prefabPath);
                record.modelName = EditorGUILayout.TextField("Model Name", record.modelName);
                record.modelLicense = EditorGUILayout.TextField("Model License", record.modelLicense);
                record.toolchainNotes = EditorGUILayout.TextField("Toolchain Notes", record.toolchainNotes);
                record.licenseStatus = (GeneratedAssetLicenseStatus)EditorGUILayout.EnumPopup("License Status", record.licenseStatus);
                record.humanApproved = EditorGUILayout.Toggle("Human Approved", record.humanApproved);
                record.sourcePrompt = EditorGUILayout.TextArea(record.sourcePrompt, GUILayout.MinHeight(48f));
                record.negativePrompt = EditorGUILayout.TextArea(record.negativePrompt, GUILayout.MinHeight(48f));
                record.notes = EditorGUILayout.TextArea(record.notes, GUILayout.MinHeight(36f));
                record.SyncIdentity();
                EditorGUILayout.SelectableLabel(record.stableId, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.HelpBox("Preview folder: " + GeneratedAssetPathUtility.PreviewFolder(record, _previewPreset), MessageType.None);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Approve")) record.humanApproved = true;
                    if (GUILayout.Button("Needs Review")) record.licenseStatus = GeneratedAssetLicenseStatus.NeedsReview;
                    if (GUILayout.Button("Forbidden")) record.licenseStatus = GeneratedAssetLicenseStatus.Forbidden;
                }

                if (record.licenseStatus != GeneratedAssetLicenseStatus.Clean || string.IsNullOrWhiteSpace(record.modelLicense))
                    EditorGUILayout.HelpBox("Commercial licensing must be reviewed before runtime use.", MessageType.Warning);

                DrawPipelineActions(record);
                EditorGUILayout.EndScrollView();
                EditorUtility.SetDirty(_database);
            }
        }

        private static System.Collections.Generic.List<string> CsvField(string label, System.Collections.Generic.List<string> current)
        {
            var joined = string.Join(", ", current ?? new System.Collections.Generic.List<string>());
            var edited = EditorGUILayout.TextField(label, joined);
            return edited
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();
        }
    }
}
