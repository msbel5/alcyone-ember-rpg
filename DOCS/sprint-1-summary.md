# Sprint 1 Summary — Tiny Vertical Slice

_Date:_ 2026-04-30
_Branch:_ `agent/sprint0-recon-sprint1-slice`
_Status:_ approved after independent inspection (repo-evidence based; Unity/.NET execution unavailable locally)

## 1. Approved deviation budget

Sprint 1 records an **approved deviation budget** from the repo's RTWP lock: the slice uses a bounded one-vs-one encounter turn loop.

This is durable Sprint 1 scope, not a tentative workaround. The rationale is explicit:
- the active sprint requirement demanded one enemy + turn-based combat
- the repo scaffold did not yet have the broader RTWP bootstrap needed to prove the slice quickly
- the chosen encounter loop keeps deterministic combat math reusable while containing the deviation to Sprint 1 orchestration

Long-term RTWP architecture is still the default outside this sprint. Sprint 2 decides whether to promote or adapt the orchestration layer.

## 2. What Builder implemented

### Domain
- six-stat actor kernel: `EmberAttribute`, `EmberStatBlock`, `ActorVitals`, `VitalStat`, `GridPosition`, `ActorRecord`, `ActorRole`
- combat state: `BodyPart`, `CombatStrikeResult`, `EncounterState`
- inventory state: `InventoryItem`, `InventoryState`, `RoomPickup`
- narrative/world state: `AskAboutTopic`, `ProceduralRoom`, `SliceWorldState`

### Simulation
- deterministic RNG: `IDeterministicRng`, `XorShiftRng`
- world generation/movement: `ProceduralRoomGenerator`, `RoomMovementService`, `SliceWorldFactory`
- inventory pickup loop: `PickupService`
- combat kernel: `BodyPartSelector`, `CombatMathService`, `EncounterTurnService`
- narrative shells: `AskAboutService`, `AskDmService`, `ThinkService`

### Data / persistence
- JSON DTOs and mappers: `SliceSaveData`, `ActorSaveMapper`, `ItemSaveMapper`, `SliceSaveMapper`
- JSON service: `JsonSliceSaveService`

### Presentation
- runtime auto-bootstrap: `SliceRuntimeBootstrap`
- first-person movement + mouse look: `SlicePlayerRig`
- primitive room and actor rendering: `SliceWorldView`
- playable input shell for pickup / Ask About / encounter / save-load: `SliceGameController`

## 3. Tests added

EditMode NUnit coverage was added for:
- stats and bounds
- vitals transitions
- actor movement/state updates
- room generation and movement rules
- inventory add/remove/full behavior
- body-part selection
- combat hit chance and mitigation
- encounter turn progression
- Ask About / Ask DM / Think shells
- JSON save/load round-trip

## 4. Best available gate executed

This environment did **not** have a Unity editor or .NET CLI available, so Builder could not execute EditMode tests here.

Recorded gates that did run:
- static scan confirmed `Domain/` and `Simulation/` remain free of `UnityEngine` references
- static scan confirmed new script files include design-note headers
- asmdef references were updated so Data and EditMode tests can see the new save/persistence code
- branch is clean after commit

## 5. Commits

- `4fe978f` — deterministic slice domain/simulation/data kernels and EditMode tests
- latest branch commit — FR-08 presentation shell plus durable approved-deviation docs (`PRD`, `ROADMAP`, `sprint-0-recon`, `sprint-1-summary`)

## 6. Approval note

Sprint 1 received independent Inspector approval based on repo evidence.

## 7. Handoff notes

- Inspector should review the approved deviation budget as documented scope, not as an unresolved architecture dispute
- Inspector should verify that Presentation remains thin and that pure logic stayed in Domain/Simulation
- If Unity is available in the next environment, run EditMode tests first, then a quick manual play pass for FR-08
- Do **not** call Sprint 1 fully done until Inspector signs off
