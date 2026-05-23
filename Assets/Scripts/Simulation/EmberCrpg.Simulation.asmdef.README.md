# EmberCrpg.Simulation — asmdef reference exception

## Why this asmdef references `EmberCrpg.Data` + `EmberCrpg.Data.SliceJson`

The Simulation asmdef points *outward* at Data, which is architecturally
unusual (the normal layering is `Data -> Simulation -> Domain`). This is a
documented exception, not an oversight.

### Root cause

`SliceSaveRehydration` (under `Assets/Scripts/Simulation/Composition/`) needs
to hydrate the runtime simulation world from the persisted **`Data.Save` DTO
types** that live in `Assets/Scripts/Data/Save/SliceJson/`. The rehydration
code translates DTOs back into the in-memory `SliceWorldState` shape that the
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
