# Ember — Current State

_Last updated: 2026-05-31_

This file is the short truth snapshot. For full backlog details use
`docs/REMEDIATION_V2_COUNTER.md`.

## Runtime/Proof mode labels

- `source-only`: no LFS runtime bytes required; static/code validation only.
- `LFS-runtime`: LFS bytes resolved; runtime model/art/plugin checks allowed.
- `Unity PlayMode`: scene behavior verification.
- `manual screenshot`: visual evidence captured by a human run.
- `historical`: old evidence kept for traceability, not current-proof alone.

`LFS-runtime` byte checks and Unity/player behavior checks are separate claims;
name both labels only when both have current evidence.

## Verified now (source-only)

- `tools/validation/static-audit.sh` is the structural hygiene gate.
- `tools/validation/run-validation.sh --mode fallback` validates pure C#
  logic/tests only.
- Build/test jobs in `.github/workflows/unity-test.yml` are clearly labelled as
  `lfs:false` source-only unless runtime-LFS is explicitly requested.

## Verified only in LFS-runtime/Unity sessions

- Real local LLM replies (non-fallback)
- ONNX/forge generation with real model bytes
- Runtime visuals/screenshots as player-proof
- Runtime-complete player artifact quality

## Open audit themes (see `docs/Audit.md` + remediation tracker)

- Docs truth/stale-reference cleanup
- Runtime-vs-source proof boundaries
- Save/load characterization hardening
- Actor identity migration from display-name paths
- Full 13-scene playable tour proof
- Architecture tail splits without behavior change
