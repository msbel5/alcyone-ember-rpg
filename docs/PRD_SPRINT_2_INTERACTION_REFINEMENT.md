# PRD — Sprint 2 Interaction Refinement and Presentation Cleanup

_Date:_ 2026-04-30
_Status:_ active
_Owner:_ Captain
_Implementer:_ Builder
_Reviewer:_ Inspector

## 1. Purpose

Sprint 1 proved the repo can ship a tiny playable slice from a scaffold.
Sprint 2 should harden that slice without exploding scope: clean the presentation seam, make doors real, and upgrade Merchant/Guard from placeholders into actual interaction surfaces.

## 2. Context carried from Sprint 1

- Sprint 1 is approved.
- The one-vs-one encounter turn loop remains an **approved Sprint 1 deviation budget** and stays bounded for now.
- Long-term repo architecture is still RTWP by default outside the slice.
- Sprint 2 should prepare cleaner seams for later RTWP reconciliation, not re-litigate the approved deviation.

## 3. Hard constraints

1. Mechanics remain deterministic-first.
2. Domain/Simulation remain free of UnityEngine references.
3. Presentation stays thin and should move back under the soft ~100 LOC target where practical.
4. Each file keeps the short header comment/docstring contract.
5. Tests should be added for every new deterministic system.
6. Small atomic commits only.

## 4. Functional requirements

### FR-01 — Presentation seam cleanup
Refactor Sprint 1 presentation wiring so responsibilities are narrower.

Acceptance:
- `SliceGameController` is split into smaller presentation-facing files or helper services.
- `SliceWorldView` is split if needed so rendering responsibilities are clearer.
- Pure mechanics stay out of Presentation.

### FR-02 — Door state becomes real gameplay state
Promote the visual doorway into deterministic world state.

Acceptance:
- Domain/Simulation model closed/open door state.
- Movement rules respect the door state.
- Player can toggle/open the door through the slice input shell.
- Save/load persists the door state.
- NUnit tests cover door movement and persistence.

### FR-03 — Merchant becomes a real interaction surface
Upgrade the merchant from a placed marker to a usable slice system.

Acceptance:
- Merchant has at least a tiny stock/inventory surface.
- Player can buy or transfer at least one deterministic item through a narrow rules-based interaction.
- Inventory limits still apply.
- Save/load persists merchant state.
- Tests cover the merchant interaction rules.

### FR-04 — Guard becomes a real interaction surface
Upgrade the guard beyond a positional stub.

Acceptance:
- Guard exposes a distinct deterministic interaction response.
- At least one guard-specific topic, warning, or stateful response exists.
- This interaction is not just the Talker reused with a renamed string.
- Tests cover the guard interaction shell.

### FR-05 — Role differentiation
Different actor roles should no longer feel cloned.

Acceptance:
- Player / Talker / Merchant / Guard / Enemy have role-appropriate starting vitals or combat fields.
- The distinction is encoded in pure Domain/Simulation code, not only in UI text.
- Tests cover at least one differentiated-role expectation.

## 5. Non-functional requirements

- no binaries/assets committed
- keep code self-explaining
- prefer service extraction over fat controllers
- document any remaining Sprint 2 deviations honestly in summary docs

## 6. Suggested implementation order

1. extract presentation responsibilities
2. add door state + movement integration + save/load
3. add merchant interaction + tests
4. add guard interaction + tests
5. add role differentiation cleanup + summary updates

## 7. Definition of done

Sprint 2 is done only when:
- the above systems are implemented
- tests exist for new deterministic behavior
- best available validation is recorded honestly
- Inspector approves the sprint
- a Sprint 2 summary doc exists
