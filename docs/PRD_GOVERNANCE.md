# Ember — PRD Governance (which PRD do I obey?)

> Audit item EMB-048. Three PRD locations exist and confused agents about which is authoritative.
> This page is the decision tree. Read it before acting on any PRD.

## The decision tree
1. **Building a Unity feature right now?** → obey `docs/prds/PRD_*_v1.md` (the active Unity PRDs).
   Today that's `PRD_visual_architecture_3d_billboard_v1.md` and `PRD_overland_map_v1.md`.
2. **Need the design intent / "what should this feel like" for a frontend or system?** → read the
   specific `Reference/PRDs/PRD_*_v1.md`. These are the **Godot-era design-intent reference**.
   **Translate to the Unity 3D-billboard reality — do not copy literally** (the old PRDs assume a
   Godot 2D client; our game is Unity 3D-billboard).
3. **Need the index / cross-reference of every PRD?** → `docs/reference/PRD_IMPLEMENTATION_MATRIX.md`
   (the full 115-line matrix; it is the single index).

## The three locations (roles)
| Location | Role | Status |
|---|---|---|
| `docs/prds/` | **Active Unity PRDs** you build against (+ a few audit/mission notes) | CANONICAL for implementation |
| `Reference/PRDs/` (97 files) | **Godot-era design-intent reference** — referenced by `docs/EMBER_VISION_BIBLE.md` and code (`EmberHud.cs`) | CANONICAL reference location |
| `docs/reference/prd/` (removed) | Was a near-exact **mirror** of `Reference/PRDs/`; reconciled away under EMB-050 | REMOVED — use `Reference/PRDs/` and the index at `docs/reference/PRD_IMPLEMENTATION_MATRIX.md` |

## The single matrix
- Canonical: **`docs/reference/PRD_IMPLEMENTATION_MATRIX.md`** (115 lines, the real index).
- The 33-line `PRD_IMPLEMENTATION_MATRIX.md` stub under `Reference/PRDs/` is partial —
  treat it as a pointer to the canonical matrix above.

## Rules for any agent
- A frontend task obeys the matching `Reference/PRDs/PRD_frontend_*_v1.md` design intent, rendered
  in Unity per `docs/prds/PRD_visual_architecture_3d_billboard_v1.md`.
- Do **not** recreate the removed `docs/reference/prd/` mirror; author reference PRDs under `Reference/PRDs/`.
- When a `Reference/PRDs/` PRD is implemented in Unity, record it in the canonical matrix with an
  `active / reference / deprecated` tag rather than forking another copy.
- Physical consolidation (removing the deprecated mirror, archiving stale reports/sprint docs) is a
  separate, reviewed cleanup — see `docs/AUDIT_COUNTER.md` EMB-050 / EMB-022. Until then, this
  governance page is the authority on precedence.
