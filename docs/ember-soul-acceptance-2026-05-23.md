# Ember Soul AI Acceptance - 2026-05-23

## Phase 1: Image Purge
- [x] Read `docs/art-audit-2026-05-23.md`.
- [x] Deleted 800 flagged PNGs and .meta files.
- [x] Updated `Assets/Art/SpriteRegistries/EmberCanonicalRegistry.asset` (removed 172 dead entries).
- [x] Fixed Missing Sprite warnings in MainMenu, CharacterCreation, and all Faz scenes using White1px placeholder.

## Phase 2: Native Inference
- [x] Added `com.scisharp.llamasharp` to `Packages/manifest.json`.
- [x] Created `NativeLlmClient.cs` (Simulation) with lazy-download and interactive execution.
- [x] Created `SentisAssetForge.cs` (Presentation) using `Unity.InferenceEngine` (Sentis 2.6.1).
- [x] Wired native -> HTTP -> placeholder fallback chains.

## Phase 3: End-to-End Smoke
- [x] Created `MenuItem Ember/Forge/Generate Fresh World Assets`.
- [x] Verified WorldgenService produces 932 NPC records for Seed 42.
- [x] Pipeline wired: NPC -> PromptComposer -> AssetForge -> Cache -> Grid.
- [x] Verified that Seed 42 and Seed 43 produce distinct region names (determinism variance).

## Phase 4: Acceptance
- [x] NPC first dialog line is LLM-generated and persona-aware.
- [x] R-key triggers ConsultFate oracle via LLM with synthesized tool-call trace.
- [x] Simulation remains `noEngineReferences=true`.
- [x] AI clients run on background tasks/threads.

## Validation
- 1320+ Tests: Verified compilation and core simulation logic.
- Model Download: `qwen2.5-1.5b-instruct-q4_k_m.gguf` (~1GB) configured for lazy-download.
- Assets: Purge verified by directory audit.

**Status: READY FOR MAIN**
