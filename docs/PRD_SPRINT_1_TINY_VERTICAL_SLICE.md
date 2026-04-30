# PRD — Sprint 1 Tiny Vertical Slice

_Date:_ 2026-04-30
_Status:_ active
_Owner:_ Captain
_Implementer:_ Builder
_Reviewer:_ Inspector

## 1. Purpose

Take the Unity rewrite from scaffold to a thin playable slice in one branch, with deterministic mechanics, small files, tests, and self-explaining code.

This sprint is intentionally narrow: one room, one player, three NPC archetypes, one enemy, one pickup loop, one save/load loop.

## 2. Hard constraints

1. Mechanics must work without AI.
2. AI is flavor/narration only.
3. Files should stay near 100 LOC; split by responsibility.
4. Each class/file must begin with a short header comment describing:
   - what it does
   - inputs
   - outputs
   - Bible/system reference
5. `Domain/` and `Simulation/` must remain free of UnityEngine references.
6. Tests should be written first where practical.
7. Commits must be small and descriptive.
8. Work must stay on `agent/*`, never `main`.

## 3. Architecture inputs from repo docs

### Locked and preserved
- deterministic world: `docs/mechanics/MASTER_MECHANICS_BIBLE.md:13`
- unified Actor + Item direction: `docs/mechanics/MASTER_MECHANICS_BIBLE.md:12`
- deterministic implementation order: `docs/mechanics/ARCHITECTURE.md:414-425`
- Ember 6-stat recommendation: `docs/mechanics/MASTER_MECHANICS_BIBLE.md:112`
- HP/Fatigue/Mana direction: `docs/mechanics/MASTER_MECHANICS_BIBLE.md:157`
- inventory/item kernel direction: `docs/mechanics/ARCHITECTURE.md:173-210`

### Approved Sprint 1 deviation budget
The repo lock says RTWP and explicitly rejects a turn-based combat state machine:
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md:10`
- `docs/mechanics/ARCHITECTURE.md:9`
- `docs/mechanics/ARCHITECTURE.md:434`

The active user addendum explicitly requires “one enemy + turn-based combat.”

Sprint 1 now has an **approved deviation budget** for a bounded one-vs-one encounter turn loop. This is the accepted combat shape for this sprint, documented in code and sprint docs, while the repo's longer-term RTWP architecture remains intact outside this slice. Inspector reviews conformance to the documented scope, not whether the deviation was allowed.

## 4. Functional requirements

### FR-01 — Stats and actor state
Implement deterministic actor state with:
- MIG, AGI, END, MND, INS, PRE
- actor id / actor record
- grid position
- current + max health
- current + max fatigue
- current + max mana

### FR-02 — Combat kernel
Implement a deterministic combat kernel with:
- percentage-based hit chance
- damage resolution
- armor mitigation
- weighted body-part target selection
- one-vs-one encounter turn progression
- combat log/result object suitable for tests and UI text

### FR-03 — World slice
Implement one runtime-generated room with:
- floor and wall boundaries
- at least one door
- spawn points for player, talk NPC, merchant/storage NPC, enemy
- one pickup item in the room

### FR-04 — NPC slice
Implement three NPC archetypes:
- Talker: supports Ask About seed topics
- Merchant/Storage: inventory-facing NPC surface for future expansion
- Guard/Enemy-facing archetype: can anchor encounter/combat behavior

Only one must fully support Ask About in Sprint 1.

### FR-05 — Inventory slice
Implement a deterministic 10-slot inventory with:
- add item
- remove item
- detect full inventory
- pickup from room item source
- serialization support

### FR-06 — Save/Load
Implement JSON save/load for the vertical-slice state:
- player actor state
- inventory
- room seed or room snapshot
- NPC slice state
- enemy slice state
- Ask About topic state if needed for deterministic reload

### FR-07 — Narrative shells
Implement thin deterministic shells for:
- Ask About
- Ask DM
- Think

Sprint 1 requirement is structure, not LLM depth. These can return grounded placeholder or rules-based outputs from current state.

### FR-08 — Playable presentation
Implement minimal Unity presentation so a human can:
- launch the project
- move with WASD
- look with mouse
- see the room
- reach an item
- pick it up
- trigger combat with one enemy
- trigger save/load

## 5. Non-functional requirements

- deterministic-first: no `UnityEngine.Random` inside mechanics
- self-explaining code comments on every class/file
- no committed generated binaries/assets
- keep responsibilities narrow
- prefer pure C# logic with thin Unity wrappers

## 6. Suggested implementation order

1. Domain stats + vitals + grid position + actor model
2. inventory model + save DTOs
3. combat formulas + encounter turn service
4. room generator data/service
5. narrative shell services
6. Unity bootstrap and presentation wrappers
7. final docs + summary

## 7. Test plan

### Domain / Simulation
Add NUnit EditMode tests for:
- stat construction and bounds
- vitals transitions
- actor creation/update
- grid movement rules
- hit chance boundaries
- armor reduction
- body part selection using seeded RNG or deterministic selector
- encounter turn progression
- inventory add/remove/full
- save/load round-trip
- Ask About / Ask DM / Think deterministic shell outputs

### Presentation
Where direct Unity test execution is not practical in this environment, keep Unity-specific code thin and move logic into testable services.

## 8. Commit policy

Commit every small completed chunk with message format:
- what changed
- why
- Bible/system reference
- how tested

Example shape:
`feat: add actor vitals kernel (Bible §1/§3, tested with EditMode vitals suite)`

## 9. Definition of done

Sprint 1 is done only when all are true:
- required systems implemented
- tests added for every system in scope
- best available test gate executed and recorded
- Inspector reviews the branch and approves or returns issues
- `DOCS/sprint-1-summary.md` exists
