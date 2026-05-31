# PRD — SDXL-Turbo image generation correctness + portrait wiring (v1)

_Owner: gameplay/forge lane. Written 2026-05-31. Must NOT break the existing SOLID layering or asmdef
boundaries (Domain/Sim/Data/Infra/Presentation). This is a correctness + wiring PRD, not a rewrite._

## 1. Problem (from live playtest + runtime diagnostic)

- The character-creation **portrait** is a procedural swatch (`CharacterCreationPortraitSwatch`), NOT a
  real generated image — it never goes through the SDXL forge.
- The **loading-screen** decoration images and any **generated icon** (e.g. a "dice" asset) come out as a
  **rainbow / non-meaningful blob**, not a recognizable image.
- Runtime diagnostic (`validation-output/forge-diag-player.log`):
  `Forge Connectivity: … OnnxForge=True … Failure=''` — so the ONNX/SDXL forge **IS** initialising and
  running on the CUDA provider. **This is NOT placeholder mode** (the placeholder is an 8×8 gray square,
  `OnnxAssetForge.PlaceholderPng`). The pipeline runs but **decodes near-noise → rainbow**.

**Conclusion:** the bug is inside `SdxlTurboPipeline.Run` — the latents are not actually being denoised
(or are mis-decoded), so the VAE emits noise. Fixing it is a focused numerical/pipeline fix, not a
re-architecture.

## 2. Current pipeline (Assets/Scripts/Simulation/Forge/SdxlTurboPipeline.cs)

Single-step SDXL-Turbo:
1. CLIP BPE tokenize prompt → tokens (`ClipBpeTokenizer`).
2. `EncodeText(TextEncoder)` → `hidden768`; `EncodeText2(TextEncoder2)` → `hidden1280` + `pooled1280`.
3. `SdxlConditioning.Concat` → `HiddenStates2048` + `Pooled1280`.
4. `latents = SampleGaussian(seed, len, SigmaMax)` with `SigmaMax = 14.6146`.
5. `scaled = ScaleLatentsForEulerInput(latents, SigmaMax)`.
6. `eps = RunUnet(scaled, t=999, conditioning, timeIds)`.
7. **Single Euler step:** `latents[i] -= SigmaMax * eps[i]`.
8. `latents[i] /= VaeScale` (`0.13025`).
9. `decoded = DecodeLatents(latents)`; `rgba = (decoded + 1) * 0.5`.

## 3. Root-cause hypotheses (ranked — instrument, then fix the one that's wrong)

> **Method:** add temporary `Debug.Log` of the L2 norm / min / max of the latents after steps 4, 7, 8 and
> of `decoded` after step 9, for a fixed seed+prompt. A working denoise drops the latent norm from
> ~σ·√N toward the data manifold; if it stays ~σ·√N, the UNet/step is the culprit; if the latent is fine
> but `decoded` is out of [-1,1] noise, the VAE/scale is the culprit.

1. **`time_ids` / added-conditioning wrong (most likely).** SDXL's UNet requires `time_ids`
   (original_size H,W; crop_top,left; target_size H,W → 6 values) fed via `text_embeds` + `time_ids`
   add-embeds. If `timeIds` is zero/misordered, the UNet prediction is garbage → no denoise. **Verify the
   exact `time_ids` vector and that it is passed to the right UNet input name.**
2. **Single step insufficient / wrong σ schedule.** SDXL-Turbo's 1-step expects a specific
   (timestep ↔ σ) pair. Confirm `t=999` maps to `SigmaMax=14.6146` for this exported model, and that
   `ScaleLatentsForEulerInput = x / sqrt(σ²+1)` (k-diffusion `c_in`). If unsure, switch to the documented
   **4-step** SDXL-Turbo Euler-ancestral schedule (still fast) — robustness over 1-step.
3. **eps vs v-prediction.** SDXL-Turbo is ε-prediction, so `x0 = x − σ·ε` is correct — but confirm the
   exported UNet's output is ε (not v). If v-pred, use `x0 = x − σ·(σ·v + x/√(σ²+1))`-style conversion.
4. **VAE scale direction / output range.** `latents /= 0.13025` then decode is correct for SDXL; confirm
   the exported VAE-decoder expects pre-scaled latents and outputs roughly [-1,1] (the `(x+1)*0.5` map).
5. **Channel / layout (NCHW vs NHWC).** Confirm `DecodedNchwToRgba` reads the VAE output in the layout the
   exported decoder actually produces.

## 4. Deliverables

### D1 — SDXL produces a recognizable image (the core fix)
Fix whichever stage §3 identifies. **Acceptance:** generating with prompt `"a single carved bone die,
studio lighting, dark fantasy"` at 512×512 produces an image a human recognizes as a die/object — NOT
rainbow noise. Validate by running the proof driver's asset-gen capture and a human screenshot review
(`validation-output/…`). Keep generation deterministic per seed.

### D2 — Wire the character-creation portrait through the forge
- Route the CC portrait request through the existing `IAssetForge` (SDXL) using the NpcPromptJson to build
  the prompt (archetype + hues + features + clothing). On success, show the generated PNG.
- **Keep `CharacterCreationPortraitSwatch` as the deterministic fallback** when the forge is unavailable /
  in placeholder mode / times out — never a blank box. Off the main thread (mirror the existing
  `GeneratePortrait` Task.Run + poll pattern). Reroll re-generates with a new seed.
- **Boundary:** the portrait request goes through the `IAssetForge` interface (Presentation → contract),
  NOT a direct dependency on `OnnxAssetForge`. No new layering violations.

### D3 — Loading-screen + menu decorations use the same fixed pipeline
Once D1 lands, the loading/menu images (already routed through the forge) become meaningful automatically.
No separate code path — verify they look correct.

## 5. Out of scope / guardrails
- Do **not** move the forge across asmdefs here (that's E7-017 / CWP-1, separate).
- Do **not** change the `IAssetForge` contract shape or the cache/queue.
- Keep `Simulation` engine-free where it already is; the portrait wiring lives in Presentation behind the
  interface.
- Keep the placeholder/fallback provenance honest (`isPlaceholder` flag) — never present a fallback as a
  real generation.

## 6. Verification
- `bash tools/validation/run-validation.sh --mode fallback` green (no regression).
- Win64 batchmode build (Editor closed) `Result: Success`, 0 `error CS`.
- A proof-driver run that generates the die + a portrait; a human confirms the screenshots are
  recognizable images, not rainbow noise.
- Determinism: same seed+prompt → same bytes.

## 7. Suggested execution order
D1 (instrument → fix the broken stage → recognizable die) → D3 (confirm loading/menu) → D2 (portrait
wiring + swatch fallback). D1 is iterative (generate → inspect → adjust); do it with the proof driver +
human screenshot review, since image quality cannot be asserted headlessly.
