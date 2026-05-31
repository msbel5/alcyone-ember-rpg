# Codex work packages — mechanical staged refactors (2026-05-31)

Hand-off for Codex while Claude works the **gameplay / live-playtest / UI** lane. These 7 packages are
**isolated, test-only, no-playtest-needed** refactors. Each is one focused PR/commit.

## Ground rules (avoid clobbering Claude's lane)
- **Do NOT touch these files** (Claude's active gameplay/UI lane):
  `Assets/Scripts/Presentation/Ember/Bootstrap/EmberWorldHost*.cs`,
  `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter*.cs`,
  `Assets/Scripts/Presentation/Ember/UI/*` (EmberHud, DialogBoxPanel, InventoryGrid, PauseMenu, EmberMainMenuUI),
  `Assets/Scripts/Presentation/Ember/CharacterCreation/*`, the scene `.unity` files, the scene recipes.
- **Verify before every commit:** `bash tools/validation/run-validation.sh --mode fallback` (must stay
  green) for Domain/Sim/Data/Infra changes; a Win64 batchmode build (Editor CLOSED) for anything
  Presentation/Editor. Zero `error CS`.
- **Zero behaviour change** unless the package explicitly says otherwise; partial-class / file splits are
  pure moves (the compiler + tests prove equivalence).
- Commit trailer: `Co-Authored-By: <your codex id>`. Push to `main`. One package per commit. **Pull
  before each commit** — Claude is committing to `main` in parallel.

## Packages

### CWP-1 — E7-017: move the forge provider impl off the Simulation boundary
- **Why:** `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs` (+ `Sd15LcmPipeline.cs`, `SdxlTurboPipeline.cs`)
  are runtime ONNX provider implementations living in the deterministic `EmberCrpg.Simulation` asmdef.
  Simulation must stay headless/deterministic; providers are Infrastructure.
- **Do:** move the provider *implementations* to `Assets/Scripts/Infrastructure/Forge/` (new folder +
  asmdef ref) OR, if the move is too invasive, keep the contracts/manifests in Simulation and relocate
  only the I/O impl. Keep `ModelManifest`/contracts where the consumers expect them.
- **Verify:** Simulation asmdef compiles without the provider I/O; fallback green; forge EditMode tests pass.

### CWP-2 — E7-018: make the world tick data-driven (AFTER a digest test)
- **Why:** `Assets/Scripts/Simulation/Composition/WorldTickComposer.cs` hardcodes crop species, stock
  thresholds, price step, and cadence constants.
- **Do FIRST:** add a same-seed deterministic **digest test** (tick N times, hash the world state) so any
  refactor is provably behaviour-preserving. THEN extract the hardcoded catalogs/rules into data
  definitions (`ScriptableObject` or pure-C# data tables). Do not change the numbers — only their home.
- **Verify:** the digest is byte-identical before/after; fallback green.

### CWP-3 — E7-015: convert sync-over-async provider calls to cancellable async
- **Why:** 6 `.GetAwaiter().GetResult()` sites in `Assets/Scripts/Infrastructure/AiDm/LlmHttpClientCore.cs`,
  `LocalQwenClient.cs`, `NativeLlmClient.cs` (+ `Presentation/Ember/Forge/ComfyUiAssetForge.cs`).
- **Do:** add proper `CompleteAsync(LlmRequest, CancellationToken)` paths with timeout/cancellation; keep
  the existing sync `Complete()` as a thin wrapper for current callers (do not break the public surface).
- **Verify:** add timeout/cancellation unit tests; fallback green. **Coordinate** on `NativeLlmClient.cs`
  (Claude touched `IsUsableModelFile`/`StripTrailingTurnMarkers`/`EnsureModelReady` recently) — pull first.

### CWP-4 — E7-020: Input System migration behind the EmberInput facade
- **Why:** legacy `UnityEngine.Input` only; no rebinding/action-maps. `Assets/Scripts/Presentation/Ember/
  Inputs/EmberInput.cs` is the verified single choke point (all 25 direct reads live there).
- **Do:** add `com.unity.inputsystem`; set `activeInputHandler` to Both; author an `.inputactions` asset;
  rewrite EmberInput's *internals* against `InputAction` while keeping its public members (Move/Look/
  Interact/SaveQuick/…) byte-identical; add a minimal rebinding screen. **Public API frozen.**
- **Verify:** Win64 build; a PlayMode smoke of movement/look/interact/save/menu. This is the largest one.

### CWP-5 — E7-021: reduce Resources.Load footprint
- **Why:** fonts/theme/loading-flavours/textures loaded via `Resources.Load`.
- **Do:** inventory every `Resources.Load`; keep only the tiny global fallbacks (the 2 fonts + theme
  tokens); move the rest to explicit serialized references / a small registry. Stage it; don't move fonts
  blindly (they're referenced by runtime UI).
- **Verify:** Resources inventory diff; Win64 build; UI still renders.

### CWP-6 — E7-025: classify the NuGet marker
- **Why:** `Assets/Plugins/NuGet/.nuget-installed.json` is a NuGetForUnity restore marker (Unity ignores
  dot-prefixed files, so no meta).
- **Do:** add a one-line note to `docs/REPO_HYGIENE.md` documenting it as an intentional restore marker.
  Trivial.

### CWP-7 — E7-012 (HALF): introduce `ILlmRouter` interface ONLY
- **Why:** the adapter reaches the `ForgeLocator.LlmRouter` static (concrete `LlmRoutingService`) from 6
  sites. **Claude owns `DomainSimulationAdapter*.cs`** — so Codex does ONLY the non-adapter half:
- **Do:** define `public interface ILlmRouter { LlmResponse Complete(LlmRequest req, out string chosen); }`
  in the Simulation.AiDm asmdef; make `LlmRoutingService : ILlmRouter`; retype `ForgeLocator.LlmRouter`
  + `ForgeBootstrap.Register(...)` to `ILlmRouter`. **Stop there** — leave the 6 adapter call-sites to
  Claude (they already call `.Complete`, so they keep compiling against the interface).
- **Verify:** fallback green; Win64 build.

---

Items NOT in this hand-off (Claude's lane): **E7-007** (save slots — needs UX design), **E7-019**
(faction decay — needs a deterministic-design call), **E7-004/006** (scene actor-ID migration — gameplay
identity + Editor), plus all live playtest bugs. **E7-003** (proof labels) is already done in CURRENT_STATE.
