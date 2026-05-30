# Faz 10 - Atom map (DM Query API)

_Date:_ 2026-05-20
_Branch:_ `mami/faz-6-12-atom-maps`
_Primary boxes:_ `AI/DM`
_Roadmap:_ `docs/ROADMAP.md`
_Mechanic map:_ `docs/mechanic-map-v1.md`
_Execution ledger:_ `docs/faz-5-12-execution-ledger.md`
_Agent rules:_ `docs/agent-rules-v2.md`
_Vision notes:_ `docs/EMBER_VISION_NOTES_MAMI.md`
_Inspector checklist:_ `docs/inspector-audit-checklist.md`

## Vision anchors (this is the tool-calling architecture sprint)

Per `docs/EMBER_VISION_NOTES_MAMI.md` section 1.9: **tool-calling actors are NPC + party member + DM**. All three hold separate but isomorphic deterministic tool surfaces. NPCs can escalate to the DM on stale state. The DM has its own tool surface. This sprint defines those surfaces and the typed query / mutation contracts. **It does NOT call an LLM** — that ships in Faz 12.

1. Living-world over showroom: the API only reads simulation state; LLM is decorative.
7. Tool-calling interpreter: simulation exposes a typed query/mutation surface; LLM consumes it later, deterministic agents consume it now.
8. Systemic interaction: queries span actor / item / site / faction / memory / time stores; mutations route through validated commands only.

## Phase fences

- No LLM fallback wiring before Faz 12 (the **mock** LLM client lands here only for test execution; the **default** execution path remains deterministic agents).
- No procedural genesis.
- No multiverse / 100K-year / interplanetary implementation.
- No free-text dialogue parsing.

## Tool-calling architecture

Three actor classes share isomorphic surfaces (per Mami's vision addition):

```
       ┌──────────────────────────────────────────────────────────────┐
       │                       Simulation stores                       │
       │  (ActorStore, ItemStore, SiteStore, FactionStore, Memory,    │
       │   PriceLedger, JobBoard, WorldEventLog, …)                   │
       └────────────┬──────────────────┬──────────────────┬────────────┘
                    │                  │                  │
                    │ readonly query   │                  │
                    ▼                  ▼                  ▼
              ┌──────────┐       ┌──────────┐       ┌──────────┐
              │ NpcAgent │       │PartyAgent│       │ DmAgent  │
              │   tools  │       │   tools  │       │   tools  │
              └────┬─────┘       └────┬─────┘       └────┬─────┘
                   │                  │                  │
                   │ ToolCallRequest  │                  │
                   ▼                  ▼                  ▼
              ┌──────────────────────────────────────────┐
              │       ToolCallValidator + Router         │
              │  (rejects unknown / out-of-fence / OOB)  │
              └──────────────┬───────────────────────────┘
                             │ validated mutation
                             ▼
                ┌────────────────────────────┐
                │  CommandService(...stores) │
                │  emits WorldEventLog rows  │
                └────────────────────────────┘
```

NPC → DM escalation path: if an `NpcAgent` returns `EscalateToDm` because its local memory + topic surface cannot answer, the DM is invoked with a `DmQueryRequest` carrying the full traced context.

## Debt ledger action

Faz 10 does not consume `CO-02 / CO-03 / CO-06 / CO-07`. These carry forward.

## Sprint goal

Target acceptance sentence (refined for tool-calling):

`player can press F9, see the same world snapshot the DM has, including memory facts and faction state, and call ToolCall.ListAvailable to enumerate the deterministic tools the DM may invoke`.

## Atomic decomposition

| Atom | Primary box | File / class | Responsibility | Closing proof | Status |
|---:|---|---|---|---|---|
| 1 | AI/DM | `Assets/Scripts/Domain/AiDm/ToolId.cs` | Stable string id for a tool. | `ToolIdTests` | queued |
| 2 | AI/DM | `Assets/Scripts/Domain/AiDm/ToolSurfaceKind.cs` | Stable string surface kind (`npc`, `party`, `dm`). | `ToolSurfaceKindTests` | queued |
| 3 | AI/DM | `Assets/Scripts/Domain/AiDm/ToolDescriptor.cs` | Data row: tool id, surface kind, input schema (typed params), output schema, side-effect class (`read | mutate`). | `ToolDescriptorTests` | queued |
| 4 | AI/DM | `Assets/Scripts/Domain/AiDm/ToolCallEnvelope.cs` (folds ToolCallRequest + ToolCallResult + ToolCallRejection) | Immutable typed request and result envelopes. | `ToolCallEnvelopeTests` | landed |
| 5 | AI/DM | `Assets/Scripts/Simulation/AiDm/ToolRegistry.cs` | Loads `ToolDescriptor` rows per surface kind; deterministic lookup. | `ToolRegistryTests` | queued |
| 6 | AI/DM | `Assets/Scripts/Simulation/AiDm/ToolCallValidator.cs` | Reject unknown tool, wrong surface, out-of-fence (e.g., Memory before Faz 9 already shipped, so OK now), bad schema. | `ToolCallValidatorTests` | queued |
| 7 | AI/DM | `Assets/Scripts/Simulation/AiDm/ToolCallRouter.cs` | Route validated calls to deterministic command/query services; emit `ToolInvoked` to WorldEventLog. | `ToolCallRouterTests` | queued |
| 8 | AI/DM | `Assets/Scripts/Simulation/AiDm/NpcAgentToolSurface.cs` | Concrete NPC surface: `ask_about`, `remember`, `query_relation`, `escalate_to_dm`. | `NpcAgentToolSurfaceTests` | queued |
| 9 | AI/DM | `Assets/Scripts/Simulation/AiDm/PartyAgentToolSurface.cs` | Concrete party member surface: `request_item`, `report_status`, `vote_on_action`. | `PartyAgentToolSurfaceTests` | queued |
| 10 | AI/DM | `Assets/Scripts/Simulation/AiDm/DmAgentToolSurface.cs` | Concrete DM surface: `query_world_snapshot`, `propose_event`, `consult_fate`, `escalate_resolve_or_pass`. | `DmAgentToolSurfaceTests` | queued |
| 11 | AI/DM | `Assets/Scripts/Simulation/AiDm/DmAgentEscalationService.cs` | NPC `escalate_to_dm` lands here; DM runs its own tool surface to draft a response and returns to NPC. | `DmEscalationServiceTests` | queued |
| 12 | AI/DM | `Assets/Scripts/Simulation/AiDm/MockLlmClient.cs` | Deterministic in-process mock LLM client used **only by tests**; never wired as default execution path. | `MockLlmClientTests` | queued |
| 13 | AI/DM | `Assets/Scripts/Simulation/AiDm/ToolCallTracer.cs` | Append every validated call + result to `WorldEventLog` with a `ReasonTrace` chain (player_intent → tool_call → mutation). | `ToolCallTracerTests` | queued |
| 14 | TIME | `Assets/Scripts/Data/Save` ToolCall log save mapper | Round-trip the tool-invocation history per session. | `ToolCallLogRoundTripTests` | queued |
| 15 | AI/DM | `Assets/Tests/EditMode/AiDm/FazTenDmQueryAcceptanceTests.cs` | Deterministic replay: NPC fails on local memory → `escalate_to_dm` → DM `query_world_snapshot` → DM responds → NPC delivers reply; full trace pinned. | acceptance replay note | queued |

## Suggested bundles

1. `tool-primitives` — Atoms 1, 2, 3, 4.
2. `tool-registry-validator-router` — Atoms 5, 6, 7.
3. `npc-and-party-surfaces` — Atoms 8, 9.
4. `dm-surface-and-escalation` — Atoms 10, 11.
5. `mock-llm-and-tracer` — Atoms 12, 13.
6. `dm-query-save-and-acceptance` — Atoms 14, 15.

## Promotion checklist

- [ ] Every Faz 10 atom row above is checked off.
- [ ] No `using` of any real LLM client (e.g. `Anthropic.*`, `OpenAI.*`) anywhere outside the Faz 12 sandboxed path.
- [ ] Mock LLM client is only referenced by tests.
- [ ] `./tools/validation/run-validation.sh --mode fallback` passes on the promotion branch.
- [ ] Product-visible PR count is greater than zero (NPC→DM escalation replay).
- [ ] Faz 10 promotion summary reports Debt ledger status.

## Next increment

Implement the `tool-primitives` bundle first: Atoms 1, 2, 3, 4. Keep schemas typed (no `Dictionary<string, object>` payloads).
