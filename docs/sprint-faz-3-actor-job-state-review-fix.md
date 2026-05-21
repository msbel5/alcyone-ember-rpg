# Faz 3 — Actor job state review fix

_Date:_ 2026-05-14
_Branch:_ `agent/sprint-faz-3-actor-job-state`
_Box:_ `[box=LIVING]`
_Thalamus packet:_ `pkt_20260514153737_9a7a89bb1438`
_Resolver:_ `sha256:c00419f71c01af1c943ff196a5d9950b0009d0c1db3c325496d64411b1fa8604`
_PR:_ <https://github.com/msbel5/alcyone-ember-rpg/pull/109>
_Bot review:_ `chatgpt-codex-connector[bot]` P2 duplicate preference validation comment

## Increment goal

Address the PR #109 P2 review finding: `ActorRecord.ApplyJobPreferences` must
validate a replacement preference set before clearing the actor's existing valid
job preferences.

## Files changed

- `Assets/Scripts/Domain/Actors/ActorRecord.cs` — builds and duplicate-checks a
  replacement list before swapping it into `_jobPreferences`.
- `Assets/Tests/EditMode/Actors/ActorRecordTests.cs` — pins that a failed
  duplicate replacement preserves the actor's prior valid preference rows.
- `DOCS/sprint-faz-3-actor-job-state-review-fix.md` — records this review
  follow-up evidence.

## Validation

- `git diff --check` — PASS.
- `./tools/validation/run-validation.sh --mode fallback` — PASS (`854` passed, `0` failed, `0` skipped). Unity editor remains blocked locally by `unity_editor=BLOCKED reason=not_found`, so the fallback harness is the local evidence gate.
- Fallback TRX: `validation-output/fallback-test-results/fallback.trx`.

## Atom-map accounting

No new Faz 3 atom is checked off here. This is a review-fix follow-up for the
already-open `actor-job-state` bundle.

## Next increment

After PR #109 is green and review feedback is addressed, continue with the
`assignment-system` bundle.
