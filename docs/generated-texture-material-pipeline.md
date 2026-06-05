# Generated Texture + Material Pipeline

## Goal

Import tileable albedo-first textures and build reviewable URP materials without calling AI tools at runtime.

## Why wall/floor outputs go wrong

Image models happily follow scene language like `tavern wall with hearth` by baking a fireplace glow, perspective, or props into the texture. When tiled, that glow repeats across every wall.

The corrective prompt pattern is:

- `seamless tileable wall material swatch`
- `albedo only`
- `fronto-parallel orthographic`
- `flat diffuse color`
- `evenly lit`
- `no objects`
- `no scene`
- `no baked lighting`

and negatives such as:

- fireplace, hearth, torch, candle
- room, corridor, perspective, horizon
- shadow, glow, reflection, highlight, vignette
- furniture, prop, character, doorway, text, logo

## Editor workflow

1. Use a `TileableWall`, `TileableFloor`, `TileableCeiling`, or `MaterialSet` record in the Generated Asset Library.
2. Build the deterministic prompt/job if you want a Forge job export.
3. Import an existing albedo PNG.
4. Run `Validate Albedo` to inspect edge mismatch, warm glow blobs, and strong lighting gradients.
5. Optionally dry-run or run the configured de-light and PBR tool commands.
6. Import de-lit / normal / AO maps if you have them.
7. Click `Generate Material` to build or update a URP Lit material asset.

## Validation heuristics

The built-in validator is intentionally simple and deterministic:

- compares left/right and top/bottom edges,
- warns on large edge mismatch,
- warns on large warm bright blobs,
- warns on strong side-to-side lighting gradients.

It is a review aid, not a vision model.

## URP material rules

- Base map uses the de-lit albedo when available, otherwise raw albedo.
- Normal map binds to `_BumpMap` when provided.
- AO binds to `_OcclusionMap` when provided.
- URP Lit has no direct roughness texture slot, so roughness still needs manual packing or a later mask-map pass.

## Runtime rule

- Runtime only consumes approved imported textures and material assets.
- No external tool or AI call happens in build/runtime.
