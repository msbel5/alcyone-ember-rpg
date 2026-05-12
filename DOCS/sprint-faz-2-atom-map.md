# Faz 2 — Atom map (Recipe + Worksite)

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-atom-map`
_Primary Faz 2 boxes:_ `[box=PROCESS]`, `[box=MATTER]`.
_Support boxes:_ `[box=LIVING]` (worker skill/actor identity), `[box=WORLD]` (worksite location), `[box=TIME]` (40-tick deterministic proof), `[box=PLAYABLE]` (acceptance proof).
_Acceptance gate:_ `player can craft an iron ingot from ore and fuel and watch the stockpile increase` (from `docs/ROADMAP.md` Faz 2).

This atom map decomposes Faz 2 against `DOCS/mechanic-map-v1.md` and `DOCS/agent-rules-v2.md` before any gameplay code is written. Per `CRON_CODES.md @EMSPR`, Faz 2 is not promotion-ready until every row below is checked off, every sub-area has merged PR evidence, `tools/validation/run-validation.sh --mode fallback` passes, and a final sprint summary records atom count, bundle count, and product-visible PR count.

Format: `- [ ] file/path :: scope :: brief responsibility [box=...]`.

## Sub-area: Recipe definitions (PROCESS/MATTER)

- [x] `Assets/Scripts/Domain/Process/RecipeId.cs` :: `RecipeId` :: readonly value handle for deterministic recipe lookup, default = empty [box=PROCESS] — landed on `agent/sprint-faz-2-recipe-primitives`
- [x] `Assets/Tests/EditMode/Process/RecipeIdTests.cs` :: tests :: pin `RecipeId` value semantics and debug string behavior [box=PROCESS] — landed on `agent/sprint-faz-2-recipe-primitives`
- [x] `Assets/Scripts/Domain/Process/RecipeIngredient.cs` :: `RecipeIngredient` :: pure input row describing item/material tag and quantity requirements [box=MATTER][box=PROCESS] — landed on `agent/sprint-faz-2-recipe-primitives`
- [x] `Assets/Tests/EditMode/Process/RecipeIngredientTests.cs` :: tests :: reject blank item tags and non-positive quantities [box=MATTER][box=PROCESS] — landed on `agent/sprint-faz-2-recipe-primitives`
- [x] `Assets/Scripts/Domain/Process/RecipeOutput.cs` :: `RecipeOutput` :: pure output row describing produced item/material/quality/quantity [box=MATTER][box=PROCESS] — landed on `agent/sprint-faz-2-recipe-output`
- [x] `Assets/Tests/EditMode/Process/RecipeOutputTests.cs` :: tests :: pin output quantity and material/quality storage [box=MATTER][box=PROCESS] — landed on `agent/sprint-faz-2-recipe-output`
- [x] `Assets/Scripts/Domain/Process/RecipeDef.cs` :: `RecipeDef` :: pure definition for inputs, outputs, worksite kind, skill tag, and tick duration [box=PROCESS][box=MATTER] — landed on `agent/sprint-faz-2-recipe-def`
- [x] `Assets/Tests/EditMode/Process/RecipeDefTests.cs` :: tests :: pin constructor invariants, defensive copies, and `SmeltIronIngot` shape [box=PROCESS][box=MATTER] — landed on `agent/sprint-faz-2-recipe-def`

## Sub-area: Worksite state (WORLD/PROCESS)

- [x] `Assets/Scripts/Domain/Process/WorksiteKind.cs` :: `WorksiteKind` :: seed enum for none/furnace used by Faz 2 [box=WORLD][box=PROCESS] — landed on `agent/sprint-faz-2-worksite-record`
- [x] `Assets/Scripts/Domain/Process/WorksiteRecord.cs` :: `WorksiteRecord` :: pure site-cell worksite component with kind, site id, grid position, and active flag [box=WORLD][box=PROCESS] — landed on `agent/sprint-faz-2-worksite-record`
- [x] `Assets/Tests/EditMode/Process/WorksiteRecordTests.cs` :: tests :: pin constructor invariants and active/inactive state [box=WORLD][box=PROCESS] — landed on `agent/sprint-faz-2-worksite-record`
- [x] `Assets/Scripts/Domain/Process/WorksiteStore.cs` :: `WorksiteStore` :: dictionary-backed registry over site-position keys, deterministic enumeration [box=WORLD][box=PROCESS] — landed on `agent/sprint-faz-2-worksite-store`
- [x] `Assets/Tests/EditMode/Process/WorksiteStoreTests.cs` :: tests :: pin add/get/remove/enumeration/default-key rejection [box=WORLD][box=PROCESS] — landed on `agent/sprint-faz-2-worksite-store`

## Sub-area: Recipe execution (PROCESS/MATTER/LIVING/TIME)

- [x] `Assets/Scripts/Simulation/Process/RecipeSystem.cs` :: `RecipeSystem` :: validate worksite + actor + inventory inputs, consume ore/fuel, advance progress, produce outputs [box=PROCESS][box=MATTER][box=LIVING][box=TIME] — landed on `agent/sprint-faz-2-recipe-system`
- [x] `Assets/Tests/EditMode/Process/RecipeSystemTests.cs` :: tests :: deterministic 40-tick smelt consumes 2 iron ore + 1 fuel and produces 1 iron ingot [box=PROCESS][box=MATTER][box=TIME] — landed on `agent/sprint-faz-2-recipe-system`
- [x] `Assets/Scripts/Domain/World/WorldEventKind.cs` :: `WorldEventKind` :: add a recipe/worksite event kind only when `RecipeSystem` emits it in the same PR [box=PROCESS] — landed on `agent/sprint-faz-2-recipe-system`
- [x] `Assets/Tests/EditMode/Process/RecipeEventLogTests.cs` :: tests :: pin `RecipeSystem` writes an ordered `WorldEventLog` line with `ReasonTrace` [box=PROCESS][box=PLAYABLE] — landed on `agent/sprint-faz-2-recipe-system`

## Sub-area: Save/load and player-facing proof (TIME/PLAYABLE)

- [ ] `Assets/Scripts/Data/Save/SliceSaveMapper.cs` :: extend mapper :: serialize recipe/worksite progress only after runtime state exists [box=TIME]
- [ ] `Assets/Tests/EditMode/Save/RecipeWorksiteRoundTripTests.cs` :: tests :: round-trip active worksite progress and produced stock [box=TIME][box=PROCESS]
- [ ] `DOCS/sprint-faz-2-smelt-iron-acceptance.md` :: `player can ...` :: deterministic replay proof for crafting iron ingot from ore + fuel at furnace [box=PLAYABLE]

## Bundling guidance

- Bundle 1 candidate: `RecipeId` + `RecipeIngredient` + tests if they remain pure and under one PR.
- Bundle 2 candidate: `RecipeOutput` + `RecipeDef` + tests if constructor invariants stay narrow.
- Do not bundle `RecipeSystem` with save/load or playable proof; that crosses PROCESS, TIME, and PLAYABLE gates.

## Promotion checklist

- [ ] every Faz 2 atom above is checked off
- [ ] every sub-area has at least one merged PR: Recipe definitions, Worksite state, Recipe execution, Save/load/proof
- [ ] `tools/validation/run-validation.sh --mode fallback` passes on the final Faz 2 branch
- [ ] final sprint summary records atom count and bundle count
- [ ] product-visible PR count for Faz 2 ≥ 1

## This atom map

- [x] `DOCS/sprint-faz-2-atom-map.md` :: this file :: canonical Faz 2 decomposition required before first gameplay atom [box=meta] — created on `agent/sprint-faz-2-atom-map`

## Thalamus packet

- packet_id: `pkt_20260511213524_1a8c556dd6e1`
- resolver_key: `sha256:f03d726809ee9adaf77d58d3a2505c4575a3eb382bcdfd94b66a6e038050fee7`

## Next increment after this PR

RecipeSystem now emits the first product-visible Faz 2 EventLog slice. Next implementation should target the save/load and player-facing proof sub-area: serialize active recipe/worksite progress only after runtime state exists, then add a deterministic acceptance replay for crafting iron ingot from ore + fuel at a furnace.

- packet_id: `pkt_20260511222722_4531e566a532`
- resolver_key: `sha256:aef062b231ad626b049d9166098ecef0131cad4633ea6727d52c0efaf3805b09`

- packet_id: `pkt_20260512151432_3f3e838ea4f3`
- resolver_key: `sha256:51368bad478cc9049ff6efb3539955fe2559a6f8a0186140c5a324b0adc2b0ce`

- packet_id: `pkt_20260512155456_1e0b33ca071d`
- resolver_key: `sha256:8412025a3dc3a147474a87cabd5502591a859a9356bbdd9114d87d9bc5255b5a`
- packet_id: `pkt_20260512172605_70e9bd2bfe2e`
- resolver_key: `sha256:27b28c9ecc4741edc2e79c1b1a200f96245e731fb218e152dd71df9d735e76a4`

- packet_id: `pkt_20260512182245_50ced2e83e39`
- resolver_key: `sha256:fb5f0604997918a5753c37c549c3b790e6abf8ab952a8da6d613eae21b3fc16a`
