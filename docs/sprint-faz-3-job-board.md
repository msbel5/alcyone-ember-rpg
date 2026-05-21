# Faz 3 — Job board bundle

_Date:_ 2026-05-14
_Branch:_ `agent/sprint-faz-3-job-assignment-atom-map`
_PR:_ `https://github.com/msbel5/alcyone-ember-rpg/pull/108`
_Box:_ `[box=PROCESS]`
_Thalamus packet:_ `pkt_20260514131419_488b3d60813e`
_Resolver:_ `sha256:c22aa16c072fbe3befa904f89a2e27c50b595ccbb1f3e700d596b61c6427aa5a`

## Increment goal

Land the Faz 3 `job-board` bundle as a pure Domain increment: a validated
immutable `JobRequest`, a deterministic `JobBoard` pending/claimed lifecycle,
and focused fallback EditMode tests for ordering and duplicate-claim guards.

## Files changed

- `Assets/Scripts/Domain/Process/JobRequest.cs` — validates recipe/site/worksite/kind/priority/quantity/requester fields for a pending work request.
- `Assets/Scripts/Domain/Process/JobBoard.cs` — adds deterministic add, lookup, priority/insertion-order peek, actor claim, complete/cancel, and clear semantics.
- `Assets/Tests/EditMode/Process/JobRequestTests.cs` — pins constructor storage and invalid sentinel/quantity/priority rejection.
- `Assets/Tests/EditMode/Process/JobBoardTests.cs` — pins add/get/peek/claim/terminal removal/clear behaviour and duplicate guards.
- `DOCS/sprint-faz-3-atom-map.md` — marks the job-board atoms landed and moves next increment guidance to actor job state.

## Validation

- `git diff --check` — PASS.
- `./tools/validation/run-validation.sh --mode fallback` — PASS (`840` passed, `0` failed, `0` skipped). Unity editor remains blocked locally by `unity_editor=BLOCKED reason=not_found`, so the fallback harness is the local evidence gate.
- Fallback TRX: `validation-output/fallback-test-results/fallback.trx`.

## Bot-review queue

Oldest actionable PR #108 Codex comment (`discussion_r3241364567`) was already
addressed by commit `fe99986`, which corrected Faz 3 save/load atom paths from
`Assets/Scripts/Domain/Save/...` to `Assets/Scripts/Data/Save/...` and received
an inline reply with validation evidence. The remaining GitHub Actions entries
are informational PlayMode screenshot artifacts.

## Next increment

Continue with the `actor-job-state` bundle: `ActorJobPreference`,
`ActorScheduleState`, ActorRecord integration methods, and focused tests before
starting assignment-system or save/load atoms.
