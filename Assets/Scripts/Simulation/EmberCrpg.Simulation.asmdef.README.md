# EmberCrpg.Simulation — asmdef reference exception

Tracker: `docs/REMEDIATION_V2_COUNTER.md` marks `LEFT-10` closed as a documented
exception. Re-open it only if Simulation gains a Data reference outside
save-rehydration.

## Why this asmdef references `EmberCrpg.Data` + `EmberCrpg.Data.SliceJson`

The Simulation asmdef points *outward* at Data, which is architecturally
unusual (the normal layering is `Data -> Simulation -> Domain`). This is a
documented exception, not an oversight.

### Root cause

`WorldSaveRehydration` (under `Assets/Scripts/Simulation/Process/`) needs
to hydrate the runtime simulation world from the persisted **`Data.Save` DTO
types** that live in `Assets/Scripts/Data/Save/SliceJson/`. The rehydration
code translates DTOs back into the in-memory `WorldState` shape that the
tick composer operates on.

### Why we didn't invert the dependency

A "clean" inversion would require moving the save DTOs into `EmberCrpg.Domain`
so that Simulation references Domain (which it already does) instead of Data.
That move is rejected because:

1. `EmberCrpg.Domain` has `noEngineReferences: true` *and* must remain free of
   serialization plumbing (it holds rules + ports, not Unity/JSON contracts).
2. Save DTOs intentionally live next to the JSON readers/writers in
   `Data.SliceJson` so the serialization shape and the on-disk shape stay
   colocated.
3. Moving the DTOs would either pollute Domain with `[Serializable]` /
   `JsonUtility`-shaped data, or it would push the DTOs into a third asmdef
   that both Data and Simulation would need to depend on — a strict cost
   increase for zero behavioural gain.

### Invariant the team enforces in code review

`EmberCrpg.Simulation` is allowed to reference `EmberCrpg.Data` /
`EmberCrpg.Data.SliceJson` **only** for save-rehydration purposes. Any new
forward reference from Simulation into Data for runtime logic (not
rehydration) is a layering violation and must be rejected.

### Audit history

- Eighth-pass audit flagged this as a layering smell (raised again in the
  ninth pass as B-P2).
- Codex offered two remediations: "move save composition outward" or
  "document the exception". This file is the chosen "document" path.
- EMB-AUD-015/016 (2026-05-31) re-raised the same smell a third time. Re-investigated:
  the sole offender is still `WorldSaveRehydration` (one file, save-rehydration only),
  and the dangerous inverse (`Data.SliceJson -> Simulation`) was already removed. The
  only zero-exception fix is a new `SaveContracts` asmdef of pure DTOs — a strict cost
  increase for zero behavioural gain. **Closed as won't-fix / documented exception.**
