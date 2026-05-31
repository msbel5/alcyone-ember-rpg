# Ember — Repo Hygiene Policy

## 1) Source vs runtime artifacts

- Source of truth is tracked code/docs/assets required for clean clone
  determinism.
- Runtime-heavy binaries/models/art may be LFS-backed and unresolved in
  source-only checkouts.
- Runtime-generated images are cache outputs, not canonical authored source.

## 2) Generated outputs

- Keep `Assets/Generated/Core/` folder tracked for path stability.
- Do not commit regenerated per-run images unless explicitly requested for
  curated fixtures.
- Runtime failure logs under `Logs/` are append-only runtime evidence.

## 3) Proof folders

- `Reports/` is active evidence storage in this repository.
- Each proof artifact must carry a mode label:
  `source-only`, `LFS-runtime`, `Unity PlayMode`, `manual screenshot`, or
  `historical`.

## 4) Validation contract

- Source-only gate:
  `bash tools/validation/static-audit.sh`
- Runtime plugin/model gate:
  `bash tools/validation/static-audit.sh --require-runtime`
- Runtime visual gate:
  `bash tools/validation/static-audit.sh --require-runtime --require-runtime-visual`

Never treat source-only green as runtime completeness.

## 5) Docs hygiene

- `docs/CURRENT_STATE.md` is the short truth snapshot.
- `docs/REMEDIATION_V2_COUNTER.md` is the long running remediation tracker.
- `docs/Audit.md` is historical audit input.
- `Reference/**` and `docs/reference/**` are reference/history, not active
  implementation requirements.

## 6) Guardrails

- No scene YAML mass edits by hand.
- No forced migration of old assets without `.meta` integrity checks.
- No fake runtime claims when LFS/runtime bytes are unresolved.
- `Resources.Load` is allowed only for small global fallbacks/defaults. New
  gameplay/runtime assets should use explicit references or registries; new
  `Assets/Resources/**` entries require owner + reason in the PR.
