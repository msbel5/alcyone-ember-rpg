# Faz 11 - Atom map (Unity visual layer)

_Date:_ 2026-05-20
_Branch:_ `mami/faz-6-12-atom-maps`
_Primary boxes:_ Unity presentation only (not a mechanic-map gameplay box)
_Roadmap:_ `docs/ROADMAP.md`
_Mechanic map:_ `DOCS/mechanic-map-v1.md`
_Execution ledger:_ `DOCS/faz-5-12-execution-ledger.md`
_Agent rules:_ `DOCS/agent-rules-v2.md`
_Vision notes:_ `DOCS/EMBER_VISION_NOTES_MAMI.md`
_Inspector checklist:_ `DOCS/inspector-audit-checklist.md`

## Ownership boundary (per agent-rules-v2 Rule 6)

This sprint is **Mami territory**. Captain MAY atom-map and write specs here; Captain MAY NOT implement scenes, prefabs, sprites, materials, shaders, or any binary asset under `Assets/Scenes/`, `Assets/Art/`, `Assets/Prefabs/`, or `DOCS/screenshots/`. Captain MAY add pure-C# snapshot rows under `Assets/Scripts/Presentation/VisualLayer/` that read simulation stores and produce serializable view DTOs.

Mami implements Unity scenes, prefabs, art, and visual acceptance evidence. No fake screenshots, no transparent PNGs.

## Vision anchors

4. Morrowind-shaped immersion: the player experiences the world from a character's perspective. Faz 11 is the first time the player **sees** the simulation results.
8. Systemic interaction: the same screen surfaces actor needs, faction relations, plant growth, combat events, memory cues, and DM checkpoints together.

## Phase fences

- No new gameplay mechanics are introduced here. Faz 11 only renders what stores already hold.
- No LLM-driven UI before Faz 12.

## Debt ledger action

Faz 11 does not consume `CO-02 / CO-03 / CO-06 / CO-07`. These remain PROCESS-owned and stay on the Faz 5 / 6 / 7 ledgers.

## Sprint goal

Target acceptance sentence from `docs/ROADMAP.md`:

`every previous phase has a one-screenshot Unity proof in DOCS/screenshots/<phase>.png` — Mami-produced, not Captain-produced.

## Atomic decomposition (Mami-owned unless tagged "Captain")

| Atom | Owner | File / class | Responsibility | Closing proof | Status |
|---:|---|---|---|---|---|
| 1 | Captain | `Assets/Scripts/Presentation/VisualLayer/JobDebugSnapshot.cs` | Pure-C# read snapshot: actor / job / worksite / queue index rows. No UnityEngine. | `JobDebugSnapshotTests` | queued |
| 2 | Captain | `Assets/Scripts/Presentation/VisualLayer/ColonyNeedsSnapshot.cs` | Pure-C# read snapshot of actor hunger/fatigue/mood/refusal state. | `ColonyNeedsSnapshotTests` | queued |
| 3 | Captain | `Assets/Scripts/Presentation/VisualLayer/SeasonClockSnapshot.cs` | Pure-C# read snapshot of current season, day-of-year, weather. | `SeasonClockSnapshotTests` | queued |
| 4 | Captain | `Assets/Scripts/Presentation/VisualLayer/FactionRelationSnapshot.cs` | Pure-C# read snapshot of faction pair reputation. | `FactionRelationSnapshotTests` | queued |
| 5 | Captain | `Assets/Scripts/Presentation/VisualLayer/CombatEventTailSnapshot.cs` | Pure-C# read snapshot: tail of `CombatResolved` events with attacker, defender, damage. | `CombatEventTailSnapshotTests` | queued |
| 6 | Captain | `Assets/Scripts/Presentation/VisualLayer/MemoryFactSnapshot.cs` | Pure-C# read snapshot of actor memory facts indexed by topic. | `MemoryFactSnapshotTests` | queued |
| 7 | Captain | `Assets/Scripts/Presentation/VisualLayer/ToolCallTraceSnapshot.cs` | Pure-C# read snapshot of recent ToolCall traces. | `ToolCallTraceSnapshotTests` | queued |
| 8 | Mami | `Assets/Scenes/Faz3SmithingOverworld.unity` | Smith queue / furnace acceptance scene; renders `JobDebugSnapshot`. | screenshot at `DOCS/screenshots/faz-3.png` | Mami |
| 9 | Mami | `Assets/Scenes/Faz4ColonyNeeds.unity` | Hunger/fatigue/mood + refusal HUD. | `DOCS/screenshots/faz-4.png` | Mami |
| 10 | Mami | `Assets/Scenes/Faz5SeasonFarm.unity` | Season clock, plant growth, harvest. | `DOCS/screenshots/faz-5.png` | Mami |
| 11 | Mami | `Assets/Scenes/Faz6TradeFaction.unity` | Caravan, prices, faction relations. | `DOCS/screenshots/faz-6.png` | Mami |
| 12 | Mami | `Assets/Scenes/Faz7Combat.unity` | Equip, attack, armor reduction, hostile faction. | `DOCS/screenshots/faz-7.png` | Mami |
| 13 | Mami | `Assets/Scenes/Faz8Magic.unity` | Fire on oil tile, area damage. | `DOCS/screenshots/faz-8.png` | Mami |
| 14 | Mami | `Assets/Scenes/Faz9DialogueMemory.unity` | Ask About / Tell Me About surface, NPC refusal on remembered crime. | `DOCS/screenshots/faz-9.png` | Mami |
| 15 | Mami | `Assets/Scenes/Faz10DmQuery.unity` | F9 DM snapshot HUD with memory + faction + ToolCall list. | `DOCS/screenshots/faz-10.png` | Mami |
| 16 | Mami | Faz 11 promotion screenshot index | Index page listing every screenshot. | `DOCS/screenshots/INDEX.md` | Mami |

## Suggested bundles (Captain side only)

1. `snapshot-rows-batch-1` — Atoms 1, 2, 3 (Faz 3 / 4 / 5 snapshots).
2. `snapshot-rows-batch-2` — Atoms 4, 5 (Faz 6 / 7 snapshots).
3. `snapshot-rows-batch-3` — Atoms 6, 7 (Faz 9 / 10 snapshots).

Mami-owned scene atoms (8-16) ship in `mami/*` branches against the corresponding faz milestone, not within a single mega-PR.

## Promotion checklist

- [ ] Atoms 1-7 (Captain snapshot rows) are checked off with passing pure-C# tests.
- [ ] Atoms 8-16 (Mami scenes + screenshots) live under `Assets/Scenes/` and `DOCS/screenshots/`.
- [ ] No Captain commit touches `Assets/Scenes/`, `Assets/Art/`, `Assets/Prefabs/`, or `DOCS/screenshots/`.
- [ ] `./tools/validation/run-validation.sh --mode fallback` passes on each Captain merge.
- [ ] Faz 11 promotion summary documents the ownership split and confirms no fake artifacts shipped.

## Next increment

Captain begins with the `snapshot-rows-batch-1` bundle: Atoms 1, 2, 3 with pure-C# tests. Mami begins with Atom 8 (Faz 3 smithing scene) in parallel.
