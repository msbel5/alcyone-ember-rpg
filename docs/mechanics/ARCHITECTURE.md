# Ember CRPG — Architecture Blueprint

**Date:** 2026-04-19
**Status:** Source-verified against DFU + OpenMW at micro (class/method) level.
**Supersedes:** any earlier architectural assumptions in the bible.

## Design Pillars (locked)

1. **Real-time with pause.** Both DFU and OpenMW are real-time; don't invent a turn-based state machine.
2. **Deterministic world simulation.** All RNG routed through a single seeded service. Replay must be bit-exact given the same seed + input trace.
3. **Unified Actor + Item primitives.** Player, NPC, creature = same `Actor` handle. Every pickable, equippable, consumable = `Item`.
4. **Persistent NPC memory.** Per-NPC disposition, witnessed events, interaction log. Survives save/load and cell unload.
5. **DM query surface.** LLM narrator asks the engine typed questions ("can the player see the guard?", "would this lie succeed?"). Engine returns deterministic result + reason trace. LLM narrates; it never invents mechanics.

All five flow through two primitives: `Actor` and `Item`. Everything else is a service that operates on those two.

---

## Part 1 — The Actor Abstraction

### 1.1 Architectural choice: OpenMW's Ptr+Class pattern

OpenMW's `MWWorld::Ptr` + `MWClass` split is the cleanest generic-actor abstraction in either codebase. DFU's `DaggerfallEntity` → `PlayerEntity`/`EnemyEntity` subclass tree is simpler but forces type branches at every combat/dialogue/loot site.

**We port OpenMW's pattern but populate it with DFU's field set** (because DFU's fields are already tuned for Daggerfall-style play).

```
Actor              — handle (value type / struct). Holds an index into the world actor store.
ActorClass         — virtual dispatch interface (GetStats, Hit, OnHit, Activate, …).
ActorStats         — runtime state. Lives in the world store, keyed by Actor.
ActorMemory        — persistent NPC-specific memory. NEW for Ember.
```

A single `combat.Attack(attacker, target)` call dispatches through `target.Class().OnHit(...)` without knowing if target is NPC / creature / player. The LLM DM calls `mechanics.GetDisposition(npc)` on any `Actor` handle — creatures return a default, NPCs return their stored value.

### 1.2 `ActorStats` — field list (source-verified)

Merge of DFU's `DaggerfallEntity` state (`Game/Entities/DaggerfallEntity.cs:41-70`) + OpenMW's `CreatureStats` (`creaturestats.hpp:41-99`). Every field tagged with source engine + whether it serializes.

#### Core vitals (serialize: yes)
```
attributes      : int[8]         — STR/INT/WIL/AGI/END/PER/SPD/LCK      [DFU stats + OpenMW mAttributes]
skills          : int[N]         — per-skill value (34 DFU / 26 OMW)    [DFU DaggerfallSkills]
resistances     : int[5]         — Fire/Frost/Dis+Poison/Shock/Magic    [DFU DaggerfallResistances]
health          : (cur, max)     — current + max                        [DFU currentHealth/maxHealth]
fatigue         : (cur, max)     — current + derived max                [DFU currentFatigue]
magicka         : (cur, max)     — current + derived max                [DFU currentMagicka]
breath          : (cur, max)     — underwater oxygen                    [DFU currentBreath]
level           : int                                                   [DFU level]
```

#### Transient per-frame (serialize: no — rebuilt by effect system)
```
statMods         : int[8]        — effect bonuses, reset each tick      [DFU mods]
skillMods        : int[N]        — effect bonuses                       [DFU skillMods]
resistMods       : int[5]
magicEffects     : MagicEffects  — aggregated active effects            [OMW mMagicEffects]
```

#### AI + combat state (serialize: partial)
```
aiSettings       : int[4]        — Hello / Fight / Flee / Alarm         [OMW aisetting.hpp — NOT "Combat/Calm"]
aiSequence       : AiPackage[]   — stack of active packages             [OMW mAiSequence]
drawState        : enum          — Sheathed / Equipping / Drawn         [OMW mDrawState]
knockdown        : (bool, bool, bool) — one-frame / over-one-frame / base [OMW mKnockdown*]
block            : bool                                                  [OMW mBlock]
attackingOrSpell : bool                                                  [OMW mAttackingOrSpell]
fallHeight       : float         — accumulated since last ground touch   [OMW mFallHeight]
lastHitObject    : ItemId        — last weapon that hit me               [OMW mLastHitObject]
hitAttemptActor  : ActorId       — last actor who ATTEMPTED (persists in-combat, clears out of combat) [OMW mHitAttemptActor — NOT serialized in OMW; Ember CAN serialize]
```

#### Memory / persistence (serialize: yes — this is what the DM reads)
```
dead / died / murdered / deathAnim / timeOfDeath   [OMW mDead/mDied/...]
talkedTo         : bool          — has the player ever spoken to me?     [OMW mTalkedTo]
alarmed          : bool          — currently aware of nearby crime       [OMW mAlarmed]
attacked         : bool          — have I been attacked recently?        [OMW mAttacked]
goldPool         : int           — merchant gold (not in inventory)      [OMW mGoldPool]
lastRestock      : TimeStamp     — last inventory refresh                [OMW mLastRestock]
```

### 1.3 `NpcStats` — NPC-specific fields (serialize: yes)

From `mwmechanics/npcstats.hpp:25-51`. These matter for persistent memory.

```
disposition              : int       — BASE liking of NPC for player (-100..+100)
crimeDispMod             : int       — temporary penalty from witnessed crimes (decays in Ember)
reputation               : int       — global rep as known to this NPC
crimeId                  : int       — ID of crime event this NPC witnessed (-1 if none)
bounty                   : int       — player's bounty in this NPC's jurisdiction
factionRank              : Map<FactionId, int>
factionReputation        : Map<FactionId, int>
expelled                 : Set<FactionId>
usedIds                  : Set<ItemId>  — items/NPCs player has interacted with
timeToStartDrowning      : float
isWerewolf               : bool
```

### 1.4 `ActorMemory` — Ember extension (NEW)

Neither engine has this. Must build. Attached per-NPC; serialized.

```
events          : RingBuffer<InteractionEvent, N=64>   — recent player-NPC events
                   each event: (timestamp, type, actorSeen, itemId?, amount?, location)
                   types: Greeted, Insulted, Gifted, Bribed, Attacked, Stolen-from,
                          WitnessedCrime, TradedWith, RentedRoom, DialogueTopic(id)

dispositionHistory : List<DispositionDelta>
                   each: (timestamp, deltaValue, reason, decayHalfLifeDays?)
                   current disposition = base + Σ delta * exp(-daysSince / halfLife)

dialogueSeen    : Set<TopicId>                         — "I've already told you that"
transactions    : List<TransactionRecord>              — merchant/innkeeper ledger
                   each: (timestamp, type, itemId, count, goldDelta)
knownAboutPlayer: BitSet<PlayerFactTag>                — e.g. IsWerewolf, IsVampire,
                                                         KilledMyFriend, PaidMyDebt
```

`CrimeWitnessLog` (world-level, one per run):
```
crimeEvents    : Map<CrimeId, CrimeEvent>
                 each: (time, location, perpetrator, victim?, itemId?, gravity, witnesses : Set<ActorId>)
```
When a crime happens: create event, add every aware NPC as witness, set their `crimeId` field to this id. Replaces OpenMW's single-int `crimeId` with a proper event log while keeping the per-NPC pointer.

### 1.5 Event callbacks (from DFU `DaggerfallEntity:876-897`)

```
OnDeath(actor)
OnExhausted(actor)     — fatigue ≤ 0
OnMagickaDepleted(actor)
OnAttacked(victim, attacker)   — Ember addition; hooks memory writes
OnWitnessCrime(witness, event) — Ember addition
OnDialogueTopic(npc, player, topicId) — Ember addition
```

All callbacks are the hooks the DM subscribes to for narration prompts.

### 1.6 Update loop contract

One `Tick(dt)` per actor per frame. Sub-schedules inside:

- **Per-frame**: AI package step, movement, animation, effect application.
- **Every 0.2s (OMW cadence)**: recompute effect mods → write to `statMods`/`skillMods`.
- **Per-game-minute (DFU cadence, `PlayerEntity:402-520`)**: fatigue drain from activity.
- **Per-game-hour**: HP/fatigue/magicka regen (rates from `FormulaHelper:205,215,228`), disease progression tick.
- **Per-game-day**: regional prices, faction power, rent expiry, memory decay application.
- **Per-game-week/month/year**: faction-power updates (DFU 7/38/84/112-day cadence).

No hidden "per-cell-entry" logic — when a cell loads, each actor's serialized state rehydrates and its `Tick()` resumes from last-recorded game time.

---

## Part 2 — The Item Abstraction

### 2.1 Port DFU's `DaggerfallUnityItem` verbatim

DFU's item design is already excellent for our needs (`Game/Items/DaggerfallUnityItem.cs:30-1885`). Port as-is, rename namespace. Key design properties:

- **UID-keyed**. Every item has an immutable `ulong uid` assigned at construction. UID survives save/load, enables O(1) lookup.
- **Template-driven**. Static data in `ItemTemplate` (weight, base price, slot type, variants). Instance holds only what diverges from template.
- **Stack-aware**. `stackCount` on the item; `AddItem` auto-merges stackable ingredients/arrows/gold.
- **Condition/durability built in**. `currentCondition` / `maxCondition`. `LowerCondition(damage, owner, collection)` is the single mutation point — combat code calls it and it handles break + auto-unequip + enchantment-break payloads.
- **Dual enchantment system**. `legacyMagic` (classic type+param pairs, max 10) + `customMagic` (modern key+param). Charges can be modeled as `maxCondition = N_uses`.

### 2.2 Irreducible field set

Ordered by necessity tier — each tier adds what the next gameplay feature needs:

**Tier 1 — inventory basic (6 fields)**
```
uid, itemGroup, groupIndex, stackCount, shortName, weightInKg
```

**Tier 2 — equipment (4 fields)**
```
currentCondition, maxCondition, nativeMaterialValue, equipSlot
```

**Tier 3 — combat (2 fields)**
```
value (gold), flags (identified | artifact)
```

**Tier 4 — enchantment (3 fields)**
```
legacyMagic[], customMagic[], enchantmentPoints
```

**Tier 5 — niche (defer)**
```
dyeColor, currentVariant, poisonType, trappedSoulType, isQuestItem, questUID,
potionRecipeKey, repairData, timeForItemToDisappear, timeHealthLeechLastUsed, message
```

### 2.3 Equipment slots — 27 fixed, not generic

DFU's `EquipSlots` enum (`ItemEnums.cs:105-135`) is hardcoded 27 slots. Port verbatim. Two-handed weapon logic lives in `ItemEquipTable.EquipItem()` (`ItemEquipTable.cs:94-154`) — equipping 2H clears both hands; equipping shield clears 2H from right hand. Keep that logic; it's battle-tested.

If Ember wants a different slot count later, change the enum. For v1: identical to Daggerfall.

### 2.4 `ItemCollection` — the one and only container type

Same class serves inventory / loot pile / merchant stock / container / quest stash. `OrderedDictionary<uid, item>`. Public API is ~20 methods (`ItemCollection.cs`):

```
AddItem, RemoveItem, RemoveOne, SplitStack, ReorderItem
Contains(uid|item|group+index|questItem), GetItem(uid|index|group+index+filter)
TransferAll, CopyAll, ReplaceAll, Transfer, Import, AddItems, Clear
CloneAll, Export, ExportQuestItems
SerializeItems, DeserializeItems
GetNumItems, GetWeight, GetCreditAmount, RemoveExpiredItems
```

### 2.5 Gold model

Gold is **both** a field AND an item:
- Player's wallet: `int goldPieces` on `Actor` (DFU `PlayerEntity.goldPieces:64`). Weight = `goldPieces * 0.0025 kg` (DFU default).
- Merchant/NPC/loot: stack of `Currency.Gold_pieces` item in their `ItemCollection`.

Accept the asymmetry — it's how DFU shipped, and it makes pickpocket/merchant flows natural.

---

## Part 3 — The DM Query API

The LLM DM never touches actor fields directly. It calls typed query functions. Three tiers by determinism.

### 3.1 Tier 1 — Pure queries (no RNG, no side-effects)

Safe to call any number of times. The DM uses these to describe state.

```
// Actor queries
GetActor(id)                                  -> ActorView
GetDisposition(npc)                           -> int (-100..+100)       [OMW derived]
GetReputation(npc)                            -> int
GetBounty(player, region?)                    -> int
GetFactionRank(actor, faction)                -> int (-1 if not member)
GetFactionReputation(actor, faction)          -> int
GetSkill(actor, skill)                        -> int
GetAttribute(actor, attr)                     -> int
GetHealth(actor) / GetFatigue(actor) / GetMagicka(actor) -> (cur, max)
GetEncumbrance(actor)                         -> (cur, max)
GetEquippedItem(actor, slot)                  -> ItemView?
GetInventory(actor)                           -> List<ItemView>
GetActiveEffects(actor)                       -> List<EffectView>
IsDead(actor) / IsKnockedDown(actor) / IsParalyzed(actor)  -> bool
IsWerewolf(actor) / IsVampire(actor)          -> bool

// Memory queries (Ember extension)
GetNpcMemory(npc)                             -> NpcMemoryView       // events, dispositionHistory, dialogueSeen
GetDialogueContext(npc, topic)                -> DialogueMemoryContext // new/repeated/well-worn from dialogueSeen + topic events
GetGuardContext(npc, passage)                 -> GuardMemoryContext    // stance from passage requests + clearance history
GetMerchantContext(npc)                       -> MerchantMemoryContext // familiarity from transactions/trade events
GetWitnessedCrimes(npc)                       -> List<CrimeEvent>
HasMet(npc, other)                            -> bool
GetTransactionLog(npc)                        -> List<TransactionRecord>
KnowsAboutPlayer(npc, fact)                   -> bool                 // e.g. KilledMyFriend

// World / spatial
GetDistance(a, b)                             -> float
CanSee(observer, actor)                       -> bool                 // LOS + light + awareness [OMW awarenessCheck; deterministic]
IsDetected(actor, observer)                   -> bool                 // stricter, includes LOS
GetTimeOfDay()                                -> GameTime
GetWeather(region)                            -> WeatherState
```

All Tier-1 calls are pure functions of world state. The DM can chain them.

### 3.2 Tier 2 — Computed probabilities (deterministic, no RNG)

The DM uses these to present choices or decide whether to narrate a roll.

```
ComputeHitChance(attacker, target, weapon, bodyPart?)   -> int (0..100)
ComputeDodgeChance(target, attacker)                    -> int
ComputeBlockChance(blocker, attacker)                   -> int
ComputeStealthDetectChance(sneaker, observer)           -> int
ComputeLockpickChance(actor, lock)                      -> int
ComputePickpocketChance(actor, target, item)            -> int
ComputePersuadeChance(player, npc, type)                -> int          // Admire/Intimidate/Taunt/Bribe
ComputeSpellSuccessChance(caster, spell)                -> int
ComputeTradePrice(player, shop, item, buying)           -> int
ComputeRepairCost(item, smith?)                         -> int
ComputeSaveVsEffect(target, element, magnitude)         -> int
ComputeLoanMax(player, region)                          -> int
```

These wrap DFU `FormulaHelper` methods that return chances WITHOUT calling `Dice100`. Expose the probability; let the DM decide whether to roll.

### 3.3 Tier 3 — Seeded rolls (RNG, with reason trace)

The DM invokes these when narrative progression requires an outcome. Every call takes an explicit seed (derived from `eventId + worldSeed`) so the world stays reproducible.

```
RollCheck(actor, skill, difficulty, seed)              -> RollResult
RollHit(attacker, target, weapon, seed)                -> HitResult     // miss | hit(bodyPart, raw)
RollDamage(attacker, weapon, target, seed)             -> DamageResult  // raw, afterArmor, condDamage
RollSave(target, element, magnitude, seed)             -> SaveResult
RollDisease(attacker, target, monsterType, seed)       -> DiseaseResult?
RollLockpick(actor, lock, seed)                        -> LockpickResult
RollPersuade(player, npc, type, seed)                  -> PersuadeResult
RollLoot(container, playerLevel, seed)                 -> Item[]
```

`RollResult` is not just pass/fail:
```
RollResult {
    success     : bool
    rollValue   : int           // the d100 that came up
    threshold   : int           // what it needed to beat
    breakdown   : List<(name, value)>   // every modifier that fed into threshold
                                        // e.g. [("skill",45),("AGI_mod",3),("racial",10),("luck",1),("armor_penalty",-4)]
}
```

The `breakdown` IS the reason trace. DM narrates: "You slip past — your skulking in low-town was enough to beat the guard's slight alertness (45+3+10+1 = 59 vs 35)."

### 3.4 Tier 4 — Mutations (side-effects; DM only invokes when the narrative has committed)

```
CommitDamage(target, amount, source, type)             -> DeathResult
CommitSpellEffect(target, effect)
CommitDispositionDelta(npc, delta, reason, halfLife?)
CommitCrime(perpetrator, victim?, type, gravity)       -> CrimeEvent     // auto-assigns witnesses
CommitTransaction(player, npc, txn)
CommitDialogueTopic(npc, player, topicId)
CommitGiveItem(from, to, item, count)
CommitEquip / CommitUnequip
```

These are the ONLY way the DM changes world state. Every mutation writes an entry to the event log → `ActorMemory.events` on affected NPCs → next time DM asks `GetNpcMemory(npc)`, the event is there.

### 3.5 Determinism contract

- One global `IRng` service. Backed by a seeded PCG or xoshiro.
- No code outside this service may call `UnityEngine.Random`. Linter rule enforces.
- Tier 3 rolls derive sub-seeds: `eventSeed = hash(worldSeed, gameTime, eventId)`. Same event at same time = same roll.
- DFU's 24+ `UnityEngine.Random.Range` sites (mapped in audit) get replaced with `rng.Range(seed, min, max)`.
- OpenMW's `world->getPrng()` pattern is the model — one reference, handed out from a world service.

---

## Part 4 — Persistence & Serialization

### 4.1 Save model

Follow DFU's `ItemData_v1` / `PlayerEntityData_v1` / `EnemyData_v1` DTO pattern (`Game/Serialization/SerializableGameObject.cs`). Benefits:

- Version suffix on the type name (`_v1`) lets us migrate safely later.
- DTO is pure data, no behavior — swap formats (JSON, binary, msgpack) without touching entity code.
- Clear round-trip: `entity.GetSaveData() → dto → Restore(dto) → entity`.

Per Ember additions:
- `NpcMemoryData_v1` — `events[], dispositionHistory[], dialogueSeen[], transactions[], knownAboutPlayer`
- `CrimeWitnessLogData_v1` — one per save, not per actor
- `WorldRngState_v1` — the RNG counter + seed so determinism survives saves

### 4.2 What must round-trip

| Layer | Fields | Strategy |
|-------|--------|----------|
| `Actor` core | attributes, skills, resistances, vitals, level | direct DTO copy |
| `Actor` effect state | active `MagicEffects` bundles | OMW `ActiveSpells` pattern — each bundle has source spell id + remaining duration + magnitudes |
| `Actor` AI transient | aiSequence | top package only; re-prime on load |
| `NpcStats` | disposition, crimeId, reputation, bounty, faction data | direct copy |
| `ActorMemory` | everything | direct copy |
| `ItemCollection` | each item as `ItemData_v1` | existing DFU path |
| `Equipment` | uid[27] | existing DFU path |
| `RNG` | worldSeed, counter | one int + one long |
| `GameTime` | total game minutes since epoch | one long |

### 4.3 Cell unload safety

OpenMW unloads distant cells. DFU never does (small maps). Ember will have large regions, so:
- Actor `Tick()` computes a `nextEventTime` cheaply per actor.
- When cell unloads, actor is serialized; on reload, we fast-forward `(now - lastTick)` by running event-based updates (not one tick per frame) up to current game time.
- This means every `Tick` must be idempotent w.r.t. being skipped and summarized. Use per-hour/per-day aggregates, not per-frame integration, for anything that matters across unload.

---

## Part 5 — How This Serves the LLM DM

End-to-end example: **player tries to lie to guard about a theft**.

1. DM receives: "I tell the guard it was someone else."
2. DM calls `GetNpcMemory(guardId)` → sees `events` containing `{type:WitnessedCrime, time:-3min, item:"silver goblet"}`.
3. DM calls `ComputePersuadeChance(player, guardId, Type.Intimidate)` → 23% (low because disposition is now -40 from the crime).
4. DM decides to allow the attempt; calls `RollPersuade(player, guardId, Intimidate, seed)` → `{success:false, roll:62, threshold:23, breakdown:[...]}`.
5. DM calls `CommitDispositionDelta(guardId, -5, "insulting lie", halfLife:7d)` → guard now at -45.
6. DM narrates the failure using the breakdown + memory event. The guard mentions the silver goblet because it's in her memory.
7. Save later — everything persists: the lie, the disposition, the decay curve.
8. Seven days later, decay has halved; guard is at -42.5. DM can query and narrate "the guard still eyes you, though less sharply than before."

Nothing in this flow uses floating-point randomness outside the seeded RNG. Nothing is stored in LLM context — every query reads the world. Replay the same inputs and the same things happen.

---

## Part 6 — What to Build First (revised order)

The 12-step order in `BIBLE_AUDIT.md` is still right but needs primitives prepended. The finest-grained teaching order lives in `CODEX_PROMPT.md` and is authoritative; the summary below is for reading flow.

**Steps 0a–0f — primitives** (see `CODEX_PROMPT.md` for exact file paths):
- 0a `ActorId` — value-type handle, zero deps. Teaches the handle pattern.
- 0b `ItemId` — same pattern for items.
- 0c `GameTime` — total-minutes value type.
- 0d `IRng` interface — determinism contract.
- 0e `PcgRng` — seeded implementation (PCG-XSH-RR).
- 0f `ILog` + `LogEvent` — structured logging primitive.

None of 0a/0b/0c depend on 0d/0e/0f, so the teaching order (IDs → time → rng → log) is safe even though RNG is conceptually the "most fundamental" primitive. We write the simplest zero-dependency primitives first to build reading comfort with the codebase.

Then the original 12:

1. Stats → 2. Health/Fatigue/Mana → 3. Save/Load wiring → 4. Encumbrance → 5. Movement → 6. Skills → 7. Weapons+Armor → 8. Inventory → 9. Combat (attack+damage+defense together) → 10. Skill advancement → 11. Level-up (DFU bonus + OMW skill multiplier) → 12. Rest/Recovery.

Then the Ember-specific:

13. **DM API Tier 1 (pure queries)** — read-only, no new data. Just wraps existing state.
14. **NPC Memory store** — `ActorMemory`, `CrimeWitnessLog`, serialization.
15. **DM API Tier 3 (seeded rolls)** — requires RNG + DTO + combat to exist.
16. **DM API Tier 4 (mutations)** — requires memory store.
17. **Dialogue topic tracking** — small, but gates LLM "I've already told you" realism.
18. Everything else (quests, factions, magic, etc.) per bible priority.

---

## Part 7 — What we DROP from the bible now

Given the DFU+OpenMW lock-in and real-time commitment:

- **GemRB D&D 2E THAC0 system** — out. Use DFU's percentage system exclusively. GemRB stays as a reference for containers/pathfinding/morale only.
- **Turn-based combat state machine** — out. Real-time with pause, matching DFU.
- **D&D save categories (save vs. spell/breath/death/paralysis/wands)** — out. Use DFU `SavingThrow(element, ...)` with 5 element resistances.
- **Dual-class / multi-class / kits** — out. Single-class with skill-driven progression.
- **DF world-gen / language / temperature-state material sim** — out (v1). Use DF *only* for body/tissue (§19), materials tiers (§13), syndromes pattern (§36), personality tags (§47), and creature variants (B-12 in audit).
- **GemRB GameScript DSL as NPC AI** — out. OpenMW AI package queue is the model. The LLM DM replaces scripted dialogue.

Bible sections for these systems stay as reference but get marked `REFERENCE ONLY — not implemented in Ember v1`.

---

## Appendix — File-level index for the port

For when implementation begins, here are the canonical source files to open:

**Actor core**
- DFU: `Assets/Scripts/Game/Entities/DaggerfallEntity.cs`, `PlayerEntity.cs`, `EnemyEntity.cs`, `DaggerfallStats.cs`, `DaggerfallSkills.cs`, `DaggerfallResistances.cs`, `MagicAndEffects/EntityEffectManager.cs`
- OpenMW: `apps/openmw/mwworld/ptr.hpp`, `mwworld/class.hpp`, `mwclass/npc.cpp`, `mwclass/creature.cpp`, `mwmechanics/creaturestats.hpp`, `mwmechanics/npcstats.hpp`, `mwmechanics/spells.hpp`, `mwmechanics/magiceffects.hpp`, `mwmechanics/activespells.hpp`

**Item core**
- DFU only: `Assets/Scripts/Game/Items/DaggerfallUnityItem.cs`, `ItemCollection.cs`, `ItemTemplate.cs`, `ItemHelper.cs`, `ItemEquipTable.cs`, `ItemEnums.cs`

**Formula/DM surface**
- DFU: `Assets/Scripts/Game/Formulas/FormulaHelper.cs` (all ~100 public statics)
- OpenMW: `apps/openmw/mwmechanics/combat.cpp`, `spellcasting.cpp`, `pickpocket.cpp`, `repair.cpp`, `alchemy.cpp`, `mechanicsmanagerimp.cpp`

**Serialization reference**
- DFU: `Assets/Scripts/Game/Serialization/SerializableGameObject.cs`, `SerializablePlayer.cs`, `SerializableEnemy.cs`
- OpenMW: `components/esm3/creaturestats.hpp`, `npcstats.hpp`, and their `readState`/`writeState` impls

**NPC AI reference**
- OpenMW: `apps/openmw/mwmechanics/aisequence.*`, `aicombat.*`, `aipackage.*`, `aiwander.*`, `aifollow.*`, `aiescort.*`, `aitravel.*`

**NPC memory extension (Ember — does not exist yet; build here)**
- `Assets/Scripts/Domain/Memory/ActorMemory.cs`
- `Assets/Scripts/Domain/Memory/CrimeWitnessLog.cs`
- `Assets/Scripts/Data/Saves/NpcMemoryData_v1.cs`

**DM API (Ember — growing in thin deterministic slices)**
- `Assets/Scripts/Simulation/Narrative/NpcMemoryQueryService.cs` — current memory-backed Tier 1 query contexts for dialogue, guard stance, and merchant familiarity
- `Assets/Scripts/Domain/AiDm/IDmQueryService.cs (planned)` — future broader Tier 1 + 2 contract
- `Assets/Scripts/Domain/AiDm/IDmRollService.cs (planned)` — future Tier 3 contract
- `Assets/Scripts/Domain/AiDm/IDmMutationService.cs (planned)` — future Tier 4 contract
- `Assets/Scripts/Simulation/Rng/IRng.cs` — deterministic RNG primitive
