# Sprint 2 Summary — Interaction Refinement and Presentation Cleanup

_Date:_ 2026-04-30
_Branch:_ `agent/sprint0-recon-sprint1-slice`
_Status:_ approved after independent inspection (repo-evidence based; local Unity/.NET execution unavailable)

## 1. What Builder implemented

### Domain / aggregate state
- extended `SliceWorldState` with persistent door, merchant-stock, and guard-clearance state

### Simulation
- door rules: `DoorInteractionService`, `RoomMovementService` door-threshold handling
- merchant rules: `SliceItemCatalog`, `MerchantTradeService`
- guard rules: `GuardInteractionService`
- role loadouts: `SliceActorLoadoutFactory`
- world bootstrap updates in `SliceWorldFactory`

### Data / persistence
- save DTO + mapper updates for door state, merchant inventory, and guard warning/clearance state

### Presentation
- split controller-facing orchestration into `SliceGameSession`
- split HUD formatting into `SliceHudFormatter`
- split world rendering into `SliceRoomView` and `SliceMarkerView`, composed by `SliceWorldView`
- updated the runtime bootstrap/controller shell to expose Sprint 2 inputs: trade, guard interaction, and door toggle

## 2. New deterministic behavior now in the slice

- the south door starts closed, blocks threshold movement, and persists through save/load
- Quartermaster Ivo trades one `Ember Shard` for one `Gate Writ` from finite stock
- Sentinel Rook escalates warnings without a writ and grants door clearance when shown one
- player / talker / merchant / guard / enemy now start from role-differentiated stat/vital/combat profiles

## 3. Tests added or expanded

EditMode coverage now includes:
- door threshold movement and door toggle rules
- merchant trade success + inventory-capacity refusal
- guard warning escalation + clearance grant
- role differentiation expectations from the world factory
- save/load round-trip for door, merchant, guard, and enemy state

## 4. Best available gate executed

This environment still had **no Unity editor, .NET CLI, or standalone C# compiler**, so Builder could not execute EditMode tests locally.

Recorded gates that did run:
- `git diff --check`
- static scan confirmed `Assets/Scripts/Domain` and `Assets/Scripts/Simulation` remain free of `UnityEngine`
- presentation file-length scan confirmed the refactor pulled `SliceGameController` and `SliceWorldView` back down into narrow files, with `SliceGameSession` only slightly above the soft target

## 5. Commits

- `e5ce40f` — deterministic door / merchant / guard / role-differentiation systems + tests
- `ab48b0f` — presentation seam split into session/HUD/room/marker helpers
- latest branch commit — Sprint 2 roadmap + summary updates

## 6. Remaining risks / follow-up

- Inspector should run Unity EditMode tests when a Unity-capable environment is available
- do one manual play pass for: pickup → trade → guard clearance → door toggle → save/load → encounter still functioning
- Sprint 1's approved encounter-loop deviation remains intentionally bounded; Sprint 2 did not expand that combat scope


## 7. Approval note

Sprint 2 received independent Inspector approval based on repo evidence and static gates. Follow-up items were recorded as Sprint 3 backlog, not approval blockers.
