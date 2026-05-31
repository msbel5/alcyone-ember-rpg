# Black-box play-proof — living world + playability (2026-05-31)

Real Play-Mode run of the freshly-built `Builds/Windows64/alcyone-ember-rpg.exe`, after the
SOUL living-world wiring + the breaking dialog-interface change + HUD-02/SCN-01. Driven via
Windows screen-capture + computer-use clicks. This is a runtime regression + living-world proof,
not a static green.

> Honesty note: a Play-Mode run proves boot/flow/no-crash and that the tick advances live; it does
> NOT by itself prove real (non-fallback) LLM output or generated art (those need resolved LFS
> binaries). The deterministic living-world claim is independently proven headlessly by
> `WorldLivesOverNTicksTests`.

## What was observed (screenshots in `docs/proofs/`)

1. **Boot → Main Menu** (`playproof-soul.png`): boots cleanly to the Ember-styled main menu
   (gold "EMBER CRPG" title, New Game / Resume / Load Game / Options / Exit). **No boot regression**
   despite the 4 new `WorldState` stores, the living-world tick wiring, the breaking
   `GetDialogSource(ActorId)` / `TryReadActor(ActorId)` interface change, the HUD-02 runtime
   DialogBoxPanel ensure, and the SCN-01 EmberScenes constants.

2. **New Game → live gameplay** (`playproof-2026-05-31-living-world-gameplay.png`): reaches a
   3D-billboard gameplay scene. Visible: **"Tick 0015  Day 001"** (the deterministic sim clock),
   multiple **billboard NPC actors**, the **EmberHud vitals** (Health / Fatigue / Mana), and the
   **action bar including a TALK button** (HUD-02 Ask-About is reachable in-scene).

3. **The world ticks live** (`playproof-2026-05-31-tick-advanced.png`): ~6 seconds later the
   counter reads **"Tick 0096  Day 001"** — the in-game `WorldTickComposer` (now carrying the
   SOUL-01 PlantGrowth / JobAssignment / PriceUpdate systems + SOUL-03 ScheduleSystem) is
   advancing live. A gold **"Showroom Gate ←" portal label** is also visible (SCN-01 portals
   present/reachable). No crash.

## Verdict
- Boot → menu → New Game → live gameplay all function; the simulation tick advances in the running
  game. The deep SOUL + dialog-interface refactor did **not** regress the playable flow.
- Combined with: fallback harness green at 1220 (incl. the real `WorldLivesOverNTicksTests` proving
  crops grow + a job is claimed + a price drifts over 2 game-days, deterministically) and five
  Win64 batchmode builds at `Build Finished, Result: Success` / 0 `error CS`.

## Still requires an Editor/visual pass (flagged, not faked)
- **SOUL-04 worldgen spawner**: the per-tick id-keyed ActorView position-sync is wired (so SOUL-03
  movement reaches existing billboards). The from-worldgen spawner now **exists** —
  `EmberGeneratedActorSpawner` instantiates one camera-facing billboard per generated NPC, stamps each
  with its `ActorId`, and (fixed 2026-05-31) uses real registry sprite keys + 2.1u sizing + ring-spiral
  scatter + a 6-actor cap so they no longer stack as magenta giants. What remains is the **visual
  confirmation** (same-seed screenshot of a readable, speakable generated population) — still `[E]`.
- **ARCH-06 OracleShrine dialog**, deep Ask-About topic exchange, and the SCN-01 portal *traversal*
  between all scenes were not click-walked end-to-end here.
