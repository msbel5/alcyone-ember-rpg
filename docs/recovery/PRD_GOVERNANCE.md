# Recovery PRD Governance

## Default one-PRD rule

Only the explicitly approved PRD is active. Work stops after its draft PR.
Later PRDs, new gameplay, new architecture, broad refactors, and documentation
restoration are out of scope unless separately approved.

## Session execution waiver — 2026-07-27

For the current recovery run, the user's 2026-07-27 execution directive
supersedes only the workflow mechanics in the default one-PRD rule:

- Work may continue directly on `main`.
- PRD-00 through PRD-10 may proceed continuously in numeric order after each
  PRD's targeted validation passes.
- A separate branch, commit, draft pull request, approval pause, and stop are
  not required after each PRD.
- Per-PRD full validation is replaced by one source/static and one full pure-C#
  pass after PRD-01 through PRD-08, followed by the final runtime/proof lanes
  required by PRD-09 and PRD-10 when the environment supports them.
- `_codex_handoff/` remains local-only and excluded; its `.gitignore` rule is
  an authorized repository change.

This waiver does not relax problem definitions, architecture targets,
acceptance criteria, authority ordering, evidence-layer honesty, determinism,
save compatibility, enum numeric compatibility, replacement-only cutover, or
the prohibition on parallel gameplay frameworks. Source-only evidence still
cannot be described as runtime or visual proof.

## Authority order

When evidence conflicts, use this order:

1. Current source at the recorded repository SHA.
2. Passing behavior evidence at the correct proof layer.
3. The active recovery PRD.
4. The recovery handoff/audit contract.
5. This repaired README and the current-state document.
6. Atlas documents as explanatory/historical maps.
7. Commit messages, old reports, old repositories, and conversations.

Method existence, grep output, a historical commit label, or an old build log
cannot close a finding.

## Proof language

| Evidence lane | Maximum supported claim |
|---|---|
| Source/static | Structure, references, and source constraints |
| Pure C# tests | Domain behavior covered by those tests |
| Unity EditMode | Unity integration and compilation exercised there |
| Unity PlayMode | Scene/runtime behavior exercised there |
| Windowed built player | Visible/player-facing behavior captured there |

Source-only green must never be presented as runtime, visual, AI, asset, or
build proof. LFS-required claims also require resolved LFS objects.

## Default change and closure rules

- Start with a red test that demonstrates the named problem.
- Make the smallest replacement-only change within the active PRD.
- Preserve deterministic ordering, seeded RNG, save compatibility, and enum
  numeric values whenever code is in scope.
- Record out-of-scope discoveries without fixing them.
- Run targeted validation, then one full relevant pass.
- Produce a final evidence report, request independent review, open a draft PR,
  never auto-merge, and stop.

The dated session waiver changes the cadence of red artifacts, full passes,
branches, commits, pull requests, and pauses only; all architectural and
evidence rules above remain in force.
