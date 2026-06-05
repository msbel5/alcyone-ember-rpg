# Generated Asset Pipeline — Sprites

## Scope

This pipeline is editor-only. The shipping game never runs Forge, Python, matte tools, or any external AI command at runtime.

## Why SDXL keeps making sheets and duplicates

SDXL is heavily trained on concept art sheets, turnarounds, reference boards, and lineups. Wide canvases plus vague prompts like `sprite`, `character design`, or `dice` often produce multiple figures, mirrored bodies, or stray props.

The corrective pattern is:

- positive prompt: `one solitary character, exactly one person, centered full-body, standing, front-facing game sprite, plain studio backdrop`
- negative prompt: `character sheet, turnaround, model sheet, design sheet, multiple views, lineup, duplicate, twin, extra limbs, floating prop`
- portrait-ish resolution for characters instead of wide sheet-like canvases

## Editor workflow

1. Open `Ember > Generated Assets > Library`.
2. Select a `GeneratedAssetDatabase`, a `GeneratedAssetPromptPreset`, and a `GeneratedAssetPipelineSettings` asset.
3. Select a record and click `Build Job`.
4. Click `Dry Run Forge` to export `job.json` and command preview without running external tools.
5. Optionally run the configured Forge command.
6. Optionally dry-run or run the configured matte command.
7. Import an existing generated PNG. The pipeline:
   - copies the raw PNG into the deterministic job folder,
   - analyzes alpha coverage,
   - crops the largest connected alpha component,
   - imports the cropped PNG with sprite settings,
   - optionally creates a billboard prefab,
   - updates the `GeneratedAssetDatabase` record.

## Alpha cleanup

The built-in cleanup pass keeps the largest connected alpha component and warns on:

- multiple large disconnected components,
- the main component touching the image edge,
- a component that is too wide for a typical humanoid billboard.

This is a deterministic cleanup pass, not a matte model. For better cutouts, use an external matte tool such as BiRefNet or rembg, then re-import the alpha PNG.

## Import settings

The import utility reuses Ember's existing sprite category conventions:

- character, creature, foliage, and prop billboards land under `Assets/Art/Characters/Generated`
- item billboards land under `Assets/Art/Items/Generated`
- the same `TextureImporter` conventions are then applied with explicit settings overrides for PPU, filter mode, mipmaps, compression, and max size

## Runtime rule

- Runtime resolves approved imported sprites and prefabs by stable id or query.
- Runtime never executes Forge or matte tooling.
- Save data should store stable ids or generated-asset keys, not raw output file paths.

## License rule

- Every record should keep model/tool/license notes.
- Imported sprites default back to `NeedsReview`.
- Only approved records should be used in the shipping build.
