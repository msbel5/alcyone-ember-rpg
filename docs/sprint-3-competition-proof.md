# Sprint 3 — Competition proof fixtures

## Goal

Complete the Faz 3 `competition-proof` bundle: centralize canonical smelting/bread recipe fixtures and prove job assignment chooses the higher-priority smithing lane while bread jobs wait when their concrete bakery worksite is absent.

## Files changed

- `Assets/Scripts/Domain/Inventory/ItemMaterial.cs`
- `Assets/Scripts/Domain/Process/JobKind.cs`
- `Assets/Scripts/Domain/Process/WorksiteKind.cs`
- `Assets/Tests/EditMode/Process/RecipeFixtureCatalog.cs`
- `Assets/Tests/EditMode/Process/JobAssignmentCompetitionTests.cs`
- `DOCS/sprint-faz-3-atom-map.md`
- `DOCS/sprint-3-competition-proof.md`

## Behaviour

- `RecipeFixtureCatalog.SmeltIronIngot` is the canonical furnace fixture for Faz 3 job-assignment tests.
- `RecipeFixtureCatalog.BakeBread` adds the second concrete recipe lane using `JobKind.Baker`, `WorksiteKind.Bakery`, and `ItemMaterial.Food` with immediate tests as consumers.
- `HigherPrioritySmithWinsFurnace` proves actor preference priority beats actor/job insertion order when smithing and baking jobs compete.
- `BreadJobWaitsWithoutBakery` proves a baking job remains pending instead of claiming an actor when the bakery worksite is missing.

## Product-visible count

Product-visible PR count for this increment: 0. This is a focused PROCESS/MATTER competition proof that prepares the job-assignment acceptance path; the next visible increment should emit job-specific EventLog lines.

Acceptance sentence: player can later queue smithing and baking jobs with deterministic priority ordering, and the simulation will not start bread work until a bakery exists.

## Validation

- `git diff --check` — passed.
- `./tools/validation/run-validation.sh --mode fallback` — passed (fallback harness: 875 passed, 0 failed, 0 skipped; Unity editor blocked: not found).

## Thalamus

- packet_id: `pkt_20260515115051_7c05a78a801d`
- resolver_key: `sha256:ce3356cda2fcb210c0273de673de2bcabb6498bb23fef179c703dd9c214ce998`

## Next increment

Continue Faz 3 with the `job-save-proof` bundle: add concrete `JobAssigned` / `JobCompleted` EventLog rows before save/load mapping.
