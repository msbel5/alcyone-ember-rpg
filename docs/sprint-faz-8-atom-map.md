# Faz 8 - Atom map (Data-driven magic)

_Date:_ 2026-05-20
_Branch:_ `mami/faz-6-12-atom-maps`
_Primary boxes:_ `CRPG`, `AI/DM`
_Roadmap:_ `docs/ROADMAP.md`
_Mechanic map:_ `DOCS/mechanic-map-v1.md`
_Execution ledger:_ `DOCS/faz-5-12-execution-ledger.md`
_Agent rules:_ `DOCS/agent-rules-v2.md`
_Vision notes:_ `DOCS/EMBER_VISION_NOTES_MAMI.md`
_Inspector checklist:_ `DOCS/inspector-audit-checklist.md`

## Vision anchors

1. Living-world over showroom: magic is one more system that touches matter, time, and society — not a bespoke pipeline.
5. Data-driven extension: every new effect is a data row + operation handler, never a hard-coded effect branch.
8. Systemic interaction: fire effect ignites oil tile, ice effect freezes water, healing effect closes wounds from Faz 7 — all via shared operation handlers.

## Phase fences

- No Memory state before Faz 9 (spell history is event-log only, no actor memory recall here).
- No shared NPC / party / DM tool surface before Faz 10.
- No LLM fallback wiring before Faz 12.
- No procedural genesis (spell seeds are fixture rows).
- No multiverse / 100K-year / interplanetary implementation.
- No free-text dialogue parsing before Faz 9.

## Anti-pattern reminder (from Sprint 5 audit)

Sprint 5 burned weeks expanding hard-coded spell-effect branches. The `agent-rules-v2.md` Rule 3 hard-blocks new branch entries. This sprint promotes the existing seven legacy effect codes to data rows backed by 2-3 operation handlers, then ships every subsequent effect as a row.

## Debt ledger action

Faz 8 does not consume `CO-02 / CO-03 / CO-06 / CO-07`. These carry forward.

## Sprint goal

Target acceptance sentence:

`player can cast fire on an oil tile and watch it ignite spreading damage to nearby actors deterministically`.

Faz 8 makes magic a typed, data-driven verb: `EffectDefinition` rows declare which `EffectOperation` handlers fire (damage, restore, status_apply, area_apply); the registry resolves and applies. New spells = new rows.

## Atomic decomposition

| Atom | Primary box | File / class | Responsibility | Closing proof | Status |
|---:|---|---|---|---|---|
| 1 | CRPG | `Assets/Scripts/Domain/Magic/EffectOperationKind.cs` | Stable-string operation kinds (`direct_damage`, `direct_restore`, `status_apply`, `area_apply`, `terrain_apply`). | `EffectOperationKindTests` | queued |
| 2 | CRPG | `Assets/Scripts/Domain/Magic/EffectOperation.cs` | Pure record carrying op kind + magnitude + target rules + cost. No execution. | `EffectOperationTests` | queued |
| 3 | CRPG | `Assets/Scripts/Domain/Magic/EffectDefinition.cs` | Data row: id, name, school tag, list of `EffectOperation`, cost, cooldown. | `EffectDefinitionTests` | queued |
| 4 | CRPG | `Assets/Scripts/Simulation/Magic/EffectRegistry.cs` | Loads `EffectDefinition` rows; deterministic lookup by id. | `EffectRegistryTests` | queued |
| 5 | CRPG | `Assets/Scripts/Simulation/Magic/EffectOperationHandlers.cs` | One C# handler per `EffectOperationKind`; deterministic, side-effect routed through stores + event log. | `EffectOperationHandlersTests` | queued |
| 6 | CRPG | `Assets/Scripts/Simulation/Magic/SpellResolver.cs` | Orchestrate: validate cost, run each op, emit `SpellResolved`. Replaces the old `SpellEffectCode` switch. | `SpellResolverTests` | queued |
| 7 | CRPG | `Assets/Scripts/Data/Magic/SeedEffectDefinitions.json` (or registry) | Re-express the seven legacy `SpellEffectCode` values as data rows; keep only migration adapters. | `SeedEffectDefinitionsTests` | queued |
| 8 | CRPG | Legacy branch removal | Delete legacy branch-based routing; route call sites through `SpellResolver`. | full test suite stays green | queued |
| 9 | WORLD | `Assets/Scripts/Domain/World/TerrainComponent.cs`, `TerrainEffectDef` | Tile-level effect data row (oil, water, snow). Required for the fire-on-oil acceptance. | `TerrainComponentTests` | queued |
| 10 | CRPG | `Assets/Scripts/Simulation/Magic/AreaEffectSystem.cs` | Tick area-of-effect operations on terrain + actors; deterministic spread. | `AreaEffectSystemTests` | queued |
| 11 | TIME | `Assets/Scripts/Data/Save` effect/spell/terrain mappers | Round-trip active effects, terrain state, cooldowns. | `MagicTerrainRoundTripTests` | queued |
| 12 | CRPG | `Assets/Tests/EditMode/Magic/FazEightMagicAcceptanceTests.cs` | Deterministic replay: cast fire on oil tile, fire spreads, nearby actor takes damage. | acceptance replay note | queued |

## Suggested bundles

1. `effect-primitives` — Atoms 1, 2, 3.
2. `effect-registry` — Atoms 4, 5.
3. `spell-resolver` — Atom 6.
4. `seed-effects-and-enum-removal` — Atoms 7, 8 (one PR; closes the Sprint 5 anti-pattern).
5. `terrain-area-effects` — Atoms 9, 10.
6. `magic-save-and-acceptance` — Atoms 11, 12.

## Promotion checklist

- [ ] Every Faz 8 atom row above is checked off.
- [x] `SpellEffectCode` is a stable value object, not an enum.
- [ ] All new effects ship as `EffectDefinition` rows + operation handlers — zero new hard-coded branches.
- [ ] `./tools/validation/run-validation.sh --mode fallback` passes on the promotion branch.
- [ ] Product-visible PR count is greater than zero (fire-on-oil replay or equivalent).
- [ ] Faz 8 promotion summary reports Debt ledger status.

## Next increment

Implement the `effect-primitives` bundle first: Atoms 1, 2, 3. Hard rule from `agent-rules-v2.md` Rule 3: no new `SpellEffectCode` branches accepted.
