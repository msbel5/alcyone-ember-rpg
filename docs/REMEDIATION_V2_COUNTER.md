# Remediation V2 Counter (Current)

_Last updated: 2026-05-31_

This is the active remediation register. It intentionally stays short; detailed
historical audit narrative lives in `docs/Audit.md`.

## Status summary

- Closed now (this pass): `LEFT-01`, `LEFT-02`, `LEFT-03`, `LEFT-04`,
  `LEFT-05`, `LEFT-08`, `LEFT-10`, `LEFT-19`, `LEFT-20`
- Open/staged: `LEFT-06`, `LEFT-07`, `LEFT-09`, `LEFT-11` to `LEFT-18`,
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
| LEFT-17 | P3 | ⏳ staged | WorldTickComposer data-driven catalog extraction |
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
