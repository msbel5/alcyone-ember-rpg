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

            if (IsMaterialKind(record.kind))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Material Pipeline", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Import Albedo")) ImportMaterialMap(record, GeneratedTexturePathPolicy.ResolveAlbedoPath(record), "PNG");
                    if (GUILayout.Button("Validate Albedo")) ValidateAlbedo(record);
                    if (GUILayout.Button("Generate Material")) GenerateMaterial(record);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Dry Run DeLight")) RunDeLight(record, dryRun: true);
                    if (GUILayout.Button("Run DeLight")) RunDeLight(record, dryRun: false);
                    if (GUILayout.Button("Import DeLit")) ImportMaterialMap(record, GeneratedTexturePathPolicy.ResolveDeLitAlbedoPath(record), "PNG");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Dry Run PBR")) RunPbr(record, dryRun: true);
                    if (GUILayout.Button("Run PBR")) RunPbr(record, dryRun: false);
                    if (GUILayout.Button("Import Normal")) ImportMaterialMap(record, GeneratedTexturePathPolicy.ResolveNormalPath(record), "PNG", normalMap: true);
                    if (GUILayout.Button("Import Roughness")) ImportMaterialMap(record, GeneratedTexturePathPolicy.ResolveRoughnessPath(record), "PNG", sRgb: false);
                    if (GUILayout.Button("Import AO")) ImportMaterialMap(record, GeneratedTexturePathPolicy.ResolveAoPath(record), "PNG", sRgb: false);
                }
            }

            if (IsMeshKind(record.kind))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Mesh Pipeline", EditorStyles.boldLabel);
                _meshSource = (GameObject)EditorGUILayout.ObjectField("Mesh Source", _meshSource, typeof(GameObject), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Build Mesh Job")) BuildMeshJob(record);
                    if (GUILayout.Button("Dry Run Mesh Tool")) RunMeshTool(record, dryRun: true);
                    if (GUILayout.Button("Run Mesh Tool")) RunMeshTool(record, dryRun: false);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Analyze Source")) AnalyzeMeshSource(record);
                    if (GUILayout.Button("Create Prefab")) CreateMeshPrefab(record);
                    if (GUILayout.Button("Validate Mesh")) ValidateMesh(record);
                }
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

        private void ImportMaterialMap(GeneratedAssetRecord record, string targetAssetPath, string extension, bool normalMap = false, bool sRgb = true)
        {
            if (_pipelineSettings == null) return;
            var sourcePath = EditorUtility.OpenFilePanel("Import Texture Map", null, extension);
            if (string.IsNullOrWhiteSpace(sourcePath)) return;

            if (targetAssetPath == GeneratedTexturePathPolicy.ResolveAlbedoPath(record))
            {
                var report = GeneratedTextureImportUtility.ImportAlbedo(record, sourcePath, _pipelineSettings);
                record.validationWarnings = new List<string>(report.warnings);
                _validationSummary = "Imported albedo with " + report.warnings.Count + " warning(s).";
            }
            else
            {
                GeneratedTextureImportUtility.ImportMap(sourcePath, targetAssetPath, _pipelineSettings, normalMap, sRgb);
                if (targetAssetPath == GeneratedTexturePathPolicy.ResolveDeLitAlbedoPath(record)) record.deLitAlbedoPath = targetAssetPath;
                if (targetAssetPath == GeneratedTexturePathPolicy.ResolveNormalPath(record)) record.normalPath = targetAssetPath;
                if (targetAssetPath == GeneratedTexturePathPolicy.ResolveRoughnessPath(record)) record.roughnessPath = targetAssetPath;
                if (targetAssetPath == GeneratedTexturePathPolicy.ResolveAoPath(record)) record.ambientOcclusionPath = targetAssetPath;
                _validationSummary = "Imported texture map: " + targetAssetPath;
            }

            EditorUtility.SetDirty(_database);
        }

        private void ValidateAlbedo(GeneratedAssetRecord record)
        {
            if (_pipelineSettings == null || string.IsNullOrWhiteSpace(record.albedoPath)) return;
            var report = GeneratedTextureImportUtility.ValidateAlbedo(record.albedoPath, _pipelineSettings);
            record.validationWarnings = new List<string>(report.warnings);
            EditorUtility.SetDirty(_database);
            _validationSummary = "Tileability warnings: " + report.warnings.Count;
        }

        private void GenerateMaterial(GeneratedAssetRecord record)
        {
            record.materialPath = GeneratedUrpMaterialBuilder.CreateOrUpdate(record);
            EditorUtility.SetDirty(_database);
            _validationSummary = "URP material updated: " + record.materialPath;
        }

        private void RunDeLight(GeneratedAssetRecord record, bool dryRun)
        {
            if (_pipelineSettings == null) return;
            var result = GeneratedTextureDeLightAdapter.ExportOrRun(record, _pipelineSettings, dryRun);
            if (dryRun) record.validationWarnings.Add("delight_command_exported");
            EditorUtility.SetDirty(_database);
            _validationSummary = result.success ? "De-light command prepared." : result.stderr;
        }

        private void RunPbr(GeneratedAssetRecord record, bool dryRun)
        {
            if (_pipelineSettings == null) return;
            var result = GeneratedTexturePbrAdapter.ExportOrRun(record, _pipelineSettings, dryRun);
            if (dryRun) record.validationWarnings.Add("pbr_command_exported");
            EditorUtility.SetDirty(_database);
            _validationSummary = result.success ? "PBR command prepared." : result.stderr;
        }

        private static bool IsMaterialKind(GeneratedAssetKind kind)
        {
            return kind == GeneratedAssetKind.TileableWall
                || kind == GeneratedAssetKind.TileableFloor
                || kind == GeneratedAssetKind.TileableCeiling
                || kind == GeneratedAssetKind.MaterialSet;
        }

        private void BuildMeshJob(GeneratedAssetRecord record)
        {
            if (_pipelineSettings == null) return;
            record.meshJob = GeneratedMeshJobBuilder.Build(record, "external-mesh-tool");
            GeneratedMeshPathPolicy.ConfigureJobPaths(record, record.meshJob, _pipelineSettings);
            EditorUtility.SetDirty(_database);
            _validationSummary = "Built mesh job: " + record.meshJob.jobId;
        }

        private void RunMeshTool(GeneratedAssetRecord record, bool dryRun)
        {
            if (_pipelineSettings == null) return;
            var result = GeneratedMeshToolAdapter.ExportOrRun(record.meshJob, _pipelineSettings, dryRun);
            EditorUtility.SetDirty(_database);
            _validationSummary = result.success ? "Mesh command prepared." : result.stderr;
        }

        private void AnalyzeMeshSource(GeneratedAssetRecord record)
        {
            if (_meshSource == null) return;
            GeneratedMeshImportUtility.Analyze(_meshSource, record);
            EditorUtility.SetDirty(_database);
            _validationSummary = "Mesh analyzed: " + record.triangleCount + " tris.";
        }

        private void CreateMeshPrefab(GeneratedAssetRecord record)
        {
            if (_meshSource == null) return;
            record.prefabPath = GeneratedMeshPrefabBuilder.CreateOrUpdate(_meshSource, record);
            EditorUtility.SetDirty(_database);
            _validationSummary = "Mesh prefab updated: " + record.prefabPath;
        }

        private void ValidateMesh(GeneratedAssetRecord record)
        {
            if (_pipelineSettings == null) return;
            var report = GeneratedMeshValidator.Validate(record, new GeneratedMeshValidationPolicy
            {
                smallPropTriangleWarning = _pipelineSettings.smallPropTriangleWarning,
                largeStructureTriangleWarning = _pipelineSettings.largeStructureTriangleWarning,
                terrainTriangleWarning = _pipelineSettings.terrainTriangleWarning,
            });
            record.validationWarnings = new List<string>(report.warnings);
            EditorUtility.SetDirty(_database);
            _validationSummary = "Mesh warnings: " + report.warnings.Count;
        }

        private static bool IsMeshKind(GeneratedAssetKind kind)
        {
            return kind == GeneratedAssetKind.SmallPropMesh
                || kind == GeneratedAssetKind.LargeStructureMesh
                || kind == GeneratedAssetKind.TerrainMesh;
        }
    }
}
