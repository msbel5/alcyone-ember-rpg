# Sprint Faz 1 — Acceptance replay review fix

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-acceptance-proof`
_Box:_ `[box=PLAYABLE]`
_Atom-map:_ `docs/sprint-faz-1-atom-map.md`
_Thalamus:_ `pkt_20260511202241_663cef0a2776` / `sha256:58a4dbf582206903d8cdf60c6b19b0e4d29df978b1807535ed57ab3ac7c7b21c`
_PR:_ https://github.com/msbel5/alcyone-ember-rpg/pull/96

## Increment goal

Address Copilot review on PR #96 without widening the sprint: make the Faz 1
acceptance replay use the same gameplay traversal path as the slice and record
the player as the actor entering the second site.

## Files changed

- `Assets/Tests/EditMode/World/Faz1AcceptanceReplayTests.cs` — chooses the
  guarded door, grants clearance through `GuardInteractionService`, opens it
  with `DoorInteractionService`, traverses through `DungeonTraversalService`,
  and records `loaded.Player.Id` on the `SiteEntered` event.
- `docs/sprint-faz-1-acceptance.md` — corrects the Unity API wording and
  documents the service-backed traversal proof.
- `docs/sprint-faz-1-acceptance-review-fix.md` — this summary.

## Validation

- `git diff --check`: PASS (no output).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `Passed!  - Failed:     0, Passed:   759, Skipped:     0, Total:   759`; `fallback_exit_code=0`; log `validation-output/validation-20260511T202552Z.log`.

## Bot review disposition

- Copilot comment about `without Unity-only APIs`: fixed by rephrasing to
  `without UnityEditor-only APIs` / fallback harness language.
- Copilot comment about direct room mutation: fixed by routing room movement
  through `DungeonTraversalService.Traverse` after guard clearance and door open.
- Copilot comment about `ActorId(0)`: fixed by using `loaded.Player.Id` for the
  `WorldEventKind.SiteEntered` payload.
- GitHub Actions screenshot comment is informational only; no code change needed.

## Next increment

After PR #96 updates, keep it open until GitHub checks and bot review status are
green, then merge/delete the branch if all EMSPR merge gates are still true.
