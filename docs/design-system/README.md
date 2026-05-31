# Ember CRPG — Design System

> Historical/creative reference document. For current runtime and validation
> truth use `docs/CURRENT_STATE.md`, `docs/AI_STACK.md`, and
> `docs/REPO_HYGIENE.md`.

> A deterministic, living-world CRPG that runs like a colony sim and reads like
> a text adventure. Ember is the game that "should have come after the original
> *Hitchhiker's Guide to the Galaxy: Anniversary Edition*, evolved through
> *Fallout 1*, expressed in a *Morrowind*-shaped world" — finished with an
> embedded AI agent layer where every NPC and the Dungeon Master are persistent
> minds that stay deterministic until reality demands otherwise.

This folder is a **design system**: the brand, colors, type, assets, voice, and
UI-kit recreations needed to design new screens, marketing, slides, or
prototypes that look and feel like Ember — without re-reading the whole engine.

---

## 1. What Ember is

Ember is a single-player CRPG built on three stacked ideas:

1. **Colony simulation** (RimWorld + Dwarf Fortress lineage) — the world keeps
   ticking off-screen. NPCs, factions, weather, seasons, plant growth, trade
   routes and economies advance whether the player is present or not.
2. **CRPG** (Fallout 1 / Morrowind / Daggerfall lineage) — turn-aware combat,
   body-part targeting, percentage hit chance, persistent stats, equipment,
   faction reputation, skill-gated dialogue, save/load.
3. **AI agent layer** (the last thing built) — every named NPC and the **DM**
   are persistent agents. They run a deterministic local mind 99% of the time
   and only fall back to a local LLM (`Qwen2.5` local runtime path) when the
   deterministic path genuinely can't answer. **The simulation is always
   authoritative; the LLM never writes the world.** No chat overlays, ever —
   model output is always translated into in-game tool calls.

Visually the game is a **Daggerfall-style 3D world of painterly billboard
sprites** — characters and items are generated on the fly by local image models
(Gemini "Nano Banana" / ONNX / ComfyUI forge) against a fixed dark-fantasy
style, so no two playthroughs ship the same art. The player can summon the DM
with a **`Consul Fate`** shortcut and talk to any NPC, Fallout-1 style.

**Status (May 2026):** playable vertical slice, deliberately rough. The HUD is
programmatic flat-color rectangles; sprites are placeholders; there is no
character-creation art pass yet. The design system below captures the *intended*
visual language already encoded in the UI scripts, not a shipped showcase build.

It is **open source, free, MIT.** Maintainer: [@msbel5](https://github.com/msbel5)
(Mami) + the Alcyone agent crew.

---

## 2. Sources (provenance)

Everything here was reverse-engineered from one repository. The reader is
**encouraged to explore it** to design with higher fidelity:

- **Primary repo:** https://github.com/msbel5/alcyone-ember-rpg
  (Unity / C# rewrite — the canonical project)
- **Original prototype (read-only reference):** https://github.com/msbel5/ember-rpg
  (the older Godot + Python FastAPI version)

Files this system was built from (all paths within the primary repo):

| Source | What it gave us |
|---|---|
| `MainMenu_Screenshot.png` | The only real rendered frame — wordmark, gold, void background |
| `docs/EMBER_VISION_BIBLE.md` | Genre synthesis, lineage, agent contract, DM evolution, tone |
| `docs/prds/visible-generation-and-consistent-ui.md` | Intended design-token + prefab UI foundation |
| `Assets/Scripts/Presentation/Ember/UI/*.cs` | **Every color, font size, layout and animation token** (programmatic HUD) |
| `Assets/Scripts/Domain/CharacterCreation/CharacterCreationCatalog.cs` | Classes, birthsigns, scenario questions, stat block |
| `Assets/Resources/loading-flavors.json` | Loading-screen voice samples |
| `Assets/Generated/Core/**` | Runtime-generated painterly billboards/icons cache paths |
| `docs/forge-samples/grid.png` | Generated terrain texture sample |

> The repo is large (Unity project, 1500+ asset stubs). Most `Assets/Art/*.png`
> are 131-byte placeholders — the *real* art is generated at runtime, so look in
> `Assets/Generated/Core/` and forge prompts, not only placeholder source art under `Assets/Art/`.

---

## 3. CONTENT FUNDAMENTALS — how Ember writes

Ember has **two distinct voices** and the line between them is sacred:

### A. The world voice (narrative / DM / loading / lore) — *literary second person*
Spare, ominous, mythic. Present tense. Fate, stars, forges, shadows. Never
jokey, never marketing-y. This is the Hitchhiker's-Guide-meets-Fallout-1 prose.

Verbatim loading-screen samples:
> "The stars align in silence."
> "The forge fire never truly dies."
> "Fortune favors the bold, but fate chooses the persistent."
> "The DM is watching. Always."
> "In the halls of kings, every shadow has a name."
> "A goblin's screech is the first sign of a bad day."

World-gen prompts are written as **questions of fate**, never settings forms:
> "What is the world's mood? *(e.g. Grim, Vibrant)*"
> "What is the player's calling? *(e.g. Smith, Mage)*"
> "Where does fate begin? *(e.g. Forge, Tavern)*"

Character-creation scenarios are short moral dilemmas in second person, each
ending in an ellipsis the player completes by choosing:
> "A merchant is robbed in front of you. You…"
> "You find a sealed shrine beneath old stone. You…"

### B. The system voice (UI labels, buttons, HUD) — *terse, uppercase, functional*
Single words or short imperatives. ALL CAPS for actions and bars. No
punctuation, no emoji, no filler.
- Buttons: `NEW GAME`, `CONTINUE`, `QUIT`, `BEGIN JOURNEY`, `BEGIN ADVENTURE`
- Panel headers: `WORLD GENERATION`, `CHARACTER CREATION`
- HUD bars: `HEALTH`, `FATIGUE`, `MANA`
- Dialog topics are numbered, system-voice scaffold wrapping world-voice nouns:
  `1. Ask about <the missing caravan>` (the topic noun is highlighted gold).

### Conventions
- **Person:** the player is addressed as **"you"**; the game refers to the
  avatar as "the player" / "actor", never "the user". Multiplayer-ready language
  ("controlled actors") under the hood, but player-facing copy says "you".
- **Casing:** Title Case for proper nouns and screen titles; UPPERCASE for
  interactive controls and vitals; sentence case inside narrative prose.
- **Default name:** unnamed heroes are **"Adventurer."**
- **Emoji:** **never.** Not in UI, not in copy.
- **Numerals:** lean. Stats are six three-letter attributes (see below); the
  design avoids stat-soup. Show a number only when the player acts on it.
- **Tone test:** if a line could appear in a productivity app, rewrite it. Every
  string should feel like it was carved, rolled, or whispered.
- **Cultural note:** the author is Turkish; the shipped build's `ToUpper()`
  renders a dotted **İ** ("CONTİNUE", "QUİT") in the current screenshot. Treat
  that as a placeholder locale artifact — English copy uses a standard "I".

### The vocabulary (use these exact terms)
- **Attributes:** Might (MIG), Agility (AGI), Endurance (END), Mind (MND),
  Insight (INS), Presence (PRE).
- **Vitals:** Health, Fatigue, Mana.
- **Classes:** Warrior, Mage, Rogue, Scholar, Diplomat, Wanderer.
- **Birthsigns** (Morrowind-style): The Tower, The Lover, The Lady, The
  Atronach, The Warrior, The Mage, The Thief, The Serpent, The Steed, The
  Ritual, The Shadow, The Lord.
- **Systems:** the DM module, the Forge (asset generation), worldgen, off-world
  tick, the `Consul Fate` shortcut, Ask About / Think.

---

## 4. VISUAL FOUNDATIONS

The whole look is **"a candle-lit menu in a dark room."** Almost-black canvas,
one warm ember-gold accent, painterly creatures emerging from shadow. Restraint
is the aesthetic — the engine literally builds the HUD from flat colored
rectangles, and that flatness is the style, not an accident to be "fixed" with
gradients.

### Color
- **Two near-blacks.** Menus/boot use a **cool void `#0D0D12`**; in-world panels
  use a **warm void `#0A0908`**. These backgrounds are a deliberately **genre-
  neutral stage** — Ember generates worlds of any kind (fantasy, space,
  anything), so the canvas stays dark and quiet enough to host whatever the
  Forge paints on top. Pick warm when creatures/forge-light are present, cool
  for the title/boot moment. Treat them as painterly washes (warm grain, soft
  pooling), not flat cold fills.
- **One accent.** Ember gold (`#FFD94C` / `#FFD152`), pushed to bright amber
  `#F1C40F` for selection and highlighted nouns. Gold is precious — wordmark,
  titles, selection, highlighted topic words. Don't flood with it.
- **Warm-brown UI furniture.** Buttons `#2E2417`, inputs `#1F1A14`, worldgen
  fields `#26201A` — barely-lit leather/wood tones, never grey chrome.
- **Parchment inks** for text on dark: pale gold `#F2DB9E`, dimmer `#E6D9B3`,
  bone white `#FFFFFF`. On light/parchment frames, text is **deep charcoal
  brown `#261A0D`**.
- **Vitals are the only saturated colors:** health red `#D9331F`, fatigue amber
  `#D9B31A`, mana blue `#3373F2`. They appear only in bars.

### Type
- No fonts ship in the repo (TMP placeholders). Substitutes (flagged): **Jost**
  (geometric grotesque) for the wordmark, titles, buttons and HUD — it matches
  the clean, wide-tracked gold EMBER mark; **Spectral** (literary serif) for all
  narrative/dialogue/loading prose, honoring the text-adventure lineage;
  **Cinzel** as an optional engraved alt for ceremonial headers.
- The EMBER wordmark is **wide-letterspaced uppercase** (≈0.18em). UI labels and
  bar captions are uppercase with ~0.08em tracking. Narrative prose is
  sentence-case, generous line-height (~1.55), often italic for flavor.

### Backgrounds, imagery & texture
- **Backgrounds are flat near-black** — no gradients, no vignettes baked into
  UI. Depth is darkness, not blur.
- **Imagery = painterly digital art, isolated on transparent background.**
  Full-body character billboards, front-view, single centered figure, "high
  fantasy dark RPG style." Palette is desaturated, earthy, low-key lit — muted
  greens, browns, charred blacks, the occasional glowing eye. Daggerfall-style
  billboards in a 3D world. (See `assets/characters/`.)
- Terrain/material textures are soft, muddy, painterly tiles (see
  `assets/textures/terrain-dirt.png`) — not crisp PBR.

### Motion
- **Fast, restrained, never bouncy.** Panels open in **180ms on EaseOutCubic**
  (fade 0→1 + scale **0.92→1**) and close in **120ms on EaseInQuad** (fade out +
  scale →0.94). That's the house transition for every modal.
- **Typewriter dialogue** at **45 characters/second**; click to skip to full
  line. "Thinking…" cycles 1→2→3 dots every 0.3s while an LLM call is in flight.
- **Low-health flash:** the health bar ping-pongs toward white below 25%.
- Loading spinner rotates ~200°/s. No parallax, no easing-heavy hero animations.

### Hover / press / selection states
- **Hover:** button fill lifts one step warmer (`#2E2417` → `~#3A2E1D`) and the
  hairline border picks up amber. Subtle — a warming, not a glow.
- **Press:** scale shrink to ~0.97 (mirrors the close-anim shrink).
- **Selection:** a soft **3px yellow-gold outline** (`#E9C93A`, softened from the
  placeholder build's raw `#FFFF00`) marks the active spell slot — warm, not
  retina-searing.

### Borders, corners, elevation
- **Softly rounded, never sharp glass.** Corners are gently rounded on a small
  scale — **6px** for buttons/inputs/slots, **12px** for panels/cards, **20px**
  for modals, and **pill** (fully round) for vitals bars. The earlier build
  shipped hard 90° rectangles as a placeholder; the intended language is worn
  paper and carved wood — rounded, warm, painterly.
- **Fills are warm gradients,** not flat blocks: buttons run a leather gradient
  (`#3A2D1C → #281F13`), the “white” menu button is actually **aged parchment**
  (`#E6D6AD → #D3C098`) with dark ink, never pure `#FFFFFF`.
- **Borders** are faint gold hairlines (`rgba(242,219,158,0.22)`) or, for
  emphasis, a 1px solid ember-gold line. Framed parchment surfaces add a gentle
  top highlight (`inset 0 1px 0 rgba(242,219,158,0.12)`).
- **Elevation** is a soft warm shadow (`0 16px 44px rgba(0,0,0,0.55)` on modals,
  `0 2px 6px` on buttons) plus the gold hairline — not flat scrims alone. HUD
  vitals tracks are `black @ 45%` capsules.
- **Transparency & blur:** used as **scrims, not glass.** Semi-opaque warm-black
  panels (0.8–0.95 alpha) over the world; no frosted-glass backdrop-blur.

### Layout rules
- HUD is **Daggerfall-compact and bottom-anchored**: three short vitals bars
  (~22% wide × 4% tall) in the lower-left gutter, a damage-log line above, a
  5-slot spell bar. The center of the screen stays clear for the 3D world.
- Modals (dialog, worldgen, character creation) are **centered**, ~60% of the
  viewport, over a dimmed world; portrait left, prose top, choices stacked below.
- Reference resolution is **1920×1080**, scaled with screen size.

---

## 5. ICONOGRAPHY

Ember has **almost no traditional icon system — and that is the point.**

- **No icon font, no SVG icon set, no sprite sheet of glyphs.** The UI scripts
  build everything from `UnityEngine.UI.Image` rectangles and TMP text. There is
  nothing like Lucide/Heroicons in the project.
- **"Icons" are generated painterly art.** Spell slots, inventory cells and NPC
  portraits are filled by **runtime-generated PNGs** from the Forge (Gemini
  "Nano Banana" / ONNX / ComfyUI), each `preserveAspect` inside a flat slot.
  They are little paintings, not flat vector icons. Examples and their exact
  generation prompts live in `assets/characters/`.
- **Numbers are the icon system.** Affordance comes from **numeric key prefixes**
  rather than pictograms: dialog topics read `1. Ask about …`, spell slots map to
  `Alpha 1-5`, character-creation choices are lettered `A. / B. / C.`. The player
  reads keys, not symbols.
- **The selection outline is the only "UI chrome" glyph** — a hard yellow box
  around the active spell slot.
- **No emoji. No Unicode dingbats.** If a future screen needs a marker, prefer a
  number, a short uppercase word, or a tiny generated painting in a slot — never
  an emoji or a hand-drawn vector icon.
- **Logo:** there is no logo file; the brand mark is the **typeset word "EMBER"**
  in ember-gold, wide-tracked uppercase, on void. Treat the wordmark itself as
  the logo (recreate with `--font-display` + `--ember-gold`).

> When building Ember designs, **do not invent a flat-icon UI.** Use generated
> painterly fills in flat slots, numeric keys, and uppercase word-labels.

---

## 6. Index — what's in this folder

| Path | What it is |
|---|---|
| `README.md` | This file — context, sources, content + visual foundations, iconography |
| `SKILL.md` | Agent-Skill manifest so this system works inside Claude Code |
| `colors_and_type.css` | All design tokens as CSS vars + semantic element styles |
| `assets/characters/` | Real generated painterly billboards (goblin, bandit, beggar) |
| `assets/textures/` | Generated terrain/material texture sample |
| `assets/reference/` | The original main-menu screenshot |
| `preview/` | Small HTML specimen cards (populate the Design System tab) |
| `ui_kits/game/` | High-fidelity recreation of Ember's in-game UI (menu → worldgen → world HUD → dialog) |
| `Assets/Scripts/Presentation/Ember/UI/*.cs` | The original Unity UI source kept for reference |

### UI kits
- **`ui_kits/game/`** — the one product surface Ember has: the game client. A
  clickable recreation that walks Main Menu → World Generation → Character
  Creation → in-world HUD → NPC dialog, using the real tokens and assets.

---

_Built from the Ember CRPG repository. Explore
[msbel5/alcyone-ember-rpg](https://github.com/msbel5/alcyone-ember-rpg) and its
`docs/EMBER_VISION_BIBLE.md` to design with deeper fidelity._
