# Faz 5 - Atom map (Plant growth + Season)

_Date:_ 2026-05-17
_Branch:_ `agent/faz-5-season-ledger`
_Primary boxes:_ `TIME`, `PROCESS`
_Roadmap:_ `docs/ROADMAP.md`
_Mechanic map:_ `docs/mechanic-map-v1.md`
_Execution ledger:_ `docs/faz-5-12-execution-ledger.md`
_Agent rules:_ `docs/agent-rules-v2.md`
_Vision notes:_ `docs/EMBER_VISION_NOTES_MAMI.md`
_Inspector checklist:_ `docs/inspector-audit-checklist.md`

## Vision anchors

This sprint serves:

1. Living-world over showroom: seasons and plant growth tick even when the player is waiting.
2. Deterministic-first, LLM-last: time and farming resolve through typed data and tests.
5. Data-driven extension: plant behaviour is expressed as rows, not content-specific enum branches.
8. Systemic interaction: time, weather, soil, plants, stockpiles, and later needs connect through EventLog traces.

## Phase fences

No fence crossed by this bundle.

- No Memory state before Faz 9.
- No shared NPC / party / DM tool surface before Faz 10.
- No LLM fallback wiring before Faz 12.
- No procedural genesis.
- No multiverse / 100K-year / interplanetary implementation.
- No free-text dialogue parsing before Faz 9.

## Debt ledger action

`CO-03` from the Faz 4 debt ledger is deferred to Faz 5's path/farming hook atom. Reason: the first TIME atom (`SeasonCalendar`) does not consume actor pathing, while Faz 5 Atom 11 is the first place where planting/harvest jobs need `PathfindingSystem.Tick`.

## Sprint goal

Target acceptance sentence from `docs/ROADMAP.md`:

`player can wait until spring, plant wheat, harvest in summer, and see food stockpile rise`.

Faz 5 adds time consequence: a deterministic season calendar, plant data rows,
daily growth, snow blocking, harvest output, and a replayable food-stockpile
increase. Every new atom row carries exactly one `primary_box`.

## Atomic decomposition

| Atom | Primary box | File / class | Responsibility | Closing proof | Status |
|---:|---|---|---|---|---|
| 1 | TIME | `Assets/Scripts/Domain/Time/Season.cs`, `SeasonDefinition.cs`, `SeasonCalendar.cs` | Resolve `GameTime.DayOfYear` to data-defined seasons without Unity or wall-clock time. | `Assets/Tests/EditMode/Time/SeasonCalendarTests.cs` | implemented on this branch |
| 2 | TIME | `Assets/Scripts/Simulation/Time/GameTimeAdvanceSystem.cs` | Advance minutes/days and emit deterministic day/season transition events. | `GameTimeAdvanceSystemTests` | implemented on this branch |
| 3 | WORLD | `Assets/Scripts/Domain/World/WorldComponentId.cs`, `Assets/Scripts/Domain/World/ComponentStore.cs` | Stable component handles and deterministic component enumeration; shipped with same-PR soil consumer. | `WorldComponentIdTests`, `ComponentStoreTests` | implemented locally |
| 4 | PROCESS | `Assets/Scripts/Domain/Process/SoilComponent.cs` | Tilled soil tile component with site/position/fertility/moisture and optional plant reference. | `SoilComponentTests` | implemented locally |
| 5 | PROCESS | `PlantStageId`, `PlantGrowthStageDef`, `PlantSpeciesDef`, `PlantGrowthRule` | Data rows for wheat stages and snow-blocked growth; no species branch. | `PlantDefinitionTests` | implemented locally |
| 6 | PROCESS | `PlantComponent`, `PlantingSystem` | Consume a wheat seed from inventory and attach a plant entity to tilled soil. | `PlantComponentTests`, `PlantingSystemTests` | implemented locally |
| 7 | PROCESS | `WorldProcessId`, `WorldProcessDef`, `WorldProcessInstance` | Non-crafting transformation shape for slow world processes. | `WorldProcessDefinitionTests` | implemented locally |
| 8 | PROCESS | `PlantGrowthSystem` | Daily season/weather rule matching, growth advancement, harvestable-stage transition events. | `PlantGrowthSystemTests` | implemented locally |
| 9 | PROCESS | `HarvestSystem` | Convert ripe wheat into stockpile output with deterministic item factory injection. | `HarvestSystemTests` | implemented locally |
| 10 | TIME | `Data/Save` plant/season DTOs and mappers | Round-trip time/plant/process state without adding named `SliceWorldState` fields. | `PlantSeasonRoundTripTests` | implemented locally |
| 11 | PROCESS | Farming job/path hook | Connect planting/harvest jobs to the existing job/path lane and close/advance relevant pathing debt. | `FarmingJobIntegrationTests` | implemented locally |
| 12 | PROCESS | `docs/sprint-faz-5-plant-season-acceptance.md` | Deterministic replay proof for spring planting through summer harvest and food stockpile increase. | `PlantSeasonAcceptanceTests` and replay note | queued |

## Next increment

Wait for Atom 11 GitHub EditMode evidence, then implement Atom 12:
plant-season acceptance replay. Keep
Atom 1 proof tied to `SeasonCalendarTests`, Atom 2 to
`GameTimeAdvanceSystemTests`, Atoms 3-4 to `WorldComponentIdTests`,
`ComponentStoreTests`, and `SoilComponentTests`, Atom 5 to
`PlantDefinitionTests`, Atom 6 to `PlantComponentTests` plus
`PlantingSystemTests`, Atom 7 to `WorldProcessDefinitionTests`, and
Atom 8 to `PlantGrowthSystemTests`, Atom 9 to `HarvestSystemTests`, and
Atom 10 to `PlantSeasonRoundTripTests`, and Atom 11 to
`FarmingJobIntegrationTests`.
