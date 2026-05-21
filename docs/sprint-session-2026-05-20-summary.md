# Sprint session — 2026-05-20 night → morning (Mami workstation)

## Goal

Mami went to sleep at ~05:00 GMT+3 with a directive to push the backend forward
to Faz 4-12 as far as possible by 10:00 GMT+3, using the Codex CLI worker after
06:00 quota reset and SSH access to the Alcyone Pi for Captain status. Plan
file at `~/.claude/plans/alcyone-beceremedi-ruhunu-anladin-shimmering-moler.md`.

This file records what landed in the five-hour window.

## Merged to `main` during this session

| PR | Title | Box | Effect |
|---:|---|---|---|
| #133 | Faz 4: Colony needs acceptance replay | LIVING | rail-6 acceptance proof |
| #134 | (closed) Faz 4: DRY refactor JobAssignmentSystem | PROCESS | superseded by #128 / #131 |
| #135 | (closed) Sprint 4: IPathfinder C# 9 compatible | PROCESS | ported into #136 |
| #136 | Faz 5: Season calendar foundation | TIME / PROCESS | Faz 5 atoms 1-11 (Captain large bundle); fixed `IPathfinderTests` defensive-copy + `JobKind` symbol drift during merge |
| #137 | CO-05: JobStatus lifecycle value object + JobBoard.GetStatus | PROCESS | CO-05 closed |
| #138 | CO-04: JobBoard.GetQueueIndex deterministic claim order | PROCESS | CO-04 closed |
| #139 | docs(faz-4): promotion summary + Debt ledger CO-04/CO-05 closed | meta | Faz 4 promoted |
| #140 | docs(faz-6-12): atom maps for Trade/Combat/Magic/Dialogue/DM/Visual/LLM | meta | 7 new atom maps |
| #141 | CO-02 + CO-03: GridPathfinder concrete A* + PathfindingSystem | PROCESS | CO-02 + CO-03 closed; new `ActorStepped` event |
| #142 | Faz 11 Atoms 1-3: visual snapshot rows (job/needs/season) | Presentation | snapshot rows landed |
| #143 | Faz 6 Atoms 1-3: faction primitives | SOCIETY | FactionRelationKind + FactionReputation + FactionStore.WithReputation/GetReputation |
| #144 | CO-06: WorksiteSlot value type + FromWorksite factory | PROCESS | CO-06 closed |
| #145 | CO-07: ProductionRecipeRegistry — BakeBread + SmeltIronIngot | PROCESS / MATTER | CO-07 closed |
| #146 | Faz 10 Atoms 1-4: tool-calling primitives | AI/DM | NPC/party/DM tool surface scaffold |

## Still in flight at session end

| PR | Title | Status |
|---:|---|---|
| #147 | Faz 11 Atoms 4-5: faction relation + event log tail snapshots | open, CI running |
| #148 | Faz 6 Atom 4: FactionReputationSystem + FactionReputationChanged event | open, CI running |

## Debt ledger state after the session

| ID | Pre-session | Post-session | Notes |
|---|---|---|---|
| CO-01 | open | advanced | `IPathfinder` interface in `Assets/Scripts/Domain/World/IPathfinder.cs` (landed via #136 chain) |
| CO-02 | open | closed | `GridPathfinder` concrete deterministic A* (#141) |
| CO-03 | open | closed | `PathfindingSystem.Tick` + `ActorStepped` event (#141) |
| CO-04 | open | closed | `JobBoard.GetQueueIndex` per-worksite claim order (#138) |
| CO-05 | open | closed | `JobStatus` value object + `JobBoard.GetStatus` migration (#137) |
| CO-06 | open | closed | `WorksiteSlot` value type with `FromWorksite` factory (#144) |
| CO-07 | open | closed | `ProductionRecipeRegistry.BakeBread()` + `SmeltIronIngot()` (#145) |
| CO-08 | closed pre-session | closed | `NeedMoodEvaluator.memoryPressure` overload cleanup |
| CO-09 | deferred-to-faz-9 | deferred-to-faz-9 | Sprint 1 Narrative iskelet audit booked at Faz 9 kickoff |

## Faz status after the session

- **Faz 0** — done (audit + control plane in #125).
- **Faz 1** — done (Core stores).
- **Faz 2** — done (Recipe + Worksite).
- **Faz 3** — done (Job assignment + idle behaviour); all carry-over rows closed except CO-01 advanced.
- **Faz 4** — done (Colony Needs): rails 1-6 checked, acceptance replay green, promotion summary merged.
- **Faz 5** — foundation merged via #136 (atoms 1-11); plant/season/farming primitives live in `Assets/Scripts/Domain/Time/`, `/Process/`, and `/Simulation/Process/`. Atom 12 (acceptance replay) queued.
- **Faz 6** — atom map merged (#140); primitives merged (#143); reputation system pending merge (#148).
- **Faz 7** — atom map merged (#140); no implementation yet.
- **Faz 8** — atom map merged (#140); no implementation yet. Sprint 5 `SpellEffectCode` enum still present, awaits the promotion bundle.
- **Faz 9** — atom map merged (#140); CO-09 still deferred; no implementation yet.
- **Faz 10** — atom map merged (#140); primitives merged (#146).
- **Faz 11** — atom map merged (#140); Captain-side snapshot rows batch 1 merged (#142); batch 2 pending merge (#147); Mami-side scene atoms (8-16) await Mami's own sprint.
- **Faz 12** — atom map merged (#140); no implementation. Fence: LLM client wiring blocked until Faz 12.

## Test count

Local fallback validation continued to track each PR; CI Unity EditMode passed
through every merged PR.

## Out-of-scope (by design)

- Unity scenes, prefabs, sprites, materials, screenshots — Mami territory per
  `agent-rules-v2.md` Rule 6 (ownership boundaries).
- AI image generation and "real screenshots" — explicit Mami territory.
- LLM client wiring — fenced to Faz 12 per `DOCS/EMBER_VISION_NOTES_MAMI.md`.
- OpenMW source code copy — algorithmic reference only, never verbatim port.

## Next session priority (for the morning)

1. Watch the merge queue: #147 and #148 should be green and mergeable on wake.
2. If clean, merge them; if not, fix tests and merge.
3. Begin Faz 6 Atoms 5-9 implementation (price ledger, stockpile, trade route,
   caravan, caravan system) or Faz 8 promotion-of-magic bundle.
4. Mami opens a `mami/*` Unity scene PR for Faz 3 smithing acceptance using the
   already-merged `JobDebugSnapshot` rows.
