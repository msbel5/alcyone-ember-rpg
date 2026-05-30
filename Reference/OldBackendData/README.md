# Reference / OldBackendData — IMPORT & REFERENCE ONLY

> Audit item EMB-049. This folder holds **48 JSON data files from the old Godot/Python Ember
> backend** (factions, schedules, items, classes, worldgen, caravans, colony config, character
> creation, UI plan, …).

## What this is
A frozen snapshot of the previous prototype's data, kept so the Unity rewrite can **mine it for
design intent and seed values** — class lists, faction relationships, schedule shapes, item
provenance, worldgen parameters.

## How to use it
- **Reference and import source only.** Read it to understand what a system should contain, then
  author the equivalent Unity-canonical data under `Assets/` (ScriptableObjects / data rows /
  `Assets/StreamingAssets`) or the `EmberCrpg.Data` assembly.
- **Do NOT wire these JSON files directly into the live runtime.** They are not loaded by the game;
  the Unity data pipeline is the authority.
- Treat the schema as historical — the Unity domain types may differ; translate, don't assume parity.

## Status
Not active implementation truth. Kept under `Reference/` (alongside `Reference/PRDs/`) as the
old-prototype reference corpus. Safe to consult; not safe to treat as canonical Unity data.
