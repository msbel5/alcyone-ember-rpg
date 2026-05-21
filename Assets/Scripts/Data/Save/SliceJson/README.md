# `EmberCrpg.Data.SliceJson` — presentation infrastructure

This folder lives under `Assets/Scripts/Data/` for historical / discovery
reasons, but architecturally it is **presentation infrastructure**, not Data.

Codex audit follow-up: the parent `EmberCrpg.Data` assembly remains pure; this
sub-assembly is intentionally the Unity JSON bridge.

## Why it stays under Data/

The `EmberCrpg.Data.SliceJson` asmdef is a separate compilation unit from
the parent `EmberCrpg.Data` (which IS now Domain-only + `noEngineReferences=true`).
Moving the folder physically would require updating ~6 caller asmdefs +
re-pinning a Unity GUID, which is disruptive for a non-runtime cleanup.

The asmdef name (`EmberCrpg.Data.SliceJson`) preserves backward
compatibility with every caller; the **architectural boundary it enforces**
is what matters:

- Parent `EmberCrpg.Data` asmdef: Domain-only, no engine references — pure DTOs.
- This sub-asmdef: Domain + Simulation + UnityEngine — the bridge between
  the deterministic snapshot DTOs and Unity's `JsonUtility`.

## What lives here

| File | Role |
|---|---|
| `SliceSaveMapper.cs` | Translates `SliceWorldState` ↔ `SliceSaveData` DTO graph. Needs Simulation types for `SliceWorldFactory` bootstrap + `WorksiteStore` mapping. |
| `JsonSliceSaveService.cs` | Wraps `UnityEngine.JsonUtility` around the mapper output. The single touch-point that couples save to Unity. |

## Where new save code should live

If you are adding **new save DTO rows** (pure data, no Unity/Simulation
dependency), put them in the parent `Assets/Scripts/Data/Save/` next to
`ActorSaveMapper.cs`, `ItemSaveMapper.cs`, etc. They will compile against
the engine-free `EmberCrpg.Data` asmdef.

If you are adding **new engine-coupled save logic**, add it here in
`SliceJson/` so it stays inside this sub-asmdef and does not pollute the
pure-Data boundary.
