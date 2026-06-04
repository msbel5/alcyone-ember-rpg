# In-Game UI — Implementation Plan (Claude Design → Unity)

Source design: `Downloads/ingame-ui/` (ig-ds + ig-world + ig-player + ig-interaction + ig-system + ig-app).
Same design language as the character-creation redesign that already shipped — so we **reuse the proven
pattern** rather than invent a new one.

## The proven pattern (from char-creation, do not re-derive)

- One UI-Toolkit **view class per screen** built in C# `VisualElement`s (no UXML/USS).
- Hardcode the **DS palette** as `Color` constants; load **Jost + Spectral** from `Resources/Fonts`
  (Cinzel → Spectral fallback). These already exist.
- Each view **self-scales a 1920×1080 stage** to fit the panel (the `FitStage` trick), so design px map 1:1.
- Route every screen through a controller/host; **verify each in-game** with the proof-screenshot driver.

The in-game design literally reuses the same tokens (`G.gold = #FFD94C`, etc.) and adds shared primitives
(`GameModal`, `TabbedModal`, `BottomHud`, `TopBar`, `VBar`, `ItemSlot`, `SBtn`) — these become reusable C#
builders, exactly like char-creation's `ChoiceTile`/`Card`.

## Two layers

1. **World overlay** (always over the live 3D scene): `TopBar` (day/time/location · gold · level) +
   `BottomHud` (event-log line · HP/FAT/MP vitals · 5-slot spell bar · I/C/M/J/K/DM buttons). This **replaces
   the current `EmberHud` + 12-button action bar** — the trickiest integration, because the live HUD wires to
   real actions and keybinds. Same data source (`IEmberHudSource` on `EmberWorldHost`), new presentation.
2. **Modals** (full-screen, opened by a key): `GameModal`/`TabbedModal` wrap them; each reads **real game
   data**, not mock data.

## 16 screens → Unity mapping (reuse vs build-new)

| Screen (design fn) | Opens via | Unity data source (exists) | Build |
|---|---|---|---|
| World HUD (`BottomHud`/`TopBar`) | always | `EmberWorldHost` (IEmberHudSource, gold, time, spell bar) | **new view, replaces EmberHud** |
| NPC Dialog (`DialogScreen`) | E / interact | `DialogBoxPanel` + adapter `IDialogSource` (just fixed) | **new view** (floating thread + compact panel) |
| Consul/DM (`ConsulFateScreen`) | R | `IConsultFateOracle` (just fixed) | new view |
| Combat HUD (`CombatScreen`) | in combat | combat vitals (ICombatHudSource), MeleeStrike/spell | new overlay |
| Inventory (`InventoryScreen`) | I | `InventoryGrid` + adapter inventory/equip | new view, retire `InventoryGrid` UI |
| Character (`CharacterScreen`) | C | player stats/skills/birthsign/alignment (adapter) | new view |
| Spellbook (`SpellbookScreen`) | — | spell catalog + known spells (6 schools) | new view |
| Journal/Quests (`JournalScreen`) | J | quest log store | new view |
| World Map (`WorldMapScreen`) | M | `OverlandMapPanel` data (overland + settlements) | new view, reuse map render |
| Colony (`ColonyScreen`) | K | `JobQueuePanel`/`ColonyNeedsPanel`/`FactionPanel` data | new view, reuse data |
| Loot (`LootScreen`) | on kill/container | loot tables | new view |
| Trade/Barter (`TradeScreen`) | merchant | merchant inventory + gold | new view |
| Pause (`PauseScreen`) | Esc | `PauseMenu` | new view, replace PauseMenu UI |
| Level Up (`LevelUpScreen`) | on level | level/attribute system | new view **+ ADD spell-pick step (missing)** |
| Death (`DeathScreen`) | on death | death/respawn | new view |
| Save/Load (`SaveLoadScreen`) | pause | save system | new view |
| **Crafting (MISSING)** | crafting station | recipes/forge | **new screen — design first, then build** |

## Gaps to close (design is missing these — user flagged)

- **Crafting menu** — not in the 16-screen registry at all. Add a `CraftingScreen` to the design first
  (mirror the trade/inventory two-pane layout), then implement.
- **Level-up spell selection** — `LevelUpScreen` has attribute/skill points but no spell pick. Add a spell
  step to the design + the implementation.

## Phasing (each phase = build → wire to real data → screenshot-verify → commit)

1. **Foundation + World HUD.** DS tokens + the InGame view base (self-scaling stage, `GameModal`/`TabbedModal`
   /`ItemSlot`/`VBar`/`SBtn` builders) + `TopBar`/`BottomHud` wired to live `EmberWorldHost` data, replacing
   `EmberHud` + the action bar. **Immediate visible win in the playable game.**
2. **NPC Dialog + Consul/DM.** The floating-thread-over-compact-panel dialog (the one you liked) over the live
   `DialogBoxPanel`/adapter, plus the Oracle screen. (Dialog/Oracle logic was just fixed, so this is presentation.)
3. **Player modals.** Inventory, Character, Spellbook — wired to real inventory/stats/spells.
4. **World modals.** Journal, World Map (reuse overland render), Colony (reuse colony data).
5. **Interaction.** Combat HUD, Loot, Trade.
6. **System + gaps.** Pause, Level Up (+ spell pick), Death, Save/Load, and the new Crafting screen.

## Integration risks (call them out, don't let them surprise us)

- The World HUD is the **live game HUD** — replacing `EmberHud`/action bar must preserve every action + keybind
  (ATK/CAST/TALK/INV/CHAR/MAP/JOURN/SRCH/STLTH/MODAL/FORM/EQUIP map onto the new spell bar + I/C/M/J/K/DM + keys).
  Do it behind the same `IEmberHudSource` so no gameplay logic moves.
- Several modals already have working **data + logic** (map, colony, inventory) behind older UI — reuse the data,
  retire only the old presentation, one at a time, so the game stays playable every commit.
- Deterministic/Domain rules unchanged — this is pure Presentation.

## Verification

Extend the proof-screenshot driver with an in-game screen tour (open each modal, capture), the same way the
char-creation playthrough captured all 11 steps — so every screen is proven in a real build, not asserted.

## Codex

The per-screen **view builders** (VisualElement layout matching the design JSX) are mechanical + parallelizable —
a good Codex job, fenced to the new `Assets/Scripts/Presentation/Ember/UI/InGame/` folder, following this plan +
the design files + the proven char-creation view as the template. The **wiring to game systems** (data binding,
replacing the live HUD, keybinds) stays in one hand (Claude) for coherence, exactly as planned for char-creation.
