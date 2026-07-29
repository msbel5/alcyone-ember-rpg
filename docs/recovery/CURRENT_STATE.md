# Ember Recovery Current State

Recorded: 2026-07-27 (Europe/Istanbul)

## Authority and scope

- Completed work item: `PRD-00_BASELINE_AUTHORITY_AND_SCOPE_LOCK.md`.
- Live recovery progression, latest targeted proof, and the exact next PRD are
  recorded only in `IMPLEMENTATION_STATUS.md`; this file remains the PRD-00
  baseline snapshot and does not duplicate the moving counter.
- Current source at the recorded baseline overrides Atlas, commit messages,
  historical reports, and old repositories.
- No gameplay, simulation, save, UI, AI, or architecture behavior is changed
  or certified by PRD-00.

## Repository baseline

| Item | Recorded state |
|---|---|
| Repository | `msbel5/alcyone-ember-rpg` |
| Baseline SHA | `f301feb0e8538c39fe17f4ca80b34d64e8a4dacb` |
| Working branch | `main` by explicit user override on 2026-07-27 |
| Authorized repository change | `.gitignore`; included so `_codex_handoff/` remains local-only |
| Local-only recovery input | `_codex_handoff/`; excluded from commit scope |
| LFS status | No staged or unstaged LFS objects |
| LFS checkout | 913 tracked objects resolved; 0 pointer-only objects |

## Proof state

| Lane | Current claim |
|---|---|
| Source/static | PASS: README/Atlas checks pass and the authorized metadata index cleanup passes the post-fix static gate. Artifact: `Reports/recovery/PRD-00/full/static-audit-post-baseline-fix-valid.log`. |
| Pure C# / Unity EditMode | See `IMPLEMENTATION_STATUS.md` for current per-PRD targeted source-test proof; this PRD-00 snapshot makes no later runtime claim. |
| PlayMode / built player / visual | Not run and not claimed. |
| Historical logs | `build-w38.log` and `build-w38b.log` are historical evidence only, never current runtime proof. |

See `Reports/recovery/PRD-00/FINAL_REPORT.md` for the completed validation
record and exact artifact paths.
