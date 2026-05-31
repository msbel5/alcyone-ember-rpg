# PRD Implementation Matrix (Unity-normalized)

_Last updated: 2026-05-31_

This matrix exists to prevent old Godot/backend PRDs from being interpreted as
active Unity implementation requirements.

## Status legend

- `active`: current Unity implementation PRD
- `reference`: design-intent source only
- `historical`: retained for audit/history, not implementation

## Active Unity PRDs

| PRD | Status | Notes |
| --- | --- | --- |
| `docs/prds/PRD_visual_architecture_3d_billboard_v1.md` | `active` | Canonical visual/runtime architecture for Unity 3D + billboard stack |
| `docs/prds/PRD_overland_map_v1.md` | `active` | Canonical Unity overland/world navigation direction |

## Reference PRD families (not direct implementation)

| Location | Status | Use |
| --- | --- | --- |
| `Reference/PRDs/PRD_frontend_*` | `reference` | UI flow/tone ideas; re-interpret for Unity |
| `Reference/PRDs/PRD_character_*` | `reference` | Character concepts; do not copy text/data tables |
| `Reference/PRDs/PRD_world*` / `PRD_colony*` | `reference` | Simulation design intent and naming |
| `Reference/PRDs/PRD_*godot*` | `historical` | Explicitly old client architecture |
| `Reference/PRDs/PRD_*backend*` / API/runtime bindings | `historical` | Old backend pathing, not Unity runtime |

## Decision rule

1. Implement against `docs/prds/*` only.
2. Pull flavor/intent from `Reference/PRDs/*` when needed.
3. Record any new Unity PRD in `docs/prds/` and add it to this file as
   `active`.
