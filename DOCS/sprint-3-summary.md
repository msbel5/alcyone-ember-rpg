# Sprint 3 Summary — Validation hardening and simulation depth

- Branch: `agent/sprint-3-validation-and-depth`
- Base: `main` @ `0f36891`
- Inspector verdict: **APPROVED**
- Status: **implemented on branch, PR-ready, with local Unity/manual validation blocker documented**

## What shipped

Sprint 3 landed in three small feature commits:

1. `116ae2e` — item identity hardening, persistent NPC memory, Tier 1 DM query surface
2. `607ab91` — equipment state, richer room templates, faction/reputation hooks
3. `b6fb9ed` — layered deterministic DM query tiers and HUD surfacing

Net diff from `main`: `36 files changed, 1490 insertions(+), 98 deletions(-)`.

## Scope completed

### 1) Inventory identity hardening
- stable `ItemInstanceSequence`-backed item ids remain the source of truth
- non-stackable items no longer merge by template id
- exact-instance take/equip flow works by `ItemId`
- save/load preserves item identity, equipment, and reputation state

### 2) Richer room templates
- `CheckpointAxis`, `OffsetWatch`, and `SplitHall` layouts added
- layout selection is deterministic from room seed
- tests cover distinct layout selection plus walkable/unique spawn positions

### 3) Faction / reputation hooks
- persistent `FactionReputationLedger` added
- `city_watch` reputation now influences guard attitude and checkpoint responses
- warnings degrade reputation; clearance improves it
- save/load round-trip covers reputation persistence

### 4) Expanded equipment state
- `Weapon` and `Armor` slots added as a separate `EquipmentState`
- equip/unequip flow is deterministic and reversible
- equipped items are separated from backpack inventory
- gate writ is intentionally non-stackable in this slice

### 5) Deeper DM query layers
- Tier 1 summary remains the compact current-state surface
- Tier 2 detail adds layout, attitude, equipment, pickups, and focus reason
- Tier 3 narrative formats the same deterministic facts into a fuller scene summary
- routing is keyword-based and deterministic by design for this sprint

## Validation evidence

### Repo / static gates
- `git diff --check main...HEAD` → clean
- static scan of `Assets/Scripts/Domain` and `Assets/Scripts/Simulation` found no `UnityEngine` usage
- branch is fresh from `main` and contains exactly the 3 Sprint 3 feature commits above

### Independent pure-C# test gate
Because this Pi currently has no usable local Unity editor runtime, I ran the slice's pure C# layers through an independent NUnit harness that linked the repo's `Assets/Scripts` (excluding Presentation), linked all `Assets/Tests/EditMode`, and used a minimal `UnityEngine.JsonUtility` stub only for JSON round-trip coverage.

Command result:

```text
Passed!  - Failed:     0, Passed:    79, Skipped:     0, Total:    79, Duration: 437 ms
```

Covered test footprint at this gate: **79 tests across 19 EditMode test files**.

### Inspector gate
Inspector result:

> APPROVED: 3 commits verified on branch, clean diff, 79 tests across 19 files, no UnityEngine in Domain/Simulation; Unity editor run remains blocked (no editor binary on device) but pure-C# NUnit coverage is complete and non-blocking.

## Exact blockers still present

### Blocker A — local Unity EditMode run on Pi
I could not run Unity EditMode tests directly on this Pi because no local Unity editor binary was available in the usual install paths during validation.

### Blocker B — real/manual slice play pass on Pi
A real manual in-engine pass is blocked by the same missing Unity editor/runtime availability. This is **not** claimed as complete.

### Blocker C — raw `dotnet test` in repo is not a Unity substitute
Running `/home/msbel/.dotnet/dotnet test` in the repo fails because the Unity project has no `.sln` or `.csproj` in the repository root. That command was used only as blocker evidence, not as a meaningful Unity gate.

## Deferred / follow-up items
- run real Unity EditMode suite on a machine with the matching editor version
- run one real/manual slice play pass and capture screenshots / notes
- decide whether CI should watch agent branches directly or stay PR-to-main only
- tune guard reputation curve if it feels too punitive in play
- expand equipment beyond `weapon/armor` only when a broader item model justifies it
- broaden faction hooks beyond `city_watch` only when additional actors/systems need them

## Notes on scope discipline
- `SliceGameSession` was **not** split again; current size did not justify another seam
- DM tiers remain deterministic and data-grounded; no freeform AI logic was introduced into simulation
- the missing Unity/manual gates are documented honestly instead of being papered over
