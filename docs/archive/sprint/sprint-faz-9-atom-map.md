# Faz 9 - Atom map (Dialogue + Memory + Faction)

_Date:_ 2026-05-20
_Branch:_ `mami/faz-6-12-atom-maps`
_Primary boxes:_ `CRPG`, `AI/DM`
_Roadmap:_ `docs/ROADMAP.md`
_Mechanic map:_ `docs/mechanic-map-v1.md`
_Execution ledger:_ `docs/faz-5-12-execution-ledger.md`
_Agent rules:_ `docs/agent-rules-v2.md`
_Vision notes:_ `docs/EMBER_VISION_NOTES_MAMI.md`
_Inspector checklist:_ `docs/inspector-audit-checklist.md`

## Vision anchors

This is the vision-critical sprint: it lights up the Fallout 1 "Ask About / Tell Me About" loop and the Hitchhiker's Guide text-adventure feel through deterministic data, not LLM hand-waving.

1. Living-world over showroom: NPCs remember and react across sites and days.
3. Fallout-1 dialogue freedom: topics are data rows; the player can ask about anything in the actor's known topic set, with deterministic fallback for unknown topics.
4. Morrowind-shaped immersion: NPC answers blend personality + culture + region + politics + economics + relations + current mood — all already represented in the stores.
5. Data-driven extension: topics, knowledge facts, and dialogue templates ship as rows.
7. Tool-calling interpreter sits ABOVE this sprint: the Faz 10 DM Query API will read these stores; Faz 9 only writes the typed retrieval surface.

## Phase fences

- No shared NPC / party / DM tool surface before Faz 10 (this sprint exposes per-actor query services only).
- No LLM fallback wiring before Faz 12 (Faz 9 NPC responses come from templated rows, not LLM completions).
- No procedural genesis.
- No multiverse / 100K-year / interplanetary implementation.
- Free-text dialogue parsing is bounded to **topic key lookup**, not natural-language understanding — that ships in Faz 12.

## CO-09 audit (mandatory)

The Sprint 1 narrative iskelet under `Assets/Scripts/Domain/Narrative/` and `Assets/Scripts/Simulation/Narrative/` MUST be audited at kickoff. For each file below the Faz 9 kickoff doc records one of `reuse / refactor / deprecate` with a one-line reason:

- `Assets/Scripts/Domain/Narrative/AskAboutTopic.cs`
- `Assets/Scripts/Simulation/Narrative/AskAboutService.cs`
- `Assets/Scripts/Simulation/Narrative/AskDmService.cs`
- `Assets/Scripts/Simulation/Narrative/NpcMemoryQueryService.cs`
- `Assets/Scripts/Simulation/Narrative/ThinkService.cs`
- `Assets/Scripts/Simulation/Narrative/GuardInteractionService.cs`

CO-09 row in the Debt ledger flips to `closed` only after this audit doc lands.

## Debt ledger action

Faz 9 closes `CO-09` via the audit above. CO-02 / CO-03 / CO-06 / CO-07 still carry forward unless an in-sprint dependency surfaces.

## Sprint goal

Target acceptance sentence from `docs/ROADMAP.md`:

`player can witness an NPC remember a crime committed two days ago and refuse to trade`.

Faz 9 makes memory and dialogue first-class:
- Actors hold a deterministic `MemoryComponent` of facts indexed by topic.
- NPCs answer asked topics from their known facts, gated by faction relation and mood.
- A crime two days ago becomes a `MemoryFact(actor_id, topic=stole_from, target_id, when)` that persists through save/load and feeds dialogue refusal.

## Atomic decomposition

| Atom | Primary box | File / class | Responsibility | Closing proof | Status |
|---:|---|---|---|---|---|
| 1 | AI/DM | `docs/kickoff-faz-9.md` | CO-09 audit: reuse/refactor/deprecate decisions for the six Sprint 1 narrative files. | doc landed | queued |
| 2 | CRPG | `Assets/Scripts/Domain/Narrative/TopicId.cs` | Stable string topic id. Replaces or extends Sprint 1 `AskAboutTopic` per CO-09 decision. | `TopicIdTests` | queued |
| 3 | CRPG | `Assets/Scripts/Domain/Narrative/TopicDef.cs` | Data row: topic id, prompt phrasing, gating predicate id, default answer template id. | `TopicDefTests` | queued |
| 4 | LIVING | `Assets/Scripts/Domain/Memory/MemoryFact.cs` | Immutable fact: `(actorId subject, TopicId topic, ActorId object, GameTime when, string detail)`. | `MemoryFactTests` | queued |
| 5 | LIVING | `Assets/Scripts/Domain/Memory/MemoryComponent.cs` | Actor-local indexed set of `MemoryFact`s with `Add`, `Forget(after)`, `Query(topic)`, deterministic enumeration. | `MemoryComponentTests` | queued |
| 6 | LIVING | `Assets/Scripts/Domain/Actors/ActorRecord.cs` :: extensions | `Memory` accessor + `ApplyMemory` immutable update routed through ActorStore. | `ActorRecordMemoryTests` | queued |
| 7 | LIVING | `Assets/Scripts/Simulation/Memory/MemoryRecallService.cs` | Recall facts by topic + relevance threshold; deterministic ordering. | `MemoryRecallServiceTests` | queued |
| 8 | AI/DM | `Assets/Scripts/Simulation/Narrative/NpcDialogueService.cs` | Given asker + askee + TopicId, return a typed `DialogueResponse(text, refusal_reason?)`. Pulls from memory + faction + mood. | `NpcDialogueServiceTests` | queued |
| 9 | AI/DM | `Assets/Scripts/Domain/Narrative/DialogueTemplate.cs`, `DialogueTemplateRegistry.cs` | Data row templates with placeholders (`{topic}`, `{subject}`, `{relation}`); deterministic substitution. | `DialogueTemplateTests` | queued |
| 10 | LIVING | `Assets/Scripts/Simulation/Memory/MemoryWriteSystem.cs` | Subscribe to `CrimeCommitted` / `TradeCompleted` / `CombatResolved` events; write memory facts to subject actors. | `MemoryWriteSystemTests` | queued |
| 11 | SOCIETY | `Assets/Scripts/Simulation/Narrative/TradeRefusalHook.cs` | When `TradeService.TryTrade` runs, query memory + faction; emit `TradeRefused(reason=memory)` if a recent crime fact exists. | `TradeRefusalHookTests` | queued |
| 12 | TIME | `Assets/Scripts/Data/Save` memory/topic/template save mappers | Round-trip MemoryComponent, TopicDef registry, DialogueTemplateRegistry. | `MemoryDialogueRoundTripTests` | queued |
| 13 | AI/DM | `Assets/Tests/EditMode/Narrative/FazNineDialogueMemoryAcceptanceTests.cs` | Deterministic replay: actor commits theft on day 1, asks to trade on day 3, NPC refuses citing the remembered fact. | acceptance replay note | queued |

## Suggested bundles

1. `co-09-audit` — Atom 1.
2. `topic-primitives` — Atoms 2, 3.
3. `memory-primitives` — Atoms 4, 5, 6.
4. `memory-recall` — Atom 7.
5. `dialogue-service` — Atoms 8, 9.
6. `memory-writer-and-trade-refusal` — Atoms 10, 11.
7. `dialogue-memory-save-and-acceptance` — Atoms 12, 13.

## Promotion checklist

- [ ] Every Faz 9 atom row above is checked off.
- [ ] CO-09 audit doc landed (Atom 1) → `CO-09` flipped to `closed` in the Debt ledger.
- [ ] No new enum branches; topics + templates ship as data rows.
- [ ] `./tools/validation/run-validation.sh --mode fallback` passes on the promotion branch.
- [ ] Product-visible PR count is greater than zero (dialogue/refusal replay or equivalent).
- [ ] Faz 9 promotion summary reports Debt ledger status.

## Next increment

Land Atom 1 first: the CO-09 audit kickoff doc that decides reuse/refactor/deprecate for the six Sprint 1 narrative files. No other Faz 9 atom merges until that doc is on `main`.
