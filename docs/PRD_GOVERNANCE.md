# Ember — PRD Governance

Use this precedence when instructions conflict.

## 1) Active Unity implementation PRDs (canonical)

Located in `docs/prds/`:

- `PRD_visual_architecture_3d_billboard_v1.md`
- `PRD_overland_map_v1.md`

These are the only active implementation PRDs in this repository today.

## 2) Reference-only PRDs (design intent, not direct implementation)

- `Reference/PRDs/**`
- legacy index material under `docs/reference/**`

Use these for tone/pattern extraction only. Do not copy text/code/assets and do
not treat old backend/Godot runtime paths as active Unity tasks.

## 3) PRD index

- `docs/reference/PRD_IMPLEMENTATION_MATRIX.md` is the normalized index with
  `active` vs `reference` labels.

## Rules

- If a task is Unity implementation work, obey only section (1) directly.
- If a prompt references old Godot/backend PRDs, reinterpret against Unity
  architecture and current scene/runtime contracts.
- If uncertainty remains, prefer `docs/CURRENT_STATE.md` + remediation tracker
  over historical PRD text.
