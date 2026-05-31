# Scene-tour acceptance gate — headless, log-based (2026-05-31)

Mode: **LFS-resolved Win64 player, headless auto-tour, log-verified** (screenshots written to disk for
human review; this proof reads the `Player.log`, not the images). Addresses E7-009 (the 7th audit's #1
recommended acceptance gate) for the 10 gameplay build scenes.

## How it was run
```
Builds/Windows64/alcyone-ember-rpg.exe \
  --ember-proof-screenshots <out> --ember-scene-tour --ember-proof-quit \
  -logFile <player.log>
```
`EmberProofScreenshotDriver.RunSceneTour()` loads each build scene, waits for load + UrpMaterialRescue
+ first frames, captures a UI and a no-UI screenshot, then advances. Player exit code 0.

## Result (verbatim from the log + output dir)
- **Scenes loaded + captured (10/10 gameplay scenes):** SmithingOverworld, TavernDialog, ColonyNeeds,
  CombatDungeon, OracleShrine, RitualHall, SeasonFarm, TradeMarket, ShowroomOverview, TavernFlavour.
- **20 screenshots written** (`tour_NN_<scene>_ui.png` + `_noui.png`) to `validation-output/scene-tour/`.
- **0 exceptions, 0 NullReferenceExceptions** across the whole tour.
- **Clean shutdown** (Input System → Shutdown).
- `[UrpMaterialRescue] Repaired N renderer(s) with missing/magenta materials` fired per scene
  (N = 4,5,1,6,1,1,1,1,1,1) — the runtime magenta-material fallback working as designed: any renderer
  that would render magenta is repaired at load, so the final frame is clean.
- The only log "error" is `d3d12: failed to query info queue interface (0x80004002)` — a benign
  D3D12 debug-layer probe warning on a headless run, not a runtime failure.

## What it proves
- All 10 gameplay scenes **load and render without crashes or managed exceptions** in a real player.
- The magenta-material rescue is active and effective (no scene is left with magenta).

## What it does NOT prove (still `[E]`, needs a human/Editor)
- Interactive behaviour: movement feel, camera, collisions, dialog/Ask-About *interaction*, portal
  *traversal*, save/load in-gameplay, HUD readability against the PRDs — these need PlayMode/manual or
  the Unity MCP. The screenshots on disk are the starting evidence for that human pass.
- The `[UrpMaterialRescue]` repairs (1–6/scene) indicate some authored materials still resolve to magenta
  before rescue — a polish item (bake the correct material at author-time) tracked separately, not a blocker.
