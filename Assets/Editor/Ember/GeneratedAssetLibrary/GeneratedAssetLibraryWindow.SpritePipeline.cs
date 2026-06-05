using System.Collections.Generic;
using EmberCrpg.Data.GeneratedAssets;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public sealed partial class GeneratedAssetLibraryWindow
    {
        private void DrawPipelineActions(GeneratedAssetRecord record)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Sprite Pipeline", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Job")) BuildJob(record);
                if (GUILayout.Button("Dry Run Forge")) RunForge(record, dryRun: true);
                if (GUILayout.Button("Run Forge")) RunForge(record, dryRun: false);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Dry Run Matte")) RunMatte(record, dryRun: true);
                if (GUILayout.Button("Run Matte")) RunMatte(record, dryRun: false);
                if (GUILayout.Button("Import PNG")) ImportExistingPng(record);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Billboard Prefab")) CreateBillboardPrefab(record);
            }
        }

        private void BuildJob(GeneratedAssetRecord record)
        {
            if (_pipelineSettings == null || _previewPreset == null) return;
            record.spriteJob = GeneratedAssetPromptBuilder.BuildJob(
                record,
                _previewPreset,
                _previewPreset.defaultWidth,
                _previewPreset.defaultHeight,
                _pipelineSettings.defaultSteps,
                _pipelineSettings.defaultCfgScale,
                _pipelineSettings.defaultSampler,
                _pipelineSettings.defaultScheduler,
                _pipelineSettings.defaultModelName,
                overrides: new Dictionary<string, string>());
            GeneratedSpritePathPolicy.ConfigureJobPaths(record, record.spriteJob, _pipelineSettings);
            EditorUtility.SetDirty(_database);
            _validationSummary = "Built deterministic sprite job: " + record.spriteJob.jobId;
        }

        private void RunForge(GeneratedAssetRecord record, bool dryRun)
        {
            if (_pipelineSettings == null) return;
            var previous = _pipelineSettings.dryRun;
            _pipelineSettings.dryRun = dryRun;
            var result = GeneratedAssetForgeAdapter.ExportOrRun(record.spriteJob, _pipelineSettings);
            _pipelineSettings.dryRun = previous;
            record.spriteJob.validationWarnings = new List<string>(record.spriteJob.validationWarnings);
            EditorUtility.SetDirty(_database);
            _validationSummary = result.success ? "Forge command prepared." : result.stderr;
        }

        private void RunMatte(GeneratedAssetRecord record, bool dryRun)
        {
            if (_pipelineSettings == null) return;
            var previous = _pipelineSettings.dryRun;
            _pipelineSettings.dryRun = dryRun;
            var result = GeneratedAssetMatteAdapter.ExportOrRun(record.spriteJob, _pipelineSettings);
            _pipelineSettings.dryRun = previous;
            EditorUtility.SetDirty(_database);
            _validationSummary = result.success ? "Matte command prepared." : result.stderr;
        }

        private void ImportExistingPng(GeneratedAssetRecord record)
        {
            if (_pipelineSettings == null) return;
            var sourcePath = EditorUtility.OpenFilePanel("Import Generated PNG", null, "png");
            if (string.IsNullOrWhiteSpace(sourcePath)) return;
            GeneratedSpriteImportUtility.ImportExistingPng(record, sourcePath, _pipelineSettings);
            EditorUtility.SetDirty(_database);
            _validationSummary = "Imported sprite and updated record paths.";
        }

        private void CreateBillboardPrefab(GeneratedAssetRecord record)
        {
            if (_pipelineSettings == null) return;
            record.prefabPath = GeneratedBillboardPrefabBuilder.CreateOrUpdate(record, _pipelineSettings);
            EditorUtility.SetDirty(_database);
            _validationSummary = "Prefab updated: " + record.prefabPath;
        }
    }
}
