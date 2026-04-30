# Ember CRPG — Master Mechanics Bible
## 4-Engine Cross-Reference: DFU + OpenMW + GemRB + Dwarf Fortress

**Date:** 2026-04-18
**Purpose:** Her sistemi, her mekanik, her formulu 4 legendary engine'den cikarip tek referans dokumana toplamak. Unity C# rewrite icin blueprint.

> **Audit status:** See `BIBLE_AUDIT.md` (2026-04-18) for source-verified corrections, confidence scores, and missed mechanics. Sections below marked ⚠️ have been corrected post-audit; sections marked 🔍 have known gaps documented in the audit.
>
> **Architectural lock-ins (2026-04-19):** See `ARCHITECTURE.md` for the Actor + Item blueprint, DM query API, and NPC memory design. The following decisions are now committed:
> - **Real-time with pause** (matches DFU/OpenMW). Turn-based combat is OUT.
> - **Primary engines: DFU + OpenMW.** GemRB and DF are reference-only for specific subsystems (containers/pathfinding/morale from GemRB; body tissues/materials/syndromes/personality from DF).
> - **Unified `Actor` + `Item` primitives.** Everything in the world is one of those two. See Architecture Part 1–2.
> - **Deterministic world.** One seeded `IRng` service; no uncontrolled `UnityEngine.Random` calls.
> - **LLM "DM" narrator** talks to the engine via typed query/roll APIs (Architecture Part 3). The DM never invents mechanics; it asks, the engine answers, the DM narrates the answer.
> - **Persistent NPC memory** (Ember extension; neither engine has it). See Architecture §1.4.
>
> Sections that conflict with these lock-ins (GemRB D&D 2E paradigm, turn-based combat, dual/multi-class, save-vs-spell categories, DF world-gen) are kept as reference but not implemented in v1.

---

## INDEX

1. [Stats & Attributes](#1-stats--attributes)
2. [Skills](#2-skills)
3. [Health / Vitality / Resources](#3-health--vitality--resources)
4. [Character Creation](#4-character-creation)
5. [Races & Species](#5-races--species)
6. [Classes & Careers](#6-classes--careers)
7. [Level Progression](#7-level-progression)
8. [Combat — Attack Resolution](#8-combat--attack-resolution)
9. [Combat — Damage Calculation](#9-combat--damage-calculation)
10. [Combat — Defense & Armor](#10-combat--defense--armor)
11. [Combat — Death & Knockdown](#11-combat--death--knockdown)
12. [Weapons](#12-weapons)
13. [Armor & Equipment](#13-armor--equipment)
14. [Magic — Spell System](#14-magic--spell-system)
15. [Magic — Effects & Opcodes](#15-magic--effects--opcodes)
16. [Magic — Enchanting](#16-magic--enchanting)
17. [Alchemy & Potions](#17-alchemy--potions)
18. [Inventory & Items](#18-inventory--items)
19. [Body & Wounds (DF-style)](#19-body--wounds-df-style)
20. [Materials System (DF-style)](#20-materials-system-df-style)
21. [Movement & Physics](#21-movement--physics)
22. [Pathfinding & Navigation](#22-pathfinding--navigation)
23. [Stealth & Concealment](#23-stealth--concealment)
24. [Lockpicking & Security](#24-lockpicking--security)
25. [NPC AI — Behavior Packages](#25-npc-ai--behavior-packages)
26. [NPC Schedules & Routines](#26-npc-schedules--routines)
27. [Dialog System](#27-dialog-system)
28. [Faction & Reputation](#28-faction--reputation)
29. [Morale System](#29-morale-system)
30. [Quest System](#30-quest-system)
31. [Economy — Merchants & Trading](#31-economy--merchants--trading)
32. [Economy — Banking](#32-economy--banking)
33. [Crime & Punishment](#33-crime--punishment)
34. [Disease & Poison](#34-disease--poison)
35. [Vampirism & Lycanthropy](#35-vampirism--lycanthropy)
36. [Syndromes & Curses (DF-style)](#36-syndromes--curses-df-style)
37. [Weather System](#37-weather-system)
38. [Time & Calendar](#38-time--calendar)
39. [World Generation](#39-world-generation)
40. [Dungeon Generation](#40-dungeon-generation)
41. [Door / Lock / Trap System](#41-door--lock--trap-system)
42. [Container System](#42-container-system)
43. [Rest & Recovery](#43-rest--recovery)
44. [Fast Travel](#44-fast-travel)
45. [Crafting & Reactions (DF-style)](#45-crafting--reactions-df-style)
46. [Plants & Agriculture (DF-style)](#46-plants--agriculture-df-style)
47. [Personality & Emotions (DF-style)](#47-personality--emotions-df-style)
48. [Save / Load](#48-save--load)
49. [Turn Undead](#49-turn-undead)
50. [Detect Effects](#50-detect-effects)
51. [Mark & Recall / Teleport](#51-mark--recall--teleport)
52. [Encumbrance](#52-encumbrance)
53. [NPC Civilization & Entities (DF-style)](#53-npc-civilization--entities-df-style)

---

## 1. Stats & Attributes

### DFU (8 stats, base 50, max 100)
| Stat | Combat Use | Formula |
|------|-----------|---------|
| STR | DamageModifier | (STR - 50) / 5 |
| INT | SpellPoints | INT * multiplier |
| WIL | MagicResist | WIL / 10 |
| AGI | ToHitModifier | (AGI / 10) - 5 |
| END | HitPointsModifier | (END / 10) - 5 |
| PER | NPC Reaction | Personality checks |
| SPD | Movement speed | 1 + (SPD - 50) / 100 |
| LCK | General luck | Lerp(0, 10, LCK * 0.01) |

### GemRB/BG (6 stats, 3-18+, D&D rules)
| Stat | Key Use |
|------|---------|
| STR | Melee to-hit, damage, carry weight. STR 18 has sub-range 01-00 |
| DEX | AC bonus, ranged to-hit, thief skills |
| CON | HP per level (warrior gets higher column) |
| INT | Mage spell slots, lore |
| WIS | Cleric spell slots, will saves |
| CHA | Store prices, NPC reactions |

### OpenMW/Morrowind (8 stats + 3 derived)
- STR, INT, WIL, AGI, SPD, END, PER, LCK
- Derived: Health = (STR+END)/2 * level, Magicka = INT * multiplier, Fatigue = STR+WIL+AGI+END

### DF (6 physical + 12 mental)
**Physical:** STRENGTH, AGILITY, TOUGHNESS, ENDURANCE, RECUPERATION, DISEASE_RESISTANCE
**Mental:** ANALYTICAL_ABILITY, FOCUS, WILLPOWER, CREATIVITY, INTUITION, PATIENCE, MEMORY, LINGUISTIC_ABILITY, SPATIAL_SENSE, MUSICALITY, KINESTHETIC_SENSE, EMPATHY, SOCIAL_AWARENESS

### EMBER RECOMMENDATION
Use **6 stats** (MIG/AGI/END/MND/INS/PRE as in current Python backend) with DFU-style formula mapping. Add DF-style personality traits as a separate layer (not core stats).

---

## 2. Skills

### DFU (34 skills in 5 categories)
**Combat (7):** ShortBlade, LongBlade, HandToHand, Axe, BluntWeapon, Archery, CriticalStrike
**Stealth (4):** Lockpicking, Pickpocket, Stealth, Backstabbing
**Magic (6):** Destruction, Restoration, Illusion, Alteration, Thaumaturgy, Mysticism
**Languages (9):** Orcish, Harpy, Giantish, Dragonish, etc.
**Utility (8):** Medical, Etiquette, Streetwise, Jumping, Mercantile, Swimming, Climbing, Dodging, Running

**Advancement formula:** UsesNeeded = (SkillValue * AdvMultiplier * CareerMultiplier * 1.04^Level * 2/5) + 1

### GemRB/BG (D&D proficiency system)
- Weapon proficiencies (0-5 stars): 0 = -4 to hit, 5 = +3 to hit + extra attacks
- Thief skills: Open Locks, Find Traps, Pick Pockets, Move Silently, Hide in Shadows, Detect Illusion, Set Traps
- Non-weapon proficiencies from 2E rules

### DF (150+ skills)
- Attack-bound: PUNCH, KICK, BITE, AXE, SWORD, HAMMER, etc.
- Craft-bound: SMELT, CARPENTER, MASON, COOK, BREW, etc.
- Skill improves with USE (implicit XP)
- Rust mechanic: unused skills degrade over time

### EMBER RECOMMENDATION
Start with **20 core skills** covering combat (6), magic (4), stealth (4), social (3), utility (3). Use DFU's "advance with use" model. Add DF-style rust later.

---

## 3. Health / Vitality / Resources

### DFU
- **Health:** MaxHP = 25 + Career.HPPerLevel + sum(per-level rolls + END modifier). Min gain = 1/level
- **Fatigue:** MaxFatigue = STR + END + AGI + WIL. Depletes on run/jump/attack. Recovery = MaxFatigue/8 per hour
- **Magicka:** MaxMagicka = INT * multiplier. Recovery = MaxMagicka/8 per hour
- **Health Recovery:** max((END_mod + Medical*MaxHP/1000), 1) per hour

### GemRB/BG
- **HP:** d10 (fighter), d8 (cleric), d6 (thief), d4 (mage) per level + CON modifier
- **Spell slots:** Memorization system (prepared spells per level)
- No fatigue/mana bar (classic D&D)

### EMBER RECOMMENDATION
Use DFU model: HP + Fatigue + Mana. All recover over time with different rates. Fatigue affects combat effectiveness (speed, damage reduced at low fatigue).

---

## 4. Character Creation

### DFU
1. Choose race (15 options)
2. Choose class (predefined) or create custom class
3. Custom class: pick 3 primary + 3 major + 6 minor skills
4. Set weapon/armor proficiencies, resistances, special abilities
5. Roll stats (reroll allowed)
6. Background questions affect starting bonuses

### GemRB/BG
1. Race → Class → Alignment → Abilities (roll 6x d6-drop-lowest) → Skills → Proficiencies → Spells → Appearance → Name
2. Dual-class and multi-class options (BG-unique)

### EMBER (current Python backend)
1. Name → Discipline (Ash Scout/Court Emissary/Hunter/Ember Adept)
2. World Questions (5 questions shape the universe)
3. World History
4. Stat Roll (pool distribution: MIG/AGI/END/MND/INS/PRE)
5. Class & Skills
6. Review & Begin

---

## 5. Races & Species

### DFU (15 races with stat modifiers)
| Race | Key Bonuses |
|------|------------|
| Breton | INT+1, WIL+1, 25% Magic Resist |
| Nord | STR+1, END+2, 50% Frost Resist |
| Redguard | STR+1, Poison Resist 75% |
| Dark Elf | STR+1, INT+1 |
| Wood Elf | AGI+1, SPD+2, Archery+30 |
| High Elf | INT+2, AGI+1 |
| Khajiit | AGI+2, SPD+1, Climbing+30 |
| Orc | STR+2, END+1, PER-2 |

### GemRB/BG (D&D 2E races)
- Human (no limits), Elf (+1 DEX -1 CON), Dwarf (+1 CON -1 CHA), Halfling (+1 DEX -1 STR), Half-Elf, Half-Orc, Gnome

---

## 6. Classes & Careers

### DFU Career fields
- HitPointsPerLevel (1-30)
- PrimarySkills[3], MajorSkills[3], MinorSkills[6]
- ExpertProficiencies, Resistances, Immunities
- RapidHealing: Always/InLight/InDarkness/Never
- NoRegenSpellPoints flag
- Reflexes: 0-3 combat speed

### GemRB/BG (D&D classes)
Fighter, Ranger, Paladin, Cleric, Druid, Mage, Sorcerer, Thief, Bard, Monk, Barbarian
+ Kits (specializations) + Dual-class + Multi-class

---

## 7. Level Progression ⚠️

### DFU
- **Level formula:** floor((CurrentSkillSum - StartingSkillSum + 28) / 15)
- **Per level:** random(Career.HPPerLevel/2, Career.HPPerLevel) + END modifier + bonus pool of random(4,6) points

### OpenMW/Morrowind — Skill → Attribute Multiplier (MISSING FROM BIBLE V1)
**Source:** `apps/openmw/mwmechanics/npcstats.cpp:268-280`, `getLevelupAttributeMultiplier()`

This is Morrowind's signature progression mechanic — without it, level-up is just a random stat roll.

```
On skill increase, increment the governing attribute's skill-up counter.
On rest/level-up, for each attribute:
   n = clamp(counter, 1, 10)
   statGain = iLevelUp01Mult ... iLevelUp10Mult table[n]   // ascending x1..x5
   (counter reset)
```
- Each skill maps to 1 governing attribute (e.g. Long Blade → STR, Athletics → SPD).
- Leveling 10+ of a skill's governing skills in one level → x5 attribute gain on next level-up.
- Lazy level-up (e.g. only Sneak going up) → x1 gains, you fall behind.

**Ember recommendation:** port this. It turns skill choice into attribute strategy and makes every pre-rest decision matter.

### GemRB/BG
- XP thresholds per class (Fighter: 2000/4000/8000/16000...)
- HP die per class + CON modifier
- Proficiency points every 3-4 levels
- Mage/Priest gain spell slots per level

---

## 8. Combat — Attack Resolution

### DFU (percentage-based)
**Hit Chance = Weapon_skill + swing_mods + proficiency + racial + backstab + weapon_toHit - armor_value - target_dodge + stat_differential + luck**
- Clamped to 3-97%
- Roll d100 < chance = hit

### GemRB/BG (D&D THAC0)
**Roll = d20 + THAC0_bonus + proficiency + ability_mod + racial_bonus + backstab**
- Hit if Roll >= Target AC
- Natural 20 always hits, natural 1 always misses
- Backstab: 2x-5x damage multiplier (thief level dependent)

### OpenMW/Morrowind
- Weapon skill percentage check
- Fatigue affects hit chance
- Luck factor

### DF (penetration physics)
- Attack contact area vs tissue resistance
- Penetration depth determines tissue layer damage
- Material shear/impact yield vs strike force
- Multi-attack rounds possible

### EMBER RECOMMENDATION
Use DFU percentage-based system (simpler than THAC0, more intuitive). Add DF-style body part targeting as an advanced option.

---

## 9. Combat — Damage Calculation

### DFU
```
Weapon: random(WeaponMin, WeaponMax) + STR_mod + material_mod + proficiency_bonus
Unarmed: random(H2H_skill/10+1, H2H_skill/5+1) + modifiers
Backstab: 3x multiplier
Skeleton: 0.5x from non-edged, 2x from silver
```

### GemRB/BG
```
Weapon: DiceThrown * random(1, DiceSides) + DamageBonus + STR_mod + proficiency_bonus
Average: DiceThrown * (DiceSides + 1) / 2 + DamageBonus
Damage types: Crushing(0), Acid(1), Cold(2), Lightning(3), Fire(4), Magic(7)
```

### DFU weapon damage table
| Weapon | Min-Max |
|--------|---------|
| Dagger | 1-6 |
| Shortsword | 1-8 |
| Longsword | 2-16 |
| Claymore | 2-18 |
| Dai-Katana | 3-21 |

---

## 10. Combat — Defense & Armor

### DFU
- Armor per body part reduces hit chance
- 7 body parts: Head, RightArm, LeftArm, Chest, Hands, Legs, Feet
- Struck body part weighted: [0,0,1,1,1,2,2,2,3,3,3,3,4,4,4,4,5,5,5,6]
- 13 armor materials: Leather → Daedric (ascending quality)

### GemRB/BG
- AC = Base + DEX_mod + Armor + Shield + damage_type_mods + style_bonus
- Damage type modifiers: Crushing, Piercing, Slashing, Missile (separate AC for each)
- Invisible attacker: defender loses DEX bonus
- Shield style: +AC vs missiles

---

## 11. Combat — Death & Knockdown

### OpenMW
- mDead, mDied (one-shot trigger), mMurdered (crime flag)
- mDeathAnimation index, mTimeOfDeath
- Knockdown: mKnockdown (duration), mKnockedDown, mHitRecovery, mBlock

### DFU
- HP <= 0 = death
- Death animation plays
- Loot drops from corpse

---

## 12. Weapons

### DFU (27 equipment slots)
Head, RightArm, LeftArm, ChestClothes, ChestArmor, RightHand, LeftHand, Gloves, LegsArmor, LegsClothes, Feet, Cloaks(2), Amulets(2), Bracelets(2), Rings(2), Bracers(2), Marks(2), Crystals(2)

### GemRB/BG
- Melee attack speed based on weapon speed factor
- Attacks per round: 1 base, +0.5 per proficiency level, max 4 (doubled in storage)

### DF
- Multi-attack type: PUNCH/KICK/SCRATCH/BITE
- Contact area (10-100000) determines force concentration
- Penetration (200-12000) determines tissue depth
- Velocity multiplier (1000-2000) affects impact force
- Preparation/Recovery ticks: 3-4 per attack cycle

---

## 13. Armor & Equipment

### DFU materials (ascending quality)
Leather → Chain → Iron → Steel → Silver → Elven → Dwarven → Mithril → Adamantium → Ebony → Orcish → Daedric

### DF material physics
| Material | Impact Yield (KPa) | Shear Yield (KPa) | Max Edge | Density (kg/m3) |
|----------|-------------------|-------------------|----------|-----------------|
| Wood | 10000 | 40000 | 0 | 500 |
| Copper | 175000 | 50000 | 10000 | 8930 |
| Iron | 542500 | 155000 | 10000 | 7850 |
| Steel | 1505000 | 430000 | 10000 | 7850 |
| Gold | — | — | 0 | 19320 |

---

## 14. Magic — Spell System ⚠️

### DFU (6 schools, 164 effects)
**Schools:** Destruction, Restoration, Illusion, Alteration, Thaumaturgy, Mysticism
**Casting cost (corrected — `FormulaHelper.cs:2261-2370`):**
```
costPerEffect = magnitudeCost(effect, magMin, magMax)
              + durationCost(effect, duration)
              + chanceCost(effect, chanceBase, chancePlus)
totalCost     = Σ(costPerEffect) * targetMultiplier
targetMultiplier: 1.0 caster-only/touch | 1.5 single-target | 2.0 area-caster | 2.5 area-at-range
```
Each effect has its own per-component coefficients — there is no single product formula.
**Max castable:** Cost <= current Spell Points (no hard INT*2 cap in DFU code — that's a design heuristic for UI, not a validation).

### GemRB/BG (Vancian magic)
- Memorize spells per rest
- Spell levels 1-9
- Spell slots per level based on class + INT/WIS
- Casting time in rounds
- Spell failure chance in armor

### OpenMW/Morrowind
**Spell types:** Spell (castable), Power (1/day), Ability (passive), Constant Effect (enchantment)
**Casting:** Check mana >= cost, calculate success chance (INT + skill), apply effects

---

## 15. Magic — Effects & Opcodes

### GemRB effect system
- Opcode-based: each effect has a numeric ID
- Parameters: DiceThrown, DiceSides, DiceBonus, Duration, TimingMode
- Probability range: ProbabilityRangeMin to ProbabilityRangeMax
- Save for half: flag on each effect
- Resistance check: Willpower/Intelligence based
- Effect flags: DICED, NO_LEVEL_CHECK, REINIT_ON_LOAD

### DFU (164 effect scripts)
**Destruction:** ContinuousDamage (HP/Fatigue/Mana), direct damage
**Restoration:** Heal HP/Fatigue/Mana, cure disease/poison
**Illusion:** Invisibility, Chameleon, Light/Darkness, Silence, Blind, Paralysis
**Alteration:** Levitate, SlowFall, Shield, Open
**Thaumaturgy:** Summon, Polymorph
**Mysticism:** Detect, Soultrap, Mark/Recall, Teleport

---

## 16. Magic — Enchanting

### OpenMW
**Types:** Cast Once (charges), Cast When Strikes (melee trigger), Constant Effect (always on)
- Cost based on magnitude + duration + item value
- Effects loaded on equip, removed on unequip

### DFU
- Spellmaker: choose effects, set magnitude/duration/range, calculate cost
- Enchanter: apply effects to items, charge-based or constant

---

## 17. Alchemy & Potions

### DFU
- 6 ingredient groups (plants x2, creatures x3, misc/metals)
- Each ingredient has primary property (ability) + secondary property (effect)
- Recipe key = combination of 4 ingredients → determines effect, strength, color, icon
- Potion creation success based on INT + Alchemy skill

---

## 18. Inventory & Items

### GemRB item properties
- Charges per header (capped at CHARGE_COUNTERS)
- Charge depletion types: None, Recharge, EveryLevel, RechargeOnRest
- Item value = base * condition * enchantment + material modifier
- Condition: 1.0 (perfect) to 0.1 (ruined)
- Undroppable, Unstealable, Critical flags

### DFU encumbrance
- MaxEncumbrance = STR * 1.5 (kg)
- Light armor 10-20kg, Heavy armor 30-60kg, Weapons 2-10kg
- Exceeding: reduced movement, no jumping

---

## 19. Body & Wounds (DF-style)

### DF body system (LEGENDARY depth)
- Hierarchical body parts tree (torso → upper arm → lower arm → hand → fingers)
- Each part has relative size (2000 torso, 300 head, 80 hands)
- Multi-layer tissues per part: skin → fat → muscle → bone → cartilage
- Each tissue has: healing rate, vascular rating (0-5), pain receptors (0-50), structural flags
- Healing rates: bone=1000, skin/fat=100 (lower = faster)
- Nervous system: nerve damage when muscles damaged in limbs
- Sensory: vision arcs, smell triggers, defined per creature

### EMBER RECOMMENDATION
Start with DFU's 7-body-part system for v1. Add DF-style tissue layers in v2 for deep wound mechanics.

---

## 20. Materials System (DF-style)

### DF material properties (per material)
- 50+ variables: density, specific heat, melting/boiling point, all 6 yield strengths
- States: solid, liquid, gas with separate display properties
- Edge quality: MAX_EDGE 0-10000 (0 = can't hold edge)
- Reaction classes: CAN_GLAZE, FLUX, CALCIUM_CARBONATE
- Material value: 1-30 base multiplier
- Temperature-dependent state changes

---

## 21. Movement & Physics ⚠️

### DFU
**States:** Idle, Walking, Running (1.5-2x, costs fatigue), Sneaking (0.5x), Falling, Climbing, Swimming, Jumping
**Speed:** 1 + (SPD - 50) / 100
**Fatigue effect:** 50% speed at 0 fatigue

**Fall damage (OpenMW, corrected — `character.cpp`):**
```
x  = fallHeight - fFallDistanceMin
x -= 1.5 * Acrobatics + jumpSpellBonus     // acrobatics skill + fortify-jump reduce fall
x  = max(0, x)
a  = fFallAcroBase + fFallAcroMult * (100 - Acrobatics)
x  = fFallDistanceBase + fFallDistanceMult * x
damage = x * a
```
Five game-setting floats tune the curve. Acrobatics skill is load-bearing — a trained jumper takes ~0 damage from heights that kill an untrained one.

**OpenMW fatigueTerm (central primitive — `creaturestats.cpp:39-53`):**
```
fatigueTerm = fFatigueBase - fFatigueMult * (1 - current/max)
```
This term multiplies hit chance, block chance, melee damage, AND fall damage. If Ember picks OpenMW combat, this is one of the most used numbers in the engine.

---

## 22. Pathfinding & Navigation

### GemRB
- SearchMap: 2D grid with passability flags
- IMPASSABLE, SIDEWALL (blocks LOS but not movement)
- Actor size: large/small different blocked radius
- LOS: Bresenham-style line tracing

### OpenMW
- Pathgrid: navigation mesh with waypoints per cell
- Cell-based loading/unloading (3-5 cell radius)
- Interior vs exterior cells (different rules)

---

## 23. Stealth & Concealment

### DFU
**Stealth check:** Chance = 2 * ((Distance / 51.2) * StealthSkill >> 10)
**Factors:** Distance, lighting (darkness bonus), crouching required, metal armor penalty
**Detection:** Vision cone + peripheral + hearing (weapon sounds, running)

### GemRB/BG
- Hide in Shadows + Move Silently (average for stealth check)
- Backstab: thief behind target, hidden, melee weapon → 2x-5x damage

---

## 24. Lockpicking & Security

### DFU
- **Interior:** Chance = 5*(Level - LockValue) + LockpickingSkill, clamp 5-95
- **Exterior:** Chance = LockpickingSkill - 5*LockValue, clamp 5-95
- Trap damage related to lock level

### GemRB
- **Pick Lock:** actor_skill + roll >= lock_difficulty
- **Trap Detection:** skill >= TrapDetectionDiff / 5
- **Trap Disarm:** thievery_skill vs TrapRemovalDiff
- XP rewards: gamedata->GetXPBonus(XP_LOCKPICK/XP_DISARM, level)

---

## 25. NPC AI — Behavior Packages

### OpenMW (priority-based queue)
| Package | Description |
|---------|------------|
| AiCombat | Attack target, manage distance, flee if critical |
| AiCast | Select spell and cast |
| AiActivate | Use object, drink potion, equip |
| AiTravel | Walk to destination |
| AiFollow | Follow player/NPC |
| AiEscort | Follow NPC to destination |
| AiWander | Random walk in area |
| AiPursue | Chase fleeing target |
| AiBreathe | Periodic sounds |
| AiFace | Face direction/target |
| AiAvoidDoor | Pathfind around doors |

### OpenMW Combat AI
- AttackCooldown timer
- FleeState: None → Idle → RunBlindly/RunToDestination
- LOS checks per frame
- AI Settings (`aisetting.hpp`): **Hello(0), Fight(1), Flee(2), Alarm(3)** — each 0-100. No "Calm"; disposition/Hello governs pacification.

---

## 26. NPC Schedules & Routines

### OpenMW
- Hour-by-hour daily activities
- Location changes: travel between cells
- Activities: sleep, eat, work, socialize
- Condition triggers: weather, time of day, quest state

---

## 27. Dialog System

### GemRB
- DLG format: states → transitions → response triggers
- Journal integration: IE_DLG_TR_JOURNAL, UNSOLVED, SOLVED
- Disposition system: NPC attitude affects available options
- Rumor system: shared info between NPCs
- Script integration: dialog triggers game events

### OpenMW
- Topics: player-selectable keywords
- Greetings: NPC introductions
- Conditional responses based on: faction, race, class, reputation, quest state, time of day

---

## 28. Faction & Reputation

### DFU
- Factions: Knightly Orders, Temples (8), Guilds, Vampire/Werewolf clans
- Reputation: -4 to +4 per faction
- Rank: 0-9, promotion via reputation + gold
- Guild services: Training, spellmaking, item enchanting

### GemRB/BG
- Party reputation: 1-20 scale
- NPC reactions based on reputation + charisma
- Faction-specific quests and rewards
- Reputation modifiers: loaded from REPMOD* tables

---

## 29. Morale System

### GemRB
- Morale value per NPC
- Morale break threshold → panic
- Panic modes: RunAway, RandomWalk, Berserk
- Recovery over time (MORALERECOVERYTIME)
- Happiness modifier from party reputation

---

## 30. Quest System

### DFU
- Quest: container with tasks, resources, conditions, rewards
- QuestMachine: central manager
- Lifecycle: accept → track objectives → resolve (success/fail) → reward
- Quest types: fetch, kill, escort, talk, explore, timed
- Questor tracking: NPC remembers active quests

### GemRB
- Journal entries: User(0), Unsolved(1), Solved(2), Main(3)
- Strref-based text with conditional visibility
- Chapter tracking

---

## 31. Economy — Merchants & Trading

### GemRB
- Buy price: Value * (100 - Mercantile) / 100
- Sell price: Value * Mercantile / 100
- Charisma modifier: 5% per point above/below 50
- Reputation affects markup
- Store types: shop, inn, temple, tavern, fence
- Acceptable categories: stores restrict what they buy
- Item recharge service: restores charges to max

### DFU
- Banking: deposit, withdraw, loans (10% interest/month)
- Max loan based on reputation + level

---

## 32. Economy — Banking ⚠️

### DFU (corrected — `FormulaHelper.cs:2023`)
- Deposit/withdraw gold
- Loans: borrow principal, repay **principal + 10% flat fee** (one-time origination; no monthly compounding in source)
- Default consequences: bounty + NPC/faction hostility
- Max loan: reputation * level * multiplier

---

## 33. Crime & Punishment

### DFU
- Shoplifting: Chance = 100 - Pickpocket + ShopQuality + ItemWeight
- Detection → guards alerted
- Bounty = stolen item value * 1.5
- Consequences: arrest, item confiscation

---

## 34. Disease & Poison

### DFU
**Common diseases:** Fever, Plague, Syphilis, Tuberculosis, Stomach Rot
**Blights:** Corprus (incurable), Ash Woe
**Mechanics:** Infection chance on hit, 1 hour incubation, cure via potions/spells

### DFU poisons (12 types)
Nux Vomica, Arsenic, Moonseed, Drothweed, Somnalius, Pyrrhic Acid, Magebane, Thyrwort

---

## 35. Vampirism & Lycanthropy

### DFU
**Vampirism:** Contract via Bloodpox or Clan kiss. Grants: enhanced stats (+2 STR/END), immortality, night vision, blood hunger. Cure: Daedric artifact quest.
**Lycanthropy:** Contract via Sanies Lupinus. Manual or forced transformation. Beast form: enhanced combat, berserk mode. Cure: Hircine's Ring quest.

---

## 36. Syndromes & Curses (DF-style) 🔍

### DF
- Interaction system: source → target → effect chain
- Syndrome effect tags (all verified in raws): ADD_TAG, PHYS_ATT_CHANGE, MATERIAL_FORCE_MULTIPLIER, BP_APPEARANCE_MODIFIER
- Tags present on "undead/mythical" creatures in raws: BLOODSUCKER, NO_AGING, NOT_LIVING, NOEXERT, NOPAIN
- **Ember-picked magnitudes (NOT from DF raws):** vampire +200 STR/AGI/TOUGH stat deltas — shipped DF raws define the TAGS but not an explicit vampire syndrome with these numbers. Treat as Ember design values inspired by the DF pattern.
- Conditional: requires CAN_LEARN, HAS_BLOOD; forbidden if already cursed
- Time-based progression: DF-pattern (e.g. "worsens every 24 hours") — Ember's Corprus analog must define its own progression curve.

---

## 37. Weather System

### DFU
Types: Clear, Cloudy, Overcast, Fog, Rain, Thunder, Snow, Blizzard
Transitions: 30-120 second gradual change
Effects: visibility, XP gain rate, NPC schedules

---

## 38. Time & Calendar ⚠️

### DFU (corrected — `WorldTime.cs:32`, `DaggerfallDateTime.cs:38-40`)
- **1 real second = 12 game seconds** (TimeScale default = 12f, NOT 20)
- 24 game hours per day, 30 days per month, 12 months (360 days/year)
- Seasons affect weather, plant growth, NPC behavior

> **Ember note:** If faster game-time is desired for gameplay reasons, adjust this scalar explicitly — don't cite DFU as the source of a 20x value.

---

## 39. World Generation

### DF (LEGENDARY)
- 257x257 tile world
- Elevation (1-400), Rainfall (0-100), Temperature (25-75), Drainage, Volcanism, Savagery
- Z-levels: 15 above, 3 cavern layers
- Civilization generation: 40 civs, 15000 population
- Supernatural: 75 megabeasts, 33 titans, 52 demons, 26+ vampires/werebeasts
- Mineral scarcity: configurable ore density
- Erosion cycles, river systems, fault lines

---

## 40. Dungeon Generation

### DFU
- Pre-built dungeon blocks assembled procedurally
- Block types: rooms, corridors, dead ends, treasure rooms, boss rooms
- Dungeon types: natural cave, castle, crypt, mine, tower

---

## 41. Door / Lock / Trap System

### GemRB
- Lock difficulty: 0-100
- Trap detection difficulty vs thief skill
- Trap disarm difficulty vs thief skill
- Bash door: STR check
- Key system: keys bypass locks if matching KeyIndex

---

## 42. Container System

### GemRB
- Heap type (ground piles) with visual icons (max 3 drawn)
- Lock difficulty, trapped flag, trap detection flag
- Loot tables: random contents by dungeon level

---

## 43. Rest & Recovery

### DFU
- Inn: 8 hours full recovery
- Campfire: 4-8 hours wilderness
- Recovery rates per hour: HP=HealthRecoveryRate, Fatigue=MaxFatigue/8, Mana=MaxMagicka/8
- Rest interruption: random encounter chance (wilderness)

---

## 44. Fast Travel

### DFU
- Travel to known locations
- Transport: stables, ships
- Travel time based on distance
- Random encounters during travel

---

## 45. Crafting & Reactions (DF-style)

### DF
- Reaction format: BUILDING + REAGENTS → PRODUCTS + SKILL + optional FUEL
- Example: 1 copper ore + 1 tin ore → 8 bronze bars (requires smelter + smelting skill)
- Product yield: percentage-based (100 = full)
- Skill requirements per reaction

---

## 46. Plants & Agriculture (DF-style)

### DF
- Seasonal growth flags (spring/summer/autumn/winter)
- Biome distribution
- Products: structural (wood), drink (alcohol), mill (flour), leaf, seed, root
- Edibility: raw, cooked, vermin-edible
- Crop examples: wheat → wheat beer + wheat flour + seeds

---

## 47. Personality & Emotions (DF-style)

### DF (15+ traits, range 0-100)
ABSTRACT_INCLINED, ART_INCLINED, CURIOUS, PERSEVERANCE, VENGEFUL, ACTIVITY_LEVEL, PRIDE, VANITY, GREED, IMMODERATION, STRESS_VULNERABILITY, BASHFUL, SINGLEMINDED, INTUITION, PATIENCE, EMPATHY, SOCIAL_AWARENESS

---

## 48. Save / Load

### All engines
- Full state serialization to file (JSON, binary, or custom format)
- Multiple save slots
- Autosave on area transition
- Quick save / quick load

---

## 49. Turn Undead

### GemRB
- Turn level vs undead level + modifier
- Success: undead destroyed or panicked
- Level difference determines effect severity

---

## 50. Detect Effects

### DFU
- Detect Life/Undead/Artifact/Enchantment
- Range: 50-1000 feet based on magnitude
- Duration-limited effect

---

## 51. Mark & Recall / Teleport

### DFU
- Mark: set teleport destination (one active)
- Recall: teleport to marked location
- Cannot mark while paralyzed/silenced

---

## 52. Encumbrance

### DFU
- MaxEncumbrance = STR * 1.5 (kg)
- Exceeding: reduced speed, no jumping

---

## 53. NPC Civilization & Entities (DF-style)

### DF
- Entity types: MOUNTAIN (dwarf), FOREST (elf), PLAINS (human), etc.
- Equipment access: 20+ weapon types, 10+ armor types per civ
- Currency evolution by year (copper → silver → gold)
- Biome support with multiplier
- Religion: PANTHEON with multiple spheres
- Military: active season, sieger flag
- Diplomacy: bodyguards, merchant caravans

---

## PRIORITY MATRIX — What to Build First

### Phase 1 (Week 1-2): CORE
| System | Priority | Source Reference |
|--------|----------|-----------------|
| Stats (6 attributes) | P0 | DFU DaggerfallStats.cs |
| Actor/Entity model | P0 | All engines |
| Grid movement | P0 | GemRB Map.cpp |
| First-person camera | P0 | DFU PlayerMotor.cs |
| Health/Fatigue/Mana | P0 | DFU FormulaHelper.cs |

### Phase 2 (Week 3-4): COMBAT
| System | Priority | Source Reference |
|--------|----------|-----------------|
| Attack resolution (%-based) | P0 | DFU FormulaHelper.cs |
| Damage calculation | P0 | DFU FormulaHelper.cs |
| Armor & body parts | P0 | DFU (7 parts) |
| Death/knockout | P0 | OpenMW CreatureStats |
| Weapons (damage tables) | P0 | DFU FormulaHelper.cs |
| Turn-based combat state machine | P0 | Custom |

### Phase 3 (Week 5-6): WORLD
| System | Priority | Source Reference |
|--------|----------|-----------------|
| Dungeon room generation | P1 | DFU dungeon blocks |
| Door/lock/trap | P1 | GemRB Container.cpp |
| Inventory | P1 | GemRB Item.cpp |
| NPC dialog (basic) | P1 | GemRB DialogHandler.cpp |

### Phase 4 (Week 7-8): DEPTH
| System | Priority | Source Reference |
|--------|----------|-----------------|
| Quest system | P1 | DFU Questing/ |
| Spell system (basic) | P1 | DFU MagicAndEffects/ |
| Skills + advancement | P1 | DFU DaggerfallSkills.cs |
| Save/Load | P1 | All engines |

### Phase 5 (Week 9-12): RICHNESS
| System | Priority | Source Reference |
|--------|----------|-----------------|
| Faction/reputation | P2 | DFU Guilds/ |
| Economy/merchants | P2 | GemRB Store.cpp |
| Stealth/pickpocket | P2 | DFU FormulaHelper.cs |
| Disease/poison | P2 | DFU MagicAndEffects/ |
| Enchanting/alchemy | P2 | OpenMW mwmechanics/ |
| Weather/time | P2 | DFU Weather/ |

### Phase 6 (Month 3+): LEGENDARY
| System | Priority | Source Reference |
|--------|----------|-----------------|
| DF-style body/wounds | P3 | DF body_default.txt |
| DF-style materials | P3 | DF inorganic_metal.txt |
| DF-style crafting | P3 | DF reaction_smelter.txt |
| DF-style personality | P3 | DF creature_standard.txt |
| DF-style world gen | P3 | DF world_gen.txt |
| Vampirism/Lycanthropy | P3 | DFU + DF syndromes |
