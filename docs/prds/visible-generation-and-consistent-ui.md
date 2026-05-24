# PRD: Visible Asset Generation & Consistent UI Foundation

**Status:** Draft (awaiting approval)
**Owner:** msbel + Claude + Codex (parallel execution)
**Branch policy:** small focused PRs per phase; never merge before user review

---

## Problem Statement

Today, three things are invisible:

1. **Asset generation is silent.** ~800 assets need to be produced (NPC portraits, item icons, spell icons, terrain tiles, etc.). When `ForgeMenu` runs in batch mode, nothing is shown — no log, no thumbnail, no progress. If it fails, we don't know which asset, why, or whether to retry.
2. **Worldgen is silent.** The simulation asks questions (skill selection, dice rolls, decisions), but the user sees nothing on screen — no choice modal, no rolled value, no log of what was decided or what NPCs were seeded.
3. **UI is inconsistent.** Each scene has its own ad-hoc canvas (or none). No shared design tokens (colors/typography/spacing), no reusable prefabs (Button, Modal, ProgressBar, LogView), no theme.

The user cannot see, debug, or trust the generation pipeline.

## Vision

When the user double-clicks `start.exe`:

1. **Boot screen** opens immediately, in the project's design system.
2. **Asset discovery** runs: scan disk cache, diff against required manifest.
3. If anything is missing, a **visible generation screen** plays through every missing asset sequentially — current prompt, live log, thumbnail preview as it bakes, status icon (queued / generating / cached / failed), failure log to disk.
4. When the manifest is satisfied, **Main Menu** loads — using the same UI system.
5. **New Game** → **Loading Screen** → worldgen runs **step by step** with every decision, every dice roll, every NPC seed visible. User can pause, scroll the log, and skip auto-advance.
6. Same UI components on every screen. Design tokens come from a single source; Figma drives them where possible.

## Non-Goals (v1)

- Multiplayer UI
- Localization (English only)
- Mobile / touch
- Replacing existing gameplay logic — only the **view layer** changes

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Phase 1: UI Foundation                                     │
│  ─────────────────────────                                  │
│  • Design tokens   (ScriptableObject)                       │
│  • Theme system    (Dark default)                           │
│  • Prefab library: Button / Label / Panel / Modal /         │
│                    ProgressBar / LogScrollView /            │
│                    ThumbnailGrid / DiceRoll / ChoiceList    │
└────────────────────────────┬────────────────────────────────┘
                             │ depends on
┌────────────────────────────┴────────────────────────────────┐
│  Phase 2: Asset Manifest + Discovery                        │
│  ─────────────────────────────────                          │
│  • AssetManifest.asset (ScriptableObject)                   │
│  • AssetManifestScanner (Editor + Runtime)                  │
│  • Per-asset record: id, category, prompt, path, status     │
│  • Diff against AssetForgeCache (disk)                      │
│  • Editor menu: Ember/Forge/Scan Missing Assets             │
└────────────────────────────┬────────────────────────────────┘
                             │ depends on
┌────────────────────────────┴────────────────────────────────┐
│  Phase 3: Boot Scene + Asset Generation Visualizer          │
│  ────────────────────────────────────────────               │
│  • New Boot scene (first in build settings)                 │
│  • BootBootstrap.cs (MonoBehaviour)                         │
│  • AssetGenerationScreen (uses Phase 1 prefabs)             │
│  • Sequential generation via ForgeBootstrap + Queue         │
│  • Live: current prompt, progress, thumbnail, log           │
│  • Failure → Logs/generation-failures.json + continue       │
│  • "Continue to Main Menu" when manifest satisfied          │
└────────────────────────────┬────────────────────────────────┘
                             │ depends on
┌────────────────────────────┴────────────────────────────────┐
│  Phase 4: LoadingScreen Library                             │
│  ──────────────────────                                     │
│  • LoadingScreen prefab (uses Phase 1)                      │
│  • LoadingScreenController.cs — public static API:          │
│      Show/Hide/SetProgress/LogLine/ShowThumbnail            │
│  • Persistent across scene loads (DontDestroyOnLoad)        │
└────────────────────────────┬────────────────────────────────┘
                             │ depends on
┌────────────────────────────┴────────────────────────────────┐
│  Phase 5: Worldgen Visible Flow                             │
│  ──────────────────────                                     │
│  • WorldgenViewController (overlay UI)                      │
│  • Question modals: skill choice, dice roll, decision       │
│  • Live log panel: every decision, roll, NPC seed           │
│  • Auto-advance toggle + manual "Continue" button           │
│  • Hooks into existing WorldgenService (no domain change)   │
└────────────────────────────┬────────────────────────────────┘
                             │ optional
┌────────────────────────────┴────────────────────────────────┐
│  Phase 6: Figma + Code Connect                              │
│  ────────────────────────                                   │
│  • Figma file: heQZMVLSJlWbJiEAdqX3Px (already in env)      │
│  • Design tokens authored in Figma → exported to            │
│    Phase 1 ScriptableObjects                                │
│  • Code Connect: Figma components ↔ Unity prefabs           │
│  • Scheduled task (cron, weekly): pull Figma → diff → PR    │
└─────────────────────────────────────────────────────────────┘
```

## Technical Decisions (resolved 2026-05-24)

### D1. UI rendering — **swappable abstraction layer, UI Toolkit default**

The user explicitly anticipates Unity UI Toolkit (and TextMesh Pro) shipping breaking changes in future LTS releases, and wants the option to fall back to UGUI or even an HTML/CSS-based renderer (ReactUnity / Noesis / WebView) without rewriting screens.

**Decision:** Introduce a thin abstraction (`IUiSurface`, `IUiPanel`, `IUiPrefab`) so any screen is built against the interface, not the renderer. Ship one concrete backend first:

- **Default backend:** UI Toolkit (`UiToolkitSurface`) — modern, Unity 6 native, USS for tokens.
- **Fallback backend:** UGUI (`UguiSurface`) — drop-in if UI Toolkit blocks us on a future Unity update.
- **Stretch backend:** Web/CSS via Noesis GUI or ReactUnity (`WebSurface`) — explored in Phase 6+.

The Boot, Loading, and Worldgen screens are built against the abstraction. The existing `MainMenuCanvas` stays UGUI until a proper migration PR.

### D2. Asset manifest — **hand-authored core + LLM-driven dynamic prompts + RGB variants**

User insight: instead of pre-generating thousands of unique portraits, ship a small set of **generic NPC base silhouettes** and at runtime synthesize unique NPCs by combining them.

The base set already on disk in `Assets/Art/BodySilhouettes/` (verified 2026-05-24):

- `humanoid_male.png`
- `humanoid_female.png`
- `beast_quadruped.png`
- `undead_humanoid.png`
- `construct.png`
- `aberration.png`

Other archetypes such as fairy, dragon, or elemental are **not** pre-existing. When Phase 2 detects a manifest entry for an unsupported archetype it must be flagged `requires_generation` and queued through the normal Forge path (using the closest existing archetype as a seed reference) rather than assumed to be on disk. Phase 2 acceptance must cover this case explicitly.

At runtime we synthesize unique NPCs by combining:

1. **Base silhouette** (from the core manifest, hand-authored)
2. **RGB recolor** (deterministic from NPC seed — palette swap on the base)
3. **LLM-generated prompt** (NativeLlmClient / AIDm composes prompts conditioned on world style + NPC traits)
4. **Optional ONNX refinement** (only when the player examines the NPC closely)

**Decision:**

- `Assets/Manifests/CoreAssetManifest.asset` — hand-authored, ~50 entries: UI icons, fonts, generic silhouettes, sample spell icons, base item icons, core sounds. Checked into git.
- `Assets/Manifests/GenericNpcBaseManifest.asset` — silhouette + RGB palette range per archetype (humanoid_male, humanoid_female, beast_quadruped, undead_humanoid, construct, fairy, dragon, aberration). Each entry maps to a base PNG already in `Assets/Art/BodySilhouettes/` + recolor rules.
- Runtime dynamic prompts: `LlmPromptComposer` consumes NPC seed + world style → produces prompt text → handed to `OnnxAssetForge`. Cache key includes both NPC seed and world style so deterministic across runs.

This guarantees **super-consistent style** (same LLM, same world style) while never shipping unique portrait files.

### D3. Generation timing — **hybrid: 50 core on first launch + lazy per scenario**

Resolved: ship the ~50 core assets in the boot generation if missing, then lazy-generate scenario-specific assets when New Game runs.

### D4. Failure policy — **skip + log + continue**

- Failure write to `Logs/generation-failures.json` with timestamp, asset id, prompt, exception message.
- Boot/Loading UI shows red icon on the failed asset in the live grid + adds line to log scroll.
- "Retry failed" button appears in the main menu when failures exist.
- Generation never blocks the user.

## Phases & Ownership

| # | Phase | Owner | Est. Effort | Deliverable |
|---|-------|-------|-------------|-------------|
| 0 | PRD approval | user | now | this doc merged |
| 1 | UI Foundation | Claude | 1 PR, ~15 files | tokens + prefab library + theme |
| 2 | Asset Manifest | Codex | 1 PR, ~8 files | scanner + manifest + tests |
| 3 | Boot + Visualizer | Claude | 1 PR, ~10 files | start.exe → visible gen |
| 4 | LoadingScreen | Claude | 1 PR, ~5 files | reusable lib |
| 5 | Worldgen Visible | Codex | 1 PR, ~15 files | step-by-step UI |
| 6 | Figma sync | Claude | 1 PR, ~5 files | weekly cron + Code Connect |

Phases 1 must merge first. Phase 2 can start in parallel. Phase 3-5 depend on 1+2. Phase 6 optional but unlocks ongoing design work.

## Test Plan (per phase)

- **Phase 1:** snapshot tests for prefabs (Unity Test Runner Visual Tests), token validator (ensures every token referenced is defined)
- **Phase 2:** EditMode tests — scanner finds missing, persists manifest, idempotent re-scan
- **Phase 3:** PlayMode test — boot scene → generation runs → continues to main menu (mocked Forge)
- **Phase 4:** PlayMode test — Show/Hide/Progress/Log API contract
- **Phase 5:** PlayMode test — full worldgen run with overlay UI, every question resolved
- **Phase 6:** dry-run on a forked Figma file

## Acceptance Criteria

- [ ] `start.exe` → Boot screen (new UI system) → if assets missing, generation screen plays through every missing asset visibly → "Continue to Main Menu"
- [ ] Boot, Loading, and Worldgen screens use the new UI system end-to-end. The existing `MainMenuCanvas` and in-scene HUD stay on UGUI for v1 (see D1) — a follow-up migration PR (out of scope here) ports them once the new system is proven.
- [ ] New Game → Loading Screen → Worldgen runs step by step, every question/dice/decision is on screen
- [ ] Generation failures are logged to disk **and** shown in UI
- [ ] All in-game UI (HUD, modals, panels) uses the same design tokens and prefab library
- [ ] Figma changes propagate via weekly scheduled task (optional Phase 6)

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| 800 assets take hours on first launch | Lazy generation (D3 Option A) + per-scenario subsets |
| Refactoring existing scenes breaks gameplay | Phase 1 introduces *new* prefabs without touching old canvases; scene migration is a later, separate PR |
| UI Toolkit learning curve | Limit to Boot/Loading/Worldgen first; HUD stays UGUI until proven |
| Figma token rotation | Already in env (`QA_FIGMA_TOKEN`); document rotation steps |
| ai.assistant patcher gets out of sync with future ai.assistant versions | `AiAssistantTokenizerPatch.cs` already idempotent and no-ops when call site is absent; if Unity ships a fix, our patch becomes a no-op automatically |

## Out of Scope

- Replacing AssetForgeQueue / OnnxAssetForge (existing domain logic stays)
- Replacing WorldgenService (only view added)
- Localization
- Save UI redesign (separate PRD when needed)

## Appendix: Existing Building Blocks Already in Tree

| Component | Path | Status |
|---|---|---|
| OnnxAssetForge | `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs` | Working, tests passing |
| ForgeBootstrap | `Assets/Scripts/Presentation/Ember/Forge/ForgeBootstrap.cs` | MonoBehaviour, registers via `ForgeLocator` |
| ForgeMenu | `Assets/Editor/Ember/Menu/ForgeMenu.cs` | Editor batch generation, works |
| AssetForgeCache | `Assets/Scripts/Simulation/Forge/AssetForgeCache.cs` | Disk cache by prompt key |
| PromptComposers | (referenced from ForgeMenu) | Builds NPC portrait prompts |
| WorldgenService | `Assets/Scripts/Simulation/Worldgen/` | Domain logic, no view |
| MainMenu scene | `Assets/Scenes/Ember/MainMenu.unity` | Exists with `MainMenuCanvas` |
| AI Assistant | Embedded under `Packages/com.unity.ai.assistant/`; `SigLip2Text.cs` line 59 rewritten to the no-arg `SentencePieceTokenizer.Create(stream)` overload (defaults preserved). Lands via PR #207. | Stable once PR #207 merges |
