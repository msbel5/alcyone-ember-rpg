# Sprint 3 - Job save round-trip

## Goal

Complete the Faz 3 save/load persistence atoms for job assignment without adding new named fields to `SliceWorldState`.

## Files changed

- `Assets/Scripts/Data/Save/SliceSaveData.cs`
- `Assets/Scripts/Data/Save/ActorSaveMapper.cs`
- `Assets/Scripts/Data/Save/SliceSaveMapper.cs`
- `Assets/Scripts/Data/Save/JsonSliceSaveService.cs`
- `Assets/Tests/EditMode/Save/JobAssignmentRoundTripTests.cs`
- `DOCS/sprint-faz-3-atom-map.md`
- `DOCS/sprint-3-job-save-roundtrip.md`

## Behaviour

- `SliceSaveData.jobs` now carries pending `JobBoard` rows, including the claimed actor id.
- `ActorSaveData` now carries actor job preferences and `ActorScheduleState` fields, so claimed jobs restore their actor target after JSON round-trip.
- `JsonSliceSaveService.Jobs` mirrors the existing worksite bridge until the full PROCESS store lands on a world root.
- `JobAssignmentRoundTripTests` proves a claimed smithing job, actor schedule, active recipe work order, and active furnace worksite survive save/load together.

## Product-visible count

Product-visible PR count for this increment: 0. This is a PROCESS persistence rail; Faz 3 already has visible EventLog increments, and the next increment should be the deterministic replay/playable proof.

Acceptance sentence: player can save after a smith claims a furnace job, reload, and still see the job board claim, actor schedule target, and active recipe progress available to continue.

## Validation

- `git diff --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at 2026-05-15T15:49Z (fallback harness: 878 passed, 0 failed, 0 skipped; Unity editor blocked: not found).

## Thalamus

- packet_id: `pkt_20260515154518_c972b44762f6`
- resolver_key: `sha256:72f06431d025d03b8909040d697419a8c01a9e4a2968166a38bb3f8b40c8350d`

## Next increment

Finish Faz 3 with `DOCS/sprint-faz-3-job-assignment-acceptance.md`: write the deterministic replay/playable proof and final `player can ...` sentence before promotion.
