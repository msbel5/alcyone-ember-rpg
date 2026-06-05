using System.IO;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.Common;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public sealed partial class GeneratedAssetLibraryWindow : EditorWindow
    {
        private const string DefaultDatabasePath = "Assets/Manifests/GeneratedAssets/GeneratedAssetDatabase.asset";
        private const string DefaultSettingsPath = "Assets/Settings/GeneratedAssetPipelineSettings.asset";

        private GeneratedAssetDatabase _database;
        private GeneratedAssetPromptPreset _previewPreset;
        private GeneratedAssetPipelineSettings _pipelineSettings;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private int _selectedIndex = -1;
        private string _validationSummary = string.Empty;

        [MenuItem("Ember/Generated Assets/Library")]
        public static void Open()
        {
            GetWindow<GeneratedAssetLibraryWindow>("Generated Asset Library");
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (_database == null)
            {
                EditorGUILayout.HelpBox("Select or create a GeneratedAssetDatabase asset.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRecordList();
                DrawSelectedRecord();
            }

            if (!string.IsNullOrWhiteSpace(_validationSummary))
                EditorGUILayout.HelpBox(_validationSummary, MessageType.None);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _database = (GeneratedAssetDatabase)EditorGUILayout.ObjectField(_database, typeof(GeneratedAssetDatabase), false, GUILayout.Width(320f));
                _previewPreset = (GeneratedAssetPromptPreset)EditorGUILayout.ObjectField(_previewPreset, typeof(GeneratedAssetPromptPreset), false, GUILayout.Width(240f));
                _pipelineSettings = (GeneratedAssetPipelineSettings)EditorGUILayout.ObjectField(_pipelineSettings, typeof(GeneratedAssetPipelineSettings), false, GUILayout.Width(240f));

                if (GUILayout.Button("Create Database", EditorStyles.toolbarButton, GUILayout.Width(110f))) CreateDatabase();
                if (GUILayout.Button("Create Settings", EditorStyles.toolbarButton, GUILayout.Width(110f))) CreateSettings();
                if (GUILayout.Button("Add Record", EditorStyles.toolbarButton, GUILayout.Width(80f))) AddRecord();
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70f))) Validate();
                if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton, GUILayout.Width(90f))) ExportJson();
                if (GUILayout.Button("Import JSON", EditorStyles.toolbarButton, GUILayout.Width(90f))) ImportJson();
            }
        }

        private void CreateDatabase()
        {
            EmberSceneSavePolicy.EnsureFolderExists("Assets/Manifests");
            EmberSceneSavePolicy.EnsureFolderExists("Assets/Manifests/GeneratedAssets");
            var database = AssetDatabase.LoadAssetAtPath<GeneratedAssetDatabase>(DefaultDatabasePath);
            if (database == null)
            {
                database = CreateInstance<GeneratedAssetDatabase>();
                AssetDatabase.CreateAsset(database, DefaultDatabasePath);
                AssetDatabase.SaveAssets();
            }

            _database = database;
            _selectedIndex = _database.Records.Count > 0 ? 0 : -1;
            Selection.activeObject = _database;
        }

        private void CreateSettings()
        {
            EmberSceneSavePolicy.EnsureFolderExists("Assets/Settings");
            var settings = AssetDatabase.LoadAssetAtPath<GeneratedAssetPipelineSettings>(DefaultSettingsPath);
            if (settings == null)
            {
                settings = CreateInstance<GeneratedAssetPipelineSettings>();
                AssetDatabase.CreateAsset(settings, DefaultSettingsPath);
                AssetDatabase.SaveAssets();
            }

            _pipelineSettings = settings;
            Selection.activeObject = _pipelineSettings;
        }

        private void AddRecord()
        {
            if (_database == null) return;
            Undo.RecordObject(_database, "Add Generated Asset Record");
            var record = _database.CreateRecord();
            record.displayName = "New Generated Asset";
            EditorUtility.SetDirty(_database);
            _selectedIndex = _database.Records.Count - 1;
        }

        private void Validate()
        {
            if (_database == null) return;
            var issues = _database.ValidateRecords();
            _validationSummary = $"Validation: {issues.Count} issue(s).";
            foreach (var issue in issues)
                Debug.Log($"[GeneratedAssetLibrary] {issue.severity} {issue.stableId}: {issue.message}", _database);
        }

        private void ExportJson()
        {
            if (_database == null) return;
            var path = EditorUtility.SaveFilePanel("Export Generated Asset Manifest", Application.dataPath, "generated-asset-library", "json");
            if (string.IsNullOrWhiteSpace(path)) return;
            var manifest = _database.CreateManifest(PlayerSettings.productName, _previewPreset != null ? _previewPreset.styleVersion : "v1");
            File.WriteAllText(path, GeneratedAssetManifestJson.ToJson(manifest));
            EditorUtility.RevealInFinder(path);
        }

        private void ImportJson()
        {
            if (_database == null) return;
            var path = EditorUtility.OpenFilePanel("Import Generated Asset Manifest", Application.dataPath, "json");
            if (string.IsNullOrWhiteSpace(path)) return;

            var manifest = GeneratedAssetManifestJson.FromJson(File.ReadAllText(path));
            Undo.RecordObject(_database, "Import Generated Asset Manifest");
            foreach (var record in manifest.records)
                Upsert(record);

            _database.RebuildStableIds();
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
            _validationSummary = $"Imported {manifest.records.Count} record(s) from manifest.";
        }

        private void Upsert(GeneratedAssetRecord incoming)
        {
            incoming.SyncIdentity();
            for (var i = 0; i < _database.Records.Count; i++)
            {
                if (_database.Records[i] == null) continue;
                _database.Records[i].SyncIdentity();
                if (_database.Records[i].stableId == incoming.stableId)
                {
                    _database.Records[i] = incoming;
                    return;
                }
            }

            _database.Records.Add(incoming);
        }
    }
}
