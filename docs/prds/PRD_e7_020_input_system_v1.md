# PRD: Migrate Legacy Input → Input System Package (E7-020) v1

**Project:** Ember RPG (Unity 6 / URP)
**Audit item:** E7-020 — migrate from `UnityEngine.Input` (legacy Input Manager) to `com.unity.inputsystem`.
**Author:** Audit subagent (read-only exploration, 2026-05-31)
**Date:** 2026-05-31
**Status:** Draft — implementation contract. **No code written by this PRD.**
**Scope of work doc:** documentation only. The migration it describes is **large project-setting change on a WORKING game**, so the plan below is explicitly **staged and always-working**: every stage compiles, ships, and passes the regression baseline before the next stage starts.

> **One-line premise:** the codebase already did 90% of the hard part. Input is *not* scattered — there is exactly **one** file in `Assets/` that touches `UnityEngine.Input`, and the entire game reads input through a single static facade (`EmberInput`). E7-020 is therefore a low-blast-radius swap of that one facade's *body*, not a 41-site rewrite. This PRD's job is to keep it low-blast-radius by gating each stage behind a captured baseline.

---

## 1. Current state (verified, do not re-discover)

### 1.1 Active Input Handling
- `ProjectSettings/ProjectSettings.asset` line 689: `activeInputHandler: 0`.
  - `0` = **Input Manager (Old) only**. `1` = Input System (New) only. `2` = **Both**.
- The game today runs entirely on the legacy Input Manager.

### 1.2 Package state
- `Packages/manifest.json` does **not** list `com.unity.inputsystem`. The package is **not installed**.
- Relevant installed packages: `com.unity.render-pipelines.universal 17.3.0`, `com.unity.cinemachine 3.1.6`, `com.unity.ugui 2.0.0`, `com.unity.test-framework 1.6.0`, plus the `com.unity.modules.*` built-ins. (`com.unity.modules.input` is implied by the engine; the Input System is a *separate* registry package and is absent.)

### 1.3 The single input choke point
- **`Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs`** (asmdef `EmberCrpg.Presentation`, GUID `9131ab133b4a40eb82873bfabedb6c4b`) is the **ONLY** file in `Assets/` that references `UnityEngine.Input` in executable code. Confirmed by grepping all of `Assets/**/*.cs` for `UnityEngine.Input` and `[^.\w]Input.` — every other hit is a comment.
- It is a `static class` with a documented contract that *already anticipates this migration*:
  > "the project has ONE choke point for the legacy Input Manager today and can swap the body for com.unity.inputsystem later without editing 30+ call sites."
- The legacy `Slice*` controllers that the docstring lists as "out of scope … still use `UnityEngine.Input` directly" **no longer exist** — `glob **/Slice*.cs` returns nothing. There is no residual legacy input outside `EmberInput`.

### 1.4 `EmberInput` public surface (the API every call-site depends on)
All of these are the migration's "interface contract." Behavior must be preserved byte-for-byte across the swap.

| Member | Today's legacy body | Semantics |
|---|---|---|
| `Move` → `Vector2` | `GetAxisRaw("Horizontal")`, `GetAxisRaw("Vertical")` | WASD/arrows, raw |
| `Look` → `Vector2` | `GetAxisRaw("Mouse X")`, `GetAxisRaw("Mouse Y")` | raw mouse delta |
| `LookSmoothed` → `Vector2` | `GetAxis("Mouse X")`, `GetAxis("Mouse Y")` | smoothed mouse delta |
| `Sprint` → `bool` | `GetKey(LeftShift)` | held |
| `JumpDown` → `bool` | `GetButtonDown("Jump") \|\| GetKeyDown(Space)` | pressed |
| `JumpKeyDown` → `bool` | `GetKeyDown(Space)` | pressed |
| `Interact` → `bool` | `GetKeyDown(E)` | pressed |
| `ToggleCursor` → `bool` | `GetKeyDown(F1)` | pressed |
| `RegenWorld` → `bool` | `GetKeyDown(R)` | pressed |
| `ToggleMap` → `bool` | `GetKeyDown(Tab)` | pressed |
| `SaveQuick` → `bool` | `GetKeyDown(F5)` | pressed |
| `LoadQuick` → `bool` | `GetKeyDown(F9)` | pressed |
| `PauseDown` → `bool` | `GetKeyDown(Escape)` | pressed |
| `PauseHeld` → `bool` | `GetKey(Escape)` | held |
| `AttackClick` → `bool` | `GetMouseButtonDown(0)` | pressed |
| `SecondaryClick` → `bool` | `GetMouseButtonDown(1)` | pressed |
| `MeleeSwing` → `bool` | `GetKeyDown(F)` | pressed |
| `NumberKeyDown()` → `int` | scan `Alpha1..Alpha9` | first 1..9 pressed, else 0 |
| `NumberKeyDown(int)` → `bool` | `GetKeyDown(Alpha1+n-1)` | that 1..9 pressed |
| `FunctionKeyDown()` → `int` | scan `F1..F12` | first 1..12 pressed, else 0 |
| `KeyDown(KeyCode)` → `bool` | `GetKeyDown(key)` | passthrough (configurable bindings) |
| `Key(KeyCode)` → `bool` | `GetKey(key)` | passthrough |
| `MouseDown(int)` → `bool` | `GetMouseButtonDown(button)` | passthrough |
| `AxisRaw(string)` → `float` | `GetAxisRaw(axisName)` | passthrough |
| `Axis(string)` → `float` | `GetAxis(axisName)` | passthrough |

That is **~28 raw `UnityEngine.Input.*` calls, all inside this one file** (`EmberInput.cs` lines 26–94).

### 1.5 The five passthrough members are the only hard part
The 20 *semantic* members map cleanly to Input System actions (fixed binding). The five **passthrough** members (`KeyDown`, `Key`, `MouseDown`, `AxisRaw`, `Axis`) take a `KeyCode`/`int`/`string` **at runtime** because call-sites bind a *configurable* `KeyCode` from the inspector. The Input System has no "is this arbitrary `KeyCode` down right now" query as ergonomic as legacy `Input.GetKey(KeyCode)`. These five need a `Keyboard.current`/`Mouse.current` shim (see §6.3). They are used by exactly 4 files (CombatInputAdapter, CombatPlaygroundCameraRig, CombatPlaygroundController, EmberWorldHost) — small and contained.

---

## 2. Every input call-site (file + line)

There are **two layers**: the **raw layer** (28 calls, all in `EmberInput.cs` — these get rewritten) and the **consumer layer** (41 call-sites in 14 files — these call `EmberInput`, behind the facade, and should **not** change at all if the migration is done correctly).

### 2.1 Raw `UnityEngine.Input` calls — the ONLY code that gets rewritten

`Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs`:

| Line | Call |
|---|---|
| 26 | `Input.GetAxisRaw("Horizontal")`, `Input.GetAxisRaw("Vertical")` |
| 29 | `Input.GetAxisRaw("Mouse X")`, `Input.GetAxisRaw("Mouse Y")` |
| 32 | `Input.GetAxis("Mouse X")`, `Input.GetAxis("Mouse Y")` |
| 34 | `Input.GetKey(KeyCode.LeftShift)` |
| 37 | `Input.GetButtonDown("Jump")`, `Input.GetKeyDown(KeyCode.Space)` |
| 40 | `Input.GetKeyDown(KeyCode.Space)` |
| 43 | `Input.GetKeyDown(KeyCode.E)` |
| 44 | `Input.GetKeyDown(KeyCode.F1)` |
| 45 | `Input.GetKeyDown(KeyCode.R)` |
| 46 | `Input.GetKeyDown(KeyCode.Tab)` |
| 49 | `Input.GetKeyDown(KeyCode.F5)` |
| 50 | `Input.GetKeyDown(KeyCode.F9)` |
| 54 | `Input.GetKeyDown(KeyCode.Escape)` |
| 56 | `Input.GetKey(KeyCode.Escape)` |
| 59 | `Input.GetMouseButtonDown(0)` |
| 60 | `Input.GetMouseButtonDown(1)` |
| 61 | `Input.GetKeyDown(KeyCode.F)` |
| 69 | `Input.GetKeyDown(KeyCode.Alpha1 + i)` (loop 0..8) |
| 75 | `Input.GetKeyDown(KeyCode.Alpha1 + (oneBased-1))` |
| 83 | `Input.GetKeyDown(KeyCode.F1 + i)` (loop 0..11) |
| 90 | `Input.GetKeyDown(key)` |
| 91 | `Input.GetKey(key)` |
| 92 | `Input.GetMouseButtonDown(button)` |
| 93 | `Input.GetAxisRaw(axisName)` |
| 94 | `Input.GetAxis(axisName)` |

### 2.2 Consumer call-sites through the facade (must keep working unchanged)

These 41 sites are the regression surface — they should compile and behave identically before and after, **without edits**. Listed so the acceptance test covers each behavior.

`Assets/Scripts/Presentation/Ember/Bootstrap/EmberWorldHost.cs`
- L158 `EmberInput.RegenWorld` (R — Oracle/regen hotkey)
- L180 `EmberInput.KeyDown(KeyCode.C)` (C — character sheet)
- L193 `EmberInput.ToggleMap` (Tab)
- L246 `EmberInput.NumberKeyDown(i + 1)` (hotbar 1..9)
- L282 `EmberInput.PauseDown` (Escape)
- L292 `EmberInput.PauseHeld` (Escape held)

`Assets/Scripts/Presentation/Ember/Camera/EmberFirstPersonController.cs`
- L97 `EmberInput.ToggleCursor` (F1)
- L104–105 `EmberInput.Look.x/.y` (mouse-look)
- L124–125 `EmberInput.Move.x/.y` (WASD)
- L126, L190 `EmberInput.Sprint` (LeftShift)
- L138 `EmberInput.JumpKeyDown` (Space)

`Assets/Scripts/Presentation/Ember/Combat/EmberPlayerSpellCaster.cs`
- L39 `EmberInput.NumberKeyDown(i + 1)` (spell slots 1..9)

`Assets/Scripts/Presentation/Ember/Combat/EmberPlayerMeleeSwing.cs`
- L21 `EmberInput.MeleeSwing` (F)

`Assets/Scripts/Presentation/Ember/Save/EmberSaveService.cs`
- L46 `EmberInput.SaveQuick` (F5)
- L47 `EmberInput.LoadQuick` (F9)

`Assets/Scripts/Presentation/Ember/UI/DialogBoxPanel.cs`
- L147 `EmberInput.AttackClick` (LMB — advance dialog)
- L198 `EmberInput.NumberKeyDown(i + 1)` (dialog topics 1..9)
- L204 `EmberInput.PauseDown` (Escape — close dialog)

`Assets/Scripts/Presentation/Ember/Interaction/EmberPlayerInteractRaycaster.cs`
- L64, L71 `EmberInput.Interact` (E)

`Assets/Scripts/Presentation/Ember/UI/EmberHud.ActionBar.cs`
- L110 `EmberInput.FunctionKeyDown()` (F1..F12 action bar)

`Assets/Scripts/Presentation/Ember/UI/PauseMenu.cs`
- L34 `EmberInput.PauseDown` (Escape)

`Assets/Scripts/Presentation/Combat/CombatInputAdapter.cs`
- L35 `EmberInput.KeyDown(pauseKey)` (configurable)
- L38 `EmberInput.AttackClick` (LMB)
- L40 `EmberInput.SecondaryClick` (RMB)
- L42 `EmberInput.KeyDown(dodgeKey)` (configurable)
- L44 `EmberInput.KeyDown(castKey)` (configurable)

`Assets/Scripts/Presentation/Combat/CombatPlaygroundCameraRig.cs`
- L66 `EmberInput.KeyDown(toggleKey)` (configurable)
- L90–91 `EmberInput.LookSmoothed.x/.y` (orbit camera)

`Assets/Scripts/Presentation/Combat/CombatPlaygroundController.cs`
- L73–74 `EmberInput.Move.x/.y`
- L77–80 `EmberInput.Key(KeyCode.A/D/S/W)` (configurable-style movement)
- L86 `EmberInput.JumpDown`

**Totals:** 1 raw-layer file (~28 raw calls), 14 consumer files, 41 consumer call-sites. **Net editable surface for the whole migration = 1 file body + project settings + 1 InputActions asset + 1 generated wrapper.** No consumer `.cs` file needs to change.

---

## 3. Goal & non-goals

**Goal:** the game runs on `com.unity.inputsystem` with `activeInputHandler` = New, all current input behavior intact, and `EmberInput` remains the single facade (now backed by Input System actions instead of legacy `Input`).

**Non-goals (explicitly out of scope for E7-020):**
- Rebindable-controls UI / runtime key remapping screen. (The passthrough `KeyDown(KeyCode)` sites stay as-is; making them user-remappable is a *future* PRD.)
- Gamepad/touch support beyond what falls out for free. (Actions *can* carry gamepad bindings, but no controller QA is committed here.)
- Changing any control mapping, sensitivity, or feel. **Byte-for-byte behavior parity is the bar.**
- Touching `EventSystem` UI input modules unless the build breaks (see §7 risk R4).

---

## 4. Strategy: five always-green stages

Each stage is independently committable and **must leave the game building + passing the §2 acceptance behaviors**. The ordering guarantees there is never a "half-migrated, broken input" state on `main`.

```
Stage 0  Baseline harness + capture current behavior  (no behavior change)
Stage 1  Install package, set Active Input Handling = Both  (no behavior change — legacy still drives)
Stage 2  Author InputActions asset + generated C# wrapper  (asset only, not yet referenced by game)
Stage 3  Swap EmberInput body to Input System BEHIND a compile flag; keep legacy fallback  (parity)
Stage 4  Flip Active Input Handling = New, delete legacy body + flag  (final)
```

> Stages 1–3 all run with `activeInputHandler = 2` (Both), so at every commit the legacy Input Manager is still available as a live fallback. Only Stage 4 removes that safety net, and only after Stage 3 has proven parity.

---

## 5. Stage 0 — Regression baseline (do this FIRST, before any package change)

**Why first:** you cannot prove "behavior unchanged" without a recording of the *current* behavior taken on the *current* (legacy) system. Capture it before touching anything.

### 5.0 Reality check on the existing proof driver
The headless driver `Assets/Scripts/Presentation/Ember/Diagnostics/EmberProofScreenshotDriver.cs` (flags `--ember-proof-screenshots`, `--ember-rescue-proof`, `--ember-scene-tour`, `--ember-llm-proof`, `--ember-forge-proof`, `--ember-proof-quit`) **drives the game by direct API calls** (`creation.Continue()`, `worldgen.AnswerQuestion(0)`, `SceneManager.LoadScene(...)`), **not by injecting keystrokes**. So as-is it does **not** exercise `EmberInput` / `UnityEngine.Input` at all and cannot, by itself, prove input parity. The baseline must add an input-exercising path. Two complementary mechanisms:

### 5.1 EditMode/PlayMode unit baseline of the facade (cheapest, deterministic)
Add a PlayMode test fixture under `Assets/Tests/PlayMode/Input/` (new folder) that uses the Input System's **`InputTestFixture`** to feed synthetic device events and assert each `EmberInput` member. This is only meaningful *after* Stage 2/3 (when `EmberInput` is Input-System-backed and the package's test fixture exists), so the Stage-0 deliverable here is the **golden table**, not the runner:
- **Stage 0 deliverable:** `docs/proofs/input-baseline-2026-05-31.md` — a frozen table mapping each of the 25 `EmberInput` members to its expected truth/value for a defined sequence of key/mouse states, derived from §1.4. This is the spec the Stage-3 fixture asserts against. Capture it by **reading the legacy bodies** (already done in §1.4) — no runtime needed, it is pure mapping.

### 5.2 PlayMode behavior baseline via a new keystroke-driving proof flag (authoritative)
Add (in Stage 0, still on legacy) a new branch to `EmberProofScreenshotDriver` — flag `--ember-input-proof` — that, instead of calling gameplay APIs directly, **simulates input** and screenshots the *consequence*:
- On legacy (Stage 0): drive via the legacy path. Because legacy `Input` cannot be fed synthetic events from script without the Input System, the Stage-0 version asserts behavior **through the facade contract** rather than real keystrokes — i.e. it calls `EmberInput.PauseDown` etc. and screenshots/logs that the wired consumer reacts (pause menu opens, dialog advances, hotbar selects slot N, save file written on F5). Capture the screenshots + a `input-proof.log` listing, per hotkey, "fired → observed effect".
- From Stage 3 onward (Input-System-backed `EmberInput`): the **same** `--ember-input-proof` flow is re-run, but now it can use `InputTestFixture` / `InputSystem.QueueStateEvent` on `Keyboard.current`/`Mouse.current` to press real synthetic keys and confirm the *whole* chain (device event → action → `EmberInput` → consumer effect). Identical screenshots/log ⇒ parity proven.

**Stage 0 acceptance:** `docs/proofs/input-baseline-2026-05-31.md` exists with the 25-member golden table; `--ember-input-proof` flag added and produces a baseline screenshot set + `input-proof.log` on the **current legacy** build; both committed under `docs/proofs/`. Per `docs/proofs/README.md`, label these artifacts `Unity PlayMode` (the screenshot run) and `source-only` (the golden table).

### 5.3 Files added in Stage 0
- **Add:** `docs/proofs/input-baseline-2026-05-31.md` (golden table).
- **Modify:** `Assets/Scripts/Presentation/Ember/Diagnostics/EmberProofScreenshotDriver.cs` — add `--ember-input-proof` branch + `RunInputProof()` coroutine (mirrors existing `RunForgeProof()` pattern; gated by `HasArg`). Pure addition, no existing branch touched.
- **Add (Stage 0, runner stub):** `Assets/Tests/PlayMode/Input/EmberInputContractTests.cs` skeleton with `[Ignore]` until Stage 3 wires real assertions (so the asmdef/folder exists and is reviewed early).
- **Add:** `Assets/Tests/PlayMode/Input.meta` folder meta + the test file's `.meta`.

---

## 6. Stages 1–4 — the migration

### 6.1 Stage 1 — Install package + Active Input Handling = Both
**No behavior change. Legacy still drives the game.**

- **Modify `Packages/manifest.json`:** add to `dependencies`:
  ```jsonc
  "com.unity.inputsystem": "1.14.2"   // pin to the version Unity 6 resolves for this editor; confirm via Package Manager
  ```
  (Use the editor's recommended version for the installed Unity 6 line; do not float.)
- **Modify `Packages/packages-lock.json`:** will be regenerated by UPM on resolve — commit the regenerated lock.
- **Modify `ProjectSettings/ProjectSettings.asset`:** set `activeInputHandler: 2` (Both). This triggers an editor restart prompt; the **legacy `UnityEngine.Input` API keeps working** under "Both," so `EmberInput`'s current body is unaffected.
- **Modify `Assets/Scripts/Presentation/EmberCrpg.Presentation.asmdef`:** add `"Unity.InputSystem"` to `references` so `EmberInput` (and later the generated wrapper) can compile against the package.
- **Modify `Assets/Tests/PlayMode/EmberCrpg.Tests.PlayMode.asmdef`:** add `"Unity.InputSystem"` and `"Unity.InputSystem.TestFramework"` to `references` (the latter provides `InputTestFixture`).

**Stage 1 acceptance:** project compiles; `--ember-input-proof` baseline reproduces identically (still legacy-driven); EditMode + PlayMode suites green. Commit.

### 6.2 Stage 2 — Author the InputActions asset + generated wrapper
**Asset only. Not yet referenced by `EmberInput`. No behavior change.**

- **Add:** `Assets/Settings/Input/EmberControls.inputactions` — an Input Action Asset with **one map** (`Gameplay`) plus the actions below. Enable **"Generate C# Class"** on the asset (Unity writes the wrapper next to it).
- **Add (generated):** `Assets/Settings/Input/EmberControls.cs` — the auto-generated `IInputActionCollection2` wrapper (`@EmberControls`). Committed so CI builds without needing the editor to regenerate.
- **Add:** `.meta` files for both.

Action list (each maps a §1.4 member; bindings reproduce the exact legacy keys):

| Action | Type | Binding(s) | Backs |
|---|---|---|---|
| `Move` | Value / Vector2 | 2D Composite WASD + Arrows | `Move` |
| `Look` | Value / Vector2 | `<Mouse>/delta` | `Look`, `LookSmoothed`* |
| `Sprint` | Button | `<Keyboard>/leftShift` | `Sprint` |
| `Jump` | Button | `<Keyboard>/space` | `JumpDown`, `JumpKeyDown` |
| `Interact` | Button | `<Keyboard>/e` | `Interact` |
| `ToggleCursor` | Button | `<Keyboard>/f1` | `ToggleCursor` |
| `RegenWorld` | Button | `<Keyboard>/r` | `RegenWorld` |
| `ToggleMap` | Button | `<Keyboard>/tab` | `ToggleMap` |
| `SaveQuick` | Button | `<Keyboard>/f5` | `SaveQuick` |
| `LoadQuick` | Button | `<Keyboard>/f9` | `LoadQuick` |
| `Pause` | Button | `<Keyboard>/escape` | `PauseDown`, `PauseHeld` |
| `Attack` | Button | `<Mouse>/leftButton` | `AttackClick` |
| `Secondary` | Button | `<Mouse>/rightButton` | `SecondaryClick` |
| `MeleeSwing` | Button | `<Keyboard>/f` | `MeleeSwing` |
| `Number1..Number9` | Button ×9 | `<Keyboard>/1`..`/9` | `NumberKeyDown()/(int)` |
| `Function1..Function12` | Button ×12 | `<Keyboard>/f1`..`/f12` | `FunctionKeyDown()` |

\* **`LookSmoothed` note:** legacy `GetAxis("Mouse X")` applies Input-Manager smoothing/sensitivity; `<Mouse>/delta` is raw. To preserve feel for the orbit rig (`CombatPlaygroundCameraRig`), either (a) add a **Processor** on a second `LookSmoothed` action, or (b) keep `LookSmoothed` computing a short moving-average in `EmberInput` from `Look`. Decision recorded at Stage 3; default to (b) to avoid per-binding processor tuning. This is the one place where exact numeric parity is *approximate* — call it out in the Stage-3 proof.

**Jump composite note:** legacy `JumpDown` = `GetButtonDown("Jump") || GetKeyDown(Space)`. The "Jump" virtual button (Input Manager) historically also maps to Space, so binding `Jump` action to Space alone preserves observed behavior; if any non-Space "Jump" binding exists in `InputManager.asset`, add it to the `Jump` action too. (Verify `ProjectSettings/InputManager.asset` during Stage 2.)

**Stage 2 acceptance:** asset + wrapper compile; game still legacy-driven (wrapper unused); baseline reproduces. Commit.

### 6.3 Stage 3 — Swap `EmberInput` body, keep legacy fallback behind a flag (PARITY GATE)
**This is the only stage that changes how input is read. Game still runs under "Both," so a regression can be reverted by flipping one symbol.**

- **Modify `Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs`** — rewrite the *body* only; the **public signatures in §1.4 stay identical** so all 41 consumer sites are untouched. Strategy:
  - Wrap the new implementation in `#if EMBER_INPUTSYSTEM` … `#else` (legacy body) `#endif`. Add `EMBER_INPUTSYSTEM` to Scripting Define Symbols for Standalone+Editor only in this stage. This lets a single define toggle the whole game between old and new input for A/B parity testing without code churn.
  - New body owns a singleton `@EmberControls` instance, enables the `Gameplay` map on first access (lazy `RuntimeInitializeOnLoadMethod`), and implements:
    - Semantic buttons (`SaveQuick`, `PauseDown`, etc.) → `action.WasPressedThisFrame()`.
    - Held (`Sprint`, `PauseHeld`) → `action.IsPressed()`.
    - `Move`/`Look` → `action.ReadValue<Vector2>()`.
    - `NumberKeyDown()/(int)`, `FunctionKeyDown()` → poll the `Number*/Function*` actions' `WasPressedThisFrame()`.
    - **Passthroughs** (`KeyDown`/`Key`/`MouseDown`/`AxisRaw`/`Axis`): implement against `Keyboard.current`/`Mouse.current` directly, mapping the runtime `KeyCode`→`Key` enum (a static `KeyCode→Key` lookup), e.g. `KeyDown(KeyCode) => Keyboard.current[Map(key)].wasPressedThisFrame`. This preserves the *configurable inspector KeyCode* contract that the 4 combat/playground files rely on, with **no change to those files**.
- **Modify `Assets/Tests/PlayMode/Input/EmberInputContractTests.cs`** — remove `[Ignore]`; implement `InputTestFixture`-based assertions against the §5.1 golden table (press synthetic keys → assert `EmberInput.X`). These run under `EMBER_INPUTSYSTEM`.
- **Re-run `--ember-input-proof`** now driving real synthetic keystrokes (Stage-3 form, §5.2) and diff screenshots/log against the Stage-0 baseline.

**Stage 3 acceptance (the parity gate — all must pass before Stage 4):**
1. `EmberInputContractTests` green for all 25 members.
2. `--ember-input-proof` PlayMode run: every §2.2 behavior fires (pause opens/closes, dialog advances on LMB + closes on Esc, hotbar 1..9, action bar F1..F12, F5 save / F9 load, E interact, R oracle, Tab map, WASD move, mouse-look, sprint, melee F, RMB secondary, combat dodge/cast/toggle configurable keys). Screenshots match baseline (modulo the `LookSmoothed` numeric caveat).
3. EditMode + full PlayMode suites green.
4. Toggling `EMBER_INPUTSYSTEM` off reverts to byte-identical legacy behavior (proves the fallback is intact).
Commit.

### 6.4 Stage 4 — Flip to New, delete legacy
**Final. Removes the legacy safety net only after Stage 3 proved parity.**

- **Modify `ProjectSettings/ProjectSettings.asset`:** `activeInputHandler: 1` (Input System only).
- **Modify `EmberInput.cs`:** delete the `#else` legacy branch and the `#if EMBER_INPUTSYSTEM` guard; the Input-System body becomes the only body. Remove the `EMBER_INPUTSYSTEM` define from Player Settings.
- **Verify** no remaining `UnityEngine.Input` reference exists anywhere in `Assets/` (the static-audit grep that this PRD used should return only comments / zero).
- **EventSystem / UI:** confirm any scene `EventSystem` uses `InputSystemUIInputModule` (Unity auto-offers to replace `StandaloneInputModule` when handler = New). If a scene still has the legacy module, swap it. (See R4.)

**Stage 4 acceptance:** full §8 acceptance + §9 verification (Win64 build + `--ember-input-proof`). Commit. E7-020 closed.

---

## 7. Risks & mitigations

| # | Risk | Likelihood | Mitigation |
|---|---|---|---|
| R1 | A regression slips in because "behavior unchanged" was never actually measured. | High if skipped | **Stage 0 baseline is mandatory and first.** Stage 3 diffs against it. |
| R2 | `LookSmoothed` feels different (legacy axis smoothing vs raw `<Mouse>/delta`). | Medium | Compute smoothing in `EmberInput` from `Look` (default) or processor; flag the approx-parity in proof; only affects `CombatPlaygroundCameraRig`. |
| R3 | Passthrough `KeyDown(KeyCode)` sites (4 files, configurable keys) have no clean Input-System equivalent. | Medium | Implement `Keyboard.current`/`Mouse.current` + `KeyCode→Key` map inside the facade; **no consumer file changes**; covered by contract tests. |
| R4 | UI clicks/navigation break because `EventSystem` still uses the legacy `StandaloneInputModule` after flip to New. | Medium | Stage 4 explicitly checks + swaps to `InputSystemUIInputModule`; UI button behavior is in the `--ember-input-proof` acceptance (pause menu buttons, dialog). |
| R5 | `activeInputHandler` change forces an editor restart / domain reload mid-stage. | High (expected) | Sequenced so the restart happens at Stage 1 (Both, no behavior change) and Stage 4 (final); never mid-parity. |
| R6 | CI Win64 build (`Windows64BuildMenu.Build`) fails because the package adds new scripting defines / the generated wrapper isn't committed. | Medium | Commit `EmberControls.cs` wrapper + `packages-lock.json`; Stage 1 adds the asmdef reference so it compiles in CI before the body swap. |
| R7 | New input keystrokes can't be injected into the existing API-driven proof driver, so "it works" stays unproven. | High if ignored | Stage 0 adds `--ember-input-proof`; Stage 3 upgrades it to real `InputTestFixture` synthetic events. |
| R8 | Cinemachine 3.1.6 input axis providers expect legacy input. | Low | No Cinemachine `InputAxisController`/axis-provider hits found in the call-site survey; if a vcam uses one, point it at the new actions during Stage 3. Verify during Stage 2. |
| R9 | Some scene `.unity` references a `PlayerInput` component or input asset that doesn't exist yet. | Low | None found today (no `PlayerInput` usage); `EmberInput` is polled statically, not via `PlayerInput` callbacks, so scene assets are unaffected. |

---

## 8. Acceptance criteria (whole feature, post-Stage-4)

All verified via `--ember-input-proof` PlayMode run + manual smoke on a Win64 build:

- **Movement:** WASD/arrows move the first-person controller and combat-playground controller; sprint (LeftShift) accelerates; jump (Space) fires.
- **Mouse-look:** first-person look (raw) and combat orbit rig (smoothed) both rotate the camera; `ToggleCursor` (F1) toggles cursor lock.
- **Hotkeys (EmberWorldHost + services):** F5 quicksave writes a save; F9 quickload loads; Tab toggles map/inventory; C opens character sheet; R triggers oracle/regen; Escape opens **and** closes pause (down + held paths).
- **Action bar / slots:** F1–F12 select action-bar slots; number row 1–9 selects hotbar slots, spell slots, and dialog topics in their respective contexts.
- **Combat:** LMB attack, RMB secondary, F melee swing, configurable dodge/cast/pause keys all fire through `CombatInputAdapter`.
- **Dialog:** LMB advances `DialogBoxPanel`; number keys pick topics; Escape closes.
- **Interaction:** E interacts via the raycaster.
- **Parity:** `EmberInputContractTests` (25 members) green; proof screenshots match the Stage-0 baseline (modulo documented `LookSmoothed` caveat).
- **No legacy left:** grep of `Assets/**/*.cs` for `UnityEngine.Input` / `[^.\w]Input.` returns only comments; `activeInputHandler: 1`.

---

## 9. Verification (commands & proofs)

Per `docs/proofs/README.md`, runtime claims need `Unity PlayMode` / player evidence; source-only checks need the validation harness.

1. **Source/structure:**
   ```bash
   bash tools/validation/static-audit.sh
   bash tools/validation/run-validation.sh --mode fallback
   ```
2. **Tests (EditMode + PlayMode incl. `EmberInputContractTests`):** run the Unity Test Runner PlayMode + EditMode suites (or the `tests-run` MCP skill) — all green.
3. **Win64 build (the real proof the package didn't break the player):** the opt-in CI job `build-windows` in `.github/workflows/unity-test.yml` (`workflow_dispatch` with `run_build_windows=true`, or nightly cron) builds via `EmberCrpg.Editor.Ember.Build.Windows64BuildMenu.Build` → `Builds/Windows64`. A green Win64 build with the Input System package + new defines confirms compile/link integrity.
4. **Behavior proof:** run the player (or editor PlayMode) with
   ```
   -batchmode? no — needs a window for input; run windowed:
   alcyone-ember-rpg.exe --ember-proof-screenshots <outDir> --ember-input-proof --ember-proof-quit
   ```
   Compare the produced screenshots + `input-proof.log` against `docs/proofs/input-baseline-2026-05-31.md`. Save the post-migration artifacts as `docs/proofs/input-migrated-<date>.md` (+ screenshots), labeled `Unity PlayMode`.

---

## 10. Exact file manifest (what each stage adds/modifies)

**Add:**
- `docs/proofs/input-baseline-2026-05-31.md` (Stage 0)
- `Assets/Tests/PlayMode/Input/EmberInputContractTests.cs` (+ `.meta`) (Stage 0 stub → Stage 3 real)
- `Assets/Tests/PlayMode/Input.meta` (folder)
- `Assets/Settings/Input/EmberControls.inputactions` (+ `.meta`) (Stage 2)
- `Assets/Settings/Input/EmberControls.cs` (+ `.meta`, generated wrapper) (Stage 2)
- `docs/proofs/input-migrated-<date>.md` (+ screenshots) (Stage 4 verification)

**Modify:**
- `Packages/manifest.json` — add `com.unity.inputsystem` (Stage 1)
- `Packages/packages-lock.json` — regenerated (Stage 1)
- `ProjectSettings/ProjectSettings.asset` — `activeInputHandler: 0 → 2` (Stage 1) → `2 → 1` (Stage 4)
- `Assets/Scripts/Presentation/EmberCrpg.Presentation.asmdef` — add `Unity.InputSystem` ref (Stage 1)
- `Assets/Tests/PlayMode/EmberCrpg.Tests.PlayMode.asmdef` — add `Unity.InputSystem` + `Unity.InputSystem.TestFramework` refs (Stage 1)
- `Assets/Scripts/Presentation/Ember/Diagnostics/EmberProofScreenshotDriver.cs` — add `--ember-input-proof` branch (Stage 0)
- `Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs` — body swap behind `EMBER_INPUTSYSTEM` (Stage 3) → delete legacy branch (Stage 4)
- `ProjectSettings/ProjectSettings.asset` Scripting Define Symbols — add `EMBER_INPUTSYSTEM` (Stage 3) → remove (Stage 4)
- Scene `EventSystem` objects, **only if** they carry `StandaloneInputModule` → `InputSystemUIInputModule` (Stage 4, conditional)

**Do NOT modify:** any of the 14 consumer files in §2.2. If a consumer file needs editing, the facade swap was done wrong.

---

## 11. Definition of done
- All 5 stages committed, each green at commit time.
- `activeInputHandler: 1`; `com.unity.inputsystem` in manifest + lock.
- `EmberInput` is Input-System-backed, single facade preserved, 41 consumer sites unchanged.
- `EmberInputContractTests` (25 members) green; `--ember-input-proof` parity proof matches baseline; Win64 build green.
- Zero `UnityEngine.Input` references remain in `Assets/` (comments excepted).
- E7-020 marked closed in the audit counter.
