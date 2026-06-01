# PRD — GenerationManager: one SOLID controller for all image generation (v1)

_Owner: forge/generation lane. Written 2026-06-01. Single-user game (personal): heavy local models are
fine, but generation MUST be serialized + resource-guarded so it never crashes the game. Builds on the
typed-pipeline Domain contracts already landed (AssetKind/ImageGenSpec/ImageGenKindTemplate/
IImageGenSpecFactory). Domain/Sim stay engine-free + deterministic; LLM is flavour-only (crafts prompts,
never authoritative state)._

## 0. Why (the SOLID gap, confirmed by inspection)
Generation logic is scattered with no single owner:
- `AssetForgeQueue` (priority queue + SemaphoreSlim) **EXISTS but is orphaned** — never wired, so gens run
  concurrently → VRAM spikes / contention / the CC-portrait 45s timeout / crash risk.
- Prompt composition is split (`PromptComposers`, `LlmPromptComposer`, `StaticPromptCatalog`,
  `ImageGenKindTemplate`) with no precise "real object" discipline → "dice" renders a pile/ornate art.
- Model/size selection (`EmberForgeFactory`, manifest `modelHint`) is ad-hoc; small canvases (64–256)
  produce garbage; SDXL@1024 (which works on this machine) is underused.
- No RAM/VRAM guard anywhere.

**Fix:** a single `GenerationManager` that owns the queue (one-at-a-time), the resource guard, model
selection, and prompt composition. Everything that generates an image goes through it.

## 1. GenerationManager (the controller)
Location: `Simulation/Generation` (engine-free core) + a thin `Presentation/Ember/Forge` host that injects
Unity bits (SystemInfo). Single entry point:

```
Task<AssetGenerationResult> GenerateAsync(GenerationRequest req, AssetForgePriority priority, CancellationToken ct)
  GenerationRequest = { AssetKind kind, string subject (object name + in-game variables), uint seed,
                        string referenceImageId? }
```

Responsibilities (in order):
1. **Serialize: ONE generation at a time.** Wire the existing `AssetForgeQueue` with **capacity = 1**
   (configurable). All callers enqueue; a single worker dequeues + runs. Priority = PlayerFacing first
   (a portrait jumps ahead of background icon regen — fixes the CC-portrait stall).
2. **Resource guard (no crashes).** Before each run: check `SystemInfo.graphicsMemorySize` /
   `systemMemorySize` + current process working set. If below a kind's threshold, **downscale the target**
   (1024→512) or **defer**; never run two heavy gens at once. Log the decision. (Injected via an
   `IResourceProbe` so Sim stays engine-free.)
3. **Model selection (best available).** Via `EmberForgeFactory`: SDXL-Turbo on CUDA (native **1024**),
   SD1.5-LCM **512** fallback for no-cuDNN machines. Generate large, **downscale to the slot**.
4. **Prompt composition.** Delegate to `IPromptComposer` (§2). Deterministic geometric template +
   anti-pattern negative; optional LLM refinement.
5. **Cache + provenance.** Reuse `GeneratedAssetProvenance` (version + kind + seed). Same seed+subject ⇒
   same bytes. One worker ⇒ deterministic ordering.

Acceptance: `AssetForgeQueue` is wired (capacity 1) and exercised by a test that proves two concurrent
`GenerateAsync` calls run sequentially; a low-memory probe forces a downscale; PlayerFacing preempts
Background.

## 2. IPromptComposer — precise "real object" prompts (deterministic + LLM)
The lever proven empirically: describe the **object's geometry**, not its casual name ("dice"→pile).
- **Deterministic layer (`GeometricPromptCatalog`):** per object, a precise GEOMETRIC template + a
  per-kind anti-pattern negative. Examples (data, not hardcoded in one method):
  - die → `"a single six-sided game die, one solid {material} cube, smooth rounded corners, neat black
    pip dots (1–6) on the faces, exactly ONE cube, centered, isolated, plain dark background, studio
    product photo, sharp focus, hard edges"` · neg `"pile, many, cluster, stack, scattered, spheres,
    ornate, filigree"`.
  - sword/potion/key/etc. → analogous single-object geometric templates.
  - portrait/NPC → reuse the proven `PromptComposers` portrait template (already good).
- **LLM layer (`LlmPromptComposer`, controlled):** given the object name + in-game variables, the LLM
  expands to a precise geometric description; the deterministic template is the validated fallback +
  schema frame. Flavour-only; result cached per seed ⇒ deterministic. Configurable: off / on-device Qwen /
  best cloud model.
- Wire into `IImageGenSpecFactory` so the typed pipeline + the manager both use it.

Acceptance: a generated **die** is ONE recognizable die (not a pile/art); a human eyeball confirms a few
objects. Headless structure check stays green.

## 3. Best models + sizes
- Image: SDXL-Turbo **1024** (1-step, configurable via `ImageGenSpec.Steps`) when CUDA present; SD1.5 512
  fallback. Downscale to display size. (FLUX.1-schnell is a future drop-in if VRAM allows — note only.)
- LLM (prompt refinement): pluggable; default on-device Qwen, optional best cloud model behind a config +
  key (env-only, never committed).
- Migrate `CoreAssetManifest` icon/item/logo entries to go through the manager (SDXL@1024 where available)
  instead of hardcoded sd15-lcm small sizes.

## 4. Execution (Codex-friendly slices; PRD→tests→code→verify)
1. **Wire `AssetForgeQueue` (capacity 1) into a `GenerationManager`** + a single worker; route
   `OnnxAssetForge` calls through it. Test: concurrency→sequential.
2. **`IResourceProbe` + guard** (downscale/defer on low VRAM). Test: low-probe forces 512.
3. **`IPromptComposer` + `GeometricPromptCatalog`** (die/sword/potion/… geometric templates + negatives) +
   wire `LlmPromptComposer` as the optional refine step. Test: prompt contains geometry, not "{slot}".
4. **Route portrait + CoreAssetManifest icons through the manager** (SDXL@1024, downscale). Bump provenance
   version to regenerate.
5. **Human eyeball** a die + a couple objects (image quality can't be asserted headlessly).

## 5. Guardrails
- `IAssetForge` contract unchanged; manager wraps it. Domain/Sim engine-free + deterministic.
- One generation at a time is a HARD invariant (capacity 1) — never regress to concurrent.
- LLM crafts prompts only; never authoritative world state.
- Honest provenance (`isPlaceholder`).
- Verify each slice: `bash tools/validation/run-validation.sh --mode fallback` green + Win64 build Success.
