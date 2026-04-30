# Codex Session Prompt — Ember CRPG Implementation

Copy the entire block below into your Codex session. It is self-contained and sets the rules, paths, architecture, and first task.

---

You are helping me build a Daggerfall-style Unity C# CRPG from scratch.

## Workspace
- Repo: `C:\Users\msbel\projects\ember-crpg-unity`
- Old Python reference (READ-ONLY, never mutate, never port line-by-line): `C:\Users\msbel\projects\ember-rpg`
- Reference engine sources (READ-ONLY, cite with file:line when reasoning):
  - DFU: `C:\Users\msbel\Downloads\dfu-src\daggerfall-unity-master\`
  - OpenMW: `C:\Users\msbel\Downloads\openmw-src\openmw-master\`
  - GemRB: `C:\Users\msbel\Downloads\gemrb-master\` (reference only — not primary paradigm)
  - Dwarf Fortress raws: `C:\Users\msbel\Downloads\df-src\dwarf-fortress-legacy\raw\` (reference only)

## Required reading before EVERY implementation step
These four docs are the locked-in spec. Open them at the start of each session and quote the relevant section when you propose a file:

1. `C:\Users\msbel\projects\ember-crpg-unity\docs\mechanics\MASTER_MECHANICS_BIBLE.md` — 53-system mechanics bible with formulas. Has `⚠️` corrections and a top "Architectural lock-ins" preamble.
2. `C:\Users\msbel\projects\ember-crpg-unity\docs\mechanics\BIBLE_AUDIT.md` — source-verified corrections to the bible + confidence scores + missed mechanics.
3. `C:\Users\msbel\projects\ember-crpg-unity\docs\mechanics\ARCHITECTURE.md` — **THIS IS THE TOP-LEVEL SPEC.** Actor + Item primitives, DM query API, NPC memory design, determinism contract, implementation order (Parts 1–6).
4. `C:\Users\msbel\projects\ember-crpg-unity\docs\mechanics\MICRO_MAPPING.md` — class-by-class field/method mapping of DFU + OpenMW with file:line citations. Open this while designing any file that ports a concept from the source engines.

**If a proposal contradicts any of these docs, halt and flag the contradiction. Do not silently deviate.**

## Locked-in architectural decisions (from ARCHITECTURE.md — do not re-litigate)

1. **Real-time with pause** (matches DFU/OpenMW). No turn-based combat state machine.
2. **Primary engines:** DFU + OpenMW. GemRB/DF are reference-only for specific subsystems.
3. **Unified `Actor` + `Item` primitives.** Player/NPC/creature = same `Actor`. All pickables/equippables = `Item`.
4. **Deterministic world.** One seeded `IRng` service. No direct `UnityEngine.Random` calls anywhere except inside the RNG implementation. Replay must be bit-exact given seed + input trace.
5. **LLM DM narrator.** The DM talks to the engine through typed query/roll APIs (Architecture Part 3, four tiers: pure queries / computed probabilities / seeded rolls / mutations). The DM never invents mechanics.
6. **Persistent NPC memory** (Ember extension — neither engine has it fully). Per-NPC disposition, witnessed events, interaction log, disposition decay. See ARCHITECTURE.md §1.4.
7. **Stat paradigm is DFU, NOT D&D.** Attribute range 0..100, base 50. Modifier formulas from `FormulaHelper.cs`:
   - `DamageModifier(STR) = (STR - 50) / 5`
   - `ToHitModifier(AGI) = (AGI / 10) - 5`
   - `HitPointsModifier(END) = (END / 10) - 5`
   - `MagicResist(WIL) = WIL / 10`
   - `MaxEncumbrance(STR) = STR * 1.5`
   - `MaxStatValue = 100`
   **Do NOT use the `(score - 10) / 2` D&D formula under any circumstances.** Any test case using stat values 1-18 is wrong — use 0-100 with base 50.

## Rules

1. **I write every line.** You explain what to write and why BEFORE I type it. Quote the exact formula from the bible (with §N) or ARCHITECTURE.md section when relevant, and cite the source engine file:line when porting (use MICRO_MAPPING.md).
2. **One file at a time.** Never propose more than one production file per step. Test file + production file is OK (test first, then impl).
3. **Test-first.** Write the NUnit EditMode test file before the production file. Each test must assert ONE behavior (no mega-tests).
4. **File contract, stated first.** Before any code, tell me in this exact format:
   - `OWNS:` what state/behavior this file is the source of truth for
   - `READS:` other types/services this file depends on
   - `WRITES:` what state it mutates (if any)
   - `DOES NOT:` things this file explicitly does NOT do (helps me see boundaries)
   - `TEST:` how I verify it in isolation
5. **Keep files under 80 lines** (excluding usings and blank lines). If longer, split — propose the split plan BEFORE implementing.
6. **No PythonBridge, no HTTP, no backend server.** Pure local C# simulation.
7. **No Unity dependency in `Domain/` or `Simulation/`.** These must compile without UnityEngine — that's how we get NUnit tests to run in EditMode without a scene. `using UnityEngine;` is only allowed in `Presentation/` and `Data/` (ScriptableObjects).
8. **Do not port old Python code line-by-line.** Reference `ember-rpg` ONLY for domain knowledge about Ember's flavor (discipline names, world-questions, lore). Implementation comes from the bible + ARCHITECTURE.md + MICRO_MAPPING.md.
9. **When proposing a formula**, quote the bible section + the DFU `FormulaHelper.cs:line` that owns it. Example: "Bible §9 + `FormulaHelper.cs:72` → `(strength - 50) / 5`".
10. **When proposing a type that mirrors a DFU/OMW type**, cite the source file:line and note any divergence. Example: "Mirrors `DaggerfallUnityItem.cs:35-55` but drops `dyeColor` and `unknown` (legacy classic field)".
11. **After each file**, ask me to explain what it does back to you. If I can't explain a concept, rewrite simpler. This is not optional — it is the whole point of the pedagogy.
12. **Comments — split into three tiers, strict policy:**
    - **XML doc summary on every public type and public method.** 1–3 lines. Teaches the intent ("what is this for in Ember"), not the mechanics. Example:
      ```csharp
      /// <summary>Stable handle to an actor in the world. Value type; default value means "no actor".</summary>
      ```
    - **One "design note" comment block at the top of every file** (below usings, above namespace). 1–3 sentences: why this file exists, where it sits in Ember's architecture, what it is NOT. Like a mini file contract baked into the code.
    - **No inline comments that restate WHAT the code does.** Inline comments ONLY for non-obvious WHY — a hidden invariant, a workaround for a specific bug, a subtle constraint. If a comment paraphrases a well-named method, delete it.
13. **File paths MUST be explicit in every proposal.** State both the test file path and the production file path at the start of every step, before any code. Format: `TEST: Assets/Tests/EditMode/Core/ActorIdTests.cs` and `PROD: Assets/Scripts/Domain/Core/ActorId.cs`. No relative paths; always from the Assets root.
14. **Logging policy:**
    - **No `UnityEngine.Debug.Log(...)` in Domain/ or Simulation/.** Ever. UnityEngine dependency would break EditMode tests.
    - Logging comes via an `ILog` primitive introduced at **Step 0f** (after IRng, before any system that mutates state). Two impls: `NullLog` (tests) and `UnityConsoleLog` (Presentation-only).
    - Log events are **structured** (typed record / small struct with named fields), not concatenated strings. This enables later filtering, replay analysis, and the DM reason trace.
    - Log level taxonomy: `Trace` (RNG draws, per-tick), `Event` (state transitions, combat hits, memory writes), `Warn` (recoverable inconsistencies), `Error` (invariant violations).
    - For pure-data types (ActorId, ItemId, GameTime, Attributes, Skills), NO logging. They emit events only when USED by a system.
15. **No emojis in code or docs.** Ever.
16. **Do not generate documentation files** (`.md`, `README`) unless I explicitly ask.
17. **Respond in Turkish for all conversational text** (file contracts, plans, explanations, confirmations). Code, identifiers, XML doc summaries, design-note blocks, and technical terms like `readonly struct`, `IEquatable<T>`, `NUnit`, `EditMode` stay in English. If a Turkish word is ambiguous, keep the English technical term.

## Architecture layout (already scaffolded — do not create new top-level folders)

```
Assets/Scripts/
├── Domain/          ← pure game rules, no Unity. Stats, Actor, Item, Formulas, Combat rules.
├── Simulation/      ← state management, command processing, RNG, GameTime, systems.
├── Data/            ← ScriptableObject defs (items, actors, maps, careers, spells).
└── Presentation/    ← Unity rendering, input, UI, camera, renderer, controller.

Assets/Tests/
└── EditMode/        ← NUnit, no scene. Tests for Domain + Simulation.
```

Namespaces mirror folders: `EmberCrpg.Domain`, `EmberCrpg.Simulation`, `EmberCrpg.Data`, `EmberCrpg.Presentation`, `EmberCrpg.Tests.EditMode`.

## Implementation order (from ARCHITECTURE.md Part 6 — FOLLOW EXACTLY)

**Primitives first (do NOT skip; everything depends on them):**

- **Step 0a — `ActorId`** — value type (readonly struct), 32-bit or 64-bit identifier, equality + hash + debug ToString. Handle pattern demo.
- **Step 0b — `ItemId`** — same pattern, different type.
- **Step 0c — `GameTime`** — value type, total game-minutes as long (1 real sec = 12 game sec per bible §38; 1 day = 1440 minutes; 1 year = 518400 minutes). Helpers: `AddHours`, `AddDays`, `Hour`, `DayOfYear`. No `DateTime` — stays deterministic and compact.
- **Step 0d — `IRng` interface** — `uint NextUInt32()`, `int Range(int minIncl, int maxExcl)`, `bool Chance(int percent)`. Explicit seeded sub-streams via `IRng Fork(ulong streamId)`.
- **Step 0e — `PcgRng`** — PCG-XSH-RR 64→32 implementation of `IRng`. Seeded from `(worldSeed, streamId)`. ~50 lines.
- **Step 0f — `ILog` + structured log events** — `NullLog` (default, tests) and `UnityConsoleLog` (Presentation). `LogEvent` is a typed record with Level + Category + fields. See rule 14.

**Then the original 12 (bible/audit order):**

1. `Attributes` (8-stat DFU container: STR/INT/WIL/AGI/END/PER/SPD/LCK or Ember's 6: MIG/AGI/END/MND/INS/PRE — ASK THE USER which stat set before implementing, but in either case range 0..100 base 50)
2. `Skills` (34 DFU-style or pruned set — ASK before implementing)
3. `Resistances` (5 elements)
4. `Vitals` (Health, Fatigue, Magicka with DynamicStat base/current/modified pattern from OpenMW)
5. Save/Load DTOs stubbed for Actor+Item (no file I/O yet, just DTO types with round-trip tests)
6. `Encumbrance` (depends on Attributes+Items)
7. `Movement` primitives (speed calc)
8. Skill advancement rule (DFU formula from bible §2)
9. Weapons + Armor ItemTemplate
10. Inventory (ItemCollection port)
11. Combat (hit + damage + defense TOGETHER, inseparable)
12. Level-up (DFU bonus pool + OpenMW skill-multiplier from Audit A-01)
13. Rest/Recovery

Then Ember-specific (14-18):

14. DM Tier 1 (pure queries over existing state)
15. `ActorMemory` + `CrimeWitnessLog` (Ember extension)
16. DM Tier 3 (seeded rolls with breakdown trace)
17. DM Tier 4 (mutations)
18. Dialogue topic tracking

**After step 13 we reconvene** to decide order of 14-18 based on what the DM prototype needs first.

## Current step

**STEP 0a: `ActorId`**

Implement as a value type under `Assets/Scripts/Domain/Core/ActorId.cs`. Test file first at `Assets/Tests/EditMode/Core/ActorIdTests.cs`.

Before writing anything, propose:
1. The file contract (OWNS/READS/WRITES/DOES NOT/TEST).
2. The test list (3-5 tests max, each asserting ONE behavior). Tests must cover: construction, equality, inequality, default/empty, hash stability.
3. The production type signature (field(s), constructor, operators, `Equals`, `GetHashCode`, `ToString`).
4. A note on why `readonly struct` (value semantics, no allocation, default-constructible → "empty" sentinel), and why NOT `class`.
5. A note on whether the underlying ID is `int`, `uint`, `long`, or `ulong`, and why. (Hint: OpenMW uses a RefNum with an index + generation counter — we may want that later; for now pick the simplest type that supports future expansion.)

Wait for my confirmation on each of the 5 before writing any code. After I confirm, write ONLY the test file. I will type it. Then we discuss the production file.

Start by reading the four required docs, then propose the five items above.
