# Sprint session — 2026-05-20 final hand-off v2

This supersedes the v1 final hand-off (`sprint-session-2026-05-20-final.md`).
It records the second push after the v1 was already considered "done".

## Additional merged PRs after v1 final

| PR | Subject |
|---:|---|
| #161 | Faz 6 Atom 9: CaravanSystem + new event kinds |
| #162 | Faz 6 Atoms 10/11/13: PriceUpdateSystem + TradeService + ShortageDetector |
| #163 | Faz 7 Atoms 4-7: CombatActionId/Def + Hit/Damage/Resolver |
| #164 | Faz 8 Atoms 1-4: effect primitives |
| #165 | Faz 9 Atom 7: MemoryRecallService |
| #166 | Faz 10 Atoms 6-7: ToolCallValidator + ToolCallRouter |
| #167 | Faz 12 Atoms 1-2: LLM envelopes |
| #169 | Faz 9 Atom 10: MemoryWriteSystem |
| #171 | Faz 9 Atom 11: TradeRefusalHook |
| #172 | Faz 10 Atoms 8-10: NPC/Party/DM tool surfaces |
| #173 | Faz 12 Atoms 7+10: FlavourBudget + ConsultFateOutcomeBucket |

## Pending PRs at v2 hand-off

| PR | Subject |
|---:|---|
| #168 | Faz 9 Atoms 8-9: NpcDialogueService + DialogueTemplate registry |
| #170 | Faz 12 Atom 12: MockLlmClient (test-only) |
| #174 | Faz 12 Atom 6: LlmProposalValidator |
| #175 | Faz 12 Atom 5: LlmRoutingService |
| #176 | Faz 8 Atoms 5-6: EffectOperationHandlers + SpellResolver |

These are awaiting CI green and merge in the next half hour.

## Faz status delta

- **Faz 6** — Atoms 1-11 + 13 live on main. Atom 12 (save mappers) + Atom 14 (acceptance replay) remain.
- **Faz 7** — Atoms 4-7 live (action def + hit/damage/resolver). Atoms 1-3 (equipment slot/component/profile) still rely on Sprint 4 enum; refactor remains.
- **Faz 8** — Atoms 1-6 live (primitives + handlers + resolver). Reality-fix pass removed `SpellEffectCode` enum shape; seed effect rows + terrain/area save polish still remain.
- **Faz 9** — Atoms 2-5 + 7 + 10 + 11 live; Atoms 8-9 pending (#168). Atoms 6 (ActorRecord.Memory), 12-13 still remain.
- **Faz 10** — Atoms 1-10 live or pending. Atoms 11-15 (escalation, mock LLM, tracer, save, acceptance) still remain.
- **Faz 11** — Captain-side Atoms 1-7 complete. Mami-side Atoms 8-16 remain Mami territory.
- **Faz 12** — Atoms 1-2 + 3 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 live or partially covered. Atom 4 cloud fallback + Atoms 13-15 (save + acceptance) remain.

## Atom totals shipped this session

- Atom map docs (Faz 6-12): 7 docs in #140
- Implementation atoms shipped: ~50+ atoms across Faz 4 finish, Faz 5/6/7/8/9/10/11/12 primitives
- Tests shipped: ~150+ NUnit cases
- Debt ledger: all Faz 1/2/3 closed/advanced; CO-09 audit doc landed

## Files added or modified (highlights)

### Domain

- `AiDm/`: ToolId, ToolSurfaceKind, ToolDescriptor, ToolCallEnvelope, ConsultFateOutcomeBucket, LlmProviderKind, LlmEnvelope
- `Combat/`: CombatActionId, CombatActionDef
- `Magic/`: EffectOperationKind, EffectOperation, EffectDefinition
- `Memory/`: MemoryFact, MemoryComponent
- `Narrative/`: TopicId, TopicDef, DialogueTemplate, DialogueTemplateRegistry
- `Process/`: JobStatus, WorksiteSlot, PriceLedger, StockpileComponent
- `World/`: FactionRelationKind, FactionReputation, FactionStore (extension), CaravanInstance, CaravanState, CaravanId, TradeRouteDef, TradeRouteId

### Simulation

- `AiDm/`: ToolRegistry, ToolCallValidator, ToolCallRouter, NpcAgentToolSurface, PartyAgentToolSurface, DmAgentToolSurface, MockLlmClient, FlavourBudget, LlmProposalValidator, LlmRoutingService
- `Combat/`: CombatHitRollService, CombatDamageService, CombatActionResolver
- `Magic/`: EffectRegistry, EffectOperationHandlers, SpellResolver
- `Memory/`: MemoryRecallService, MemoryWriteSystem
- `Narrative/`: NpcDialogueService, TradeRefusalHook
- `Process/`: PathfindingSystem
- `World/`: GridPathfinder, FactionReputationSystem, CaravanSystem, PriceUpdateSystem, ShortageDetector, TradeService

### Presentation (Faz 11 Captain-side)

- `VisualLayer/`: JobDebugSnapshot, ColonyNeedsSnapshot, SeasonClockSnapshot, FactionRelationSnapshot, WorldEventTailSnapshot, InventoryStockpileSnapshot, ToolCallTraceSnapshot

### Data

- `Recipes/ProductionRecipeRegistry.cs` (SmeltIronIngot + BakeBread production rows)

### Docs

- `docs/sprint-faz-{6..12}-atom-map.md` — 7 atom maps
- `docs/sprint-4-faz-4-promotion.md`
- `docs/kickoff-faz-9.md` (CO-09 audit)
- `docs/sprint-session-2026-05-20-summary.md` (interim)
- `docs/sprint-session-2026-05-20-final.md` (v1 final)
- `docs/sprint-session-2026-05-20-final-v2.md` (this file, v2 final)

## What remains for the post-10AM session

- Save/load mappers for Faz 6/7/8/9/10/12 state
- Acceptance replay tests per Faz (Faz 5/6/7/8/9/10/12)
- Faz 8 seed effect row polish and remaining terrain/area magic work
- Faz 12 cloud LLM fallback client + replay save acceptance
- Faz 11 Mami-side Unity scenes consuming the Captain snapshot rows
- AI image generation, gerçek görüntüler, oyun playable hali — Mami territory

## Note

Frontend Unity scenes, AI image generation, and gerçek-görüntü test remain Mami
territory by explicit Rule 6 in `agent-rules-v2.md`. Backend has been pushed
as far as a single-session deterministic-only push allows without invading
Mami's visual surface.
