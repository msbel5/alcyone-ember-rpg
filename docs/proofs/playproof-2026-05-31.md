# V2 Play-Proof — black-box run of the built Win64 exe (2026-05-31)

Built `Builds/Windows64/alcyone-ember-rpg.exe` (Win64 batchmode, `Build Finished,
Result: Success`, 0 `error CS`), launched it windowed (1600x900) and drove it via
Windows-MCP (screenshot + Win32 click). This is a real Play-Mode run showing the V2
remediation (P1 correctness fixes, P2 dead-code deletion, namespace renames incl.
INP-01, ~73k lines of doc cleanup) did NOT regress the boot → menu → genesis flow.

> **Honesty note:** this is a Play-Mode scene proof (the built exe was actually launched and
> driven). A static / EditMode / "build succeeded" green is **not** equivalent to a Play-Mode
> scene proof — it does not exercise scene load, camera, input, or the forge/LLM at runtime.
> Conversely, this run boots the subsystems but does **not** by itself prove real (non-fallback)
> LLM replies or generated art; that remains UNVERIFIED (see `docs/AI_STACK.md`).

Observed, in order:
1. Boot → **"Backend ready. Missing assets are generated visibly on New Game."** —
   the ONNX forge + local LLM subsystems *initialised* without error at runtime. Note: a
   "Backend ready" banner is a boot-success signal, not proof of real (non-fallback) Qwen
   output or real generated art — that still needs the LFS binaries resolved + a verified run
   (see `docs/AI_STACK.md` and `docs/proofs/llm-roundtrip-2026-05-30.md`).
2. **Main Menu** renders with the Ember design system: gold "EMBER CRPG" title
   (Jost), "A dark fantasy chronicle" subtitle (Spectral italic), parchment-on-brown
   rounded buttons (New Game / Resume / Load Game / Options / Exit), dark-fantasy
   backdrop.
3. **New Game → "IMMERSIVE CHARACTER CREATION"** (stage 1 / Name): deterministic
   name suggestions (Ash-Born Commander / Cinder Vey / Mora of the Red Road),
   "Show Advanced Settings", Continue.
4. **Continue → stage 2 "The World's Mood"**: gold progress bar advanced, the
   Ember-unique world-genesis question rendered (A Grim / B Mythic / C Gritty /
   D High — "the canvas the Forge will paint on"), Back/Continue nav.

Verdict: build healthy; Boot → forge/LLM *init* → MainMenu → New Game → multi-stage
Genesis wizard all render and navigate. No regression from the remediation. (Deeper
in-scene checks — HUD-02 dialog reachability, the action-bar, NPC Ask-About — still
warrant a full manual playtest; those are the [E] items. Real LLM/art output is a
separate, still-UNVERIFIED proof — see `docs/AI_STACK.md`.)
