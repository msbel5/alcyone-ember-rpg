# PRD — Typed generation pipelines + in-game-variable prompts + chained references (v1)

_Owner: gameplay/forge lane (Claude implements; Codex may take isolated sub-PRDs). Written 2026-05-31._
_MUST preserve the existing SOLID layering + asmdef boundaries (Domain/Sim/Data/Infra/Presentation).
Generation is **canonical-per-seed**; the deterministic simulation stays the source of truth; the LLM/
image models are flavour-only and never mutate authoritative world state except via validated tools._

## 0. Vision (from the user)

Every **image type** has its OWN pipeline (tuned prompt template, size, sampler params, post-process):
NPC **billboard**, character **portrait**, **item/weapon**, **furniture**, **logo/UI icon**, **inventory
icon**, **real-world/environment** prop. Each entity is generated **from its in-game variables** (a bandit
is generated from its stats + clothing + weapon, not a generic prompt). Pipelines can **chain references**:
generate the portrait first, then feed it as a reference (img2img / IP-Adapter-style) for the billboard;
if the entity already has a generated weapon image, render it **holding that weapon**. **LLM pipelines**
(story, conversation, worldgen) are first-class too. All pipelines are **viewable/configurable in an
Options page before entering the game**. Old/garbage generated assets are cleared.

## 1. Foundation gate — D1: SDXL-Turbo must produce correct images (BLOCKER)

Nothing below matters until the forge decodes real images. See `PRD_image_generation_sdxl_v1.md`.
Runtime diagnostic confirms the forge runs on CUDA (`OnnxForge=True`) but decodes near-noise → rainbow.
**Top suspects (fix + verify with a generated 512² "carved bone die" that a human can recognize):**
1. **Text-encoder hidden layer.** SDXL conditions on the **penultimate** hidden state
   (`hidden_states[-2]`) of BOTH CLIP encoders; the current pipeline reads `last_hidden_state`. If the
   ONNX export exposes per-layer hidden states, switch to the penultimate; if not, re-export the encoders
   with `output_hidden_states`.
2. **1-step fragility.** Move to the documented **4-step** SDXL-Turbo Euler schedule (still fast, far more
   robust) instead of the single σ_max step.
3. **eps vs v / timestep encoding** — confirm `out_sample` is ε and `timestep=999` is what the exported
   UNet expects.
Method: numeric instrument (latent L2 norm after denoise; decoded min/max) via an **Infrastructure** log
callback (NOT `UnityEngine.Debug` inside Simulation — keep it engine-free), then a human screenshot review.

## 2. Architecture — typed pipeline registry (no SOLID break)

```
Domain.Forge (pure contracts, engine-free)
  IAssetForge                      (existing — generic generate)
  + AssetKind enum { NpcBillboard, Portrait, Item, Furniture, Logo, InventoryIcon, EnvironmentProp }
  + ImageGenSpec  (record: AssetKind, width, height, steps, guidance, prompt, negativePrompt,
                   referenceImageId?, seed)            // pure data
  + IImageGenSpecFactory                                // builds an ImageGenSpec from a domain entity

Simulation.Forge (deterministic, ONNX provider impl behind the contract)
  TypedPipelineRegistry : maps AssetKind -> per-kind prompt template + default size/steps/post-process
  SdxlTurboPipeline (fixed in D1) consumes ImageGenSpec  (size/steps/guidance/reference now honoured)

Presentation.Ember.Forge
  EntityImageRequestBuilder : turns a live WorldState entity (ActorRecord, ItemRecord, NpcSeed, Worksite)
                              into an ImageGenSpec via IImageGenSpecFactory (in-game variables -> prompt)
  GeneratedAssetStore : id -> generated PNG path/bytes + provenance (kind, seed, reference chain)
```

Rules: prompt **templates** + kind defaults live in data (Sim/Data), not hardcoded in one method. The spec
is **pure data** crossing the boundary; the ONNX impl stays in Sim/Infra behind `IAssetForge`. Presentation
never depends on `OnnxAssetForge` directly — only the interface + `ImageGenSpec`.

## 3. Per-kind pipelines (D-PIPE-*)

Each kind = a prompt template (with `{slots}` filled from entity variables) + size + steps + negative +
post-process. Initial set:

| Kind | Size | Prompt template (slots filled from in-game variables) | Post |
| --- | --- | --- | --- |
| **NpcBillboard** | 512×768 | "full-body {role} {archetype}, wearing {clothing}, holding {weapon}, {mood}, {worldStyle}, flat front view, transparent-ready, dark fantasy" | bg cutout → billboard atlas |
| **Portrait** | 512×512 | "head-and-shoulders portrait of {name}, a {role}, {distinctiveFeatures}, {clothing}, {accessory}, {worldStyle}" | gold frame |
| **Item/Weapon** | 384×384 | "a single {itemName}, {material}, {quality}, studio lighting, dark fantasy, centered, plain background" | trim |
| **Furniture** | 512×512 | "a {furnitureName} for a {settlementKind}, {material}, dark fantasy, plain background" | trim |
| **Logo/UI icon** | 256×256 | "minimal emblem of {faction}, heraldic, flat, gold-on-dark" | crop |
| **InventoryIcon** | 128×128 | "inventory icon of {itemName}, {material}, top-down, plain dark background" | downscale |
| **EnvironmentProp** | 768×512 | "{settlementKind} {biome} establishing shot, {worldStyle}, dark fantasy" | — |

D-PIPE acceptance: each kind generates a recognizable, on-theme image at the right size from a real entity.

## 4. In-game-variable-driven prompts (D-VAR)

`EntityImageRequestBuilder` reads the live domain record and fills the template slots — e.g. a bandit:
`ActorRecord{ role=Bandit, archetype, mood }` + equipped `ItemRecord`s (clothing, weapon) →
`"full-body bandit human, wearing ragged leather, holding a notched shortsword, wary, low-fantasy …"`.
Same seed + same entity ⇒ same image (canonical-per-seed). No hardcoded per-entity prompts.

## 5. Chained reference images (D-CHAIN)

- Generation order per entity: **Portrait → Billboard(reference=Portrait) → equipped items first, then
  the holding-the-weapon billboard variant**.
- `ImageGenSpec.referenceImageId` points at a previously-generated asset in `GeneratedAssetStore`. The
  pipeline uses it as an **img2img / reference** init (requires an img2img-capable path — D1+ adds the
  VAE-encode + partial-denoise route, or an IP-Adapter input if the model supports it).
- "Holding the generated weapon": once the weapon image exists, the billboard spec references both the
  portrait (identity) and the weapon (prop) so the figure holds *that* weapon.
- Determinism: the reference chain + seeds are recorded in provenance so a re-gen reproduces it.

## 6. LLM pipelines (D-LLM)

Story / conversation / worldgen already route through the LLM router. Formalize them as **named LLM
pipelines** (system-prompt template + tool set + token budget) in the same registry style, so the Options
page can show/tune them. They remain flavour-only + tool-validated (no authoritative writes except via the
validated tool router).

## 7. Options preview page (D-OPTIONS)

A new **Options → Generation** page (Presentation UI, before entering the game) that lists every pipeline
(image kinds + LLM pipelines), shows its template + params, lets the player **preview-generate** a sample,
and toggles (e.g. quality/steps, enable/disable a kind, offline-fallback). Built on the existing UI single-
source pattern (host/`IUiPanel`), no new layering violation. Read-only of the registry + a generate button.

## 8. Asset cleanup (D-CLEAN)

Clear the stale/garbage generated assets (the rainbow PNGs) from the generated cache + `Assets/Generated`
so only correctly-generated assets remain. Keep the deterministic procedural fallbacks (swatch) as the
offline floor. Provenance flag distinguishes real vs fallback.

## 9. Execution order + ownership
1. **D1 SDXL fix** (Claude, iterative + screenshot review) — BLOCKER.
2. **Typed registry + ImageGenSpec contract** (Claude; the `AssetKind`/spec/`IImageGenSpecFactory` pieces
   are isolated enough that Codex could take the pure-contract half).
3. **D-PIPE per-kind templates** + **D-VAR entity builder** (Claude).
4. **D-CHAIN reference images** (Claude; needs the img2img path).
5. **D-OPTIONS page** (Claude, UI lane).
6. **D-LLM pipeline formalization** (Codex-friendly: pure registry/config).
7. **D-CLEAN** (trivial; either).

## 10. Verification (every step)
- `bash tools/validation/run-validation.sh --mode fallback` green.
- Win64 batchmode build (Editor closed) Success, 0 `error CS`.
- A proof-driver gen of each kind from a real entity; **human screenshot review** confirms recognizable,
  on-theme, correctly-sized images (image quality can't be asserted headlessly).
- Determinism: same seed+entity+reference-chain ⇒ same bytes.
