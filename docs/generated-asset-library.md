# Generated Asset Library

## Purpose

Alcyone Ember uses a pre-baked deterministic asset library instead of runtime AI generation. The shipping game should resolve approved imported assets by stable ids and tag queries, not by calling local ML tools or online APIs during play.

## Why this exists

- Runtime AI generation breaks determinism, approval flow, and build portability.
- Save data should store stable asset ids or key fields, not transient file paths.
- The same world seed should resolve the same approved art variant on every machine.
- Editor metadata can keep prompts, toolchain notes, and license review state without leaking editor-only workflows into builds.

## Runtime model

- `GeneratedAssetKey` normalizes kind/archetype/biome/culture/faction/role/material/tier/styleVersion/seed/promptHash into a deterministic stable id.
- `GeneratedAssetRecord` stores the stable id, prompts, tool metadata, approval state, and imported asset-relative paths.
- `GeneratedAssetDatabase` is a ScriptableObject catalog with deterministic lookup and validation.
- `GeneratedAssetResolver` selects one approved record by query + seed using sorted stable ids.

## Recommended 2D vs 3D split

- Keep as billboards:
  - NPCs and humanoids
  - Creatures where full 3D turning is unnecessary
  - Items, loot, pickups
  - Foliage clumps and small props
- Keep as real 3D:
  - Buildings
  - Doors, stairs, wells, mine entrances, bridges
  - Terrain chunks
  - Collision-critical or navigable structures

## Prompt presets

Prompt presets are deterministic templates for the external Forge pipeline. They do not generate art by themselves.

Character billboard positive baseline:

`one solitary character, exactly one person, centered full body, standing, camera-facing game sprite, clean silhouette, plain studio backdrop, even lighting, no cast shadow, no environment, retro fantasy CRPG style`

Character billboard negative baseline:

`character sheet, turnaround, model sheet, design sheet, reference sheet, multiple views, triptych, collage, lineup, duplicate person, second character, mirrored character, twin, extra body, extra limbs, extra head, floating prop, flames, aura, magic trail, border, frame, caption, logo, watermark, cast shadow, environment, room, background scene`

Tileable material positive baseline:

`seamless tileable material swatch, albedo only, flat diffuse color, orthographic, evenly lit, no cast shadows, no objects, no baked lighting, retro fantasy dungeon material`

## License policy

- Preserve model/tool/license notes on every generated asset record.
- Mark anything uncertain as `NeedsReview` or `Unknown`.
- Mark forbidden assets explicitly; runtime queries should exclude them.
- Do not train on or rely on proprietary ripped assets for a commercial build.

## Runtime policy

- Runtime never calls local ML tools or online APIs.
- Runtime resolves approved imported assets only.
- Runtime should prefer stable ids or key fields in save data.

## Editor workflow

1. Open `Ember > Generated Assets > Library`.
2. Create or select a `GeneratedAssetDatabase`.
3. Add/edit records and inspect the generated stable id.
4. Validate duplicate ids, missing review state, and malformed asset paths.
5. Export/import the JSON manifest for external tooling and review.

## Designed external pipeline

This foundation is designed to support an editor-only pipeline later:

1. Deterministic prompt builder
2. SDXL/Forge output
3. Alpha matte / largest-component crop for sprites
4. Tileable texture validation and URP material generation
5. Optional external mesh generation/import
6. Human approval before runtime selection
