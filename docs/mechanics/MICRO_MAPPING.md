# Micro-Level Source Mapping — DFU + OpenMW

**Date:** 2026-04-19
**Purpose:** Class-by-class, method-by-method mapping of the two primary engines (DFU + OpenMW) Ember is porting from. Every field and method cited with file:line. This is the implementer's lookup reference — when building Ember's `Actor.cs` or `Item.cs`, open this doc alongside the source files listed at the bottom of `ARCHITECTURE.md`.

**How to use:** Find the subsystem you're implementing → read its class shape here → open the cited source file to see the actual C#/C++ → write the Ember equivalent.

---

## Section 1 — Actor class hierarchy (DFU)

### 1.1 `DaggerfallEntity` — abstract base
**Path:** `Assets/Scripts/Game/Entities/DaggerfallEntity.cs` (1130 lines)

Fields (persistent unless noted):

| Field | Type | Line | Serialize | Note |
|-------|------|------|-----------|------|
| `entityBehaviour` | `DaggerfallEntityBehaviour` | 41 | n | Unity MonoBehaviour peer |
| `gender` | `Genders` | 43 | y | M/F |
| `career` | `DFCareer` | 44 | y | Career template (stats, skill defaults) |
| `name` | `string` | 45 | y | |
| `level` | `int` | 46 | y | |
| `stats` | `DaggerfallStats` | 47 | y | 8 attributes |
| `skills` | `DaggerfallSkills` | 48 | y | 34 skills |
| `resistances` | `DaggerfallResistances` | 49 | y | 5 elements |
| `items` | `ItemCollection` | 50 | y | Inventory |
| `equipTable` | `ItemEquipTable` | 51 | derived | 27-slot equipment |
| `worldContext` | `WorldContext` | 52 | y | Floating-origin frame |
| `maxHealth` | `int` | 53 | y | Raw max HP |
| `currentHealth` | `int` | 54 | y | Clamped 0..max |
| `currentFatigue` | `int` | 55 | y | |
| `currentMagicka` | `int` | 56 | y | |
| `maxMagicka` | `int` | 57 | y | |
| `currentBreath` | `int` | 58 | y | Underwater oxygen |
| `minMetalToHit` | `WeaponMaterialTypes` | 59 | y | Creature immunity below this tier |
| `armorValues` | `sbyte[7]` | 60 | y | Per-body-part AC |
| `team` | `MobileTeams` | 61 | y | Faction/alignment |
| `quiesce` | `bool` | 63 | n | Suppress events during load |
| `isParalyzed` | `bool` | 64 | n | Effect flag, per-frame |
| `resistanceFlags` | `bool[5]` | 66 | n | Effect-driven |
| `resistanceChances` | `int[5]` | 67 | n | Effect-driven |
| `spellbook` | `List<EffectBundleSettings>` | 70 | y | Known spells |

Vital mutators (lines 308-447):
- `IncreaseHealth(n)`, `DecreaseHealth(n)`, `SetHealth(n)` → OnDeath event if ≤0
- `IncreaseFatigue`, `DecreaseFatigue`, `SetFatigue` → OnExhausted event if ≤0
- `IncreaseMagicka`, `DecreaseMagicka`, `SetMagicka` → OnMagickaDepleted if ≤0
- `FillVitalSigns()` — all to max

Events (lines 876-897): `OnDeath`, `OnExhausted`, `OnMagickaDepleted`.

Derived properties (lines 249-276): `MaxHealth`, `MaxFatigue = (LiveStrength + LiveEndurance) * FatigueMultiplier`, `MaxMagicka`, `MaxBreath = LiveEndurance/2`, `DamageModifier`, `MaxEncumbrance`, `MagicResist`, `ToHitModifier`, `HitPointsModifier`, `HealingRateModifier` — all delegate to `FormulaHelper`.

### 1.2 `PlayerEntity` — player-specific state
**Path:** `Assets/Scripts/Game/Entities/PlayerEntity.cs` (2400+ lines)

Extra fields beyond base:

| Field | Line | Note |
|-------|------|------|
| `raceTemplate` | 58 | Birth race |
| `faceIndex` | 59 | Appearance |
| `reflexes` | 60 | PlayerReflexes enum |
| `wagonItems`, `otherItems` | 61-62 | Storage inventories |
| `goldPieces` | 64 | Wallet (NOT an item) |
| `factionData` | 65 | `PersistentFactionData` — faction graph |
| `globalVars` | 66 | Custom quest vars |
| `notebook` | 67 | Journal |
| `skillUses` | 69 | short[] per skill |
| `skillsRecentlyRaised` | 70 | uint[] bitfield |
| `timeOfLastSkillIncreaseCheck` | 71 | Skill level-up gating |
| `startingLevelUpSkillSum` / `currentLevelUpSkillSum` | 76-77 | Level progression math |
| `readyToLevelUp` | 78 | Flag |
| `sGroupReputations` | 81 | 11-slot social group rep (-100..+100) |
| `biographyResistDiseaseMod` etc. | 83-88 | Q&A-based modifiers |
| `timeToBecomeVampireOrWerebeast` | 96 | Infection countdown |
| `crimeCommitted` | 101 | Active crime flag |
| `rentedRooms` | 106 | List<RoomRental_v1> |

Key Update() logic (lines 347-538):
- Per game minute: fatigue drain scaled by activity (11-88 pts)
- Per game day: regional prices, weather, rent expiry
- Per game week (7 days): faction power updates
- Per game month (38/84/112 days): faction/legal/racial events
- Enemy spawn rolls every ~12 minutes via `IntermittentEnemySpawn()`

### 1.3 `EnemyEntity` — NPC/creature state
**Path:** `Assets/Scripts/Game/Entities/EnemyEntity.cs` (502 lines)

Extra fields:

| Field | Line | Persistence | Note |
|-------|------|-------------|------|
| `careerIndex` | 54 | derived | Index into career enum |
| `entityType` | 55 | derived | Monster vs Class |
| `mobileEnemy` | 56 | reference | Template |
| `pickpocketByPlayerAttempted` | 57 | **y** | Persists across saves |
| `questFoeSpellQueueIndex` | 58 | y | Quest scripting |
| `questFoeItemQueueIndex` | 59 | y | Quest scripting |
| `suppressInfighting` | 60 | y | Disable NPC-on-NPC |
| `SoulTrapActive` | 128 | y | Active trap |
| `WabbajackActive` | 130 | y | Wabbajack effect |

**NPC memory hooks (persistent across encounter):**
- `pickpocketByPlayerAttempted` (57) — "you tried to steal from me"
- `hasEncounteredPlayer` (stored in EnemySenses, in EnemyData_v1:376) — "we've met"
- `isHostile` (in EnemyData_v1:375) — "I'm mad at you"
- Quest flags (58-59) — scripted state

`SetEnemyCareer()` (248-402) initializes health via `Random.Range(MinHealth, MaxHealth+1)` — bad RNG call, replace with seeded RNG in Ember.

### 1.4 `DaggerfallStats` — stat container
**Path:** `Assets/Scripts/Game/Entities/DaggerfallStats.cs` (302 lines)

```
[SerializeField] int Strength, Intelligence, Willpower, Agility,
                     Endurance, Personality, Speed, Luck     // lines 31-38 (permanent)
int[] mods                                                    // line 43 (per-frame effect bonuses)
int[] maxMods                                                 // line 44 (max-cap modifiers)
```

`LiveStrength` = `Permanent + mod`, clamped to `[0, max]`. Properties at 50-66.
`IsAllMax()`, `SetDefaults()` (→50 all), `Copy()`, `Clone()`, `GetLiveStatValue(DFCareer.Stats|int)`, `GetPermanentStatValue()`, `AssignMods()`, `SetPermanentStatValue()`, `SetPermanentFromCareer()`.

**No RNG in stats.** Pure container. Ember can port directly.

### 1.5 `DaggerfallSkills`
**Path:** `Assets/Scripts/Game/Entities/DaggerfallSkills.cs` (570 lines)

34 named shorts (Medical, Etiquette, …, CriticalStrike) lines 34-68. `int[] mods` line 73.

**`SetDefaults()` at line 91-98 uses `UnityEngine.Random.Range(3, 7)` for initial skill values (line 95).** This is the ONE RNG in the skill system. Ember's initial-skill roll must use seeded RNG instead.

Key methods:
- `GetPrimaryStat(DFCareer.Skills) : DFCareer.Stats` (401-483) — skill → governing stat mapping
- `GetAdvancementMultiplier(DFCareer.Skills) : int` (485-547) — XP multiplier per skill
- `IsLanguageSkill(DFCareer.Skills) : bool` (549-566)

### 1.6 `DaggerfallResistances`
**Path:** `Assets/Scripts/Game/Entities/DaggerfallResistances.cs` (178 lines)

5 ints: Fire, Frost, DiseaseOrPoison, Shock, Magic. Live = Permanent + Mod. No RNG.

---

## Section 2 — Actor class hierarchy (OpenMW)

### 2.1 `MWWorld::Ptr` — generic actor/item handle ⭐
**Path:** `apps/openmw/mwworld/ptr.hpp` (198 lines)

Three pointer fields (lines 27-29):
```cpp
LiveCellRefBaseType* mRef;           // the actual object record + class pointer
CellStoreType* mCell;                // which cell (nullptr if in container)
ContainerStoreType* mContainerStore; // if in inventory instead of cell
```

**`getClass()` at line 47-52** — returns `*(mRef->mClass)`. This is the vtable dispatch gateway. A single line, but enables all polymorphic actor/item behavior.

`getType()` (38), `get<T>()` (54-58 dynamic cast), `getCellRef()` (67 static data), `getRefData()` (73 runtime data), equality/ordering by `mRef` address (115-125).

**`SafePtr` (165-194)** — wraps a `Ptr` + `FormId`; auto-refreshes the Ptr via `WorldModel` if the underlying cell unloads/reloads. **Ember will need this pattern once cells unload.**

### 2.2 `MWMechanics::CreatureStats`
**Path:** `apps/openmw/mwmechanics/creaturestats.hpp` (294 lines)

Full runtime state for any actor. Every field below serializes to ESM3 save records.

```cpp
mAttributes : map<RefId, AttributeValue>  // 41 — 8 attrs with base+modifier
mDynamic[3] : DynamicStat<float>          // 42 — Health, Magicka, Fatigue (current+max+modified)
mDrawState : DrawState                     // 43 — Sheathed / Equipping / Drawn
mSpells : Spells                           // 44 — spellbook
mActiveSpells : ActiveSpells               // 45 — current active spells on actor
mMagicEffects : MagicEffects               // 46 — aggregated effect magnitudes
mAiSettings[4] : Stat<int>                 // 47 — Hello / Fight / Flee / Alarm
mAiSequence : AiSequence                   // 48 — stack of AI packages
mDead / mDeathAnimationFinished / mDied / mMurdered  // 49-52
mTalkedTo : bool                           // 54 — has player ever spoken?
mAlarmed : bool                            // 55 — aware of crime
mAttacked : bool                           // 56 — has been attacked
mKnockdown / mKnockdownOneFrame / mKnockdownOverOneFrame // 57-59
mHitRecovery : bool                        // 60
mBlock : bool                              // 61 — actively blocking
mMovementFlags : uint32                    // 62 — Run / Sneak / Jump bits
mFallHeight : float                        // 64 — accumulated since last ground touch
mLastHitObject : RefId                     // 66 — last weapon that hit me
mLastHitAttemptObject : RefId              // 67 — last attempted weapon (includes misses)
mLastRestock : TimeStamp                   // 70 — merchant restock timer
mGoldPool : int                            // 73 — merchant gold (not in inventory)
mHitAttemptActor : ESM::RefNum             // 77 — last actor who tried to hit me
                                            //       (NOT serialized in OMW; Ember can serialize)
mSideMovementAngle : float                 // 80 — view-vs-body angle offset
mTimeOfDeath : TimeStamp                   // 82 — for respawn
mSummonedCreatures : multimap<RefId, RefNum> // 85 — cleanup tracking
mAwarenessTimer : float                    // 87 — countdown
mAwarenessRoll : int                       // 88 — cached roll (-1 = not rolled)
mAttackType : string                       // 91 — "slash"/"bash"/"thrust"
mLevel : int                               // 92
mAttackingOrSpell : bool                   // 93
mFriendlyHits : int                        // 53 — friendly-fire count
mDeathAnimation : int8                     // 97 — which anim index played
mTeleported : bool                         // 99 — this frame only
```

**`getFatigueTerm()` (cpp:39-53) — CENTRAL PRIMITIVE:**
```cpp
normalised = (max ≈ 0) ? 1 : max(0, current/max);
fatigueTerm = fFatigueBase - fFatigueMult * (1 - normalised);
```
Multiplies hit chance, block chance, damage output, fall damage. One of the most called numbers in OpenMW.

Getters (pure, no RNG): lines 104-289. Setters: 143-262. Serialization: `writeState()`/`readState()` (265-267).

### 2.3 `MWMechanics::NpcStats` — NPC-specific extension
**Path:** `apps/openmw/mwmechanics/npcstats.hpp` (143 lines)

Private-inherits `CreatureStats`. Adds:

```cpp
mDisposition : int                          // 25 — base liking of NPC for player (-100..+100)
mCrimeDispositionModifier : int             // 26 — temporary crime-witness penalty
mSkills : map<RefId, SkillValue>            // 27 — 26 skills with base+mod+progress
mReputation : int                           // 29 — global rep as known to this NPC
mCrimeId : int                              // 30 — ID of witnessed crime event (-1 if none)
mBounty : int                               // 33 — player's bounty in this NPC's jurisdiction
mWerewolfKills : int                        // 34
mFactionRank : map<RefId, int>              // 37 — this NPC's rank in each faction
mExpelled : set<RefId>                      // 38
mFactionReputation : map<RefId, int>        // 39
mLevelProgress : int                        // 40 (player only)
mSkillIncreases : map<RefId, int>           // 41 (player only) — level-up mult input
mSpecIncreases : vector<int>                // 43 (player only)
mUsedIds : set<RefId>                       // 45 — books/potions already used
mTimeToStartDrowning : float                // 49
mIsWerewolf : bool                          // 51
```

**Persistent memory summary** (all serialize):
- `mDisposition` — per-NPC, not global
- `mCrimeId` — witnessed crime event pointer
- `mBounty` — player's warrant level
- `mFactionReputation` — per-faction standing
- `mUsedIds` — items/NPCs player has touched

**Ember extension:** this set is the STARTING POINT for persistent NPC memory, but it's still thin. Add `events[]`, `dispositionHistory[]`, `dialogueSeen[]`, `transactions[]` per `ARCHITECTURE.md §1.4`.

### 2.4 `MWMechanics::Spells`
**Path:** `apps/openmw/mwmechanics/spells.hpp` (114 lines)

```cpp
mSpellList : shared_ptr<SpellList>           // 30 — shared definitions
mSpells : vector<const ESM::Spell*>          // 31 — pointers into spellbook
mSelectedSpell : RefId                       // 34 — queued spell to cast
mUsedPowers : vector<(Spell*, TimeStamp)>    // 36 — 24h cooldown tracker
```

Interface: `hasSpell`, `add/remove`, `clear`, `canUsePower(spell)` (74-91), `purgeCommonDisease/BlightDisease/Curses` (65-68). Serialize: `readState/writeState` (105-106).

### 2.5 `MWBase::MechanicsManager` — DM service interface
**Path:** `apps/openmw/mwbase/mechanicsmanager.hpp` (316 lines)

Ember's DM Tier 1-4 API should mirror this surface. Key DM-callable methods:

| Method | Line | Deterministic? | Purpose |
|--------|------|----------------|---------|
| `getDerivedDisposition(npc, clamp=true)` | 100 | **yes** | NPC attitude inc. modifiers |
| `getPersuasionDispositionChange(npc, type, out success, out temp, out perm)` | 164 | no (rolls) | Admire/Intimidate/Taunt/Bribe |
| `awarenessCheck(actor, observer)` | 107 | **yes** | Can observer detect actor (no LOS) |
| `isActorDetected(actor, observer)` | 276 | yes | Stricter, includes LOS |
| `commitCrime(actor, victim, type, factionId, arg, victimAware)` | 134 | partial | Bounty + witnesses + dispositions |
| `actorAttacked(victim, attacker)` | 138 | yes | Sets `mHitAttemptActor` |
| `actorKilled(victim, attacker)` | 142 | yes | Murder bounty if player |
| `itemTaken(actor, item, container, count)` | 146 | partial | Theft detection |
| `startCombat(ptr, target, allies)` | 110 | yes | Force combat |
| `stopCombat(ptr)` | 115 | yes | Cancel combat |
| `isAggressive(ptr, target)` | 256 | yes | Hostility check |
| `getActorsSidingWith(actor)` | 231 | yes | Who's following me |
| `getActorsFollowing(actor)` | 232 | yes | Active `AiFollow` packages |
| `getActorsFighting(actor)` | 238 | yes | Active combat partners |

---

## Section 3 — Item class (DFU)

### 3.1 `DaggerfallUnityItem`
**Path:** `Assets/Scripts/Game/Items/DaggerfallUnityItem.cs` (1885 lines)

Core fields (serialized in `ItemData_v1`):

| Field | Type | Line | Note |
|-------|------|------|------|
| `shortName` | string | 35 | Display name |
| `nativeMaterialValue` | int | 36 | Material tier (0-9 weapon, 0x0000-0x0209 armor) |
| `dyeColor` | DyeColors | 37 | Tint |
| `weightInKg` | float | 38 | |
| `drawOrder` | int | 39 | Paper doll Z-order |
| `value` | int | 40 | Base gold value |
| `unknown` | ushort | 41 | Legacy field |
| `flags` | ushort | 42 | Bit 0x20 identified, 0x800 artifact |
| `currentCondition` | int | 43 | Durability hits |
| `maxCondition` | int | 44 | Max durability |
| `unknown2` | byte | 45 | Legacy |
| `typeDependentData` | byte | 46 | Stack count for arrows / recipe key for potions |
| `enchantmentPoints` | int | 47 | Enchantment budget (from template) |
| `message` | int | 48 | Book ID / painting ID / quest msg |
| `legacyMagic` | DaggerfallEnchantment[] | 49 | Classic (type, param) pairs, max 10 |
| `customMagic` | CustomEnchantment[] | 50 | Modern effect-key pairs |
| `stackCount` | int | 51 | Quantity |
| `poisonType` | Poisons | 52 | Weapon poison |
| `timeHealthLeechLastUsed` | uint | 53 | Vampire strike cooldown |
| `timeEffectsLastRerolled` | uint | 54 | Enchantment system |

Non-serialized cache (57-90): `playerTexture*`, `worldTexture*`, `itemGroup`, `artifactIndexBitfield`, `groupIndex`, `currentVariant`, `uid`, `trappedSoulType`, `potionRecipeKey`, `timeForItemToDisappear`, `isQuestItem`, `questUID`, `questItemSymbol`, `cachedItemTemplate`, `equipSlot`, `repairData`.

Key methods:
- `SetItem(group, index)` (538-573) — reset from template, keep UID
- `SetArtifact(group, index)` (580-639) — merge artifact template with base, set identified
- `LowerCondition(amount, owner, collection)` (**1170-1181**) — **single point of durability mutation**; breaks item and auto-unequips at 0
- `SetEnchantments(settings[], owner)` (1271-1342) — cap 10 total, unequips
- `Clone()` (526-529) — new UID, copy instance properties
- `GetSaveData()` (767-818) — build ItemData_v1
- `GetWeaponMaterialModifier()` (979-1005) — Iron-1..Daedric+6
- `GetMaterialArmorValue()` (1007-1059) — Leather 3..Daedric 21 (halved if artifact)
- `GetShieldArmorValue()` (1061-1077) — Buckler 1, Round 2, Kite 3, Tower 4
- `GetShieldProtectedBodyParts()` (1082-1097) — body-part coverage per shield type
- `IsStackable()` (681-695) — false if summoned/equipped/questItem/enchanted

### 3.2 `ItemCollection` — single container class for all inventories
**Path:** `Assets/Scripts/Game/Items/ItemCollection.cs`

`OrderedDictionary<ulong uid, DaggerfallUnityItem>` at line 30. Serves inventory, wagon, loot pile, merchant stock, container — everything.

Public API (20 methods, 157-590):
- Add/Remove: `AddItem`, `RemoveItem`, `RemoveOne`, `SplitStack`, `ReorderItem`, `Clear`
- Lookup: `Contains(item|uid|questItem|group+index)`, `GetItem(uid|index|group+index+filter)`
- Bulk: `TransferAll`, `CopyAll`, `ReplaceAll`, `Transfer`, `Import`, `AddItems`
- Export: `CloneAll` (new UIDs), `Export` (keep UIDs + clear), `ExportQuestItems`
- Serialize: `SerializeItems` → `ItemData_v1[]`, `DeserializeItems`
- Query: `Count`, `GetNumItems` (total incl. stacks), `GetWeight`, `GetCreditAmount`, `RemoveExpiredItems`

Auto-stacking: `AddItem()` calls `FindExistingStack()` on stackable items and merges `stackCount`.

### 3.3 `ItemTemplate`
**Path:** `Assets/Scripts/API/ItemsFile.cs:46-70`

Struct with: `index`, `name`, `baseWeight`, `hitPoints`, `capacityOrTarget`, `basePrice`, `enchantmentPoints`, `rarity`, `variants`, `drawOrderOrEffect`, `isBluntWeapon`, `isLiquid`, `isOneHanded`, `isIngredient`, `worldTexture*`, `playerTexture*`, `isNotRepairable`, `hasNoEncumbrance`.

### 3.4 `ItemEquipTable` — 27 equipment slots
**Path:** `Assets/Scripts/Game/Items/ItemEquipTable.cs`

`DaggerfallUnityItem[27] equipTable`. `EquipSlots` enum (ItemEnums.cs:105-135):
```
None = -1
Amulet0/1 = 0/1, Bracelet0/1 = 2/3, Ring0/1 = 4/5, Bracer0/1 = 6/7,
Mark0/1 = 8/9, Crystal0/1 = 10/11
Head = 12, RightArm = 13, Cloak1 = 14, LeftArm = 15, Cloak2 = 16
ChestClothes = 17, ChestArmor = 18, RightHand = 19, Gloves = 20, LeftHand = 21
Unknown1 = 22, LegsArmor = 23, LegsClothes = 24, Unknown2 = 25, Feet = 26
```

`EquipItem(item, alwaysEquip, playSounds)` (94-154) handles conflict logic: 2H weapon unequips both hands; shield unequips right-hand 2H. Returns list of items that got unequipped.

### 3.5 Serialization DTO `ItemData_v1`
**Path:** `Assets/Scripts/Game/Serialization/SerializableGameObject.cs:267-303`

Bit-packed for classic compatibility:
- `value2 = (unknown & 0xffff) | (flags << 16)`
- `hits3 = (unknown2 & 0xff) | (typeDependentData << 8)`
- `legacyMagic` serialized as `int[]` of `[type0, param0, type1, param1, …]` pairs
- `customMagic` serialized as array of `CustomEnchantment { EffectKey, CustomParam }`

---

## Section 4 — DFU FormulaHelper catalog (abridged)

**Path:** `Assets/Scripts/Game/Formulas/FormulaHelper.cs` (3180 lines, ~103 public statics)

Ember's DM Tier 1-3 API should wrap these. Every method has a `TryGetOverride(name)` mod-hook at its top; Ember decides whether to keep the hook pattern.

| Category | Methods | Lines | Deterministic | DM Tier |
|----------|---------|-------|---------------|---------|
| **Attribute modifiers** | `DamageModifier`, `MaxEncumbrance`, `SpellPoints`, `MagicResist`, `ToHitModifier`, `HitPointsModifier`, `HealingRateModifier`, `MaxStatValue`, `BonusPool` | 66-152 | yes (except BonusPool: **RNG**) | 1 |
| **Skill checks** | `CalculateInteriorLockpickingChance`, `CalculateExteriorLockpickingChance`, `CalculatePickpocketingChance`, `CalculateShopliftingChance`, `CalculateStealthChance`, `CalculateClimbingChance` | 232-316 | yes (return chance, caller rolls) | 2 |
| **Combat hit** | `CalculateSuccessfulHit`, `CalculateStatsToHit`, `CalculateSkillsToHit`, `CalculateAdjustmentsToHit`, `CalculateArmorToHit`, `CalculateWeaponToHit`, `CalculateAdrenalineRushToHit` | 796-1251 | partial (Dice100 inside) | 2/3 |
| **Damage** | `CalculateHandToHandMinDamage/MaxDamage`, `CalculateWeaponMinDamage/MaxDamage`, `CalculateAttackDamage`, `CalculateWeaponAttackDamage`, `CalculateHandToHandAttackDamage`, `CalculateBackstabChance`, `CalculateBackstabDamage` | 434-991 | no (Random.Range) | 3 |
| **Defense** | `CalculateProficiencyModifiers`, `CalculateRacialModifiers`, `DamageEquipment`, `ApplyConditionDamageThroughPhysicalHit`, `GetBonusOrPenaltyByEnemyType` | 908-1138 | yes/RNG mix | 2/3 |
| **Magic** | `SavingThrow` (x2), `GetEffectFlags`, `GetElementType`, `GetResistanceModifier`, `ModifyEffectAmount`, `InflictPoison`, `InflictDisease`, `OnMonsterHit` | 1371-1760 | partial | 2/3 |
| **Spell cost** | `CalculateTotalEffectCosts`, `CalculateEffectCosts` (x2), `CalculateCasterLevel`, `ApplyTargetCostMultiplier`, `GetEffectComponentCosts` | 2178-2400+ | yes | 2 |
| **Social/economy** | `CalculateEnemyPacification`, `CalculateTempleBlessing`, `CalculateTradePrice`, `CalculateCost`, `CalculateItemRepairCost/Time`, `CalculateItemIdentifyCost`, `CalculateRoomCost`, `CalculateDaedraSummoningCost/Chance`, `ApplyRegionalPriceAdjustment`, `UpdateRegionalPrices`, `CalculateMaxBankLoan`, `CalculateBankLoanRepayment` | 357-2090 | partial | 2/3 |
| **Leveling** | `CalculatePlayerLevel`, `CalculateSkillUsesForAdvancement`, `CalculateHitPointsPerLevelUp`, `RollMaxHealth`, `RollEnemyClassMaxHealth` | 160-354 | partial (DFRandom) | 3 |
| **Recovery** | `CalculateHealthRecoveryRate`, `CalculateFatigueRecoveryRate`, `CalculateSpellPointRecoveryRate` | 178-229 | yes | 1 |
| **Vampire/were** | `GetVampireClan`, `specialInfectionChance` | 400-428 | no | 3 |
| **Spawn rolls** | `RollRandomSpawn_*` (x4) | 1765-1813 | no | 3 |
| **Weapon/armor** | `IsItemStackable`, `RandomMaterial`, `RandomArmorMaterial` | 2096-2172 | partial | 3 |
| **Misc** | `GetHolidayId`, `CalculateStruckBodyPart`, `GetMeleeWeaponAnimTime`, `GetBowCooldownTime`, `FatigueDamage`, `CalculateCriticalStrike*` | 863-1850 | yes | 1/2 |

**Note on `TryGetOverride`:** Every method starts with 3-5 lines of `Func<...> del; if (TryGetOverride("Name", out del)) return del(args);`. Decide upfront: port the hook (mod extensibility) or skip it (simpler). Most Ember systems don't need modding until v2+ — skip for now.

---

## Section 5 — RNG inventory (determinism budget)

**All uncontrolled RNG sites across DFU + OpenMW that Ember must replace/seed.**

### 5.1 DFU uncontrolled RNG

| File:Line | Call | Purpose | Ember action |
|-----------|------|---------|--------------|
| `DaggerfallSkills.cs:95` | `UnityEngine.Random.Range(3, 7)` | Initial skill values | Route through seeded RNG, seed by character name hash |
| `FormulaHelper.cs:151-152` | `UnityEngine.Random.InitState(Time.frameCount); Random.Range(…)` | BonusPool roll | **PARTICULARLY BAD** — reseeds global per call. DO NOT PORT. Rewrite. |
| `PlayerEntity.cs:332` | `Random.Range(0, 2)` | Argonian breath bonus | Seeded |
| `PlayerEntity.cs:412` | `Dice100.FailedRoll(skill)` | Swimming check | Seeded |
| `PlayerEntity.cs:500,507` | `Dice100.SuccessRoll(5|10)` | Crime guard spawn | Seeded |
| `PlayerEntity.cs:635` | `Random.Range(2, 6)` | Guard count | Seeded |
| `PlayerEntity.cs:676` | `Random.Range(0, 4) == 0` | Guard spawn chance | Seeded |
| `PlayerEntity.cs:687,739` | `Random.Range(2,6) / (5,11)` | Guard reinforcements | Seeded |
| `PlayerEntity.cs:1669,1698,1718,1760,1808,1927,1955,1966,1983,1997,2009,2032,2048` | `Dice100.*Roll` | Faction power updates | Seeded |
| `PlayerEntity.cs:1742,1875` | `Random.Range(0,count)` | Random faction selection | Seeded |
| `PlayerEntity.cs:1824-1825` | `Random.Range(1, power/10+1)` | Faction power delta | Seeded |
| `PlayerEntity.cs:2155` | `Random.Range(min,max+1)` | Severe punishment | Seeded |
| `PlayerEntity.cs:2326` | `DFRandom.rand() & 1` | Voluntary surrender | Seeded |
| `EnemyEntity.cs:263,285` | `Random.Range(minHP, maxHP+1)` | Enemy HP roll | Seeded (important for deterministic encounters) |
| `EnemyEntity.cs:301` | `Random.Range(3,7)` | City guard level | Seeded |
| `EnemyEntity.cs:347` | `Random.Range(0,2)` | Equipment variant | Seeded |

Plus many RNG calls inside `FormulaHelper.Calculate*Damage` methods and effect scripts. Audit as you port.

### 5.2 OpenMW RNG (already centralized)

All combat RNG uses `world->getPrng() : Misc::Rng&`. Ember mirrors this pattern — one global seeded RNG service.

`mwmechanics/combat.cpp` sites:
- L139: `Misc::Rng::roll0to99(prng)` — block chance
- L244: `roll0to99` — hit chance
- L301: `rollProbability` — arrow pickup (fProjectileThrownStoreChance)
- L378: `roll0to99` — elemental shield reflection save
- L522: `rollDice(sounds.size())` — impact sound
- L702,704: `deviate` — hit position offsets

---

## Section 6 — Serialization DTOs

### 6.1 `PlayerEntityData_v1`
**Path:** `Assets/Scripts/Game/Serialization/SerializableGameObject.cs:155`

Full key set: `gender`, `faceIndex`, `raceTemplate`, `careerTemplate`, `reflexes`, `name`, `level`, `stats`, `skills`, `resistances`, `maxHealth/currentHealth/currentFatigue/currentMagicka/currentBreath`, `skillUses`, `skillsRecentlyRaised`, `equipTable`, `items`, `wagonItems`, `otherItems`, `goldPieces`, `globalVars`, `minMetalToHit`, `biographyMod*` (6 fields), `timeForThievesGuildLetter`, `timeForDarkBrotherhoodLetter`, `thievesGuildRequirementTally`, `darkBrotherhoodRequirementTally`, `timeToBecomeVampireOrWerebeast`, `lastTimePlayerAteOrDrankAtTavern`, `timeOfLastSkillTraining`, `regionData`, `rentedRooms`, `spellbook`, `instancedEffectBundles`, `crimeCommitted`, `haveShownSurrenderToGuardsDialogue`, `lightSourceUID`, `reputationCommoners/Merchants/Scholars/Nobility/Underworld/SGroup5..10`, `previousVampireClan`, `daedraSummonDay`, `daedraSummonIndex`, `anchorPosition`.

### 6.2 `EnemyData_v1`
**Path:** `Assets/Scripts/Game/Serialization/SerializableGameObject.cs:358`

`loadID`, `gameObjectName`, `currentPosition`, `localPosition`, `currentRotation`, `worldContext`, `worldCompensation`, `isDead`, `startingHealth`, `currentHealth/Fatigue/Magicka`, `entityType`, `careerName`, `careerIndex`, `isHostile`, `hasEncounteredPlayer`, `questSpawn`, `mobileGender`, `items`, `equipTable`, `instancedEffectBundles`, `alliedToPlayer`, `questFoeSpellQueueIndex`, `questFoeItemQueueIndex`, `wabbajackActive`, `team`, `specialTransformationCompleted`, `questResource`.

### 6.3 OpenMW `ESM::CreatureStats` / `ESM::NpcStats`
**Path:** `components/esm3/creaturestats.hpp`, `components/esm3/npcstats.hpp`

Full field list in ARCHITECTURE.md §4.2. `readState()`/`writeState()` round-trip.

---

## Section 7 — Pitfalls found during personal verification

1. **DFU `FormulaHelper.BonusPool` reseeds Unity global RNG per call with `Time.frameCount`** (line 151). This is a genuine bug in DFU. Do NOT port it — use a seeded PCG.
2. **Many FormulaHelper methods have `TryGetOverride` as their first 3 lines** — a mod hook. Ember should decide now whether to keep the hook pattern or drop it.
3. **OpenMW's `DynamicStat<float>`** holds `(base, current, modified)` as three fields. Fortify/Drain effects modify `modified` without touching `base`, so removing the effect is trivial. DFU uses parallel `int[] mods` arrays instead. The OMW pattern is cleaner — port that.
4. **`SafePtr` (OpenMW ptr.hpp:165-194)** — holds `Ptr + FormId`. Re-resolves the Ptr through the world model if cells unload. Essential for Ember's large-region world where cells unload. Agents didn't call this out.
5. **`DaggerfallSkills.SetDefaults` uses `UnityEngine.Random.Range(3,7)` on line 95** — the ONE RNG in the skill container. Replace with seeded RNG from character-creation context.
6. **DFU stat range is 0-100 (base 50), NOT 1-18.** `FormulaHelper.MaxStatValue = 100` (line 137). Any implementation that uses `(score - 10) / 2` modifier formula is using D&D paradigm, not DFU. Ember bible §1 commits to DFU paradigm.

---

## Section 8 — Source file index (paste into implementer's IDE)

When implementing system X, open this subset of files:

**Actor core** (DFU + OMW)
```
dfu-src/.../Assets/Scripts/Game/Entities/DaggerfallEntity.cs
dfu-src/.../Assets/Scripts/Game/Entities/PlayerEntity.cs
dfu-src/.../Assets/Scripts/Game/Entities/EnemyEntity.cs
dfu-src/.../Assets/Scripts/Game/Entities/DaggerfallStats.cs
dfu-src/.../Assets/Scripts/Game/Entities/DaggerfallSkills.cs
dfu-src/.../Assets/Scripts/Game/Entities/DaggerfallResistances.cs
dfu-src/.../Assets/Scripts/Game/MagicAndEffects/EntityEffectManager.cs
openmw-src/.../apps/openmw/mwworld/ptr.hpp
openmw-src/.../apps/openmw/mwworld/class.hpp
openmw-src/.../apps/openmw/mwclass/npc.cpp
openmw-src/.../apps/openmw/mwclass/creature.cpp
openmw-src/.../apps/openmw/mwmechanics/creaturestats.{hpp,cpp}
openmw-src/.../apps/openmw/mwmechanics/npcstats.{hpp,cpp}
openmw-src/.../apps/openmw/mwmechanics/spells.hpp
openmw-src/.../apps/openmw/mwmechanics/magiceffects.hpp
openmw-src/.../apps/openmw/mwmechanics/activespells.hpp
```

**Item core** (DFU only)
```
dfu-src/.../Assets/Scripts/Game/Items/DaggerfallUnityItem.cs
dfu-src/.../Assets/Scripts/Game/Items/ItemCollection.cs
dfu-src/.../Assets/Scripts/Game/Items/ItemTemplate.cs
dfu-src/.../Assets/Scripts/Game/Items/ItemHelper.cs
dfu-src/.../Assets/Scripts/Game/Items/ItemEquipTable.cs
dfu-src/.../Assets/Scripts/Game/Items/ItemEnums.cs
```

**Formulas / DM surface** (DFU primary, OMW secondary)
```
dfu-src/.../Assets/Scripts/Game/Formulas/FormulaHelper.cs
openmw-src/.../apps/openmw/mwbase/mechanicsmanager.hpp
openmw-src/.../apps/openmw/mwmechanics/mechanicsmanagerimp.cpp
openmw-src/.../apps/openmw/mwmechanics/combat.{hpp,cpp}
openmw-src/.../apps/openmw/mwmechanics/spellcasting.cpp
openmw-src/.../apps/openmw/mwmechanics/pickpocket.cpp
```

**Serialization reference**
```
dfu-src/.../Assets/Scripts/Game/Serialization/SerializableGameObject.cs
dfu-src/.../Assets/Scripts/Game/Serialization/SerializablePlayer.cs
dfu-src/.../Assets/Scripts/Game/Serialization/SerializableEnemy.cs
openmw-src/.../components/esm3/creaturestats.hpp
openmw-src/.../components/esm3/npcstats.hpp
```

**NPC AI reference** (OMW, for behavior packages)
```
openmw-src/.../apps/openmw/mwmechanics/aisequence.{hpp,cpp}
openmw-src/.../apps/openmw/mwmechanics/aipackage.{hpp,cpp}
openmw-src/.../apps/openmw/mwmechanics/aicombat.cpp
openmw-src/.../apps/openmw/mwmechanics/aiwander.cpp
openmw-src/.../apps/openmw/mwmechanics/aifollow.cpp
openmw-src/.../apps/openmw/mwmechanics/aiescort.cpp
openmw-src/.../apps/openmw/mwmechanics/aitravel.cpp
```
