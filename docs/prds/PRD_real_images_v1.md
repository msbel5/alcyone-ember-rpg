# PRD — Real generated images: portrait wiring + cache regeneration + one-centered-item prompts (v1)

_Owner: forge/gameplay lane. Written 2026-06-01. MUST preserve SOLID layering (Domain/Sim/Data/Infra/
Presentation) + determinism. Uses the typed-pipeline Domain contracts already landed (AssetKind /
ImageGenSpec / ImageGenKindTemplate / IImageGenSpecFactory, commit 2b236b45)._

## 0. Problem (from live playtest, 2026-06-01)
1. **Portrait never uses the forge.** `CharacterCreationController.Portrait.cs:142` only does
   `_panel.SetThumbnail("portrait", CharacterCreationPortraitSwatch.Build(json))` — a procedural swatch.
   The SDXL forge is never called for the portrait. (D2 was never implemented.)
2. **Loading/UI images are stale committed cache.** `LoadingScreenController.TryLoadGeneratedTexture`
   reads `Assets/Generated/Core/<entryId>.png`; those PNGs are from **before** the SDXL decode fix
   (rainbow-era). They are never regenerated → the player sees old/bad images. Boot labels them "(cached)".
3. **Generated images are wrong shape for the game.** SDXL renders **scattered multiple objects**
   ("tooth-like"), but the game needs **ONE item centered on a plain, removable background** so it can be
   cut out → transparent → used as a billboard/portrait. This is mainly a **prompt** problem.

## 1. D2 — Wire the character-creation portrait through the forge (PRIMARY, most visible)
- After `ApplyPortrait(json, …)` sets the swatch (instant fallback), kick off an **async** forge
  generation (mirror the existing `Task.Run` + coroutine-poll pattern in `CharacterCreationController.
  Portrait.cs` / `AwaitPortraitUpgrade`). On success, decode the PNG → `Texture2D.LoadImage` →
  `Sprite` → `_panel.SetThumbnail("portrait", sprite)`. Keep the swatch if the forge is unavailable /
  placeholder / times out / cancelled. Reroll re-generates with a new seed.
- **Boundary:** go through `IAssetForge` (via `ForgeLocator.AssetForge`), NOT a direct `OnnxAssetForge`
  dependency. Build the request via the typed pipeline: `IImageGenSpecFactory.Create(AssetKind.Portrait,
  subject, seed)` → map `ImageGenSpec` → the existing `AssetGenerationRequest` (kind.ToSubjectKind()).
- **Subject from in-game variables (D-VAR):** build `subject` from the `NpcPromptJson` —
  `"{archetype} with {distinctive_features}, wearing {clothing_style}, {accessory}, {mood} expression,
  {world_style_anchor} palette (hues {primary}/{secondary})"`. Deterministic per seed.
- New `PortraitPromptBuilder` (Presentation, pure function: `NpcPromptJson -> subject string`), unit-tested.

## 2. One-centered-item prompts (the quality fix — applies to ALL kinds)
Tighten `ImageGenKindTemplate` scaffolds + add a **shared negative** so every kind yields ONE centered,
isolated subject on a plain background (cut-out friendly):
- Portrait scaffold → `"a single centered head-and-shoulders portrait of {subject}, one person, facing
  forward, symmetrical, plain dark studio background, dark fantasy, painterly, sharp focus"`.
- Item scaffold → `"a single {subject}, one object, centered, isolated on a plain flat background,
  studio product shot, sharp focus, dark fantasy"`.
- Shared NEGATIVE (all kinds) → add: `"multiple, group, collage, tiled, grid, many objects, two heads,
  extra limbs, scattered, border, frame, text, watermark"`.
Acceptance: a human confirms a generated Portrait + Item each show ONE centered subject on a plain bg
(not a scattered group). Prompt-tuning is iterative (generate → eyeball → adjust) — Claude owns this loop.

## 3. Cache regeneration + versioning (the stale-image fix)
- Add a **pipeline version** constant (e.g. `ForgePipelineVersion = "sdxl-4step-penultimate-v2"`). Stamp
  generated assets' provenance with it. When the version differs from a cached asset's stamp, the asset is
  **stale → regenerate** instead of serving the old PNG.
- Provide an **Editor menu + a `--ember-regen-assets` proof-driver mode** that regenerates every
  `Assets/Generated/Core/*` entry through the live forge with the correct per-kind precise prompt, writes
  the fresh PNG + a `<id>.provenance.json` (kind, seed, version), and replaces the stale committed bytes.
- Delete genuinely-garbage stale entries (old rainbow PNGs) once their replacement is generated. Keep the
  deterministic procedural fallbacks (swatch / `Resources/Loading/generic`) as the offline floor.

## 4. Transparency (follow-up, after D2 looks right)
Add a background-removal post-process (flat-background chroma/luma key → alpha) so the centered subject
becomes a transparent cut-out usable as a billboard. Out of scope for v1's first cut; specify the seam
(`IImagePostProcessor`) so it slots in without a layering change.

## 5. Execution + ownership (per user: PRD → tests → code → verify, Codex for surgical pieces)
1. **PortraitPromptBuilder + tighter templates + shared negative** (Codex, surgical, unit-tested).
2. **D2 portrait async wiring + PNG→Sprite + swatch fallback** (Codex surgical wiring; Claude reviews +
   owns the generate→eyeball prompt loop).
3. **Cache version + regen tool/proof mode** (Codex), then Claude runs it + re-commits fresh assets.
4. **Transparency post-process** (later).

## 6. Verification (every step)
- `bash tools/validation/run-validation.sh --mode fallback` green (PortraitPromptBuilder + template tests).
- Win64 build (Editor closed) Result: Success, 0 `error CS`.
- Proof-driver generates a Portrait + an Item; **human eyeball** confirms ONE centered subject, on-theme,
  not scattered/rainbow. Determinism: same seed+subject ⇒ same bytes.
- Guardrails: `IAssetForge` contract unchanged; Domain/Sim engine-free + deterministic; honest
  `isPlaceholder`/provenance (never present a fallback as a real generation).
