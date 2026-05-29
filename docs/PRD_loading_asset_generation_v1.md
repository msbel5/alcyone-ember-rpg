# PRD: Loading-Screen Asset Generation — Environment Textures (do-it-right)

## Problem (root cause, not symptoms)
- Floors/walls render with **fallback placeholder materials** (the orange grid; `Tile_*_fallback.mat`), not correct themed textures — "every floor not rendered with correct image".
- Particle/VFX renderers shipped with **null materials** → magenta under URP (forge sparks, and a delayed combat VFX in CombatDungeon).
- The forge (`CoreAssetManifest` + `OnnxAssetForge` + `ForgeBootstrap`) generates **only** splash/logo/UI/character assets (`sd15-lcm`). It has **no floor/wall/environment entries**. Those visuals are static `Tile_*.mat` references, many pointing at fallbacks.
- Runtime material "rescues" (reassign-on-load, per-frame polls) **mask** this — they are band-aids and must not be the fix.

## Goal (per direction)
Every scene's environment image (floor, wall, key props) is **generated once during the loading screen** and **assigned to that scene's materials** before gameplay. No per-frame/per-second runtime checks. No magenta. Correct themed floors per scene.

## Design
1. **Manifest** — add environment entries to `CoreAssetManifest`, keyed by scene-theme + material role:
   e.g. `env_smithing_floor`, `env_smithing_wall`, `env_tavern_floor`, `env_dungeon_floor`, `env_oracle_floor`, … each with a themed generation prompt, target size (e.g. 512²/1024²), tiling flag, and model id.
2. **Generation** — `ForgeBootstrap` enqueues these on the existing `AssetForgeQueue`/`AssetForgeCache` during the loading screen, with progress surfaced on `LoadingScreen` (the user sees real "generating world textures…" progress, not a hidden hang).
3. **Assignment (the missing wiring)** — a deterministic `SceneEnvironmentDresser` that, as part of scene load, looks up each scene's material roles, pulls the generated texture from the forge cache, and assigns it (`_BaseMap`) to the matching `Tile_*` material. This is the one-shot step that replaces the fallback — done at load, never polled.
4. **VFX materials** — give each ParticleSystem a real URP particle material **at the prefab/source** (additive ember for forge sparks, etc.), so they are never null/magenta. No runtime VFX rescue.
5. **Fallback** — if a generation genuinely fails, assign a neutral *static* texture (never magenta). `UrpMaterialRescue` is demoted to a **last-resort one-shot safety only** (already de-polled), not the primary mechanism.

## Explicitly NOT (no band-aids, no drift)
- No per-frame / per-second material scans.
- No runtime "find magenta and swap" as the primary fix.
- No silent fallbacks that hide a missing generation — surface failures in the loading log.

## Verification
- Interactive Editor build (the headless batchmode path is currently wedged post-Editor-session).
- Manual screenshot of **every** gameplay scene (with + without UI): floors show themed generated textures, zero magenta (including the ~1-min delayed combat VFX, fixed at source), HUD pills+hotbar visible.

## Status of related work (already committed, PR #215)
- Magenta/null-material **one-shot** rescue (safety net; de-polled per direction).
- Gate-label font-material fix + always-included shader.
- Audio ring → soft noise. HUD pills + numbered hotbar. Creation wizard.
- These remain; this PRD is the *correct* replacement for the environment-texture gap that the rescues were masking.
