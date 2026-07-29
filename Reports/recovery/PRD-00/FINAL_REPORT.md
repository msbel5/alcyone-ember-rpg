# PRD-00 Final Report — PASS

## Scope completed

- Starting HEAD SHA: `f301feb0e8538c39fe17f4ca80b34d64e8a4dacb`.
- Ending HEAD SHA: `f301feb0e8538c39fe17f4ca80b34d64e8a4dacb`.
  The values are intentionally identical because PRD-00 remains uncommitted
  while the continuous recovery run advances; the ending SHA describes HEAD,
  not an empty working tree.
- Active scope remained `PRD-00_BASELINE_AUTHORITY_AND_SCOPE_LOCK.md`.
- README canonical references now target existing recovery documents.
- Atlas is labeled explanatory/historical; its dangling `RUH_TESHIS` link is
  removed and method/callsite-only rows are no longer presented as shipped.
- The two tracked metadata entries whose asset paths are ignored were removed
  from Git tracking. Their local metadata files and ignored asset directories
  remain present.
- No gameplay, simulation, save, UI, AI, asset, or architecture code changed.

## Exact changed-file inventory

Repository changes:

- `.gitignore` — includes `_codex_handoff/`; the handoff directory itself
  remains excluded.
- `README.md`
- `docs/atlas/BUG_REPORT_SCORECARD.md`
- `docs/atlas/INDEX.md`
- `tools/validation/static-audit.sh`
- `Assets/TextMesh Pro/Examples & Extras.meta` — index deletion only; local
  file retained.
- `Assets/pold.meta` — index deletion only; local file retained.

Recovery documents:

- `docs/recovery/CURRENT_STATE.md`
- `docs/recovery/DEFERRED_FINDINGS.md`
- `docs/recovery/IMPLEMENTATION_STATUS.md`
- `docs/recovery/PRD_GOVERNANCE.md`
- `docs/recovery/RECOVERY_COUNTER.md`
- `docs/recovery/RECOVERY_STATUS.md`

Evidence artifacts:

- `Reports/recovery/PRD-00/baseline/static-audit-before.log`
- `Reports/recovery/PRD-00/red/readme-links-red.log`
- `Reports/recovery/PRD-00/targeted/readme-links.log`
- `Reports/recovery/PRD-00/targeted/atlas-authority.log`
- `Reports/recovery/PRD-00/full/static-audit.log`
- `Reports/recovery/PRD-00/full/static-audit-blocker-check.log`
- `Reports/recovery/PRD-00/full/static-audit-post-baseline-fix-valid.log`
- `Reports/recovery/PRD-00/FINAL_REPORT.md`

## Old path removed

The dangling README authority set (`docs/CURRENT_STATE.md`,
`docs/REMEDIATION_V2_COUNTER.md`, `docs/EMBER_VISION_BIBLE.md`,
`docs/AI_STACK.md`, `docs/PRD_GOVERNANCE.md`, `docs/EMBER_GOAL.md`,
`docs/ROADMAP_V1.md`, and `docs/RELEASE_NOTES_v1.0.md`) was removed from
README authority. The Atlas `docs/RUH_TESHIS.md` link was removed.
The stale tracked entries for
`Assets/TextMesh Pro/Examples & Extras.meta` and `Assets/pold.meta` were
removed from the index without deleting either local file or its ignored asset
directory.

## Red test

Exact command:

`tools/validation/static-audit.sh --readme-links-only`

Result: **FAIL as expected before the fix**:
`FAIL: 8 README document reference(s) are dangling.`

Artifact: `Reports/recovery/PRD-00/red/readme-links-red.log`.

## Targeted validation

README authority command:

`tools/validation/static-audit.sh --readme-links-only`

Result: **PASS** — `static-audit PASS`.

Artifact: `Reports/recovery/PRD-00/targeted/readme-links.log`.

Atlas authority assertion batch:

```powershell
$index = Get-Content 'docs/atlas/INDEX.md' -Raw
$scorecard = Get-Content 'docs/atlas/BUG_REPORT_SCORECARD.md' -Raw
if ($index -notmatch 'Explanatory/Historical Snapshot') { throw 'Atlas authority label missing' }
if ($index -match 'RUH_TESHIS') { throw 'Dangling RUH_TESHIS reference remains' }
if ($scorecard -match '\|\s*SHIPPED(?:-NO-TEST)?\s*\|') { throw 'SHIPPED row status remains' }
if ($scorecard -notmatch 'method/callsite.*not CLOSED') { throw 'Non-closure notice missing' }
```

Result: **PASS** — all four assertions passed.

Artifact: `Reports/recovery/PRD-00/targeted/atlas-authority.log`.

## Full relevant validation

Initial full command:

`tools/validation/static-audit.sh`

Result: **FAIL** at the expected tracked/ignored metadata gate.

Artifact: `Reports/recovery/PRD-00/full/static-audit.log`.

The initial full gate found two tracked/ignored metadata entries:

- `Assets/TextMesh Pro/Examples & Extras.meta`
- `Assets/pold.meta`

The user explicitly authorized removing those entries from Git tracking.
Exact cleanup command:

`git rm --cached --ignore-unmatch -- "Assets/TextMesh Pro/Examples & Extras.meta" "Assets/pold.meta"`

The local metadata files and underlying ignored asset directories were verified
to remain present. The path/index verification is captured in
`Reports/recovery/PRD-00/full/static-audit-blocker-check.log`.

Exact post-fix command:

```powershell
& 'C:\Program Files\Git\bin\bash.exe' -c 'export PATH="/usr/bin:/mingw64/bin:$PATH"; ./tools/validation/static-audit.sh --quiet'
```

Result: **PASS**.

Post-fix artifact:
`Reports/recovery/PRD-00/full/static-audit-post-baseline-fix-valid.log`.

The earlier complete failure evidence remains in the two full-gate artifacts
above. `Reports/recovery/PRD-00/baseline/static-audit-before.log` is
**INVALID/INCOMPLETE**: it ends during section 2b and has no terminal result.
It is retained only as provenance and is superseded by the complete pre-fix
and post-fix artifacts; it supports no PASS or FAIL claim.

## Proof level

All evidence is source/static only. Pure C#, Unity EditMode, PlayMode, built
player, runtime, AI, and visual proof were not required, run, or claimed.

## Determinism and save evidence

No gameplay/domain/save source changed. The post-fix full static audit passed.

## Deferred findings and delivery

There are no unresolved PRD-00 deferred findings. The user-owned
`_codex_handoff/` ignore rule is preserved as a normal repository change.
PRD-00 is complete; the superseding execution directive authorizes PRD-01 as
the next recovery step.

No PRD-00 commit, push, or pull request was created in this phase.
