# Faz 12 - Atom map (LLM / NPC flavour + DM Consult Fate)

_Date:_ 2026-05-20
_Branch:_ `mami/faz-6-12-atom-maps`
_Primary boxes:_ `AI/DM`
_Roadmap:_ `docs/ROADMAP.md`
_Mechanic map:_ `docs/mechanic-map-v1.md`
_Execution ledger:_ `docs/faz-5-12-execution-ledger.md`
_Agent rules:_ `docs/agent-rules-v2.md`
_Vision notes:_ `docs/EMBER_VISION_NOTES_MAMI.md`
_Inspector checklist:_ `docs/inspector-audit-checklist.md`

## Vision anchors

This is the **last** sprint. It wires the LLM in **for flavour only** — narration, ambient barks, NPC speech texture — never for world mutation. The DM "Consult Fate" mechanism activates here, layered on Faz 10's deterministic tool surface.

1. Living-world over showroom: even without an LLM, the simulation runs end-to-end. LLM is decorative.
4. Morrowind-shaped immersion: NPC voice and DM narration finally surface as text the player reads.
7. Tool-calling interpreter: LLM proposals are validated through Faz 10 `ToolCallValidator`; bad proposals are rejected, never executed.
8. Systemic interaction: LLM reads simulation state through the same surfaces NPC and party agents use.

## Hard fences

- LLM **never** writes the world directly. Every proposal goes through `ToolCallValidator` + `CommandService`.
- Local-first: Qwen3:1.7B on Ollama is the default. Cloud fallback (Copilot / Claude / OpenAI) only when the local probe fails for the same packet.
- Cost ceiling: a per-tick budget caps flavour calls. Exceeded budget = silent fallback to templated text from Faz 9.
- Determinism preserved: every LLM proposal is logged with input + output + accepted/rejected verdict + the deterministic template that fired if the proposal was rejected.

## Debt ledger action

Faz 12 does not consume `CO-02 / CO-03 / CO-06 / CO-07` directly. These should be closed long before Faz 12 promotion.

## Sprint goal

Target acceptance sentence from `docs/ROADMAP.md`:

`player can stand in a tavern, hear three NPCs exchange context-aware lines, none of which mutate the world`.

Plus: `player can press the DM key and Consult Fate; the DM proposes one of three deterministic outcome buckets, surfaces narration text, and applies the outcome through validated tool calls`.

## Atomic decomposition

| Atom | Primary box | File / class | Responsibility | Closing proof | Status |
|---:|---|---|---|---|---|
| 1 | AI/DM | `Assets/Scripts/Domain/AiDm/LlmProviderKind.cs` | Stable string provider kind (`local_qwen`, `cloud_anthropic`, `cloud_openai`, `mock`). | `LlmProviderKindTests` | queued |
| 2 | AI/DM | `Assets/Scripts/Domain/AiDm/LlmEnvelope.cs` (folds LlmRequest + LlmResponse types) | Immutable typed envelopes: input is `{system_prompt_id, conversation_id, tool_descriptors, max_tokens, seed}`; output is `{text, proposed_tool_calls, tokens_used}`. | `LlmEnvelopeTests` | landed |
| 3 | AI/DM | `Assets/Scripts/Simulation/AiDm/LlmClients.cs` (folds LocalQwenClient + CloudLlmClient + LlmClientConfig + LlmHttpClientCore) | HTTP client to local Ollama; deterministic seed; timeout fallback. | `LlmHttpBoundaryTests` | landed |
| 4 | AI/DM | `Assets/Scripts/Simulation/AiDm/LlmClients.cs` (folds LocalQwenClient + CloudLlmClient) | HTTP client to one cloud provider; same envelope; circuit breaker on quota. | `CloudLlmClientTests` | queued |
| 5 | AI/DM | `Assets/Scripts/Simulation/AiDm/LlmRoutingService.cs` | Try local first, fall back to cloud only on local failure for same packet. | `LlmRoutingServiceTests` | landed |
| 6 | AI/DM | `Assets/Scripts/Simulation/AiDm/LlmProposalValidator.cs` | Take `LlmResponse.proposed_tool_calls`, route each through Faz 10 `ToolCallValidator`. Reject any that fence-violate. | `LlmProposalValidatorTests` | landed |
| 7 | AI/DM | `Assets/Scripts/Simulation/AiDm/FlavourBudget.cs` | Per-tick budget counter; rejects calls past the cap; emits `LlmBudgetExceeded`. | `FlavourBudgetTests` | landed |
| 8 | AI/DM | `Assets/Scripts/Simulation/AiDm/NarrationServices.cs` (NpcFlavourService type) | NPC ambient bark generation. Falls back to Faz 9 `DialogueTemplate` if LLM rejected or over budget. | `NpcFlavourServiceTests` | landed |
| 9 | AI/DM | `Assets/Scripts/Simulation/AiDm/NarrationServices.cs` (DmNarrationService type) | DM narration of accepted tool-call outcomes. Falls back to templated narration. | `DmNarrationServiceTests` | landed |
| 10 | AI/DM | `Assets/Scripts/Domain/AiDm/ConsultFateOutcomeBucket.cs` | Stable string outcome bucket id (`favourable`, `neutral`, `setback`). Data-driven. | `ConsultFateOutcomeBucketTests` | landed |
| 11 | AI/DM | `Assets/Scripts/Simulation/AiDm/NarrationServices.cs` (folds NpcFlavourService + DmNarrationService + ConsultFateService + StorytellerCheckpointSystem) | Player presses DM key → service selects bucket from deterministic seeded roll → DM narrates → tool calls validated → outcomes applied. | `ConsultFateServiceTests` | landed |
| 12 | AI/DM | `Assets/Scripts/Simulation/AiDm/NarrationServices.cs` (StorytellerCheckpointSystem type) | RimWorld-lineage condition checkpoints: prosperity / disaster / opportunity beats triggered when sim metrics cross thresholds. | `StorytellerCheckpointSystemTests` | landed |
| 13 | TIME | `Assets/Scripts/Data/Save` LLM proposal log save mapper | Round-trip the LLM proposal/validation log per session for replay forensics. | `LlmProposalLogRoundTripTests` | queued |
| 14 | AI/DM | `Assets/Tests/EditMode/AiDm/FazTwelveLlmFlavourAcceptanceTests.cs` | Deterministic replay with the **mock** LLM client: three NPCs in a tavern emit lines, no world mutation in WorldEventLog. | acceptance replay note | queued |
| 15 | AI/DM | `Assets/Tests/EditMode/AiDm/FazTwelveConsultFateAcceptanceTests.cs` | Deterministic replay: Consult Fate fires, validator accepts proposal in one bucket, applies one tool call, narration emitted. | acceptance replay note | queued |

## Suggested bundles

1. `llm-envelopes` — Atoms 1, 2.
2. `llm-clients` — Atoms 3, 4.
3. `llm-routing-validator-budget` — Atoms 5, 6, 7.
4. `npc-flavour-and-dm-narration` — Atoms 8, 9.
5. `consult-fate-and-storyteller` — Atoms 10, 11, 12.
6. `llm-save-and-acceptances` — Atoms 13, 14, 15.

## Promotion checklist

- [ ] Every Faz 12 atom row above is checked off.
- [ ] No LLM proposal ever mutates the world without passing `LlmProposalValidator` → `ToolCallValidator` → `CommandService`.
- [ ] Local Qwen is the default in development config; cloud is gated behind explicit env flag.
- [ ] Flavour budget enforced in tests.
- [ ] `./tools/validation/run-validation.sh --mode fallback` passes on the promotion branch.
- [ ] Product-visible PR count is greater than zero (tavern barks replay, Consult Fate replay).
- [ ] Faz 12 promotion summary closes every prior Debt ledger row or migrates them to a post-Faz-12 backlog.

## Next increment

Implement the `llm-envelopes` bundle first: Atoms 1, 2 with their tests. Wire nothing else until the typed envelope shape is pinned.

## After Faz 12

Game is feature-complete by the 12-phase plan. Backlog drains:
- Any remaining Debt ledger rows.
- Frontend polish (Mami territory).
- Procedural genesis (post-roadmap; new sprint after explicit go-ahead).
- Multiverse / interplanetary (post-roadmap; explicit go-ahead).
