# PRD: Multi-Slot Saves + Save-Schema Versioning/Migration v1

**Project:** Ember CRPG (Unity 6 / URP)
**Audit item:** E7-007 — multi-slot saves + save-schema versioning/migration
**Phase:** Post-SOUL / E7 reliability hardening
**Author:** Captain (save-system track, via Claude session 2026-05-31)
**Date:** 2026-05-31
**Status:** Draft — implementable by another agent/Codex
**Layer ownership:** Captain (Data + Presentation save service + Presentation UI host). Touches `Assets/Scripts/Data/` and `Assets/Scripts/Presentation/`.

> **Premise.** The game today has exactly one durable save: `EmberSaveService` writes the active `SaveData` to `slot_0` (`<persistentDataPath>/saves/slot_0.json`) on F5 and reads it back on F9 / menu Continue. `FileSaveRepository` already exposes integer slots, atomic writes, `.corrupt` quarantine, and `ListSlots(maxSlots)`, and `WorldSaveData` already carries a `schemaVersion` int with a forward-migration guard in `WorldSaveMapper.ToWorld`. This PRD turns that latent scaffolding into a real **N-slot save system** (named manual slots + a rotating autosave + a quicksave), gives every slot **listing metadata** (timestamp / scene / playtime), formalizes the **schema VERSION + forward-migration** path so today's single-slot saves load with zero data loss, and adds a **slot list/select UI** (load / save / overwrite / delete) built on the **existing single-UI-source host pattern** (`EmberWorldHost.EnsurePauseMenu`-style ensurers), not a per-scene UI.

> **Hard determinism + back-compat constraints (non-negotiable, restated in §8):**
> 1. Save bytes are **deterministic** — the same world state serializes to byte-identical JSON every time, on every machine. No `DateTime.Now`, `Guid.NewGuid()`, `UnityEngine.Random`, culture-sensitive formatting, or hash-set ordering inside the serialized payload. Wall-clock metadata (the slot's saved-at timestamp) lives in a **separate sidecar**, never in the payload that determinism tests hash.
> 2. **Old saves still load.** A `slot_0.json` written by today's build (a bare `SaveData`, `schemaVersion`-absent or `0`, no sidecar) MUST load through the new code path with no data loss and no crash. The legacy PlayerPrefs blob (`ember.save.v1`) remains a last-resort fallback.

---

## 1. Purpose

Players need more than one save. Today a single F5 overwrites the only durable slot, so there is no branching, no "before the boss" save, no recovery from a bad decision, and a corrupt write can lose the entire game. E7-007 delivers:

- **N named manual slots** the player creates, names, overwrites, and deletes.
- **One rotating autosave** the game writes at defined checkpoints (scene transition is the v1 trigger).
- **One quicksave** (F5) and a quickload (F9), preserved exactly as they behave today but routed through the slot system.
- **A slot browser UI** (in the pause menu and the main menu) that lists every slot with its scene, in-game playtime, and real-world saved-at time, and lets the player Load / Save / Overwrite / Delete.
- **A formal save-schema VERSION + forward-migration path** so the on-disk shape can evolve and every older save (including today's single-slot saves) migrates forward into the current schema with no data loss.

This PRD is the **contract**. It is boundary-preserving: pure file/slot mechanics stay in `EmberCrpg.Data` (System.IO only, unit-testable without Unity); the MonoBehaviour save orchestration and the UI stay in `EmberCrpg.Presentation`; the migration ladder stays a pure static mapper in `EmberCrpg.Data.SliceJson`.

---

## 2. Current-state inventory (what exists, exactly)

Read before implementing — these are the load-bearing facts the design builds on.

### 2.1 Data layer — `Assets/Scripts/Data/Save/` (asmdef `EmberCrpg.Data`)

- **`FileSaveRepository.cs`** — pure C# (`System.IO` only). Ctor takes a root dir, derives `<root>/saves`. Public surface:
  - `string SlotPath(int slot)` → `<root>/saves/slot_{n}.json`
  - `void Save(int slot, string json)` — `CreateDirectory`, write `.tmp`, then `File.Replace` (atomic on NTFS) when the slot exists else `File.Move`. **No `.tmp` is left behind** (tested).
  - `bool SlotExists(int slot)`
  - `bool TryLoad(int slot, Func<string,bool> isValid, out string json)` — missing/unreadable ⇒ false; `isValid` rejects ⇒ `Quarantine` (rename to `slot_{n}.json.corrupt[.N]`, never overwriting an earlier quarantine — DET-06) and return false.
  - `IReadOnlyList<int> ListSlots(int maxSlots = 16)` — integer indices that hold a file.
  - **Gaps for E7-007:** integer-only slots; no slot *kind* (manual/auto/quick); no metadata (timestamp/scene/playtime); no delete; no rename; no listing of names; `maxSlots` is a magic default.

- **`WorldSaveData.cs` (+ partials `WorldSaveData.ActorDungeon.cs`, `.Economy.cs`, `.Narrative.cs`, `.WorldProcess.cs`)** — the `[Serializable]` DTO tree the full simulation snapshot maps to. **Already has `public int schemaVersion;`** as field #1, documented (EMB-012) as: legacy saves written before the field deserialize it to `0`, which `ToWorld` treats as the v1 baseline. Carries `totalMinutes` (the in-game clock — the playtime source), plus the entire world (actors, dungeon, economy, narrative, process stores, inventories, cooldowns).

- **`SliceJson/WorldSaveMapper.cs` (+ slice partials)** — pure static mapper, asmdef `EmberCrpg.Data.SliceJson` (no `UnityEngine`, no `EmberCrpg.Simulation` ref).
  - `public const int CurrentSchemaVersion = 1;`
  - `ToData(WorldState)` stamps `schemaVersion = CurrentSchemaVersion`.
  - `ToWorld(WorldSaveData data, WorldState seedWorld)` — **already the migration choke point**: throws `NotSupportedException` if `data.schemaVersion > CurrentSchemaVersion` (newer-than-this-build), and otherwise maps field-by-field. Legacy `0` flows straight through as `1`. **This is where the forward-migration ladder plugs in (§6).**

### 2.2 Presentation save service — `Assets/Scripts/Presentation/Ember/Save/` (asmdef `EmberCrpg.Presentation`)

- **`SaveData.cs`** — the `[Serializable]` Presentation DTO actually written to the file slot today:
  ```
  string sceneName; Vector3 playerPosition; float playerYaw; int tickIndex; string domainStateJson;
  ```
  `domainStateJson` is the opaque round-trippable envelope produced by `IDomainSimulationAdapter.ExportStateJson()` — **this is the `WorldSaveData` JSON** (`JsonSliceSaveService.SaveToJson` → `WorldSaveMapper.ToData` → `JsonUtility.ToJson`). So a slot file = outer `SaveData` whose `domainStateJson` embeds the inner versioned `WorldSaveData`. **`SaveData` itself has no version field** — only the embedded `WorldSaveData` does.

- **`EmberSaveService.cs`** — `MonoBehaviour`, host-added in `EmberWorldHost.Awake`. Constants: `SaveKey="ember.save.v1"` (legacy blob), `LastSlotKey="ember.save.lastslot"`, **`DefaultSlot=0`**. `Update()` calls `Save()` on `EmberInput.SaveQuick` (F5), `Load()` on `EmberInput.LoadQuick` (F9). `SaveInternal()` builds a `SaveData`, `JsonUtility.ToJson`, writes `_repo.Save(DefaultSlot, json)` FIRST (DET-05), then mirrors to PlayerPrefs `LastSlotKey`+`SaveKey`. Whole body is try/caught so a quick-save can never crash the process (BUG-SAVE-CRASH). Exposes `Audit*` static hooks for tests.

- **`EmberSaveService.Load.cs`** — `LoadInternal()` resolves JSON via `ResolveLatestSaveJson(_repo)`, deserializes `SaveData`, validates scene via `IsKnownBuildScene`, loads scene if different (stashing `_pendingLoad`), else restores in place. `ApplyDomainRestore` calls `adapter.RestoreStateJson(domainStateJson)` and `EmberTickDriver.AlignTo(tickIndex)`. Returns a `DomainRestoreResult` enum (NoPayload/NoAdapter/Failed/Restored) so the UI reports partial loads honestly.

- **`EmberSaveService.Resolve.cs`** — the **single resolution choke point**:
  - `IsLoadableSaveJson(raw)` — the quarantine predicate (parses to `SaveData` with non-empty `sceneName`).
  - `ResolveLatestSaveJson(repo)` — `LastSlotKey` file slot first (with quarantine), then legacy PlayerPrefs blob.
  - `PreparePendingLoad(SaveData)` / `TryResolveLatestSave(out SaveData)` — used by the main-menu Continue/Load so menu and in-game load share one path; `TryResolveLatestSave` also re-validates the scene against the build registry (E7-008).
  - `Audit*` static accessors (`AuditDefaultSlot`, `AuditLastSlotKey`, `AuditSaveKey`, `AuditResolveLatestSaveJson`, …) — the test seam.

- **`JsonSliceSaveService.cs`** — bridges `WorldState ↔ WorldSaveData ↔ JSON`. `SaveToJson` = `JsonUtility.ToJson(WorldSaveMapper.ToData(world), pretty:true)`. `LoadFromJson` = `WorldSaveMapper.ToWorld(FromJson, CreateSeedWorld(roomSeed))`. **This is the layer that owns the embedded versioned payload.**

- **`DomainSimulationAdapter.Save.cs`** — `ExportStateJson()` ⇒ `_saveService.SaveToJson(_world)`; `RestoreStateJson(json)` ⇒ `LoadFromJson` → `_world.CopyFrom` → `EnsureInvariants` → `_tickComposer.RebuildAccumulatorsFrom(_world.Time)` (DET-01 cold-load replay equivalence).

### 2.3 Presentation UI — `Assets/Scripts/Presentation/Ember/`

- **`Bootstrap/EmberWorldHost.cs` + `Bootstrap/EmberWorldHost.Ui.cs`** — the **single MonoBehaviour entrypoint per scene** and the canonical single-UI-source pattern. `EmberWorldHost.Ui.cs` holds idempotent ensurers: `EnsurePauseMenu`, `EnsureDialogBoxPanel`, `EnsureEmberHud`, `EnsureSidePanels`, `EnsureInventoryGrid`, `EnsureGeneratedActorSpawner`, plus `ResolveOverlayCanvas`/`BuildSidePanel` helpers. Each "ensure exactly one panel on a Canvas; reuse if present" — **this is the pattern the slot UI must follow.** `EmberWorldHost.Awake` already adds `EmberSaveService` (guarded against duplicates).
- **`UI/PauseMenu.cs`** — `CanvasGroup`-hidden menu surfaced on Escape. `BuildMenu()` creates buttons: RESUME, **SAVE (F5)** → `InvokeSave()` (finds `EmberSaveService`, calls `.Save()`), **LOAD (F9)** → `InvokeLoad()`, MAIN MENU, QUIT. **This is where a SAVES… / LOAD… button opens the new browser.**
- **`UI/EmberMainMenuUI.cs` + `UI/EmberMainMenuUI.Flow.cs`** — `New Game` / `Continue` / `Load` / `Options` / `Quit`. `LoadGame()` probes `EmberSaveService.TryResolveLatestSave`; `Continue()` resolves via the same and `PreparePendingLoad` + `LoadScene`. UI Toolkit `TitleMenu` panel with `SetButtonHandler("load", LoadGame)`. **The main-menu Load button opens the same browser in "load-only" mode.**
- **`Inputs/EmberInput.cs`** — the single input facade. `SaveQuick`=F5, `LoadQuick`=F9, `PauseDown`/`PauseHeld`=Escape. **New bindings (quicksave already exists) plug in here, never via raw `UnityEngine.Input`.**
- **`EmberScenes.cs`** — the scene-name registry (`MainMenu`, `SmithingOverworld`, …). Save/load scene strings must round-trip through these constants and validate via `IsKnownBuildScene`.

### 2.4 Tests — `Assets/Tests/EditMode/Save/` (asmdef `EmberCrpg.Tests.EditMode`)

`FileSaveRepositoryTests.cs` (roundtrip / missing / corrupt-quarantine / double-quarantine / `ListSlots` / overwrite-no-tmp), `SaveSchemaVersionTests.cs` (stamps current / accepts current / legacy-0-as-v1 / rejects-future), `EmberSaveServiceResolutionTests.cs` (`Audit*` resolution precedence, quarantine fallback, scene validation). **New tests extend these files / this asmdef using the same temp-root + `Audit*` conventions.**

---

## 3. Scope

**In scope (v1):**

- A **slot model** at the `FileSaveRepository` layer: typed slots — `Manual(index)`, `Auto`, `Quick` — with stable file names and a per-slot metadata **sidecar**.
- **N manual slots** (default cap **10**, configurable), **1 autosave**, **1 quicksave**.
- **Listing + metadata**: enumerate all present slots with `{kind, index, sceneName, playtimeMinutes, savedAtUtcIso, schemaVersion, label}` — read cheaply from sidecars without loading the full payload.
- **Delete** a slot (payload + sidecar + any `.corrupt` siblings of that slot, atomically-ish).
- **Save-schema VERSION**: a top-level envelope version (`SaveEnvelopeData.envelopeVersion`) on the outer `SaveData`, complementing the already-present inner `WorldSaveData.schemaVersion`; plus a **forward-migration ladder** in the Data layer that upgrades any older `WorldSaveData` to `CurrentSchemaVersion`, and an outer-envelope upgrader for `SaveData`.
- **Autosave trigger (v1):** scene transition (the `_pendingLoad`/`LoadScene` boundary and portal traversal) writes the autosave slot. Hook is generic so additional triggers (rest, level-up) attach later.
- **Quicksave/quickload:** F5/F9 retained, re-pointed to the `Quick` slot (behaviour-identical to today's `slot_0` from the player's view).
- **Slot browser UI** in the pause menu (Save + Load modes) and main menu (Load mode), host-ensured via the `EmberWorldHost` single-UI-source pattern. Overwrite-confirm and delete-confirm prompts. Name-on-save.
- **Back-compat:** today's `slot_0.json` (bare `SaveData`, no sidecar) is adopted as the `Quick` slot on first run via a one-time migration, with a synthesized sidecar.

**Out of scope (later PRDs / explicitly deferred):**

- Cloud / Steam-cloud sync, screenshot thumbnails per slot (metadata reserves a `thumbnailPath` field but v1 does not render one).
- Save compression / encryption / binary format (stays human-readable JSON).
- Cross-version *backward* migration (loading a NEWER save in an OLDER build stays a hard refuse — already implemented).
- Per-slot autosave rotation depth > 1 (v1 is a single rotating autosave; an N-deep ring is a follow-up).
- Changing the embedded `WorldSaveData` field set — this PRD adds **no** new gameplay fields; the migration ladder is wired and tested with an identity v1→v1 step so the mechanism is proven without a real shape change.

---

## 4. SOLID layering (where each piece lives)

Per `docs/agent-rules-v2.md` ownership table (`Domain`/`Simulation`/`Data` = Captain pure-C#; `Presentation` Unity-bound). The save stack already respects the boundary; E7-007 must keep it intact.

| Layer | Assembly | E7-007 responsibility | New/changed types |
|---|---|---|---|
| **Domain** | `EmberCrpg.Domain` | none — no Domain change. Playtime is read from `WorldState.Time.TotalMinutes`, already Domain. | — |
| **Simulation** | `EmberCrpg.Simulation` | none — `WorldSaveRehydration.CreateSeedWorld` is reused as-is by the migration ladder's seed step. | — |
| **Data** | `EmberCrpg.Data` | the **slot mechanics** (typed slots, file naming, sidecar metadata read/write/list/delete) and the **outer-envelope version + migration**. Pure `System.IO`, no `UnityEngine`. | `SaveSlotId`, `SaveSlotKind`, `SaveSlotMetadata`, `FileSaveRepository` extension, `SaveEnvelopeData`, `SaveEnvelopeMigrator` |
| **Data.SliceJson** | `EmberCrpg.Data.SliceJson` | the **inner `WorldSaveData` migration ladder** (already choke-pointed in `ToWorld`). | `WorldSaveMigration` (new partial of `WorldSaveMapper`) |
| **Presentation** | `EmberCrpg.Presentation` | the **MonoBehaviour orchestration** (which slot, when; autosave trigger; metadata assembly with the wall clock) and the **UI** (browser panel, pause/main-menu buttons), built on the host single-UI-source pattern. | `EmberSaveService` slot API, `SaveSlotBrowserPanel`, `EmberWorldHost.EnsureSaveSlotBrowser`, `PauseMenu`/`EmberMainMenuUI` button wiring |
| **Tests** | `EmberCrpg.Tests.EditMode` | round-trip + migration + metadata + resolution coverage, pure-C# (fallback-harness-eligible). | extend `Save/` tests |

**Boundary rules that must hold:**
- The Data layer stays free of `DateTime`/`UnityEngine` **inside the determinism-hashed payload**. The **sidecar** is a separate file written by the Presentation service which is the only place the wall clock is read; the Data layer's sidecar type is a dumb DTO that stores whatever ISO string it is handed (it does not call `DateTime.Now`). This preserves the existing determinism guard that keeps `DateTime` out of `FileSaveRepository` (see its DET-06 comment).
- The migration ladder is **pure** and lives in Data / Data.SliceJson, called from `WorldSaveMapper.ToWorld` (inner) and `SaveEnvelopeMigrator.Upgrade` (outer). No Presentation type is referenced from Data.

---

## 5. Functional requirements — (a) multi-slot storage at the FileSaveRepository layer

### FR-A1 Typed slot identity
Introduce a pure value type in `EmberCrpg.Data.Save`:

- `enum SaveSlotKind { Manual, Auto, Quick }`
- `readonly struct SaveSlotId { SaveSlotKind Kind; int Index; }` — `Index` meaningful only for `Manual` (0..cap-1); `Auto`/`Quick` ignore it (canonical `0`).
- Equality + a stable `string FileStem()`:
  - `Manual` → `manual_{index}` (e.g. `manual_0` … `manual_9`)
  - `Auto` → `auto`
  - `Quick` → `quick`

### FR-A2 File naming
`FileSaveRepository` writes two files per slot under `<root>/saves/`:
- **Payload:** `{stem}.json` (the outer `SaveData`/`SaveEnvelopeData` JSON).
- **Sidecar:** `{stem}.meta.json` (the `SaveSlotMetadata` JSON).
Corrupt quarantine keeps the existing `{stem}.json.corrupt[.N]` scheme (now per-stem). The legacy integer `slot_{n}.json` name is **read** for back-compat (§7) but new writes use the named stems.

> File-name compatibility note: the existing `SlotPath(int)` → `slot_{n}.json` API is **retained** (do not break `FileSaveRepositoryTests` or `EmberSaveServiceResolutionTests` which call it). The new typed API is **additive** (`SlotPath(SaveSlotId)`, etc.). `slot_0.json` ⇔ `quick.json` adoption is handled once by the migration step (§7), not by aliasing every call.

### FR-A3 Slot metadata (sidecar)
`[Serializable] sealed class SaveSlotMetadata` (Data layer, `JsonUtility`-friendly, **dumb DTO — no clock calls**):

```
int    metadataVersion;     // sidecar's own format version (starts at 1)
int    envelopeVersion;     // outer SaveData envelope version this payload was written with
int    schemaVersion;       // inner WorldSaveData schema version (mirrored for cheap display)
string slotKind;            // "Manual" | "Auto" | "Quick"
int    slotIndex;           // manual index, else 0
string label;               // player-entered name (manual) or auto-generated ("Autosave", "Quicksave")
string sceneName;           // EmberScenes stem
long   playtimeMinutes;     // WorldState.Time.TotalMinutes at save
string savedAtUtcIso;       // ISO-8601 UTC, e.g. "2026-05-31T20:38:00Z" — set by Presentation, NOT by Data
string thumbnailPath;       // reserved, empty in v1
```

The sidecar is the **only** thing the slot list reads, so listing N slots never deserializes N full world payloads.

### FR-A4 Repository operations (additive on `FileSaveRepository`)
- `void Save(SaveSlotId id, string payloadJson, SaveSlotMetadata meta)` — atomic-publish the payload (existing `.tmp`+`File.Replace` flow) **then** the sidecar the same way; if the sidecar write fails the payload still stands (sidecar is reconstructible by loading the payload).
- `bool TryLoadPayload(SaveSlotId id, Func<string,bool> isValid, out string payloadJson)` — same quarantine semantics as today's `TryLoad`.
- `bool TryLoadMetadata(SaveSlotId id, out SaveSlotMetadata meta)` — reads sidecar; on a missing/garbage sidecar returns false (caller may reconstruct from the payload — FR-A6).
- `bool SlotExists(SaveSlotId id)`
- `bool Delete(SaveSlotId id)` — remove payload + sidecar + that stem's `.corrupt*` siblings; best-effort, never throws; returns whether the payload existed.
- `IReadOnlyList<SaveSlotMetadata> ListAll(int manualCap)` — scan the saves dir, return metadata for every present slot (`quick`, `auto`, `manual_0..cap-1`), each with a populated `slotKind`/`slotIndex`. Ordering is **deterministic**: Quick, Auto, then Manual ascending by index (stable for tests).
- `int ManualCapDefault { get; }` — constant (default **10**), replacing the magic `maxSlots`.

### FR-A5 Atomicity + no-tmp invariant
Every write (payload and sidecar) keeps the existing `.tmp`→`File.Replace`/`File.Move` discipline so a crash mid-write can never corrupt an existing slot and never leaves a `.tmp` (extend the `Save_Overwrite_DoesNotLeaveTmp` test to the named API and the sidecar).

### FR-A6 Sidecar reconstruction (resilience)
If a payload exists but its sidecar is missing/corrupt, `ListAll` reconstructs a best-effort `SaveSlotMetadata` by parsing the payload's outer `SaveData` (scene from `sceneName`; playtime by parsing the embedded `WorldSaveData.totalMinutes`; `savedAtUtcIso` left empty / file-mtime substituted by the **Presentation** caller, not by Data). This guarantees today's sidecar-less `slot_0.json` is listable.

---

## 6. Functional requirements — (b) save-schema VERSION + forward migration

### FR-B1 Two version fields, two ladders
There are **two** independent versioned layers; both get an explicit migration ladder:

1. **Inner — `WorldSaveData.schemaVersion`** (already exists, `CurrentSchemaVersion = 1`). Choke point already exists: `WorldSaveMapper.ToWorld`. Add a **forward-migration ladder** here (FR-B2).
2. **Outer — `SaveData` envelope.** Today `SaveData` has **no** version field, so an outer shape change (e.g. adding a field, renaming `domainStateJson`) is undetectable. Introduce `SaveEnvelopeData` as the versioned outer wrapper (FR-B3) so the outer shape is migratable too.

### FR-B2 Inner ladder — `WorldSaveMigration` (new partial of `WorldSaveMapper`, Data.SliceJson)
- `static WorldSaveData Migrate(WorldSaveData data)` — normalizes `schemaVersion == 0` → `1` (the existing legacy rule, now explicit), then applies ordered steps `Vk → Vk+1` until `data.schemaVersion == CurrentSchemaVersion`. v1 ships a single **identity** step registry (no real shape change yet) so the ladder is exercised and tested without altering the payload. Future shape changes add a `Step_1_to_2(WorldSaveData)` method, bump `CurrentSchemaVersion`, and the field-by-field map in `ToWorld` stays at the *current* shape only.
- `ToWorld` calls `Migrate(data)` **before** mapping; the existing "newer than this build ⇒ `NotSupportedException`" guard stays and runs first. Net effect: `ToWorld` always sees a `CurrentSchemaVersion` DTO, so the verbose mapper never branches on version.

### FR-B3 Outer envelope — `SaveEnvelopeData` + `SaveEnvelopeMigrator` (Data)
- `[Serializable] sealed class SaveEnvelopeData : { int envelopeVersion; SaveData save; }` — `CurrentEnvelopeVersion = 1`. (Implementation note: because Unity's `JsonUtility` does not support polymorphism, `SaveEnvelopeData` **wraps** `SaveData` by composition, it does not subclass it.)
- `static class SaveEnvelopeMigrator { const int CurrentEnvelopeVersion = 1; SaveData UpgradeToCurrent(string rawJson) ; }`:
  - **Detect legacy:** if the JSON has no `envelopeVersion` (today's bare `SaveData`), treat as envelope v0 and adopt the parsed `SaveData` directly.
  - Apply ordered outer steps to `CurrentEnvelopeVersion` (v1 identity).
  - Return the contained `SaveData`. The embedded `domainStateJson` is **not** re-parsed here — its versioning is the inner ladder's job, applied later by `LoadFromJson`/`ToWorld`.
- **Determinism:** `envelopeVersion` is a constant int in the payload (deterministic). It does **not** perturb existing byte output beyond the single added field; the determinism test (FR-D-tests) hashes the canonical serialization of a fixed world, so the field is part of the new deterministic baseline.

### FR-B4 No-data-loss guarantee
Loading any older save MUST preserve every field the older schema carried. The inner ladder only **adds/derives** fields going forward (never drops), and the `ToWorld` mapper already tolerates absent arrays (null-guards throughout). Acceptance: a fixture `slot_0.json` captured from today's build (committed under `Assets/Tests/EditMode/Save/Fixtures/`) loads via the new path and yields a `WorldState` whose scene, playtime, player vitals, and inventory equal the pre-migration values.

### FR-B5 Version visibility
The slot browser shows the slot's `schemaVersion` (from the sidecar) and disables Load with a "needs a newer build" tooltip when `envelopeVersion`/`schemaVersion` exceeds what the build supports (mirrors the `NotSupportedException` refuse, surfaced in UI rather than as a thrown load).

---

## 7. Functional requirements — (c) slot list/select UI on the single-UI-source host

### FR-C1 One panel, host-ensured (not per-scene)
Add `SaveSlotBrowserPanel` (`Assets/Scripts/Presentation/Ember/UI/SaveSlotBrowserPanel.cs`) and ensure exactly one via a new `EmberWorldHost.EnsureSaveSlotBrowser()` in `EmberWorldHost.Ui.cs`, following the **identical idempotent pattern** as `EnsurePauseMenu`/`EnsureDialogBoxPanel`:
- `FindFirstObjectByType<SaveSlotBrowserPanel>(Include)`; reuse if present (never creates a second on additive load / domain reload).
- Mount under `ResolveOverlayCanvas()`; full-screen `RectTransform`; `CanvasGroup`-hidden by default (alpha 0, `blocksRaycasts=false`), exactly like `PauseMenu`.
- Created in `Awake` after `EnsurePauseMenu` so the pause menu's "SAVES…/LOAD…" buttons can resolve it.

The panel is **DTO-driven** like every other host-bound panel: it reads `IReadOnlyList<SaveSlotMetadata>` from a source interface (`ISaveSlotBrowserSource`) implemented by `EmberSaveService` (which owns the repo). It does not touch `FileSaveRepository` directly — Presentation UI → service → Data, preserving the layering.

### FR-C2 Two modes
`SaveSlotBrowserPanel.Open(BrowserMode mode)` with `enum BrowserMode { Save, Load }`:
- **Save mode** (from pause menu "SAVE…"): lists all slots; each manual row has Overwrite + Delete; a "New Save" affordance prompts for a label then writes the next free `Manual` slot. Quick/Auto rows are shown read-only (informational) — the player saves those via F5 / automatically, not by clicking.
- **Load mode** (from pause menu "LOAD…" and main-menu "Load"): lists all slots; each row has Load + Delete; rows whose version exceeds the build are Load-disabled (FR-B5).

### FR-C3 Row content
Each row renders from the sidecar metadata only: `label` — `sceneName` — playtime `HH:MM` (from `playtimeMinutes`) — saved-at local time (from `savedAtUtcIso`, formatted for display only; storage stays ISO-UTC). Empty manual slots render as "— empty —" with a Save affordance (Save mode) / greyed (Load mode).

### FR-C4 Confirmations
- **Overwrite** an occupied manual slot → inline confirm ("Overwrite '{label}'?").
- **Delete** → inline confirm ("Delete '{label}'? This cannot be undone.") → `EmberSaveService.DeleteSlot(id)` → `FileSaveRepository.Delete`.
- **Name on save** → a TMP input field seeded with a deterministic default label (`"Slot {index+1}"`); empty input falls back to the default (never an empty label).

### FR-C5 Pause-menu wiring (`PauseMenu.cs`)
`BuildMenu()` gains two buttons between SAVE/LOAD and MAIN MENU (or repurposes them): **"SAVE…"** → `EnsureSaveSlotBrowser` + `Open(Save)`; **"LOAD…"** → `Open(Load)`. Existing **SAVE (F5)** / **LOAD (F9)** quick buttons stay (they hit the `Quick` slot). The browser pauses identically (`Time.timeScale = 0` already set by the pause menu; the browser inherits the paused state).

### FR-C6 Main-menu wiring (`EmberMainMenuUI`)
The UI Toolkit `TitleMenu` "load" handler (`SetButtonHandler("load", …)`) opens the browser in **Load** mode instead of immediately resolving the latest save. Selecting a slot calls a new `EmberSaveService.PreparePendingLoadFromSlot(id)` (static; builds the repo over `persistentDataPath`, resolves + migrates the slot's `SaveData`, validates the scene via the existing `IsKnownBuildScene`, `PreparePendingLoad`, then `LoadScene`). `Continue` keeps its current "latest save" shortcut (now = most-recently-saved slot by `savedAtUtcIso`, resolved from sidecars).

### FR-C7 Input
No new raw input. The browser is mouse/click-driven through the canvas `GraphicRaycaster` (same as `PauseMenu` buttons). Escape closes the browser (returns to pause menu) — routed through `EmberInput.PauseDown`, and guarded by the existing `IsModalOpen` pattern so it does not also toggle cursor/quit.

---

## 8. Determinism + back-compat constraints (acceptance-bearing)

**D1 — Deterministic payload bytes.** Serializing a fixed `WorldState` (e.g. `new WorldFactory().Create(1337)`) twice yields byte-identical payload JSON. The added `envelopeVersion`/`schemaVersion` ints are constants; no wall-clock, GUID, `UnityEngine.Random`, culture-formatting, or unordered-collection iteration enters the payload. **The saved-at timestamp is NOT in the payload** — it is sidecar-only. A determinism test hashes the payload (not the sidecar) and asserts stability.

**D2 — Sidecar is non-deterministic-by-design but isolated.** `savedAtUtcIso` differs per save; tests that assert determinism operate on the payload, and tests that assert metadata correctness inject a fixed clock via the Presentation service seam (the service takes a `Func<DateTime>` clock, defaulting to `DateTime.UtcNow`, overridable in tests — keeps `DateTime` out of Data).

**D3 — Old saves still load (no data loss).** A committed fixture `slot_0.json` (bare `SaveData`, `domainStateJson` carrying a `schemaVersion`-absent or `0` `WorldSaveData`, no sidecar) loads through the full new path: `SaveEnvelopeMigrator.UpgradeToCurrent` (envelope v0→1) → `SaveData` → `LoadFromJson` → `WorldSaveMigration.Migrate` (schema 0→1) → `ToWorld`. Resulting world equals the pre-migration values for scene, playtime, vitals, inventory. The legacy PlayerPrefs `ember.save.v1` blob remains the last-resort fallback in `ResolveLatestSaveJson`.

**D4 — One-time legacy adoption.** On first run after this lands, if `quick.json` is absent but a legacy `slot_0.json` (or PlayerPrefs blob) exists, `EmberSaveService` adopts it as the `Quick` slot: write `quick.json` + a synthesized `quick.meta.json` (scene/playtime from the payload, `savedAtUtcIso` = file mtime or now, `label="Quicksave"`). Idempotent — runs once (guard on `quick.json` existence). The legacy file is left in place (not deleted) so a rollback build still finds it.

**D5 — No newer-save downgrade.** Loading a save whose `envelopeVersion > CurrentEnvelopeVersion` or `schemaVersion > CurrentSchemaVersion` is refused (UI: Load disabled + tooltip; programmatic: the existing `NotSupportedException` path). No partial/half-mapped load.

**D6 — Atomic + crash-safe.** Payload and sidecar each publish via `.tmp`→replace; a crash between payload-publish and sidecar-publish leaves a valid payload with a reconstructible sidecar (FR-A6), never a corrupt slot.

---

## 9. Exact files to add / modify

### 9.1 Add (Data — `Assets/Scripts/Data/Save/`)
- `SaveSlotKind.cs` — the enum.
- `SaveSlotId.cs` — the struct + `FileStem()` + equality.
- `SaveSlotMetadata.cs` — the `[Serializable]` sidecar DTO (FR-A3).
- `SaveEnvelopeData.cs` — versioned outer wrapper (`envelopeVersion` + `SaveData save`) **(note: `SaveData` currently lives in Presentation — see §9.6 decision).**
- `SaveEnvelopeMigrator.cs` — outer-envelope upgrade ladder (FR-B3).

### 9.2 Add (Data.SliceJson — `Assets/Scripts/Data/Save/SliceJson/`)
- `WorldSaveMapper.Migration.cs` — new partial: `WorldSaveMigration.Migrate` + step registry (FR-B2).

### 9.3 Modify (Data — `Assets/Scripts/Data/Save/`)
- `FileSaveRepository.cs` — add the typed-slot + sidecar + delete + `ListAll` API (FR-A4), keep all existing integer-slot methods intact.
- `SliceJson/WorldSaveMapper.cs` — `ToWorld` calls `WorldSaveMigration.Migrate(data)` after the newer-than-build guard, before mapping (FR-B2).

### 9.4 Add (Presentation — `Assets/Scripts/Presentation/Ember/UI/`)
- `SaveSlotBrowserPanel.cs` — the browser MonoBehaviour (FR-C1..C4).
- `ISaveSlotBrowserSource.cs` — the DTO source interface the panel binds to (`IReadOnlyList<SaveSlotMetadata> ListSlots()`, `Save/Load/Delete` callbacks).

### 9.5 Modify (Presentation — `Assets/Scripts/Presentation/Ember/`)
- `Save/EmberSaveService.cs` — replace `DefaultSlot=0` usage with the `Quick` slot; inject `Func<DateTime>` clock (default `DateTime.UtcNow`); implement `ISaveSlotBrowserSource`; add `SaveToSlot(SaveSlotId,label)`, `LoadFromSlot(SaveSlotId)`, `DeleteSlot(SaveSlotId)`, `ListSlots()`, and the D4 one-time legacy adoption in `Awake`.
- `Save/EmberSaveService.Load.cs` — route `LoadFromSlot` through the same `RestorePosition`/`ApplyDomainRestore` machinery; the autosave-on-scene-transition write hook.
- `Save/EmberSaveService.Resolve.cs` — `ResolveLatestSaveJson` runs results through `SaveEnvelopeMigrator.UpgradeToCurrent`; add `PreparePendingLoadFromSlot(SaveSlotId)`; extend `Audit*` seams (`AuditListSlots`, `AuditSlotPath(SaveSlotId)`, clock injection) for tests.
- `Save/SaveData.cs` — unchanged shape (still the inner payload), but now wrapped by `SaveEnvelopeData` on write/read. (Optionally add `[Serializable]` envelope-version awareness via the wrapper only.)
- `Bootstrap/EmberWorldHost.Ui.cs` — add `EnsureSaveSlotBrowser()` (idempotent ensurer) and call it in `EmberWorldHost.cs` `Awake` right after `EnsurePauseMenu()`.
- `UI/PauseMenu.cs` — add "SAVE…" / "LOAD…" buttons that ensure + open the browser (FR-C5).
- `UI/EmberMainMenuUI.cs` / `UI/EmberMainMenuUI.Flow.cs` — "load" handler opens the browser in Load mode; `Continue` resolves "most recent by sidecar" (FR-C6).
- `Inputs/EmberInput.cs` — **no new binding required** (F5/F9/Escape already present); add a semantic `OpenSaves` only if a dedicated hotkey is desired (optional, default off).

### 9.6 Decision: where does `SaveData` live for the envelope?
`SaveEnvelopeData` (Data) must reference `SaveData`, but `SaveData` is in Presentation. **Two boundary-preserving options — pick one in implementation:**
- **(Preferred) Keep `SaveData` in Presentation; keep the envelope wrapper in Presentation too** (`SaveEnvelopeData.cs` under `Assets/Scripts/Presentation/Ember/Save/`), and keep only `SaveEnvelopeMigrator` *string-level* logic in Data operating on raw JSON (it parses/serializes via the caller). This keeps Data free of the Unity-`Vector3`-bearing `SaveData`. **This is the boundary-correct choice** because `SaveData` carries `UnityEngine.Vector3` and must not sink into Data.
- (Rejected) Moving `SaveData` to Data — rejected: `SaveData.playerPosition` is `UnityEngine.Vector3`, which Data must not import.

> **Net rule:** the **version integer detection/upgrade logic** that is pure-string can live in Data (`SaveEnvelopeMigrator` taking/returning `string`), but the **typed `SaveEnvelopeData`/`SaveData` wrapper** stays in Presentation alongside `SaveData`. Update §9.1 accordingly: `SaveEnvelopeData.cs` → Presentation; `SaveEnvelopeMigrator.cs` (string-in/string-out version ladder) → Data. The inner `WorldSaveData` ladder is fully Data.SliceJson (no Unity types involved).

### 9.7 Meta files
Every new `.cs` needs a Unity `.meta` with a fresh GUID (the project tracks metas — `FileSaveRepository.cs.meta` exists). Generate metas for all added files (the repo's standard "generate fresh-guid metas" step) before validation so the asmdefs compile them.

### 9.8 Tests (Assets/Tests/EditMode/Save/)
- `SaveSlotRepositoryTests.cs` — typed save/load/list/delete; sidecar round-trip; sidecar reconstruction; no-tmp; deterministic `ListAll` order.
- `SaveEnvelopeMigrationTests.cs` — legacy (no `envelopeVersion`) adopts; current round-trips; future refused.
- `WorldSaveMigrationTests.cs` — `Migrate` normalizes 0→1; identity step; idempotent; `ToWorld` post-migration unchanged (extends `SaveSchemaVersionTests`).
- `LegacySaveBackCompatTests.cs` — the committed fixture `Fixtures/legacy_slot_0.json` loads with no data loss (D3).
- Extend `EmberSaveServiceResolutionTests.cs` — `Quick`-slot routing, D4 one-time adoption, clock injection determinism, `PreparePendingLoadFromSlot`.
- `Fixtures/legacy_slot_0.json` — captured from today's build (a real F5 save) + its committed `.meta`.

---

## 10. Acceptance tests (player-verifiable + automated)

**Automated (EditMode, fallback-harness-eligible — pure C# only):**
- **AT-1** `FileSaveRepository.Save(SaveSlotId,…)` then `TryLoadPayload`/`TryLoadMetadata` round-trips payload and sidecar; `Delete` removes both + `.corrupt` siblings.
- **AT-2** `ListAll(cap)` returns Quick, Auto, then Manual-ascending, each with correct `slotKind`/`slotIndex`, reading sidecars only (assert no full-payload parse via a spy/`isValid` not invoked for listing).
- **AT-3** Sidecar reconstruction: delete a sidecar, `ListAll` still reports that slot with scene+playtime parsed from the payload.
- **AT-4** Determinism: two serializations of `WorldFactory().Create(1337)` produce byte-identical **payloads** (sidecar excluded).
- **AT-5** Inner migration: `WorldSaveMigration.Migrate` turns `schemaVersion=0` into a `CurrentSchemaVersion` DTO; `ToWorld` accepts it; future version still throws `NotSupportedException`.
- **AT-6** Outer migration: a JSON without `envelopeVersion` upgrades to the current `SaveData`; one with a future `envelopeVersion` is refused.
- **AT-7** Back-compat fixture (D3): `legacy_slot_0.json` loads; resulting `WorldState` scene/playtime/vitals/inventory equal expected pre-migration values.
- **AT-8** Resolution precedence preserved: `Quick` slot beats legacy PlayerPrefs; corrupt `Quick` quarantines then falls back (extends existing resolution tests).
- **AT-9** One-time adoption (D4): with only a legacy `slot_0.json` present, the first `EmberSaveService` init creates `quick.json` + sidecar; a second init does not rewrite it.
- **AT-10** Clock injection: with a fixed `Func<DateTime>`, `savedAtUtcIso` is the injected value (proves the wall clock is seam-injected, not Data-side).

**Player-verifiable (manual / PlayMode smoke, recorded in the proof log):**
- **PT-1** *player can* open the pause menu, click "SAVE…", name a slot, and see it appear in the list with the current scene, playtime, and time.
- **PT-2** *player can* create three distinct manual saves, quit to main menu, click "Load", pick the second, and resume exactly that state.
- **PT-3** *player can* overwrite an existing slot (with confirm) and delete a slot (with confirm); the list updates immediately.
- **PT-4** *player can* F5 quicksave / F9 quickload exactly as before (Quick slot), unaffected by manual slots.
- **PT-5** A scene transition writes the Autosave slot; it appears in the list labelled "Autosave".
- **PT-6** A save made by the **previous** build (single `slot_0`) still loads from the new Load list with no data loss (drop the pre-change `slot_0.json` into `persistentDataPath/saves`, launch, Load).

---

## 11. Verification

Run from repo root.

1. **Fallback harness (primary gate — must be green):**
   ```bash
   bash tools/validation/run-validation.sh --mode fallback
   ```
   All new Data-layer + migration + metadata tests are pure C# and run here. (The browser-UI MonoBehaviour code is excluded from the fallback harness by design — it is exercised by PlayMode smoke + the Win64 build, like every other Presentation MonoBehaviour.)
2. **EditMode (when a Unity editor is available):**
   ```bash
   bash tools/validation/run-validation.sh --mode unity
   ```
3. **Win64 player build** (the opt-in BD-21 job in `.github/workflows/unity-test.yml`, or a local `-buildWindows64Player`) — confirms the new `SaveSlotBrowserPanel`, `EmberWorldHost.EnsureSaveSlotBrowser`, and pause/main-menu wiring compile and link in a real player, and that PT-1..PT-6 pass against the shipped binary.
4. **Regression:** existing `FileSaveRepositoryTests`, `SaveSchemaVersionTests`, `EmberSaveServiceResolutionTests` stay green (the integer-slot API and `Audit*` seams are retained, not replaced).
5. **Determinism proof:** the AT-4 byte-equality assertion plus a re-run of any existing save-determinism proof in `docs/proofs/` (no payload drift introduced).

---

## 12. Risks & mitigations

| Risk | Mitigation |
|---|---|
| **Two version axes (envelope + schema) confuse implementers** | §6 fixes the inner ladder as the *only* place world-shape evolves; the outer envelope ladder handles only the `SaveData` wrapper. v1 ships both as identity steps so the wiring is proven before any real shape change. |
| **`SaveData` carries `UnityEngine.Vector3` → tempting to sink it into Data and break the boundary** | §9.6 decision: typed `SaveEnvelopeData`/`SaveData` stay in Presentation; only the pure-string `SaveEnvelopeMigrator` lives in Data. Inner `WorldSaveData` ladder is fully Unity-free in Data.SliceJson. |
| **Wall-clock timestamp leaking into the deterministic payload** | Timestamp is sidecar-only; the Presentation service is the sole place `DateTime` is read, via an injectable `Func<DateTime>`; determinism tests hash the payload, never the sidecar. Keeps the existing "no `DateTime` in `FileSaveRepository`" guard intact. |
| **Listing N slots loads N full world payloads (perf)** | Metadata sidecar is the listing source; full payload is loaded only on actual Load. Reconstruction path (FR-A6) only triggers for sidecar-less legacy slots. |
| **Old single-slot save silently dropped on upgrade** | D3 fixture test + D4 one-time adoption + retained PlayerPrefs fallback; the legacy `slot_0.json` is read and never deleted. PT-6 verifies against a real previous-build save. |
| **Double-UI / per-scene drift (the exact bug the host pattern fixed)** | Browser is host-ensured via `EnsureSaveSlotBrowser` (idempotent, reuse-if-present) under `ResolveOverlayCanvas`, never authored per-scene; matches `EnsurePauseMenu`. |
| **Crash between payload and sidecar write** | Both publish atomically (`.tmp`→replace); a missing sidecar is reconstructible (FR-A6), so the slot is never lost. |
| **Quicksave behaviour change angers muscle memory** | F5/F9 keep their exact semantics, just re-pointed from `slot_0` to the named `Quick` slot; PT-4 locks this. |
| **JsonUtility can't do polymorphism/dictionaries** | `SaveEnvelopeData` wraps `SaveData` by composition (not inheritance); sidecar is a flat DTO; slot lists are arrays — all `JsonUtility`-safe. |

---

## 13. Out-of-band notes for the implementing agent

- Do **not** widen the embedded `WorldSaveData` field set in this PRD — the migration ladder must land and be proven with an identity step first. A real field addition is a *separate* follow-up that adds a `Step_1_to_2` and bumps `CurrentSchemaVersion`.
- Keep `FileSaveRepository`'s existing integer-slot API and `EmberSaveService`'s `Audit*` statics — three existing test files depend on them.
- Generate `.meta` files (fresh GUIDs) for every new `.cs` before running validation, or the asmdefs won't compile the new types.
- The fallback harness is the merge gate; the Win64 build is the proof that the Presentation/UI half links. Both must pass.
