# Ember CRPG — Vision Bible

_Status: canonical reference. Every sprint reads this first._
_Author: Mami (Muhammet Sıddık Bel)_
_Curator: Captain (Alcyone)_
_Last updated: 2026-05-02_

---

## 0. The One-Sentence Vision

**Ember is a colony-sim-driven, single-player CRPG with a living procedural world,
where every NPC and the DM are persistent agents — running deterministic by
default, and falling back to a real LLM only when the deterministic path
genuinely cannot answer.**

We are not making "another Daggerfall" or "another Fallout" or "Morrowind with
chat". We are making the game that should have come after the original
*Hitchhiker's Guide to the Galaxy: Anniversary Edition*, evolved through
*Fallout 1*, expressed in a *Morrowind*-shaped world, and finished with a layer
those games never had: a real, embedded AI agent layer that respects the player's
deterministic experience until reality demands more.

---

## 1. Genre Synthesis

Three things in one engine:

1. **Colony simulation** (RimWorld + Dwarf Fortress lineage) — the world keeps
   simulating off-screen. NPCs, factions, regions, economies, weather, and
   quests all advance whether the player is present or not. Off-world NPCs run
   a tick of deterministic behaviour continuously. The world is alive.
   - From RimWorld: per-pawn skills/traits/moods/needs, social opinion graph,
     incident-driven pacing, the AI Storyteller as the *direct ancestor of our
     DM module* (Phoebe = chill, Cassandra = classic, Randy = chaos)
   - From Dwarf Fortress: tiered memory (short-term events / long-term traits /
     world chronicle), historical figures whose actions ripple, emergent
     stories from agent collision rather than scripted plot

2. **CRPG** — turn-aware combat, persistent stats, equipment, faction
   reputation, skill checks, save/load, dialog with branching. Deterministic
   game state. Reproducible. Testable.

3. **AI agent layer** (final phase) — every NPC and the DM are *single instances*
   that persist across the whole game. They remember. They have local
   deterministic minds 99% of the time. When deterministic logic runs out, they
   can call a tool — and through that tool, they can ask their personal LLM
   agent for a creative answer, which is then expressed back inside the game's
   tool surface (no fourth-wall break, no chat overlay; the game stays the
   game).

Multiplayer is an *optional later layer* — the single-player tabletop-FRP
experience is the canonical product. Multiplayer comes afterward and never
breaks the single-player covenant.

---

## 2. Evolutionary Lineage

We honour these games and inherit specific things from each:

| Source | Inheritance | Source available? |
|--------|-------------|-------------------|
| Hitchhiker's Guide (Anniversary Ed., Infocom 1984 + 2004 web port) | Narrative voice, "Ask About" / "Think" reflective shells, second-person prose, world that responds to inquiry, dry humour at edges | external (read play scripts + post-mortems online) |
| Fallout 1 (Black Isle, 1997) | DM-narrated turn structure, skill checks gating dialogue branches, percentage-based hit chance, body-part targeting, severe consequences, the Vault Boy framing of UI hits | external (game design docs, Vince D. Weller interviews) |
| The Elder Scrolls III: Morrowind | Open-world simulation depth, faction/reputation density, NPC schedules with disposition-driven dialog, world that exists without you, hand-placed lore depth | `references/openmw-master/` (full reverse-engineered C++ engine) |
| Daggerfall (Bethesda 1996) + Daggerfall Unity port | Procedural world generation, hit-chance math, deterministic combat baseline, dungeon layouts, climate-driven encounter tables | `references/daggerfall-unity-master/` (C# port, Unity-compatible — closest twin to our codebase) |
| Dwarf Fortress (Bay 12 Games) | Living world simulation, off-screen agent persistence, emergent stories from agent interaction, multi-tier memory (short-term, long-term, world chronicle), historical events that ripple | `references/dwarf-fortress-legacy/` (legacy C++ snapshot) |
| RimWorld (Ludeon Studios) | Colony-sim core: per-pawn skills, traits, moods, needs, schedules, social opinions; AI Storyteller as the ancestor of our DM module (Phoebe / Cassandra / Randy patterns); incident-driven pacing rather than scripted; *every pawn is a tracked simulated entity, not a spawn* | external (closed-source, reference docs + community wiki + decompile readings) |
| Baldur's Gate / Icewind Dale / Planescape: Torment | RTWP combat pacing (real-time with pause), party management, dialog-as-interface culture | `references/gemrb-master/` (GemRB open-source IE engine reimplementation) |
| Our prior attempt | What NOT to do: 1.5GB binary commit, scope creep, no inspector loop, no separation of concerns | `references/ember-rpg/` (kept gitignored — *clean room* rule applies) |
| **Ember (us)** | All of the above + a *real* embedded agent layer at the bottom of the stack, where each NPC and the DM are persistent agents and the LLM is the last fallback, never the first call | THIS REPO |

We do **not** inherit:
- Modern UI bloat
- Real-time clutter
- Chat-overlay AI ("type to NPC" boxes that break diegesis)
- AAA cinematic cutscenes (we ship mechanics, not movies)

---

## 3. Architectural Layers (canonical ordering)

```
┌────────────────────────────────────────────────────────────────────┐
│  LAYER 5: AI AGENT LAYER          (Sprint 15-16, last)             │
│   - per-NPC persistent agent (1 instance/NPC, lives the whole game)│
│   - DM agent (passive observer + occasional intervention)          │
│   - tool-call surface ONLY (no chat overlay, no breaks)            │
│   - fallback path: invoked only when deterministic returns "I can't"│
├────────────────────────────────────────────────────────────────────┤
│  LAYER 4: PLAYER INTERACTION FACADE   (Sprint 5-12)                │
│   - Ask About / Think / DM buttons                                 │
│   - input goes:  AI agent  →  deterministic shell  →  fallback     │
│   - skill checks gate dialogue branches (Fallout 1 style)          │
├────────────────────────────────────────────────────────────────────┤
│  LAYER 3: GAMEPLAY MECHANICS          (Sprint 1-9)                 │
│   - Combat (RTWP, body parts, hit chance, damage pipeline)         │
│   - Inventory / Equipment / Stats progression                      │
│   - Quest engine, faction reputation, skill XP                     │
│   - Magic / spells (school + components)                           │
│   - Crafting (alchemy, smithing, enchanting)                       │
├────────────────────────────────────────────────────────────────────┤
│  LAYER 2: DETERMINISTIC SIMULATION CORE   (Sprint 1-4)             │
│   - Procedural rooms / dungeon / world map                         │
│   - NPC schedules + memory (already shipping in Sprint 3)          │
│   - Day/night, weather                                             │
│   - Save/load JSON serialization                                   │
│   - Off-world NPC tick (continues when player is elsewhere)        │
├────────────────────────────────────────────────────────────────────┤
│  LAYER 1: PURE DOMAIN                  (Sprint 1, foundation)      │
│   - Actor, Stats (MIG/AGI/END/MND/INS/PRE), Health/Fatigue/Mana    │
│   - Item, Inventory, ItemId                                        │
│   - Memory (ActorMemory, NpcMemoryStore)                           │
│   - All Unity-free, all NUnit-testable, all deterministic          │
└────────────────────────────────────────────────────────────────────┘
```

**The agent layer (Layer 5) is the LAST thing built.** Every system below it
works without it. The agent layer is a *graceful enhancement*, never a
dependency. If the LLM is unreachable, the game still plays.

---

## 4. NPC Live Agent Contract

Every named NPC in Ember has the following contract, even before Layer 5 ships:

### Identity
- Stable ID across the entire campaign
- Persistent memory (NpcMemoryStore — already shipping)
- Persistent stats, inventory, location, faction relations

### Behaviour modes (priority order, top wins)

1. **Deterministic local mind** (default, 99% of frames)
   - Schedule-driven (sleep/work/eat/patrol)
   - Reaction tree to game events (combat, theft, dialog)
   - Memory-driven (greets player based on last interaction outcome)
   - **Pure C#, no LLM calls, fully testable**

2. **In-game tool fallback** (rare, deterministic-but-broader)
   - When local mind returns "no valid action," NPC consults a wider
     deterministic toolset (faction policy, world state, recent rumour DB)
   - Still no LLM. Still testable. Still deterministic.

3. **Personal LLM agent fallback** (last resort, Layer 5)
   - When even the wider toolset returns "no valid action," the NPC's *own*
     persistent LLM agent is invoked
   - The agent is given: NPC identity, recent memory, current situation,
     available game tools
   - The agent **must reply with tool calls** — `move_to`, `say_line`,
     `give_item`, `attack`, `flee`, etc.
   - The agent **never breaks the fourth wall**. Its output is translated to
     in-game actions. The player sees the NPC do something interesting; they
     don't see "GPT-5.5 says: …"
   - One agent instance lives across the entire game for that NPC. Memory
     persists.

### Off-world simulation
- NPCs the player isn't currently observing still tick
- Tick interval scales with distance / importance (e.g. capital-city NPCs tick
  every minute, edge-of-map hermits tick every hour)
- An off-world NPC that hits "no valid action" can also escalate to its agent
  fallback — even if the player will never see the result. The world stays
  alive *because* it can resolve its own crises.

---

## 5. DM Module Evolution

The DM is the orchestrating presence — Fallout 1's narrator, the Hitchhiker's
Guide voice, the silent author behind quest pacing. **The DM module ships LAST**
because it depends on everything below.

### DM phases

| Phase | What it does | When |
|-------|-------------|------|
| **Phase 0: silent** | No DM. Game runs without one. Sprint 1-9 baseline. | now → Sprint 9 |
| **Phase 1: deterministic DM** | Quest pacing, encounter difficulty scaling, mood shifts via fixed rules. | Sprint 10-11 |
| **Phase 2: tool-call DM** | DM watches the world, occasionally fires tools to spice things up: spawn a wandering encounter, drop a hint via NPC, start a faction event. All from a deterministic toolset. | Sprint 12-13 |
| **Phase 3: AI-augmented DM** | DM has its own persistent LLM agent. It can *ask* its agent for narrative ideas, but the agent's output is again translated to in-game tool calls (no chat). The DM agent is the most powerful but also the most restrained — it acts only when deterministic phases say "I'm out of ideas." | Sprint 15-16 |

### DM tool surface (read + write)

Read:
- whole world state, faction status, quest progress, NPC memory
- combat history, player choices, time-of-day, weather
- recent player frustration signals (rage-quits, repeated deaths, dialog dead-ends)

Write (always rate-limited, always justifiable from deterministic state):
- spawn encounter at distance N
- nudge an NPC's mood / opinion
- introduce a quest hook via existing NPC dialog
- shift weather, time, faction tension
- start an off-world event that propagates inward

The DM is **subtle**. Players should never feel "the GM railroaded me." A good
DM intervention looks like coincidence in retrospect.

---

## 6. Player Interaction Routing

When the player presses **Ask About**, **Think**, or **DM** during dialog or
exploration, the request is routed through this stack:

```
Player clicks "Ask About: <topic>"
            │
            ▼
┌───────────────────────────────┐
│ 1. Layer 5 (AI agent)         │
│   if available + reasonable    │ ── yes → returns tool call
│   confidence on this topic     │           (e.g. "say_line: …")
└───────────────────────────────┘
            │ no / unavailable
            ▼
┌───────────────────────────────┐
│ 2. Layer 4 (deterministic     │
│    narrative shell)            │ ── yes → returns canned response
│   skill check vs topic         │           (skill check may unlock new branch)
└───────────────────────────────┘
            │ no
            ▼
┌───────────────────────────────┐
│ 3. Generic fallback            │
│   "You think about it but      │
│    nothing comes to mind."     │
└───────────────────────────────┘
```

**Why AI first, deterministic second?** Because the AI layer can recognise
the player's *actual* question (synonyms, idioms, world-specific lore) and
route it correctly to a deterministic answer that already exists. The AI
layer is a *router and humanizer* most of the time — pure generation only
in true edge cases.

**Skill-check unlock logic (Fallout 1 inheritance):** every dialog node has
optional skill-gated branches. Speech > 60 → new line. Lore > 80 → secret
revealed. Stealth > 70 → can lie successfully. The AI layer must respect
these gates; it cannot invent unlocks.

---

## 7. Multiplayer Extension (Future, Optional)

The single-player FRP experience is the canonical Ember. But the engine is
built so that:

- A friend can join your existing game as a *named NPC who is now a player*
- Their character was always there, they just take control now
- The DM treats them like any other party member
- Off-world simulation continues whether 1 or 4 players are connected
- Save-state is authoritative on the host; clients are renderers

This means: **don't write any system that assumes single-player only.** Don't
hard-code "the player" — always say "actor[id]" or "controlled actors". The
multiplayer-readiness is structural, not a feature, until Sprint 17+.

---

## 8. Sprint Mapping (16 sprints, ~5-6 months)

| Sprint | Theme | Layer touched | AI layer? |
|--------|-------|---------------|-----------|
| 0 | Recon, planning | meta | – |
| 1 | Tiny vertical slice | L1 + L2 | – |
| 2 | Interaction refinement | L2 | – |
| 3 | Validation + sim depth | L1 + L2 | – |
| 4 | Mekanik foundation | L2 + L3 | – |
| 5 | Magic / spell system | L3 | – |
| 6 | Quest engine | L3 + L4 | – |
| 7 | Faction / reputation | L3 | – |
| 8 | Skill / progression | L3 | – |
| 9 | Crafting | L3 | – |
| 10 | World map, day/night | L2 | – |
| 11 | UI polish, animation | L4 | – |
| 12 | Audio, music states | L2 + L4 | – |
| 13 | Performance + i18n | meta | – |
| 14 | Beta + bug bash | meta | – |
| **15** | **NPC live agent backbone** | **L5** | **YES** |
| **16** | **DM AI module + Ask AI routing** | **L5** | **YES** |
| 17+ | Multiplayer, expansion | L4 + meta | optional |

**Critical**: nothing in Sprint 1-14 may *require* the AI layer to function. If
it requires AI, it belongs in Sprint 15-16. If it can be done deterministically,
it must be.

---

## 9. Notes for the Captain (Alcyone)

When you read this Bible, hold these as non-negotiable:

1. **Ship deterministic first, AI last.** Every feature from Sprint 5-14 must
   work with the LLM unplugged. If you find yourself wanting to call an LLM in
   Sprint 7's faction code, stop — that's Sprint 15 territory.

2. **NPCs are persistent identities, not stateless functions.** Every named
   NPC has a stable ID, a memory store, and a state vector that lives across
   save/load. Never spawn an NPC with `new NPC(...)` — always look it up by ID.

3. **The DM is silent until Sprint 12.** Don't sneak a "DM module" stub into
   Sprint 6's quest engine. Quest pacing in Sprint 6 is hand-tuned, period.

4. **Tool-call interfaces are designed in Sprint 5-9 even though the LLM
   doesn't use them yet.** Every NPC behaviour function should already accept
   a structured action descriptor (`{type: "say_line", line: "..."}`) so that
   Sprint 15's agent can produce the same shape and route it through the same
   pipe. Build the socket, plug in the LLM later.

5. **No chat overlays. Ever.** The player never sees raw model output. The
   model output is always translated to existing in-game actions. If the model
   wants to "describe a sunset," it must call `set_weather(clear, sunset_glow)`
   and let the engine render it.

6. **Off-world tick is real.** Don't optimize NPCs out of existence when the
   player isn't looking. Reduce their tick rate, but don't freeze them. The
   world must surprise the player when they return.

7. **Memory hygiene.** NPC memory grows unboundedly otherwise. Use a
   tiered system: short-term (last 50 events, in RAM), long-term (key
   events, summarized, on disk). Sprint 3's memory infrastructure is the
   foundation.

8. **Multiplayer-ready, single-player-shipping.** Never hard-code "the player".
   Always actor[id]. Always controlled-actors set.

9. **Skill checks are sacred.** Fallout 1 had Speech 70 unlocking a peace
   resolution. We honour that. Every dialog branch with a skill check must
   have *both* paths implemented: passing path and failing path. Don't ship a
   gated branch that just shows "[Skill check failed]" — write the failure
   text.

10. **The player's freedom is the invariant.** If a system constrains player
    choice, it's wrong. Ember sells *agency*, not script. The AI layer in
    Sprint 15-16 is the ultimate guarantor of agency: when the deterministic
    world runs out of valid responses to a player action, the AI layer
    invents one. It exists *to preserve player freedom*, not to replace
    deterministic logic.

---

## 10. Origin Notes (Mami's Words)

> "Aslında colony sim üzerine kurulu CRPG. Böylelikle hem procedural living
> world hem CRPG öğeleri ve Fallout 1 gibi oyunu yöneten DM modül ile ve
> NPC'lerle, ekip üyeleri ile human-machine-AI interface."

> "Hatta arkadaşımız yoksa ve FRP oynamak istiyorsak onlar da bize sonra
> multiplayer'la katılabilir ama bizim single player FRP experience'ı CRPG
> ile buluşturuyoruz."

> "DM modül en son modül. Sadece oyun içi araçlarla kontrol edebilir, yine
> geleneksel AI ama en son fazda bunlara yapay zeka da entegre edeceğiz. Bu
> yapay zekalar tool call yönetecek gibi düşün."

> "NPC'leri, party'deki AI ve DM kendisi - hepsi single olacak oyun boyunca
> yani yaşayacaklar ve hatırlayacaklar. Gerekmedikçe AI tool kullanılmayacak."

> "Off-world NPC'ler sürekli deterministic çalışacak. Bir sorun yaşarlarsa
> kendi agent'lerini çağırıp çözüm isteyebilecek. Oyun boyunca o agent live
> olacak."

> "DM sürekli oyunu gözetleyecek, isterse gizli gizli oyunu güzelleştirmek
> için tool call çağırabilecek."

> "Nasıl orijinal Otostopçunun Galaksi Rehberi Anniversary Edition'dan
> Fallout 1'e, en sonda Fallout 1'in Morrowind içinde olanı ama bizim AI'lı
> olanımız gibi evrilmesi gerektiğini..."

This Bible is canonical. If a future sprint plan contradicts it, the Bible
wins until Mami explicitly amends it.

---

## 11. Reference Library — Where to Look

The repo carries a `references/` directory (gitignored) containing the source
code or decompilation of every game we inherit from. **Read these before
implementing the matching mechanic. Do not copy code; read, then write your
own.** This is the **clean-room rule**.

### Local references (Pi: `~/projects-alcyone/alcyone-ember-rpg/references/`)

| Path | What it is | Read when implementing |
|------|-----------|-----------------------|
| `references/daggerfall-unity-master/` | Daggerfall Unity (C# port). Closest in language and engine to us. | combat hit-chance, dungeon procedural generation, character creation, climate/region tables, save format design |
| `references/openmw-master/` | OpenMW (C++ Morrowind reimplementation). Full engine including AI, dialog, journal, faction. | NPC schedule + disposition system, dialog response trees, faction reputation propagation, journal + quest engine, magic effect framework |
| `references/gemrb-master/` | GemRB (C++ Infinity Engine reimplementation: BG/IWD/PST). | RTWP combat pacing, party management, action queue, dialog-as-interface UX, scriptable AI behaviour blocks |
| `references/dwarf-fortress-legacy/` | DF legacy C++ snapshot. Off-world simulation reference. | living world tick architecture, agent memory tiers, emergent event chains, historical figure tracking |
| `references/ember-rpg/` | Our previous attempt. *Read it as anti-pattern.* | what NOT to do — examine `docs/lessons-from-first-attempt.md` for the post-mortem |

### External references (no source on Pi, read online)

| Source | Where to read | Why |
|--------|---------------|-----|
| Hitchhiker's Guide (Infocom 1984) | game design wiki + 2004 BBC web port playthroughs | second-person voice, "Ask About" inquiry shell, narrative restraint |
| Fallout 1 design docs | post-mortems, Vince D. Weller interviews, fan-maintained design wiki | DM-narrated turn structure, skill-gated dialog, body-part combat, the original tone |
| RimWorld | community wiki, modding API docs, decompile readings (do **not** redistribute decompiled code) | per-pawn simulation, AI Storyteller as DM ancestor, mood / need / opinion stack, incident pacing |

### Clean-room rule

You may read these references freely. You may study patterns, naming, data
shapes. You may **not** copy code into our repo. Every line in `Assets/Scripts`
must be written from scratch, in our voice, to our standards.

When you write a new module:
1. Open the relevant reference file(s) — note the file path in your commit
   message ("inspired by `references/openmw-master/apps/openmw/mwworld/...`")
2. Take a 30-minute notes pass: what does this file do, what would I do
   differently, what's the kernel idea
3. Close that file, write our version from notes only
4. Inspector audit verifies no copy-paste

### Important: where to put cross-references in our docs

Each Sprint summary doc (`docs/sprint-N-summary.md`) **must** link back to the
Bible sections it implements:

```
This sprint implements Bible §3 layer L3 (gameplay mechanics) and Bible §11
references/openmw-master/apps/openmw/mwmechanics/spells.cpp (read for shape,
not code).
```

Without this back-reference, a future sprint reader cannot rediscover the
design rationale.

---

_End of Vision Bible. Read again when in doubt. Update only with Mami's
explicit amendment._

