# PRD-10 Final Report — source and targeted runtime PASS, full lane PARTIAL

## Result

- Base HEAD: `f301feb0e8538c39fe17f4ca80b34d64e8a4dacb`.
- Exact 1/7/30-day replays now compare world digest, authoritative action trace,
  and retained event identity for the same seed and input.
- The one-day lane rejects more than one autonomous cell step per actor/tick and
  movement progress without movement.
- The soak checks bounded event/action logs, contiguous event sequence, a 4 MiB
  serialized-save budget, nonnegative stock, bounded needs, tagged carried matter,
  live reservation owners, and terminal claim cleanup.
- Mid-action save/load compares digest, action trace, and event trace on every
  post-load tick until the saved action reaches a terminal boundary.
- Five coherent, actor-owned action episodes are emitted from actual deterministic
  simulation transitions.

## Old paths removed

- A killed or maul-clamped actor can no longer discard food/crop or retain its
  reservation when interrupted in a terminal handover phase.
- An unreachable crime report now records `report_closed:unreachable`; it cannot
  re-arm the same fact in an endless fail/retry loop.
- `StrikeQuarry` cooldown ticks retain exact state instead of inventing progress.
- `WorldStateDigest` now includes the action-driving NPC memory, companion,
  guard-pursuit, hunt-target, critter, rumor/cursor, and site-unrest roots.

## Targeted proof

- `RecoveryDeterminismSoakTests`: **PASS**, 5/5.
  Evidence: `Reports/recovery/PRD-10/targeted-soak.log`.
- Release-blocker fixture set: **PASS**, 32/32.
  Evidence: `Reports/recovery/PRD-10/targeted-release-blockers.log`.
- Final selected recovery gate: **PASS**, 120/120.
  Evidence: `Reports/recovery/PRD-10/final-selected-gate.log`.
- Final strict source/static runtime-asset audit: **PASS**.
  Evidence: `Reports/recovery/PRD-10/final-static-audit.log`.
- Five simulation stories: **PASS**.
  Evidence: `Reports/recovery/PRD-10/actor-stories.txt`.

## Full/final proof status

- Pure-C# full fallback suite: **DEFERRED**. The single allowed attempt reached
  its 300-second budget. The current-diff movement performance regression exposed
  before timeout was fixed, and its focused gate plus the final selected gate pass.
- Targeted Unity EditMode determinism soak: **PASS**, 5/5.
- Targeted Unity PlayMode action projection/story pack: **PASS**, 8/8.
- Windows64 player build: **PASS**, `14,159,578,133`-byte reported total payload.
- Built-player shipcheck: **PASS**, 9/9 sections with zero logged exceptions.
- Built-player visual tour: **COMPLETE**, with zero scanned runtime exceptions.
- Full EditMode/Forge CUDA lane: **PARTIAL**. The recovery fixtures pass, but the
  Forge smoke crashes Unity inside `cudnn64_9.dll`; it is not reported as PASS.
- Built-player action-transition JSONL: **DEFERRED**. PlayMode owns the current
  runtime action-story proof.

Detailed runtime evidence and the player-facing review are in
`Reports/recovery/PRD-10/RUNTIME_VALIDATION_REPORT.md`.

## Remaining debt

- Forge's on-demand CUDA/cuDNN path needs isolation from the Unity process or a
  compatible provider stack before the full EditMode lane can pass.
- Manual keyboard/mouse play remains unproven because Computer Use was
  unavailable; the built-player self-play was harness-driven.
- Player action-transition JSONL evidence remains to be captured.
- World/dungeon presentation and camera clipping remain substantial polish debt.
- The line-oriented mutation inventory is intentionally cheaper than a Roslyn gate.
- Report dedup is bounded by the 64-entry actor-memory window.
- Reservation TTL still estimates direct distance rather than exact detour length;
  terminal recovery prevents matter loss if it expires.
