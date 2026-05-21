# Faz 4-12 backend execution map

_Generated:_ 2026-05-17
_Branch:_ agent/faz-5-season-ledger
_Depends on:_ docs/backend-method-tree.md, docs/ROADMAP.md, docs/mechanic-map-v1.md

This file is the missing execution bridge: roadmap acceptance -> backend method groups -> next small PR slices. It exists to prevent free-running agents from opening disconnected PRs.

## Hard findings

- Backend is not blocked by lack of tools. The blocker was missing method-level decomposition and persistent checkpointing.
- Local fallback harness can pass while Unity EditMode fails; Unity .meta validity and asmdef import are separate gates.
- Faz 5 has started with SeasonCalendar, but PR #136 remains draft until GitHub EditMode is green.
- Faz 6-12 are not backend-complete. Treat them as queued backend phases, not done work.

## Execution slices

| Slice | Box | Capability | Exit proof | Method/file group | Status |
|---|---|---|---|---|---|
| Faz 4 closeout | LIVING/PROCESS | Colony needs acceptance replay and cleanup | Keep #133/#134/#135 green, close/supersede #132, record final Faz 4 proof | JobAssignmentSystem, NeedsSystem, NeedRecoverySystem, ActorNeeds, ActorMood, WorldEventLog | Faz 4 not promoted until PR noise is resolved |
| Faz 5 slice 1 | TIME | Season calendar foundation | Season enum/definitions/calendar + GameTime constants + tests | Season, SeasonDefinition, SeasonCalendar, GameTime | implemented on #136; CI still failing |
| Faz 5 slice 2 | TIME | GameTime advance events | GameTimeAdvanceSystem emits day/season events | new Simulation/Time/GameTimeAdvanceSystem, WorldEventKind day/season rows | implemented locally; needs validation/CI |
| Faz 5 slice 3 | PROCESS | Component handles + soil | WorldComponentId, ComponentStore, SoilComponent | Domain/World component store + Domain/Process soil | implemented locally; needs validation/CI |
| Faz 5 slice 4 | PROCESS | Plant data rows | Plant stage/species/growth rule definitions and wheat rows | new Domain/Process plant data types | implemented locally; needs validation/CI |
| Faz 5 slice 5 | PROCESS | Planting loop | PlantComponent and PlantingSystem consume seed and attach plant to soil | Domain/Process PlantComponent + Simulation/Process PlantingSystem | implemented locally; needs validation/CI |
| Faz 5 slice 6 | PROCESS | World process shape | WorldProcessDef, WorldProcessInstance, deterministic day progress | Domain/Process world process primitives | implemented locally; needs validation/CI |
| Faz 5 slice 7 | PROCESS | Plant growth loop | PlantGrowthSystem applies season/weather rules and emits stage events | Simulation/Process PlantGrowthSystem | implemented locally; needs validation/CI |
| Faz 5 slice 8 | PROCESS | Harvest loop | HarvestSystem converts harvestable plants into stockpile output | Simulation/Process HarvestSystem, InventoryState bridge | implemented locally; needs validation/CI |
| Faz 5 slice 9 | TIME/PROCESS | Save roundtrip | Save/load plant, soil, and season state | Data/Save DTOs and mapper tests | implemented locally; needs validation/CI |
| Faz 5 slice 10 | PROCESS | Farming job/path hook | Planting/harvest jobs connect to existing job/path lane | FarmingJobRequestFactory + job/path integration tests | implemented locally; needs validation/CI |
| Faz 5 slice 11 | PROCESS | Acceptance replay | Spring planting through harvest increases food stockpile | PlantSeasonAcceptanceTests and replay docs | next |
| Faz 6 slice 1 | SOCIETY | Settlement economy primitives | Settlement stock/demand + price quote | FactionStore, SiteStore, ItemStore plus new society records | queued after Faz 5 proof |
| Faz 6 slice 2 | SOCIETY/TIME | Trade route tick | TravelEdge, Caravan state, TradeRouteSystem daily delivery event | new Domain/Society + Simulation/Society | queued |
| Faz 7 slice 1 | CRPG/MATTER | Store-backed equipment combat | Weapon/Armor item data, EquipmentState integration, durability | EquipmentService, CombatMathService, RealtimeDamageService, ItemRecord | queued |
| Faz 7 slice 2 | CRPG/MATTER | Death drops inventory | Combat death result writes item drops/event log | Combat services, InventoryState, WorldEventLog | queued |
| Faz 8 slice 1 | CRPG | EffectDefinition registry | Data-driven effect rows and handler registry without new SpellEffectCode | SpellEffectResolutionService, SliceSpellCatalog, new EffectDefinition | landed (SpellResolver + EffectOperationHandlers ship; SpellEffectResolutionService remains as legacy migration adapter) |
| Faz 8 slice 2 | CRPG | Data-only new spell proof | Add a spell row with no C# branch and event-log proof | Magic services/catalog/tests | partially landed (seventh-pass: DomainSimulationAdapter.TryCastSpell now routes through SpellExecutionService = SpellCastingService + SpellTargetValidator + SpellEffectResolutionService + SpellCastRollService, so live cast mutates target vitals. Data-only NEW-spell proof — adding a row with zero C# branch — still queued.) |
| Faz 9 slice 1 | LIVING/SOCIETY/CRPG | Memory/disposition audit | Audit Sprint 1 narrative files reuse/refactor/deprecate decisions | ActorMemory, NpcMemoryStore, AskAboutService, ThinkService | queued |
| Faz 9 slice 2 | LIVING/SOCIETY/CRPG | Crime memory affects trade/dialogue | MemoryComponent, Disposition, crime ledger, dialogue gate tests | Narrative + Memory + Faction files | queued |
| Faz 10 slice 1 | AI/DM | Read-only query views | ActorView/MemoryView/WorldSnapshot projections; no mutation | WorldEvent, ReasonTrace, Store records | queued |
| Faz 10 slice 2 | AI/DM | Typed tool envelopes | Query/Chance/Roll/Mutation validation with mock LLM client | new AI/DM backend namespace | queued |
| Faz 11 backend support | Unity-only boundary | Snapshot rows for visual layer | Captain only writes data/debug dump contracts; Mami owns screenshots/scenes | Presentation formatters only when same-PR consumer exists | parallel |
| Faz 12 slice 1 | AI/DM | Local-first flavour proposal | NpcAgent/DmAgent proposal interfaces; validated no world mutation | depends on Faz 10 query API | queued last |

## Next exact action

Validate and push Faz 5 slice 10, then wait for #136 GitHub EditMode evidence before adding acceptance replay.
