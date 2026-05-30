# Ember — Repo Hygiene Policy

> Audit items EMB-021 / 023 / 024 / 025 / 031 / 032 / 050 / 053 / 055 / 059. One place that says what
> is tracked source vs cache vs reference vs archive, so nobody mistakes generated/sample/report
> clutter for canonical content. Enforced where possible by `tools/validation/static-audit.sh`.

## Generated assets (EMB-021)
- Runtime image generation is a **per-playthrough cache on the player's machine**, not tracked source.
- `GeneratedAssets/` (root) and `Assets/Generated/Core/*.png|*.json` are **gitignored**. The
  `Assets/Generated/Core/` folder + its `.meta` stay tracked as a stable output location.
- Only seed manifests + curated authored art are tracked. Generation must surface failures (EMB-042),
  never silently ship a placeholder as canonical.

## Build delivery (EMB-053)
- **Decision: ship code-only + on-demand model download.** The repo does not commit the multi-GB ONNX
  model bytes as the distribution path; the player downloads models on first run
  (`ModelManifest` + `ModelBootstrap`, see `docs/AI_STACK.md`). cuDNN is gitignored. This matches
  Ember's bet: ship code, generate/download assets locally. (LFS holds the dev-convenience copies, but
  the *shipping* contract is code-only + downloader.)

## Samples (EMB-024)
- `Assets/TextMesh Pro/Examples & Extras/` (~284 files) is package sample clutter. **To be removed via
  the Unity Package Manager / Editor after a reference scan** (a few example `.mat` were touched by the
  URP upgrade; confirm nothing in active scenes references the Examples GUIDs before deletion). Tracked
  here as a pending Editor-verified cleanup, not a blind `rm`.

## Resources (EMB-025)
- `Assets/Resources/` hides dependencies and loads globally. Keep only tiny truly-global runtime assets
  (the two fonts, theme tokens). Larger/optional assets should move to explicit serialized references.
  Policy recorded; the font/theme migration is a later refactor (needs Editor + build).

## Prefabs vs scene recipes (EMB-032 / 055)
- Scenes are currently authored via `Assets/Editor/Ember/SceneRecipes/**` (recipe-generated) rather
  than reusable prefabs. **Policy: recipes own scene structure; do NOT mass-convert to prefabs blindly.**
  Introduce a prefab only after auditing the affected scene's GUID references, and never mix
  hand-edited scene objects with recipe regeneration without recording which owns what.

## Root scenes (EMB-031)
- `Assets/Scenes/CombatPlayground.unity` + `Sprint4Foundation.unity` are non-build root scenes (the
  duplicate-GUID hazard was fixed in EMB-001). **To be archived or deleted via the Editor** after a
  final reference scan — pending Editor pass.

## Planning docs & stale assets (EMB-023)
- Planning markdown does not belong under `Assets/` (Unity imports it). `Assets/Plans/` was moved to
  `docs/archive/plans/`. `Assets/pold/NavMesh.asset` was unreferenced stale and removed.

## Reports & sprint docs (EMB-050)
- 102 tracked `Reports/**` + 156 `docs/sprint-*` files clutter the active tree. **To be archived under
  `docs/archive/`** as a dedicated reviewed move (large diff; preserve `git mv` history; fix any active
  links). Pending — see AUDIT_COUNTER EMB-050.

## Local agent tooling (EMB-059)
- `.claude/skills/**` (65 files) are **intentionally-shared project dev tooling** (Claude Code skills
  that help any agent working this repo). Classified as dev tooling, **kept tracked on purpose** — they
  are not game source and are not bundled into the build.
