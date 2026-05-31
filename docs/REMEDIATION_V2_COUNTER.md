# Remediation V2 Counter (Current)

_Last updated: 2026-05-31_

This is the active remediation register. It intentionally stays short; detailed
historical audit narrative lives in `docs/Audit.md`.

## Status summary

- Closed now (this pass): `LEFT-01`, `LEFT-02`, `LEFT-03`, `LEFT-04`,
  `LEFT-05`, `LEFT-08`, `LEFT-10`, `LEFT-19`, `LEFT-20`
- Open/staged: `LEFT-06`, `LEFT-07`, `LEFT-09`, `LEFT-11` to `LEFT-17`,
  `LEFT-21`

## Register

| ID | Priority | Status | Scope |
| --- | --- | --- | --- |
| LEFT-01 | P0 | ✅ closed | Canonical docs read-order, `EMBER_GOAL` redirect, concise state snapshot |
| LEFT-02 | P0 | ✅ closed | Runtime proof boundary clarified (`source-only` vs `LFS-runtime`) |
| LEFT-03 | P0/P1 | ✅ closed | Static audit adds runtime visual pointer gate |
| LEFT-04 | P1 | ✅ closed | Proof labeling policy normalized in docs |
| LEFT-05 | P1 | ✅ closed | CI exposes manual runtime-LFS proof gate |
| LEFT-06 | P1 | ⏳ staged | Save/load multi-slot envelope + migration fixtures |
| LEFT-07 | P1/P2 | ⏳ staged | Authored scene actor-id migration (Unity editor migration required) |
| LEFT-08 | P2 | ✅ closed | Conversation state carries ActorId/NpcId; display name is UI-only/legacy fallback |
| LEFT-09 | P1/P2 | ⏳ staged | 13-scene runtime tour proof (PlayMode/manual) |
| LEFT-10 | P2 | ✅ closed/documented exception | Simulation dependency boundary (`Data.SliceJson`) documented in `Assets/Scripts/Simulation/EmberCrpg.Simulation.asmdef.README.md` |
| LEFT-11 | P2 | ⏳ staged | `DomainSimulationAdapter` and `EmberWorldHost` split |
| LEFT-12 | P2/P3 | ⏳ staged | Character/UI controller decomposition |
| LEFT-13 | P2 | ⏳ staged | LLM provider async/cancellation hardening |
| LEFT-14 | P2 | ⏳ staged | Explicit model-download opt-in flow with progress/cancel |
| LEFT-15 | P2 | ⏳ staged | ONNX provider placement boundary cleanup |
| LEFT-16 | P3 | ⏳ staged | Generated actor runtime screenshot/interaction proof |
| LEFT-17 | P3 | ✅ closed | Faction decay is wired/tested; tick composition now runs through a deterministic registry guarded by a whole-world digest baseline |
| LEFT-18 | P3 | ⏳ staged | Legacy input facade migration to action maps |
| LEFT-19 | P4 | ✅ closed | PRD matrix normalized to `active` vs `reference` |
| LEFT-20 | P4 | ✅ closed | Stale doc/path references removed or re-labeled |
| LEFT-21 | P5 | ⏳ staged | `Resources.Load` footprint reduction/registry path |

## Validation contract

```bash
# source-only gate
bash tools/validation/static-audit.sh

# runtime plugins/models gate
bash tools/validation/static-audit.sh --require-runtime

# runtime visuals gate
bash tools/validation/static-audit.sh --require-runtime --require-runtime-visual
```

Runtime behavior claims require Unity runtime evidence.

---

## §11 — E7 RE-AUDIT RECONCILIATION (7th audit, 2026-05-31 pm)

The 7th audit's own headline: **stop broad audits; the remainder is acceptance GATES + targeted fixes,
not another sweep.** Its 26 items (E7-001..026) reconciled below. The auditor ran a **source-only ZIP**
(no `git lfs pull`), so its "908 LFS pointers / runtime FAIL" is a ZIP artifact — verified **false in
this workspace** (GGUF 986 MB real, 9 DLLs 19.8 MB real, art PNGs 400-518 KB real, **0 pointer stubs**).
Box: `[x]` done+verified · `[~]` partial/staged (reasoned) · `[-]` won't (reasoned) · `[E]` needs Editor/PlayMode gate.

| ID | Item | Status | Resolution |
| --- | --- | --- | --- |
| E7-001 | Runtime proof (LFS model/plugins) | `[x]` | Real bytes present locally (986 MB GGUF + 9 DLLs); 0 pointer stubs; earlier headless LLM proof showed real Qwen inference (`IsAvailable: True`, `RESULT OK`). The ZIP FAIL was LFS-not-pulled. |
| E7-002 | Visual proof (art PNGs) | `[x]`/`[E]` | All sampled art PNGs are real bytes (0 pointer stubs locally). Visual *rendering* proof still needs a screen run (E7-009). |
| E7-003 | Proof/report ambiguity | `[~]` | `docs/CURRENT_STATE.md` now carries explicit mode labels (source-only / LFS-runtime / PlayMode / manual / historical); `Reports/**` are evidence-only, not source-truth (documented in §3 source map). |
| E7-004 | Scene-authored actor IDs | `[E]` next-P1 | ActorView carries `_domainActorKey` but empty `_domainActorId`; runtime bridges key→domain-actor→id. A correct fix needs a deterministic stable-hash `ActorId` stamped at author-time AND matched at runtime actor-creation — a real identity-scheme change, Editor-gated. Stamping an arbitrary id would break dialog. Recorded as the next P1 (design: deterministic key→ActorId; Editor migration + SceneValidation gate). |
| E7-005 | Dialog identity name fallback | `[x]` | Verified **id-primary already**: `SelectTopic`/`BeginConversation` resolve via `_conversation.ActorId`/`NpcId` first; the name match is an explicit `??=` LEGACY fallback (now commented) that only fires for pre-E7-004 authored actors without ids — eliminated once E7-004 lands. |
| E7-006 | Interaction identity | `[E]` | Raycaster already prefers `TryInteract(ActorId)` when present; the name path only runs for id-less authored actors → resolved by E7-004. |
| E7-007 | Save architecture (slots/migration) | `[~]` | File slot + corrupt-quarantine + unified Continue/Load path + (new E7-008) scene validation done. Multi-slot UI + typed envelope + replay digest are staged (need PlayMode). |
| E7-008 | Save validation gap | `[x]` | `TryResolveLatestSave` now validates the save's scene via `IsKnownBuildScene` at the single resolution point, so NO load entry point can hand a bogus scene to `LoadScene`. |
| E7-009 | Full 13-scene PlayMode tour | `[x]` load-gate | **Scene-tour PASS** (headless 2026-05-31): all 10 gameplay scenes load + render, **0 exceptions / 0 null-refs**, 20 screenshots on disk + `[UrpMaterialRescue]` auto-repaired magenta per scene (`docs/proofs/scene-tour-2026-05-31.md`). Interactive movement/dialog/portal-traversal/save still `[E]` for a human/PlayMode pass. |
| E7-010 | Scene static-limit (camera/collision) | `[E]` | Only provable in Editor/PlayMode (SceneValidationMenu + manual). Gate, not a code fix. |
| E7-011 | Portal target strings | `[x]` | `EmberScenePortal.Activate` validates the target via `CanStreamedLevelBeLoaded` (LEFT-014). |
| E7-012 | Adapter god object (1825 agg) | `[~]` | `.Dialog.cs` extracted (REF-a, 787→365 + 448); read-model/command/save/worldgen/combat are already separate partials. Further collaborator extraction (WorldHydrator, ILlmRouter DI) staged. |
| E7-013 | Host god object (801) | `[x]` | `EmberWorldHost.Ui.cs` extracted (REF-3, 786→530 + 271). |
| E7-014 | CharCreation god controller (1420) | `[~]` | Mechanical partial split in progress (this session). |
| E7-015 | LLM blocking calls (6 sites) | `[~]` | All off the default UI hot path (CloudLlm/ComfyUI opt-in-disabled; `NativeLlmClient.Complete` already wrapped in `Task.Run` by the adapter). Async-layer refactor staged. |
| E7-016 | Model download policy | `[x]` | `EnsureModelReady` now gates the multi-GB fetch behind explicit opt-in (`EMBER_ALLOW_MODEL_DOWNLOAD=1`); default = no silent download, fallback answers. |
| E7-017 | Forge provider boundary | `[~]` | `OnnxAssetForge` in Simulation — documented-exception; moving the impl to Infrastructure is a staged asmdef change. |
| E7-018 | Tick data-driven gap | `[x]` | Whole-world digest baseline captured, then `WorldTickComposer` moved to a deterministic `(cadence, order, id)` registry; fallback `1248/1251` proves digest unchanged. |
| E7-019 | Faction politics (decay) | `[x]` | Deterministic faction-reputation decay wired as last daily tick; fallback `1242/1245` proves unit, convergence, catch-up, and save/load replay tests. |
| E7-020 | Input System migration | `[~]` | `EmberInput` is a verified single choke point; migration = add package + InputActions + swap internals behind the frozen facade + rebind UI, gated on a PlayMode baseline. Staged plan in §6 BD-19. |
| E7-021 | Resources.Load footprint | `[~]` | Keep tiny global fallbacks (fonts/theme); move new UI assets to explicit refs. Staged inventory. |
| E7-022 | UI Foundation boundary | `[-]` | `noEngineReferences:false` is honest — `IUiPanel`/`UiTokens` legitimately use `RectTransform`/`Color`; "Foundation" = the shared UI-contract layer. Reasoned won't-rename (BD-16). |
| E7-023 | Agent rules stale/conflicting | `[x]` | `docs/agent-rules-v2.md` + `inspector-audit-checklist.md` banner-marked **LEGACY / NOT ACTIVE** (they reference a dead path + hard-fail Presentation touches). |
| E7-024 | SliceJson README inconsistency | `[x]` | README corrected: the sub-asmdef is **Domain + Data, `noEngineReferences=true`** (engine-free mapper), NOT "Domain + Simulation + UnityEngine"; the JsonUtility bridge lives in Presentation. |
| E7-025 | Hidden NuGet marker | `[~]` | `Assets/Plugins/NuGet/.nuget-installed.json` is a NuGetForUnity restore marker; Unity ignores dot-prefixed files (no meta needed). Documented as intentional; low noise. |
| E7-026 | Obsolete role shims | `[-]` | The 5 `[Obsolete]` WorldState shim accesses are intentional backward-compat tests under `#pragma warning disable`; remove only after all call sites + save fixtures migrate (BD-02 / LEFT-024). |

**E7 tally:** 26 → **9 closed `[x]`** (001/002/005/008/011/013/016/023/024) · **~12 `[~]` reasoned-staged** ·
**2 `[-]` won't** (022/026) · **remainder `[E]`** = the Editor/PlayMode acceptance gates (004/006/009/010 +
the visual half of 002) — which the audit itself says are the *right* next step, not more broad auditing.
