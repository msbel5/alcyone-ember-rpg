# Faz 3 — Actor job state bundle

_Date:_ 2026-05-14
_Branch:_ `agent/sprint-faz-3-actor-job-state`
_Box:_ `[box=LIVING]`
_Thalamus packet:_ `pkt_20260514134756_41cc296d5f90`
_Resolver:_ `sha256:c10a7e638a0f851f6cc6c1fa540b705435d62446086aeec222c6398651506e04`

## Increment goal

Land the Faz 3 `actor-job-state` bundle as a pure Domain increment: actor-local
job preference rows, an idle/assigned schedule snapshot, and `ActorRecord`
integration methods that expose job state without selecting, ticking, saving, or
logging jobs yet.

## Files changed

- `Assets/Scripts/Domain/Actors/ActorJobPreference.cs` — adds a concrete job kind + actor-local priority row with an explicit disabled/opt-out state.
- `Assets/Scripts/Domain/Actors/ActorScheduleState.cs` — adds idle and assigned schedule snapshots carrying current job id, target site, and worksite position.
- `Assets/Scripts/Domain/Actors/ActorRecord.cs` — stores deterministic job preference rows and current schedule state without changing actor identity.
- `Assets/Tests/EditMode/Actors/ActorJobPreferenceTests.cs` — pins preference storage, disabled rows, equality, invalid `JobKind.None`, and debug labels.
- `Assets/Tests/EditMode/Actors/ActorScheduleStateTests.cs` — pins idle default, assigned fields, empty job/site rejection, equality, and debug labels.
- `Assets/Tests/EditMode/Actors/ActorRecordTests.cs` — pins job preference replacement, duplicate-kind rejection, and schedule replacement.
- `DOCS/sprint-faz-3-atom-map.md` — marks the actor-job-state atoms landed and moves next increment guidance to assignment-system.

## Validation

- `git diff --check` — PASS.
- `./tools/validation/run-validation.sh --mode fallback` — PASS (`853` passed, `0` failed, `0` skipped). Unity editor remains blocked locally by `unity_editor=BLOCKED reason=not_found`, so the fallback harness is the local evidence gate.
- Fallback TRX: `validation-output/fallback-test-results/fallback.trx`.

## Scope deliberately excluded

- No actor/job matching or `JobAssignmentSystem` logic.
- No recipe start/tick/complete integration.
- No EventLog kinds or player-facing log output.
- No save/load mapping.

## Next increment

Continue with the `assignment-system` bundle: match available actors to eligible
`JobBoard` entries by actor preference priority, then claim the selected job in a
stable order. Keep event kinds, competition fixtures, and save/load mapping out
until their concrete consumer atoms land.
