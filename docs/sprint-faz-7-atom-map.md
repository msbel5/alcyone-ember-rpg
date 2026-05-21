# Faz 7 - Atom map (Combat + Equipment)

_Date:_ 2026-05-20
_Branch:_ `mami/faz-6-12-atom-maps`
_Primary boxes:_ `CRPG`, `MATTER`
_Roadmap:_ `docs/ROADMAP.md`
_Mechanic map:_ `DOCS/mechanic-map-v1.md`
_Execution ledger:_ `DOCS/faz-5-12-execution-ledger.md`
_Agent rules:_ `DOCS/agent-rules-v2.md`
_Vision notes:_ `DOCS/EMBER_VISION_NOTES_MAMI.md`
_Inspector checklist:_ `DOCS/inspector-audit-checklist.md`

## Vision anchors

1. Living-world over showroom: combat resolves through deterministic roll/effect math, not bespoke scripting.
4. Morrowind-shaped immersion: combat outcomes feed back into faction reputation, NPC memory, and DM checkpoints in later sprints.
5. Data-driven extension: weapons, armor, status effects, and combat actions ship as rows; no `WeaponKind` enum branching.
6. Composition over inheritance: equipment is a component on an actor, not a derived class.
8. Systemic interaction: combat consumes Faz 5 plant/season (terrain), Faz 6 faction (hostility), Faz 4 mood (refusal); does not assume any of them.

## Phase fences

- No Memory state before Faz 9 (combat events log to WorldEventLog but no actor memory recall here).
- No shared NPC / party / DM tool surface before Faz 10.
- No LLM fallback wiring before Faz 12.
- No procedural genesis.
- No multiverse / 100K-year / interplanetary implementation.
- No free-text dialogue parsing before Faz 9.

## Debt ledger action

Faz 7 does not consume `CO-02 / CO-03 / CO-06 / CO-07`. These carry forward.

## Sprint goal

Target acceptance sentence from `docs/ROADMAP.md`:

`player can equip a sword, strike a bandit, watch armor reduce damage, and see the bandit's faction turn hostile`.

Faz 7 makes combat a typed verb: a `CombatAction` data row consumes stamina, rolls hit/damage from `EmberStatBlock` plus equipment modifiers, applies damage with armor mitigation, emits `CombatResolved` to WorldEventLog. Equipment is a data-driven inventory slot, not a subclass.

## Atomic decomposition

| Atom | Primary box | File / class | Responsibility | Closing proof | Status |
|---:|---|---|---|---|---|
| 1 | MATTER | `Assets/Scripts/Domain/Inventory/EquipmentSlot.cs` | Stable-string slot codes (`main_hand`, `off_hand`, `head`, `chest`, `legs`, `feet`). | `EquipmentSlotTests` | queued |
| 2 | MATTER | `Assets/Scripts/Domain/Inventory/EquipmentComponent.cs` | Actor-local map of slot → ItemId; immutable `WithEquipped` / `WithUnequipped` helpers. | `EquipmentComponentTests` | queued |
| 3 | MATTER | `Assets/Scripts/Domain/Items/WeaponProfile.cs`, `ArmorProfile.cs` | Data rows on ItemRecord for damage band, reach, stamina cost, armor band, slot. | `WeaponArmorProfileTests` | queued |
| 4 | CRPG | `Assets/Scripts/Domain/Combat/CombatActionId.cs`, `CombatActionDef.cs` | Stable id + data row for a combat action: stamina cost, hit-roll formula key, damage formula key, animation tag. | `CombatActionDefTests` | queued |
| 5 | CRPG | `Assets/Scripts/Simulation/Combat/CombatHitRollService.cs` | Deterministic hit roll from attacker `Accuracy` + weapon reach vs defender `Dodge`; seeded RNG. | `CombatHitRollServiceTests` | queued |
| 6 | CRPG | `Assets/Scripts/Simulation/Combat/CombatDamageService.cs` | Roll damage band, apply armor mitigation, clamp at zero. | `CombatDamageServiceTests` | queued |
| 7 | CRPG | `Assets/Scripts/Simulation/Combat/CombatActionResolver.cs` | Orchestrate stamina check → hit roll → damage roll → vital deltas; emit `CombatResolved`. | `CombatActionResolverTests` | queued |
| 8 | LIVING | `Assets/Scripts/Domain/Actors/ActorVitals.cs` :: extensions | Add `ApplyDamage` / `ApplyHeal` returning new struct; preserve clamp at 0/max. | `ActorVitalsCombatTests` | queued |
| 9 | LIVING | `Assets/Scripts/Domain/Actors/ActorRecord.cs` :: extensions | `ApplyEquipment` / `ApplyVitals` immutable updates routed through ActorStore. | `ActorRecordEquipmentTests` | queued |
| 10 | CRPG | `Assets/Scripts/Simulation/Combat/StatusEffectRegistry.cs` | Data-driven status effects (bleed, poison, stunned) as `StatusEffectDef` rows + tick system. | `StatusEffectRegistryTests` | queued |
| 11 | SOCIETY | `Assets/Scripts/Simulation/Combat/CombatFactionHook.cs` | When `CombatResolved` involves cross-faction attackers, route a reputation delta through Faz 6 `FactionReputationSystem`. | `CombatFactionHookTests` | queued |
| 12 | TIME | `Assets/Scripts/Data/Save` combat/equipment save mappers | Round-trip equipment, status effects, weapon/armor profiles. | `CombatEquipmentRoundTripTests` | queued |
| 13 | CRPG | `Assets/Tests/EditMode/Combat/FazSevenCombatAcceptanceTests.cs` | Deterministic replay: equip sword, attack bandit, armor reduces damage by exact amount, bandit dies, reputation shifts. | acceptance replay note | queued |

## Suggested bundles

1. `equipment-primitives` — Atoms 1, 2, 3.
2. `combat-action-def` — Atom 4.
3. `combat-rolls` — Atoms 5, 6, 7.
4. `combat-vital-equipment-integration` — Atoms 8, 9.
5. `status-effects` — Atom 10.
6. `combat-faction-hook` — Atom 11.
7. `combat-save-and-acceptance` — Atoms 12, 13.

## Promotion checklist

- [ ] Every Faz 7 atom row above is checked off.
- [ ] Each sub-area has at least one merged PR.
- [ ] `./tools/validation/run-validation.sh --mode fallback` passes on the promotion branch.
- [ ] Product-visible PR count is greater than zero.
- [ ] No new `WeaponKind` or `EffectKind` enum branches; all new effects ship as data rows.
- [ ] Faz 7 promotion summary reports Debt ledger status.

## Next increment

Implement the `equipment-primitives` bundle first: Atoms 1, 2, 3 with their tests.
