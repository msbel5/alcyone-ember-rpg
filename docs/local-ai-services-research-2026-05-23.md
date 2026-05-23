# Local AI Services Research - 2026-05-23

Reference path requested in the sprint, `D:\projects\ember-rpg\frp-backend\engine\generators\`, does not exist on this machine. The live asset-generation reference is `D:\projects\ember-rpg\tools\asset_pipeline.py` plus `D:\projects\ember-rpg\tools\asset_jobs\*.json`.

## Existing Generator

- Generator: Python Diffusers pipeline in `tools/asset_pipeline.py`.
- Backend modes:
  - `local_sdxl`: `diffusers.AutoPipelineForText2Image` with `stabilityai/stable-diffusion-xl-base-1.0`.
  - legacy `hf_api_flux`: Hugging Face router to `black-forest-labs/FLUX.1-schnell`.
- LoRA stack:
  - Gerald Brom XL, scale `0.70`
  - Dark Fantasy XL, scale `0.40`
  - Dark Gothic Fantasy, scale `0.30`
  - Fallout Art SDXL, scale `0.20`
  - LCM LoRA, scale `1.00`
- Scheduler:
  - `LCMScheduler` when LCM is active.
  - `LCM_STEPS = 8`
  - `LCM_GUIDANCE = 1.5`
  - CLI default guidance is `6.0` for non-LCM.
- Image size:
  - 1024 x 1024 for sprites, items, portraits, spells, status icons, body silhouettes.
  - UI plan jobs can carry custom sizes.
- Post-processing:
  - PIL crop/alpha cleanup helpers.
  - Background-removal path via `rembg`.
  - Raw outputs tracked in `tools/asset_raw`, final outputs under `godot-client/assets/generated`.
- Batching:
  - Jobs are materialized as JSON plans under `tools/asset_jobs`.
  - `overnight_asset_regen.py` warm-loads the SDXL + LoRA + LCM stack once and processes the queue sequentially.
  - Observed overnight run: `891/891`, `29888s`, `1.79/min`.

## Prompt Material To Lift

Portrait base:

```text
painted CRPG character portrait, 3/4 view shoulders up, single subject centered, fully clothed with visible gear and armor, dark fantasy oil painting, Gerald Brom + Planescape Torment aesthetic, painterly brushwork, dramatic chiaroscuro lighting, muted tavern backdrop, expressive face, production game portrait
```

Item base:

```text
A 1024 square painted BG1 CRPG inventory icon, exactly one item centered on pure white background that will be removed by automated segmentation, rich ornate detail with visible engraving and chiaroscuro lighting, painterly Brom oil brushwork, single item only
```

Region/tile base:

```text
painted CRPG terrain tile, seamless tileable texture, top-down, consistent dark fantasy painterly palette, hand-painted brushwork, high detail, no text
```

Negative base:

```text
text, typography, letters, watermark, label, logo, blurry, low contrast, photorealistic, deformed, noisy, cluttered background, frame, border, poster layout, jpeg artifacts, lowres, pixel art, anime, cel shaded, 3d render, unreal engine
```

Portrait negative:

```text
shirtless, bare chest, topless, nude, muscular savage, loincloth, missing armor, missing clothes
```

Item negative:

```text
multiple items, two items, three items, pair of items, duplicate items, variant display, side by side items, item collection, item catalog, multiple weapons, two weapons, two swords, two daggers, two axes, weapon pair, weapon comparison, weapon set, matching pair, symmetric display
```

## Minimum Viable Local Backend

Primary choice: ComfyUI HTTP API.

- Runs locally and can reuse a loaded checkpoint/workflow without blocking Unity.
- HTTP contract is scriptable: POST `/prompt`, poll `/history/{prompt_id}`, download `/view?filename=...`.
- Better fit for 3070/1070-class machines than embedding Diffusers in Unity or shipping model binaries.
- Ember should ship only HTTP clients and prompt composers; users install checkpoints themselves.

Alternative: Stable Diffusion WebUI Forge.

- Good for low-VRAM cards and easy Windows setup.
- API is also HTTP, but workflows are less explicit than ComfyUI graphs.
- Keep it as a later adapter behind `IAssetForge`; do not couple the foundation to Forge-specific payloads.
