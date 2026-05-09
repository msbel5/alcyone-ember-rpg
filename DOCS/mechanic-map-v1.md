# Mechanic map v1 — Ember living-world core

The 8-box model. Every gameplay system fits into exactly one box.
This file is canonical: a sprint can only target work that maps to
something here, and PR descriptions must cite the box.

```
1. TIME       GameTime, Season, Weather, TickSchedule, DeterministicRng
2. WORLD      Region, Site, Settlement, Cell, Room, TravelEdge, Biome
3. LIVING     Actor, Stats, Vitals, Skills, Needs, Mood, Memory, Schedule
4. MATTER     Item, Material, Quality, Wear, Stack, Inventory, Equipment
5. PROCESS    Recipe, Reaction, Worksite, Job, PlantGrowth, CraftQuality
6. SOCIETY    Faction, Reputation, Store, TradeRoute, Price, Caravan, Shortage
7. CRPG       Combat, Magic, Dialogue, Quest, SkillCheck, Rest, Crime
8. AI / DM    Query, Chance, Roll, Mutation, ToolCall, Narration, NpcAgent
```

## How a turn flows

```
Player or NPC Intent
        |
        v
   Command (typed)
        |
        v
   Validation (deterministic rules + skill / faction / state checks)
        |
        v
   System tick (one of boxes 1 to 7)
        |
        v
   World mutation (through one of the Stores)
        |
        v
   EventLog + ReasonTrace (deterministic, replayable)
        |
        v
   Unity view + UI + (optional) DM narration
```

LLM enters at the bottom only. It reads `ActorView`, `MemoryView`,
`WorldEvent`, `ReasonTrace`, then may `say_line`, `ask_query`, or
`propose_action`. Every proposal goes through the same validation +
system tick path. LLM can never write the world directly.

## Composition over inheritance

Avoid `Thing : MonoBehaviour`. Use entity id + components:

```
EntityId
 |- IdentityComponent       name, tags, owner
 |- PositionComponent       region / site / cell / tile
 |- MaterialComponent       material, quality, mass, temperature
 |- InventoryComponent      item ids
 |- VitalsComponent         health / fatigue / mana / breath
 |- SkillComponent          xp, level, rust
 |- MemoryComponent         recent events, long-term facts
 |- LightComponent          intensity, radius, fuel
 |- EnergyComponent         heat / electric / magic / fuel
 |- ScheduleComponent       current plan, next action
```

Systems own behaviour:

```
PlantGrowthSystem    reads Season + Soil + PlantComponent
RecipeSystem        reads Worksite + Recipe + Inventory + Skill
CombatSystem        reads Actor + Weapon + Armor + Position
TradeRouteSystem    reads Settlement stock + TravelEdge + Faction
LightSystem         reads Fuel + LightEmitter + Time
MemorySystem        reads EventLog and writes ActorMemory
```

## Recipes are the universal verb, not the universal noun

Not everything is a recipe. Every transformation is a recipe or a
world process.

```
Item: IronOre
Recipe: SmeltIronIngot
  input: 2 ore (tag = iron), 1 fuel
  worksite: furnace
  actor skill: smelting
  time: 40 ticks
  output: 1 IronIngot (material = iron, quality = roll(skill))

Item: WheatSeed
Process: GrowWheatSeasonTick
  input: soil, water, season in {spring, summer}
  time: days
  output: WheatPlant stage++

Process: TradeRouteTick
  input: settlement A surplus, settlement B demand, route danger
  time: travel_hours
  output: stock transfer, price shift, event log
```

`RecipeDef` covers crafting. `WorldProcess` covers slower or
non-crafting transformations (farming, season, caravan, decay,
syndromes, etc). Both share the same input + duration + output +
event-log shape.

## Effects are data, not branches

```
EffectDefinition
  effect_id          "restore_mana"
  operation          "modify_resource"
  resource           "mana"
  mode              "restore"
  magnitude_formula  fixed | skill_scaled | distance_scaled | ...
  duration           instant | timed
  target_rule        self | touch | ranged | area
  school / tags
  save_resistance_rule
```

Operations:

```
ModifyResourceOperation
ApplyBuffOperation
SpawnLightOperation
ModifyDispositionOperation
ApplyConditionOperation
MoveItemOperation
StartWorldProcessOperation
```

A new spell ships as a row of data. C# only changes when a new
operation kind is required.

## What goes in which box (selected examples)

| Box | Examples |
|---|---|
| TIME | sun position, day/night, season turnover, plant growth tick, syndrome tick |
| WORLD | region map, dungeon site, room layout, lighting, traps |
| LIVING | actor stats, mood, schedule, needs, memory, opinion |
| MATTER | items, stacks, materials, quality, repair, decay |
| PROCESS | smelting, cooking, brewing, sewing, farming, caravan transit, harvest |
| SOCIETY | guild ranks, faction wars, reputation, prices, shortages, crime ledger |
| CRPG | weapon swing, spell cast, persuade, intimidate, lockpick, rest, sleep, dream |
| AI / DM | npc dialogue, dm narration, npc agent goal selection, llm flavor |

## What does not belong

The repo is not:

- a generic ECS engine demo
- a generic OOP teaching project
- a chatbot wrapper
- a spell-balance regression suite (tests are subordinate to gameplay)

If a sprint cannot point at a single box and a `player can ...`
sentence, it does not belong on the active branch.

## Status mapping (live)

| Box | Status (2026-05-09) | Next move |
|---|---|---|
| TIME | partial; tick-driver service exists | wire to Season + DayNight |
| WORLD | thin; only `SliceWorldState.Room` and `Dungeon` | promote to `SiteStore` |
| LIVING | partial; `ActorRecord` + Stats + Vitals + dialogue topic | promote to `ActorStore` with components |
| MATTER | minimal; placeholder slots | introduce `ItemStore` + `ItemRecord` |
| PROCESS | absent | first vertical slice after Faz 1 |
| SOCIETY | absent | after Faz 6 |
| CRPG | magic at production; combat partial; dialogue at slice level | combat + equipment integration in Faz 7 |
| AI / DM | not yet | last layer, Faz 12 |
