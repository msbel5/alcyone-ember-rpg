# Faz 5-12 execution ledger

_Date:_ 2026-05-17
_Branch:_ `agent/faz-5-season-ledger`
_Source of truth:_ `docs/ROADMAP.md`
_Control docs:_ `docs/agent-rules-v2.md`, `docs/inspector-audit-checklist.md`, `docs/mechanic-map-v1.md`, `docs/EMBER_VISION_NOTES_MAMI.md`

This ledger is the Captain-side execution queue from Faz 5 through Faz 12.
Each row quotes the roadmap acceptance sentence and narrows the next safe
Captain-owned move. Unity visuals, screenshots, scenes, prefabs, art, and
binary assets stay Mami-owned per Rule 6.

## Roadmap acceptance ledger

| Faz | Roadmap box | Roadmap acceptance sentence | Captain-safe execution lane | Current next atom | Status |
|---|---|---|---|---|---|
| Faz 5 - Plant growth + Season | TIME | `player can wait until spring, plant wheat, harvest in summer, and see food stockpile rise` | Pure calendar, plant/process domain rows, deterministic growth/harvest systems, save/replay tests. No Unity visual proof. | `GameTimeAdvanceSystem` day/season transition event atom. | Atom 1 implemented on this branch |
| Faz 6 - Trade routes + Faction | SOCIETY | `player can stand at the city gate, watch a caravan arrive from a nearby settlement, see prices drop after delivery` | Settlement stock/demand rows, travel edge, caravan actor state, daily trade route tick and price event tests. | Wait for Faz 5 acceptance proof and food stockpile shape. | queued |
| Faz 7 - Combat + Equipment integration | CRPG | `player can equip a sword, fight a bandit in the woods, loot the body, return to town` | Store-backed equipment/combat integration, durability, death inventory drop, deterministic combat replay. | Wait for store-backed item/equipment shape and path/travel dependencies. | queued |
| Faz 8 - Data-driven magic | CRPG | `player can cast a new spell that exists only in data (no C# branch added), see it succeed and write to the EventLog` | `EffectDefinition` and operation handler registry; migrate enum effects row-by-row; new spell as data only. | Wait until Faz 7 combat/equipment gives magic meaningful CRPG context. | queued |
| Faz 9 - Dialogue + Memory + Faction reputation | LIVING | `player can witness an NPC remember a crime committed two days ago and refuse to trade` | Memory/disposition/crime ledger systems, deterministic dialogue topic gating. Must audit Sprint 1 narrative files first. | Blocked by phase fence until Faz 9 opens. | queued |
| Faz 10 - DM Query API | AI/DM | `player can press F9, see the same world snapshot the LLM has, including memory facts and faction state` | Read-only typed views, deterministic query/chance/roll/mutation envelopes, mock-client tests only. | Blocked by Faz 9 memory/faction state and DM tool-surface fence. | queued |
| Faz 11 - Unity visual layer | Unity-only | `every previous phase has a one-screenshot Unity proof in DOCS/screenshots/<phase>.png` | Captain may only write docs and pure-C# snapshot rows with cited Mami consumer. Real screenshots and scene/prefab work are Mami-owned. | Keep docs synced; do not create visual assets. | parallel, Mami-owned proof |
| Faz 12 - LLM / NPC fallback flavour | AI/DM | `player can stand in a tavern, hear three NPCs exchange context-aware lines, none of which mutate the world` | Local-first flavour proposal interfaces after DM Query API; validation prevents world mutation. | Blocked by LLM fallback fence until Faz 12 opens. | queued |

## Guardrail notes

- Active implementation starts at Faz 5 because `origin/main` contains Faz 4 through PR #131; PRs #132-#135 remain cleanup/compat and are not used as a base for this branch.
- First Faz 5 PR is foundational but test-backed: `Assets/Tests/EditMode/Time/SeasonCalendarTests.cs` is the visible proof artifact.
- Current local backend validation on 2026-05-21: `dotnet test tools/validation/fallback/ValidationFallbackHarness.csproj --configuration Release --nologo` passes with 1291 passed, 0 failed, 0 skipped on .NET 8 (was 1190 on 2026-05-20; the +101 reflect the audit cleanup arc's new regression suites under `Assets/Tests/EditMode/Audit/` plus boundary tests in `LlmHttpBoundaryTests`, `LlmJsonParserHardeningTests`, `TradeServiceMissingPriceTests`, and `ConsultFateServiceTests`). Unity editor remains outside this fallback proof.
- Carry-over Faz 3 pathing debt remains relevant to Faz 5 farming jobs. This branch defers `CO-03` to the Faz 5 path/farming hook atom because SeasonCalendar does not depend on actor movement.
- This ledger has been superseded by backend reality-fix work: `SliceWorldState` now carries saveable Faz 6-12 backend rows, legacy spell effects use `SpellEffectCode`, and LLM clients are explicit-config surfaces with cloud disabled unless configured. Unity scenes, art, prefabs, screenshots, and binary files remain out of scope.

## Commands expected for each Captain atom

1. `git diff --check`
2. `./tools/validation/run-validation.sh --mode fallback`
3. PR body includes the Rule 9 audit block and points to this ledger or the active Faz atom map.
