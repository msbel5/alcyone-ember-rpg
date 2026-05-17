# Backend method tree

_Generated:_ 2026-05-18
_Branch:_ agent/faz-5-season-ledger

Purpose: make the backend visible as a class/method inventory before continuing Faz 4-12 implementation. This is a control-plane artifact for slicing GPT-5-mini/Codex work into small bounded files and tests.

## Inventory summary

- `Assets/Scripts/Domain`: 99 files, 105 types, 452 method/property rows
- `Assets/Scripts/Simulation`: 72 files, 83 types, 287 method/property rows
- `Assets/Scripts/Data`: 8 files, 35 types, 64 method/property rows
- `Assets/Tests/EditMode`: 128 files, 135 types, 1097 method/property rows

## Phase keyword index

### Faz 4 Colony needs
- `Assets/Scripts/Domain/Actors/ActorMood.cs`
- `Assets/Scripts/Domain/Actors/ActorNeeds.cs`
- `Assets/Scripts/Domain/Actors/ActorRecord.cs`
- `Assets/Scripts/Domain/Actors/NeedKind.cs`
- `Assets/Scripts/Domain/Actors/NeedValue.cs`
- `Assets/Scripts/Domain/Combat/QueuedCombatAction.cs`
- `Assets/Scripts/Domain/Process/NeedRecoveryRecipe.cs`
- `Assets/Scripts/Simulation/Combat/CombatActionTimingProfile.cs`
- `Assets/Scripts/Simulation/Living/NeedMoodEvaluator.cs`
- `Assets/Scripts/Simulation/Living/NeedRecoverySystem.cs`
- `Assets/Scripts/Simulation/Living/NeedsSystem.cs`
- `Assets/Tests/EditMode/Actors/ActorMoodTests.cs`
- `Assets/Tests/EditMode/Actors/ActorNeedsTests.cs`
- `Assets/Tests/EditMode/Actors/ActorRecordMoodTests.cs`
- `Assets/Tests/EditMode/Actors/ActorRecordNeedsTests.cs`
- `Assets/Tests/EditMode/Actors/NeedKindTests.cs`
- `Assets/Tests/EditMode/Actors/NeedValueTests.cs`
- `Assets/Tests/EditMode/Living/NeedMoodEvaluatorTests.cs`
- `Assets/Tests/EditMode/Living/NeedRecoverySystemEatTests.cs`
- `Assets/Tests/EditMode/Living/NeedRecoverySystemSleepTests.cs`
- `Assets/Tests/EditMode/Living/NeedsEventLogTests.cs`
- `Assets/Tests/EditMode/Living/NeedsSystemMoodTests.cs`
- `Assets/Tests/EditMode/Living/NeedsSystemTests.cs`
- `Assets/Tests/EditMode/Magic/SpellTargetValidatorTests.cs`
- `Assets/Tests/EditMode/Process/JobNeedsRefusalTests.cs`
- `Assets/Tests/EditMode/Process/NeedRecoveryRecipeTests.cs`
- `Assets/Tests/EditMode/Save/ActorNeedsRoundTripTests.cs`

### Faz 5 Plant growth + Season
- `Assets/Scripts/Domain/Combat/RealtimeDamageResult.cs`
- `Assets/Scripts/Domain/Core/GameTime.cs`
- `Assets/Scripts/Domain/Memory/InteractionEvent.cs`
- `Assets/Scripts/Domain/Memory/TransactionRecord.cs`
- `Assets/Scripts/Domain/Process/PlantComponent.cs`
- `Assets/Scripts/Domain/Process/PlantGrowthRule.cs`
- `Assets/Scripts/Domain/Process/PlantGrowthStageDef.cs`
- `Assets/Scripts/Domain/Process/PlantSpeciesDef.cs`
- `Assets/Scripts/Domain/Process/PlantStageId.cs`
- `Assets/Scripts/Domain/Process/SoilComponent.cs`
- `Assets/Scripts/Domain/Process/WorldProcessDef.cs`
- `Assets/Scripts/Domain/Process/WorldProcessId.cs`
- `Assets/Scripts/Domain/Process/WorldProcessInstance.cs`
- `Assets/Scripts/Domain/Time/Season.cs`
- `Assets/Scripts/Domain/Time/SeasonCalendar.cs`
- `Assets/Scripts/Domain/Time/SeasonDefinition.cs`
- `Assets/Scripts/Domain/World/ComponentStore.cs`
- `Assets/Scripts/Domain/World/WorldComponentId.cs`
- `Assets/Scripts/Domain/World/WorldEvent.cs`
- `Assets/Scripts/Simulation/Combat/RealtimeCombatActionScheduler.cs`
- `Assets/Scripts/Simulation/Combat/RealtimeCombatState.cs`
- `Assets/Scripts/Simulation/Combat/RealtimeCombatTickResult.cs`
- `Assets/Scripts/Simulation/Combat/RealtimeDamageService.cs`
- `Assets/Scripts/Simulation/Magic/SpellCostCalculator.cs`
- `Assets/Scripts/Simulation/Movement/Sprint4KinematicMotor.cs`
- `Assets/Scripts/Simulation/Process/HarvestSystem.cs`
- `Assets/Scripts/Simulation/Process/PlantGrowthSystem.cs`
- `Assets/Scripts/Simulation/Process/PlantingSystem.cs`
- `Assets/Scripts/Simulation/Time/GameTimeAdvanceSystem.cs`
- `Assets/Tests/EditMode/Combat/RealtimeCombatActionSchedulerTests.cs`
- `Assets/Tests/EditMode/Combat/RealtimeDamageServiceTests.cs`
- `Assets/Tests/EditMode/Core/GameTimeTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffActorKeyedApplicationServiceTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffApplicationServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellExecutionServiceTests.cs`
- `Assets/Tests/EditMode/Process/JobEventLogTests.cs`
- `Assets/Tests/EditMode/Process/HarvestSystemTests.cs`
- `Assets/Tests/EditMode/Process/PlantComponentTests.cs`
- `Assets/Tests/EditMode/Process/PlantDefinitionTests.cs`
- `Assets/Tests/EditMode/Process/PlantGrowthSystemTests.cs`
- `Assets/Tests/EditMode/Process/PlantingSystemTests.cs`
- `Assets/Tests/EditMode/Process/SoilComponentTests.cs`
- `Assets/Tests/EditMode/Process/WorldProcessDefinitionTests.cs`
- `Assets/Tests/EditMode/Time/GameTimeAdvanceSystemTests.cs`
- `Assets/Tests/EditMode/Time/SeasonCalendarTests.cs`
- `Assets/Tests/EditMode/World/ComponentStoreTests.cs`
- `Assets/Tests/EditMode/World/WorldComponentIdTests.cs`

### Faz 6 Trade routes + Faction
- `Assets/Scripts/Data/Save/SliceSaveData.cs`
- `Assets/Scripts/Data/Save/SliceSaveMapper.cs`
- `Assets/Scripts/Domain/Core/FactionId.cs`
- `Assets/Scripts/Domain/World/FactionRecord.cs`
- `Assets/Scripts/Domain/World/FactionStore.cs`
- `Assets/Scripts/Simulation/Inventory/MerchantTradeService.cs`
- `Assets/Scripts/Simulation/Narrative/NpcMemoryQueryService.cs`
- `Assets/Tests/EditMode/Core/FactionIdTests.cs`
- `Assets/Tests/EditMode/Inventory/MerchantTradeServiceTests.cs`
- `Assets/Tests/EditMode/Narrative/NpcMemoryQueryServiceTests.cs`
- `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`
- `Assets/Tests/EditMode/Save/JsonSliceSaveServiceTests.cs`
- `Assets/Tests/EditMode/Save/RecipeWorksiteRoundTripTests.cs`
- `Assets/Tests/EditMode/World/FactionRecordTests.cs`
- `Assets/Tests/EditMode/World/FactionStoreTests.cs`

### Faz 7 Combat + Equipment
- `Assets/Scripts/Data/Save/ItemSaveMapper.cs`
- `Assets/Scripts/Data/Save/SliceSaveData.cs`
- `Assets/Scripts/Data/Save/SliceSaveMapper.cs`
- `Assets/Scripts/Domain/Actors/ActorRecord.cs`
- `Assets/Scripts/Domain/Actors/ActorVitals.cs`
- `Assets/Scripts/Domain/Actors/VitalStat.cs`
- `Assets/Scripts/Domain/Combat/BodyPart.cs`
- `Assets/Scripts/Domain/Combat/BodyPartNode.cs`
- `Assets/Scripts/Domain/Combat/CombatActionEvent.cs`
- `Assets/Scripts/Domain/Combat/CombatActionEventKind.cs`
- `Assets/Scripts/Domain/Combat/CombatActionKind.cs`
- `Assets/Scripts/Domain/Combat/CombatDefenseIntent.cs`
- `Assets/Scripts/Domain/Combat/CombatStrikeResult.cs`
- `Assets/Scripts/Domain/Combat/EncounterState.cs`
- `Assets/Scripts/Domain/Combat/QueuedCombatAction.cs`
- `Assets/Scripts/Domain/Combat/RealtimeDamageResult.cs`
- `Assets/Scripts/Domain/Combat/WeaponHitEvent.cs`
- `Assets/Scripts/Domain/Inventory/EquipmentSlot.cs`
- `Assets/Scripts/Domain/Inventory/EquipmentState.cs`
- `Assets/Scripts/Domain/Inventory/InventoryItem.cs`
- `Assets/Scripts/Domain/Inventory/InventoryState.cs`
- `Assets/Scripts/Domain/Inventory/ItemMaterial.cs`
- `Assets/Scripts/Domain/Inventory/ItemQuality.cs`
- `Assets/Scripts/Domain/Inventory/ItemRecord.cs`
- `Assets/Scripts/Domain/Inventory/RoomPickup.cs`
- `Assets/Scripts/Simulation/Combat/BodyPartHierarchy.cs`
- `Assets/Scripts/Simulation/Combat/BodyPartSelector.cs`
- `Assets/Scripts/Simulation/Combat/CombatActionTimingProfile.cs`
- `Assets/Scripts/Simulation/Combat/CombatMathService.cs`
- `Assets/Scripts/Simulation/Combat/EncounterTurnService.cs`
- `Assets/Scripts/Simulation/Combat/RealtimeCombatActionScheduler.cs`
- `Assets/Scripts/Simulation/Combat/RealtimeCombatState.cs`
- `Assets/Scripts/Simulation/Combat/RealtimeCombatTickResult.cs`
- `Assets/Scripts/Simulation/Combat/RealtimeDamageService.cs`
- `Assets/Scripts/Simulation/Inventory/EquipmentActionError.cs`
- `Assets/Scripts/Simulation/Inventory/EquipmentActionResult.cs`
- `Assets/Scripts/Simulation/Inventory/EquipmentCombatStats.cs`
- `Assets/Scripts/Simulation/Inventory/EquipmentService.cs`
- `Assets/Scripts/Simulation/Inventory/MerchantTradeService.cs`
- `Assets/Scripts/Simulation/Inventory/PickupService.cs`
- `Assets/Scripts/Simulation/Inventory/SliceItemCatalog.cs`
- `Assets/Scripts/Simulation/Living/NeedRecoverySystem.cs`
- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`
- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionResult.cs`
- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`
- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionResult.cs`
- `Assets/Scripts/Simulation/Process/RecipeSystem.cs`
- `Assets/Scripts/Simulation/World/SliceActorLoadoutFactory.cs`
- `Assets/Tests/EditMode/Actors/ActorRecordTests.cs`
- `Assets/Tests/EditMode/Actors/VitalStatTests.cs`
- `Assets/Tests/EditMode/Combat/BodyPartSelectorTests.cs`
- `Assets/Tests/EditMode/Combat/CombatMathServiceTests.cs`
- `Assets/Tests/EditMode/Combat/EncounterTurnServiceTests.cs`
- `Assets/Tests/EditMode/Combat/RealtimeCombatActionSchedulerTests.cs`
- `Assets/Tests/EditMode/Combat/RealtimeDamageServiceTests.cs`
- `Assets/Tests/EditMode/Inventory/EquipmentServiceTests.cs`
- `Assets/Tests/EditMode/Inventory/InventoryStateTests.cs`
- `Assets/Tests/EditMode/Inventory/ItemRecordTests.cs`
- `Assets/Tests/EditMode/Inventory/MerchantTradeServiceTests.cs`
- `Assets/Tests/EditMode/Living/NeedRecoverySystemEatTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceAbsorptionTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceRegistryAbsorptionTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceRegistryBatchAbsorptionTests.cs`
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellExecutionServiceTests.cs`
- `Assets/Tests/EditMode/Narrative/NarrativeShellTests.cs`
- `Assets/Tests/EditMode/Presentation/InventoryEquipmentFormatterTests.cs`
- `Assets/Tests/EditMode/Presentation/SliceAtmosphereSelectorTests.cs`
- `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`
- `Assets/Tests/EditMode/Process/JobEventLogTests.cs`
- `Assets/Tests/EditMode/Process/NeedRecoveryRecipeTests.cs`
- `Assets/Tests/EditMode/Process/PlantingSystemTests.cs`
- `Assets/Tests/EditMode/Process/RecipeSystemTests.cs`
- `Assets/Tests/EditMode/Save/RecipeWorksiteRoundTripTests.cs`
- `Assets/Tests/EditMode/World/ItemStoreTests.cs`
- `Assets/Tests/EditMode/World/SliceWorldFactoryTests.cs`

### Faz 8 Data-driven magic
- `Assets/Scripts/Data/Save/ShieldBuffSaveMapper.cs`
- `Assets/Scripts/Data/Save/SliceSaveData.cs`
- `Assets/Scripts/Data/Save/SpellCooldownSaveMapper.cs`
- `Assets/Scripts/Domain/Magic/MagicSchool.cs`
- `Assets/Scripts/Domain/Magic/ShieldBuffState.cs`
- `Assets/Scripts/Domain/Magic/ShieldBuffStateRegistry.cs`
- `Assets/Scripts/Domain/Magic/SpellCooldownState.cs`
- `Assets/Scripts/Domain/Magic/SpellDefinition.cs`
- `Assets/Scripts/Domain/Magic/SpellEffectKind.cs`
- `Assets/Scripts/Domain/Magic/SpellEffectSpec.cs`
- `Assets/Scripts/Domain/Magic/SpellTargetKind.cs`
- `Assets/Scripts/Simulation/Magic/MagicTickDriver.cs`
- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`
- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotalsPartition.cs`
- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionResult.cs`
- `Assets/Scripts/Simulation/Magic/ShieldBuffApplicationResult.cs`
- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`
- `Assets/Scripts/Simulation/Magic/SliceSpellCatalog.cs`
- `Assets/Scripts/Simulation/Magic/SpellCastError.cs`
- `Assets/Scripts/Simulation/Magic/SpellCastResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellCastRollError.cs`
- `Assets/Scripts/Simulation/Magic/SpellCastRollResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellCastRollService.cs`
- `Assets/Scripts/Simulation/Magic/SpellCastingService.cs`
- `Assets/Scripts/Simulation/Magic/SpellCooldownService.cs`
- `Assets/Scripts/Simulation/Magic/SpellCostCalculator.cs`
- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionError.cs`
- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs`
- `Assets/Scripts/Simulation/Magic/SpellExecutionError.cs`
- `Assets/Scripts/Simulation/Magic/SpellExecutionResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellExecutionService.cs`
- `Assets/Scripts/Simulation/Magic/SpellSuccessChanceError.cs`
- `Assets/Scripts/Simulation/Magic/SpellSuccessChanceResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellSuccessChanceService.cs`
- `Assets/Scripts/Simulation/Magic/SpellTargetValidationError.cs`
- `Assets/Scripts/Simulation/Magic/SpellTargetValidationResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellTargetValidator.cs`
- `Assets/Tests/EditMode/Magic/MagicTickDriverRegistryTests.cs`
- `Assets/Tests/EditMode/Magic/MagicTickDriverTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffActorKeyedApplicationServiceTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffApplicationServiceTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceAbsorptionTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsFromStackedFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyStackedFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByStackedFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyStackedFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionFromStackedFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionManyFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionManyTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceRegistryAbsorptionTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceRegistryBatchAbsorptionTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceRegistrySweepTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffStateRegistryTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffStateTests.cs`
- `Assets/Tests/EditMode/Magic/SliceSpellCatalogTests.cs`
- `Assets/Tests/EditMode/Magic/SpellCastRollServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellCastingServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellCooldownServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellCostCalculatorTests.cs`
- `Assets/Tests/EditMode/Magic/SpellDefinitionTests.cs`
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellExecutionServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellSuccessChanceServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellTargetValidatorTests.cs`
- `Assets/Tests/EditMode/Save/JsonSliceSaveServiceTests.cs`
- `Assets/Tests/EditMode/Save/ShieldBuffSaveMapperTests.cs`
- `Assets/Tests/EditMode/Save/SpellCooldownSaveMapperTests.cs`
- `Assets/Tests/EditMode/Time/GameTimeAdvanceSystemTests.cs`

### Faz 9 Dialogue + Memory + Faction reputation
- `Assets/Scripts/Data/Save/SliceSaveData.cs`
- `Assets/Scripts/Data/Save/SliceSaveMapper.cs`
- `Assets/Scripts/Domain/Actors/ActorRecord.cs`
- `Assets/Scripts/Domain/Memory/ActorMemory.cs`
- `Assets/Scripts/Domain/Memory/ActorMemoryEventTypes.cs`
- `Assets/Scripts/Domain/Memory/InteractionEvent.cs`
- `Assets/Scripts/Domain/Memory/NpcMemoryStore.cs`
- `Assets/Scripts/Domain/Memory/TransactionRecord.cs`
- `Assets/Scripts/Domain/Narrative/AskAboutTopic.cs`
- `Assets/Scripts/Simulation/Narrative/AskAboutService.cs`
- `Assets/Scripts/Simulation/Narrative/AskDmService.cs`
- `Assets/Scripts/Simulation/Narrative/GuardInteractionService.cs`
- `Assets/Scripts/Simulation/Narrative/NpcMemoryQueryService.cs`
- `Assets/Scripts/Simulation/Narrative/ThinkService.cs`
- `Assets/Tests/EditMode/Narrative/GuardInteractionServiceTests.cs`
- `Assets/Tests/EditMode/Narrative/NarrativeShellTests.cs`
- `Assets/Tests/EditMode/Narrative/NpcMemoryQueryServiceTests.cs`
- `Assets/Tests/EditMode/Narrative/PersistentNpcMemoryTests.cs`
- `Assets/Tests/EditMode/World/Faz1AcceptanceReplayTests.cs`

### Faz 10 DM Query API
- `Assets/Scripts/Data/Save/SliceSaveData.cs`
- `Assets/Scripts/Data/Save/SliceSaveMapper.cs`
- `Assets/Scripts/Domain/World/ReasonTrace.cs`
- `Assets/Scripts/Domain/World/SliceWorldState.cs`
- `Assets/Scripts/Domain/World/WorldEvent.cs`
- `Assets/Scripts/Domain/World/WorldEventKind.cs`
- `Assets/Scripts/Domain/World/WorldEventLog.cs`
- `Assets/Scripts/Simulation/Combat/CombatMathService.cs`
- `Assets/Scripts/Simulation/Magic/SpellCastRollError.cs`
- `Assets/Scripts/Simulation/Magic/SpellCastRollResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellCastRollService.cs`
- `Assets/Scripts/Simulation/Magic/SpellExecutionResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellSuccessChanceError.cs`
- `Assets/Scripts/Simulation/Magic/SpellSuccessChanceResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellSuccessChanceService.cs`
- `Assets/Scripts/Simulation/Narrative/NpcMemoryQueryService.cs`
- `Assets/Scripts/Simulation/Rng/XorShiftRng.cs`
- `Assets/Tests/EditMode/Actors/ActorRecordTests.cs`
- `Assets/Tests/EditMode/Combat/BodyPartSelectorTests.cs`
- `Assets/Tests/EditMode/Combat/CombatMathServiceTests.cs`
- `Assets/Tests/EditMode/Combat/EncounterTurnServiceTests.cs`
- `Assets/Tests/EditMode/Combat/RealtimeDamageServiceTests.cs`
- `Assets/Tests/EditMode/Inventory/EquipmentServiceTests.cs`
- `Assets/Tests/EditMode/Living/NeedRecoverySystemEatTests.cs`
- `Assets/Tests/EditMode/Living/NeedRecoverySystemSleepTests.cs`
- `Assets/Tests/EditMode/Living/NeedsEventLogTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyStackedFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyStackedFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionManyFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionManyTests.cs`
- `Assets/Tests/EditMode/Magic/SpellCastRollServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellDefinitionTests.cs`
- `Assets/Tests/EditMode/Magic/SpellExecutionServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellSuccessChanceServiceTests.cs`
- `Assets/Tests/EditMode/Narrative/NpcMemoryQueryServiceTests.cs`
- `Assets/Tests/EditMode/Process/JobEventLogTests.cs`
- `Assets/Tests/EditMode/Process/RecipeEventLogTests.cs`
- `Assets/Tests/EditMode/World/ReasonTraceTests.cs`
- `Assets/Tests/EditMode/World/SliceWorldStateActorViewTests.cs`
- `Assets/Tests/EditMode/World/WorldEventLogTests.cs`
- `Assets/Tests/EditMode/World/WorldEventTests.cs`

### Faz 11 Unity visual layer
- `Assets/Scripts/Domain/World/SliceWorldState.cs`
- `Assets/Tests/EditMode/Presentation/InventoryEquipmentFormatterTests.cs`
- `Assets/Tests/EditMode/Presentation/SliceAtmosphereSelectorTests.cs`
- `Assets/Tests/EditMode/World/ReasonTraceTests.cs`
- `Assets/Tests/EditMode/World/SliceWorldStateActorViewTests.cs`
- `Assets/Tests/EditMode/World/WorldEventLogTests.cs`

### Faz 12 LLM / NPC fallback flavour
- `Assets/Scripts/Data/Save/SliceSaveData.cs`
- `Assets/Scripts/Data/Save/SliceSaveMapper.cs`
- `Assets/Scripts/Domain/Core/GameTime.cs`
- `Assets/Scripts/Domain/Memory/NpcMemoryStore.cs`
- `Assets/Scripts/Simulation/Living/NeedMoodEvaluator.cs`
- `Assets/Scripts/Simulation/Magic/ShieldBuffApplicationResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionResult.cs`
- `Assets/Scripts/Simulation/Narrative/AskDmService.cs`
- `Assets/Scripts/Simulation/Narrative/NpcMemoryQueryService.cs`
- `Assets/Scripts/Simulation/Narrative/ThinkService.cs`
- `Assets/Tests/EditMode/Actors/ActorRecordMoodTests.cs`
- `Assets/Tests/EditMode/Core/GameTimeTests.cs`
- `Assets/Tests/EditMode/Living/NeedMoodEvaluatorTests.cs`
- `Assets/Tests/EditMode/Living/NeedsSystemMoodTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyStackedFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionManyFilterTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionManyTests.cs`
- `Assets/Tests/EditMode/Magic/ShieldBuffStateTests.cs`
- `Assets/Tests/EditMode/Magic/SpellCastingServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellExecutionServiceTests.cs`
- `Assets/Tests/EditMode/Magic/SpellSuccessChanceServiceTests.cs`
- `Assets/Tests/EditMode/Narrative/NarrativeShellTests.cs`
- `Assets/Tests/EditMode/Narrative/NpcMemoryQueryServiceTests.cs`
- `Assets/Tests/EditMode/Narrative/PersistentNpcMemoryTests.cs`
- `Assets/Tests/EditMode/Process/NeedRecoveryRecipeTests.cs`
- `Assets/Tests/EditMode/Process/PlantingSystemTests.cs`
- `Assets/Tests/EditMode/Save/ActorNeedsRoundTripTests.cs`

## File tree

### `Assets/Scripts/Data/Save`

#### `ActorSaveMapper.cs`
- namespace: `EmberCrpg.Data.Save`
- types:
  - L13: `class ActorSaveMapper`
- members:
  - L15: `public static ActorSaveData ToData(ActorRecord actor) => ToSave(actor);`
  - L17: `public static ActorRecord ToActor(ActorSaveData data) => FromSave(data);`
  - L19: `public static ActorSaveData ToSave(ActorRecord actor)`
  - L63: `public static ActorRecord FromSave(ActorSaveData save)`

#### `DungeonSaveMapper.cs`
- namespace: `EmberCrpg.Data.Save`
- types:
  - L13: `class DungeonSaveMapper`
- members:
  - L15: `public static DungeonRoomSaveData[] ToRoomData(GeneratedDungeonLayout dungeon)`
  - L29: `public static DungeonDoorSaveData[] ToDoorData(GeneratedDungeonLayout dungeon)`
  - L45: `public static DungeonSpawnSaveData[] ToSpawnData(GeneratedDungeonLayout dungeon)`
  - L56: `public static GeneratedDungeonLayout ToLayout(int seed, int startRoomId, DungeonRoomSaveData[] rooms, DungeonDoorSaveData[] doors, DungeonSpawnSaveData[] spawns)`
  - L65: `public static DungeonRoomStateSaveData[] ToRoomStateData(System.Collections.Generic.IEnumerable<DungeonRoomState> states)`
  - L75: `public static DungeonDoorStateSaveData[] ToDoorStateData(System.Collections.Generic.IEnumerable<DungeonDoorState> states)`
  - L84: `public static System.Collections.Generic.List<DungeonRoomState> ToRoomStates(DungeonRoomStateSaveData[] data)`
  - L89: `public static System.Collections.Generic.List<DungeonDoorState> ToDoorStates(DungeonDoorStateSaveData[] data)`
  - L94: `private static DungeonRoom ToRoom(DungeonRoomSaveData data)`
  - L108: `private static DungeonDoor ToDoor(DungeonDoorSaveData data)`
  - L122: `private static DungeonSpawnPoint ToSpawn(DungeonSpawnSaveData data)`

#### `ItemSaveMapper.cs`
- namespace: `EmberCrpg.Data.Save`
- types:
  - L13: `class ItemSaveMapper`
- members:
  - L15: `public static ItemSaveData ToData(InventoryItem item)`
  - L29: `public static InventoryItem ToItem(ItemSaveData item)`
  - L34: `public static PickupSaveData ToData(RoomPickup pickup)`
  - L45: `public static RoomPickup ToPickup(PickupSaveData pickup)`

#### `JsonSliceSaveService.cs`
- namespace: `EmberCrpg.Data.Save`
- types:
  - L17: `class JsonSliceSaveService`
- members:
  - L46: `public void ReplaceRecipeWorkOrders(IEnumerable<RecipeWorkOrder> orders)`
  - L53: `public string SaveToJson(SliceWorldState world)`
  - L62: `public SliceWorldState LoadFromJson(string json)`
  - L72: `private List<RecipeWorkOrder> ToRecipeWorkOrders(RecipeWorkOrderSaveData[] data)`

#### `ShieldBuffSaveMapper.cs`
- namespace: `EmberCrpg.Data.Save`
- types:
  - L13: `class ShieldBuffSaveMapper`
- members:
  - L15: `public static ShieldBuffSaveData ToData(ShieldBuffState state)`
  - L33: `public static ShieldBuffState ToState(ShieldBuffSaveData data)`

#### `SliceSaveData.cs`
- namespace: `EmberCrpg.Data.Save`
- types:
  - L11: `class SliceSaveData`
  - L57: `class ItemRecordSaveData`
  - L66: `class SiteRecordSaveData`
  - L78: `class WorksiteSaveData`
  - L88: `class RecipeWorkOrderSaveData`
  - L99: `class FactionRecordSaveData`
  - L107: `class WorldEventSaveData`
  - L118: `class EquipmentSaveData`
  - L124: `class EquippedItemSaveData`
  - L131: `class DungeonRoomSaveData`
  - L143: `class DungeonDoorSaveData`
  - L157: `class DungeonSpawnSaveData`
  - L166: `class DungeonRoomStateSaveData`
  - L174: `class DungeonDoorStateSaveData`
  - L181: `class ActorSaveData`
  - L220: `class ActorJobPreferenceSaveData`
  - L227: `class JobRequestSaveData`
  - L243: `class InventorySaveData`
  - L250: `class ItemSaveData`
  - L262: `class PickupSaveData`
  - L271: `class TopicSaveData`
  - L279: `class NpcMemorySaveData`
  - L288: `class InteractionEventSaveData`
  - L301: `class TransactionSaveData`
  - L311: `class SpellCooldownSaveData`
  - L317: `class SpellCooldownEntrySaveData`
  - L324: `class ShieldBuffSaveData`
  - L330: `class ShieldBuffEntrySaveData`

#### `SliceSaveMapper.cs`
- namespace: `EmberCrpg.Data.Save`
- types:
  - L21: `class SliceSaveMapper`
- members:
  - L23: `public static SliceSaveData ToData(SliceWorldState world)`
  - L68: `public static SliceWorldState ToWorld(SliceSaveData data)`
  - L118: `public static WorksiteSaveData[] ToWorksiteData(WorksiteStore store)`
  - L123: `public static WorksiteStore ToWorksiteStore(WorksiteSaveData[] data)`
  - L135: `public static RecipeWorkOrderSaveData ToRecipeWorkOrderData(RecipeWorkOrder order)`
  - L151: `public static RecipeWorkOrder ToRecipeWorkOrder(RecipeWorkOrderSaveData data, Func<RecipeId, RecipeDef> resolveRecipe)`
  - L171: `public static JobRequestSaveData[] ToJobBoardData(JobBoard board)`
  - L176: `public static JobBoard ToJobBoard(JobRequestSaveData[] data)`
  - L204: `private static JobRequestSaveData ToJobRequestData(JobRequest request, JobBoard board)`
  - L222: `private static ActorSaveData[] ToActorStoreData(ActorStore store)`
  - L227: `private static ActorStore ToActorStore(ActorSaveData[] data)`
  - L238: `private static ItemRecordSaveData[] ToItemStoreData(ItemStore store)`
  - L243: `private static ItemRecordSaveData ToItemRecordData(ItemRecord record)`
  - L254: `private static ItemStore ToItemStore(ItemRecordSaveData[] data)`
  - L265: `private static SiteRecordSaveData[] ToSiteStoreData(SiteStore store)`
  - L270: `private static SiteRecordSaveData ToSiteRecordData(SiteRecord record)`
  - L284: `private static SiteStore ToSiteStore(SiteRecordSaveData[] data)`
  - L302: `private static WorksiteSaveData ToWorksiteData(WorksiteRecord record)`
  - L314: `private static WorksiteRecord ToWorksiteRecord(WorksiteSaveData data)`
  - L323: `private static FactionRecordSaveData[] ToFactionStoreData(FactionStore store)`
  - L328: `private static FactionRecordSaveData ToFactionRecordData(FactionRecord record)`
  - L338: `private static FactionStore ToFactionStore(FactionRecordSaveData[] data)`
  - L349: `private static WorldEventSaveData[] ToWorldEventLogData(WorldEventLog log)`
  - L354: `private static WorldEventSaveData ToWorldEventData(WorldEvent worldEvent)`
  - L367: `private static WorldEventLog ToWorldEventLog(WorldEventSaveData[] data)`
  - L386: `private static ReasonTrace ToReasonTrace(string[] causes)`
  - L391: `private static EquipmentSaveData ToEquipmentData(EquipmentState equipment)`
  - L402: `private static EquipmentState ToEquipmentState(EquipmentSaveData data)`
  - L410: `private static NpcMemorySaveData[] ToNpcMemoryData(NpcMemoryStore store)`
  - L421: `private static NpcMemoryStore ToNpcMemoryStore(NpcMemorySaveData[] data)`
  - L428: `private static ActorMemory ToActorMemory(NpcMemorySaveData data)`
  - L437: `private static InteractionEventSaveData ToInteractionEventData(InteractionEvent interactionEvent)`
  - L452: `private static InteractionEvent ToInteractionEvent(InteractionEventSaveData data)`
  - L464: `private static TransactionSaveData ToTransactionData(TransactionRecord transaction)`
  - L476: `private static TransactionRecord ToTransaction(TransactionSaveData data)`
  - L486: `private static InventorySaveData ToInventoryData(InventoryState inventory)`
  - L495: `private static InventoryState ToInventoryState(InventorySaveData inventory, int fallbackCapacity)`

#### `SpellCooldownSaveMapper.cs`
- namespace: `EmberCrpg.Data.Save`
- types:
  - L13: `class SpellCooldownSaveMapper`
- members:
  - L15: `public static SpellCooldownSaveData ToData(SpellCooldownState state)`
  - L32: `public static SpellCooldownState ToState(SpellCooldownSaveData data)`

### `Assets/Scripts/Domain/Actors`

#### `ActorJobPreference.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L15: `struct ActorJobPreference`
- members:
  - L27: `public JobKind Kind { get; }`
  - L30: `public JobPriority Priority { get; }`
  - L36: `public static ActorJobPreference Disabled(JobKind kind)`
  - L42: `public bool Equals(ActorJobPreference other)`
  - L48: `public override bool Equals(object obj)`
  - L54: `public override int GetHashCode()`
  - L60: `public override string ToString()`

#### `ActorMood.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L12: `struct ActorMood`
- members:
  - L51: `public bool IsAtMost(ActorMood threshold)`
  - L56: `public int CompareTo(ActorMood other)`
  - L61: `public bool Equals(ActorMood other)`
  - L66: `public override bool Equals(object obj)`
  - L71: `public override int GetHashCode()`
  - L76: `public override string ToString()`
  - L101: `private static int Clamp(int value)`

#### `ActorNeeds.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L11: `struct ActorNeeds`
- members:
  - L25: `public NeedValue Hunger { get; }`
  - L26: `public NeedValue Fatigue { get; }`
  - L27: `public NeedValue Thirst { get; }`
  - L29: `public NeedValue Get(NeedKind kind)`
  - L44: `public ActorNeeds With(NeedKind kind, NeedValue value)`
  - L59: `public ActorNeeds WithHunger(NeedValue hunger)`
  - L64: `public ActorNeeds WithFatigue(NeedValue fatigue)`
  - L69: `public ActorNeeds WithThirst(NeedValue thirst)`
  - L74: `public bool Equals(ActorNeeds other)`
  - L81: `public override bool Equals(object obj)`
  - L86: `public override int GetHashCode()`
  - L91: `public override string ToString()`

#### `ActorRecord.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L15: `class ActorRecord`
- members:
  - L57: `public ActorId Id { get; }`
  - L58: `public string Name { get; }`
  - L59: `public ActorRole Role { get; }`
  - L60: `public EmberStatBlock Stats { get; }`
  - L61: `public GridPosition Position { get; private set; }`
  - L62: `public ActorVitals Vitals { get; private set; }`
  - L63: `public int Accuracy { get; }`
  - L64: `public int Dodge { get; }`
  - L65: `public int Armor { get; }`
  - L66: `public int BaseDamage { get; }`
  - L71: `public ActorScheduleState ScheduleState { get; private set; }`
  - L72: `public ActorNeeds Needs { get; private set; }`
  - L73: `public ActorMood Mood { get; private set; }`
  - L75: `public void MoveTo(GridPosition position)`
  - L80: `public void ApplyVitals(ActorVitals vitals)`
  - L85: `public void RecordTopic(string topicId)`
  - L91: `public void ReplaceAskedTopics(IEnumerable<string> topicIds)`
  - L103: `public void ApplyJobPreferences(IEnumerable<ActorJobPreference> preferences)`
  - L125: `public void ApplyScheduleState(ActorScheduleState scheduleState)`
  - L130: `public void ApplyNeeds(ActorNeeds needs)`
  - L135: `public void ApplyMood(ActorMood mood)`

#### `ActorRole.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L9: `enum ActorRole`

#### `ActorScheduleState.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L16: `struct ActorScheduleState`
- members:
  - L32: `public static ActorScheduleState Assigned(JobId currentJobId, SiteId targetSiteId, GridPosition targetWorksitePosition)`
  - L43: `public JobId CurrentJobId { get; }`
  - L46: `public SiteId TargetSiteId { get; }`
  - L49: `public GridPosition TargetWorksitePosition { get; }`
  - L55: `public bool Equals(ActorScheduleState other)`
  - L63: `public override bool Equals(object obj)`
  - L69: `public override int GetHashCode()`
  - L75: `public override string ToString()`

#### `ActorVitals.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L9: `struct ActorVitals`
- members:
  - L18: `public VitalStat Health { get; }`
  - L19: `public VitalStat Fatigue { get; }`
  - L20: `public VitalStat Mana { get; }`
  - L23: `public ActorVitals WithHealth(VitalStat health)`
  - L28: `public ActorVitals WithFatigue(VitalStat fatigue)`
  - L33: `public ActorVitals WithMana(VitalStat mana)`

#### `EmberAttribute.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L9: `enum EmberAttribute`

#### `EmberStatBlock.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L11: `struct EmberStatBlock`
- members:
  - L26: `public int Mig { get; }`
  - L27: `public int Agi { get; }`
  - L28: `public int End { get; }`
  - L29: `public int Mnd { get; }`
  - L30: `public int Ins { get; }`
  - L31: `public int Pre { get; }`
  - L34: `public int Get(EmberAttribute attribute)`
  - L48: `private static int RequireRange(int value, string paramName)`

#### `GridPosition.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L11: `struct GridPosition`
- members:
  - L19: `public int X { get; }`
  - L20: `public int Y { get; }`
  - L22: `public GridPosition Translate(int deltaX, int deltaY)`
  - L27: `public int ManhattanDistanceTo(GridPosition other)`
  - L32: `public bool Equals(GridPosition other)`
  - L37: `public override bool Equals(object obj)`
  - L42: `public override int GetHashCode()`
  - L47: `public override string ToString()`

#### `NeedKind.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L9: `enum NeedKind`

#### `NeedValue.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L11: `struct NeedValue`
- members:
  - L31: `public int Value { get; }`
  - L33: `public bool IsAtLeast(NeedValue threshold)`
  - L38: `public NeedValue Increase(int amount)`
  - L50: `public NeedValue Decrease(int amount)`
  - L55: `public int CompareTo(NeedValue other)`
  - L60: `public bool Equals(NeedValue other)`
  - L65: `public override bool Equals(object obj)`
  - L70: `public override int GetHashCode()`
  - L75: `public override string ToString()`
  - L100: `private static int Clamp(int value)`

#### `VitalStat.cs`
- namespace: `EmberCrpg.Domain.Actors`
- types:
  - L11: `struct VitalStat`
- members:
  - L24: `public int Current { get; }`
  - L25: `public int Max { get; }`
  - L28: `public VitalStat Damage(int amount)`
  - L34: `public VitalStat Restore(int amount)`
  - L40: `public VitalStat Refill()`

### `Assets/Scripts/Domain/Combat`

#### `BodyPart.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L9: `enum BodyPart`

#### `BodyPartNode.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L11: `struct BodyPartNode`
- members:
  - L27: `public BodyPart Part { get; }`
  - L28: `public BodyPart? Parent { get; }`
  - L29: `public int SelectionWeight { get; }`
  - L30: `public int ArmorClassModifier { get; }`
  - L31: `public int DamageMultiplierPercent { get; }`

#### `CombatActionEvent.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L9: `class CombatActionEvent`
- members:
  - L18: `public QueuedCombatAction Action { get; }`
  - L19: `public CombatActionEventKind EventKind { get; }`
  - L20: `public double ElapsedSeconds { get; }`

#### `CombatActionEventKind.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L9: `enum CombatActionEventKind`

#### `CombatActionKind.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L9: `enum CombatActionKind`

#### `CombatDefenseIntent.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L9: `enum CombatDefenseIntent`

#### `CombatStrikeResult.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L9: `class CombatStrikeResult`

#### `EncounterState.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L12: `class EncounterState`
- members:
  - L23: `public ActorId PlayerId { get; }`
  - L24: `public ActorId EnemyId { get; }`
  - L25: `public bool PlayerActsNext { get; set; }`
  - L26: `public bool IsFinished { get; private set; }`
  - L27: `public string WinnerName { get; private set; }`
  - L30: `public void AddLog(string line)`
  - L35: `public void Finish(string winnerName)`

#### `QueuedCombatAction.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L12: `class QueuedCombatAction`
- members:
  - L43: `public int Sequence { get; }`
  - L44: `public ActorId ActorId { get; }`
  - L45: `public ActorId TargetActorId { get; }`
  - L46: `public CombatActionKind Kind { get; }`
  - L47: `public double RequestedAtSeconds { get; }`
  - L48: `public double StartAtSeconds { get; }`
  - L49: `public double WindupSeconds { get; }`
  - L50: `public double ActiveSeconds { get; }`
  - L51: `public double RecoverySeconds { get; }`
  - L54: `public bool IsActivated { get; private set; }`
  - L55: `public bool IsCompleted { get; private set; }`
  - L57: `public bool IsActiveAt(double elapsedSeconds)`
  - L62: `public void MarkActivated()`
  - L67: `public void MarkCompleted()`

#### `RealtimeDamageResult.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L9: `class RealtimeDamageResult`

#### `WeaponHitEvent.cs`
- namespace: `EmberCrpg.Domain.Combat`
- types:
  - L12: `struct WeaponHitEvent`
- members:
  - L35: `public ActorId AttackerId { get; }`
  - L36: `public ActorId DefenderId { get; }`
  - L37: `public string WeaponTag { get; }`
  - L38: `public BodyPart? ColliderBodyPart { get; }`
  - L39: `public int ActionSequence { get; }`
  - L40: `public int ImpactBonus { get; }`

### `Assets/Scripts/Domain/Core`

#### `ActorId.cs`
- namespace: `EmberCrpg.Domain.Core`
- types:
  - L11: `struct ActorId`
- members:
  - L42: `public bool Equals(ActorId other)`
  - L50: `public override bool Equals(object obj)`
  - L58: `public override int GetHashCode()`
  - L66: `public override string ToString()`

#### `FactionId.cs`
- namespace: `EmberCrpg.Domain.Core`
- types:
  - L13: `struct FactionId`
- members:
  - L44: `public bool Equals(FactionId other)`
  - L52: `public override bool Equals(object obj)`
  - L60: `public override int GetHashCode()`
  - L68: `public override string ToString()`

#### `GameTime.cs`
- namespace: `EmberCrpg.Domain.Core`
- types:
  - L9: `struct GameTime`
- members:
  - L37: `public long TotalMinutes { get { return _totalMinutes; } }`
  - L52: `public GameTime AddMinutes(long minutes) { return new GameTime(_totalMinutes + minutes); }`
  - L54: `public GameTime AddHours(long hours) { return AddMinutes(hours * MinutesPerHour); }`
  - L56: `public GameTime AddDays(long days) { return AddMinutes(days * MinutesPerDay); }`
  - L58: `public GameTime AddMonths(long months) { return AddMinutes(months * MinutesPerMonth); }`
  - L60: `public GameTime AddYears(long years) { return AddMinutes(years * MinutesPerYear); }`
  - L63: `public int CompareTo(GameTime other) { return _totalMinutes.CompareTo(other._totalMinutes); }`
  - L65: `public bool Equals(GameTime other) { return _totalMinutes == other._totalMinutes; }`
  - L67: `public override bool Equals(object obj) { return obj is GameTime other && Equals(other); }`
  - L69: `public override int GetHashCode() { return _totalMinutes.GetHashCode(); }`
  - L71: `public override string ToString() { return $"Year {Year} Day {DayOfYear} {Hour:00}:{Minute:00}"; }`

#### `ItemId.cs`
- namespace: `EmberCrpg.Domain.Core`
- types:
  - L11: `struct ItemId`
- members:
  - L42: `public bool Equals(ItemId other)`
  - L50: `public override bool Equals(object obj)`
  - L58: `public override int GetHashCode()`
  - L66: `public override string ToString()`

#### `SiteId.cs`
- namespace: `EmberCrpg.Domain.Core`
- types:
  - L12: `struct SiteId`
- members:
  - L43: `public bool Equals(SiteId other)`
  - L51: `public override bool Equals(object obj)`
  - L59: `public override int GetHashCode()`
  - L67: `public override string ToString()`

### `Assets/Scripts/Domain/Inventory`

#### `EquipmentSlot.cs`
- namespace: `EmberCrpg.Domain.Inventory`
- types:
  - L9: `enum EquipmentSlot`

#### `EquipmentState.cs`
- namespace: `EmberCrpg.Domain.Inventory`
- types:
  - L12: `class EquipmentState`
- members:
  - L16: `public ItemId GetEquippedItemId(EquipmentSlot slot)`
  - L21: `public bool IsEquipped(ItemId itemId)`
  - L26: `public void Equip(EquipmentSlot slot, ItemId itemId)`
  - L33: `public void Unequip(EquipmentSlot slot)`
  - L38: `public EquipmentState Clone()`

#### `InventoryItem.cs`
- namespace: `EmberCrpg.Domain.Inventory`
- types:
  - L12: `class InventoryItem`
- members:
  - L48: `public ItemId Id { get; }`
  - L49: `public string TemplateId { get; }`
  - L50: `public string DisplayName { get; }`
  - L51: `public int Quantity { get; private set; }`
  - L52: `public EquipmentSlot EquipmentSlot { get; }`
  - L53: `public int AccuracyBonus { get; }`
  - L54: `public int DamageBonus { get; }`
  - L57: `public void AddQuantity(int amount)`
  - L62: `public void RemoveQuantity(int amount)`
  - L67: `public InventoryItem Clone()`

#### `InventoryState.cs`
- namespace: `EmberCrpg.Domain.Inventory`
- types:
  - L12: `class InventoryState`
- members:
  - L21: `public int Capacity { get; }`
  - L25: `public bool TryAdd(InventoryItem item)`
  - L41: `public bool TryRemove(string templateId, int quantity, EquipmentState equipment = null)`
  - L56: `public bool TryRemoveStackable(string templateId, int quantity)`
  - L69: `public bool Contains(string templateId)`
  - L74: `public InventoryItem FindById(EmberCrpg.Domain.Core.ItemId itemId)`
  - L79: `public InventoryItem FindFirstEquipment(EquipmentSlot slot)`
  - L84: `public InventoryState Clone()`

#### `ItemMaterial.cs`
- namespace: `EmberCrpg.Domain.Inventory`
- types:
  - L10: `enum ItemMaterial`

#### `ItemQuality.cs`
- namespace: `EmberCrpg.Domain.Inventory`
- types:
  - L10: `enum ItemQuality`

#### `ItemRecord.cs`
- namespace: `EmberCrpg.Domain.Inventory`
- types:
  - L16: `class ItemRecord`
- members:
  - L33: `public ItemId Id { get; }`
  - L34: `public ItemMaterial Material { get; }`
  - L35: `public ItemQuality Quality { get; }`
  - L36: `public EquipmentSlot Slot { get; }`

#### `RoomPickup.cs`
- namespace: `EmberCrpg.Domain.Inventory`
- types:
  - L11: `class RoomPickup`
- members:
  - L19: `public InventoryItem Item { get; }`
  - L20: `public GridPosition Position { get; }`
  - L21: `public bool IsCollected { get; private set; }`
  - L23: `public void Collect()`

### `Assets/Scripts/Domain/Magic`

#### `MagicSchool.cs`
- namespace: `EmberCrpg.Domain.Magic`
- types:
  - L9: `enum MagicSchool`

#### `ShieldBuffState.cs`
- namespace: `EmberCrpg.Domain.Magic`
- types:
  - L15: `class ShieldBuffState`
  - L82: `struct ShieldBuffEntry`
- members:
  - L20: `public int GetRemainingTicks(string spellTemplateId)`
  - L30: `public int GetMagnitude(string spellTemplateId)`
  - L40: `public bool IsActive(string spellTemplateId)`
  - L45: `public IReadOnlyList<string> GetTrackedSpellTemplateIds()`
  - L56: `public void SetActiveBuff(string spellTemplateId, int remainingTicks, int magnitude)`
  - L74: `public void Clear(string spellTemplateId)`
  - L90: `public int RemainingTicks { get; }`
  - L91: `public int Magnitude { get; }`

#### `ShieldBuffStateRegistry.cs`
- namespace: `EmberCrpg.Domain.Magic`
- types:
  - L17: `class ShieldBuffStateRegistry`
- members:
  - L22: `public bool HasState(string actorId)`
  - L30: `public ShieldBuffState GetOrCreate(string actorId)`
  - L44: `public ShieldBuffState GetOrNull(string actorId)`
  - L54: `public IReadOnlyList<string> GetTrackedActorIds()`
  - L65: `public void Remove(string actorId)`

#### `SpellCooldownState.cs`
- namespace: `EmberCrpg.Domain.Magic`
- types:
  - L14: `class SpellCooldownState`
- members:
  - L18: `public int GetRemainingTicks(string spellTemplateId)`
  - L28: `public IReadOnlyList<string> GetTrackedSpellTemplateIds()`
  - L39: `public void SetRemainingTicks(string spellTemplateId, int remainingTicks)`

#### `SpellDefinition.cs`
- namespace: `EmberCrpg.Domain.Magic`
- types:
  - L14: `class SpellDefinition`
- members:
  - L91: `public string TemplateId { get; }`
  - L92: `public string DisplayName { get; }`
  - L93: `public MagicSchool School { get; }`
  - L94: `public SpellTargetKind TargetKind { get; }`
  - L95: `public int ManaCost { get; }`
  - L97: `public int RangeInTiles { get; }`
  - L99: `public int CooldownTicks { get; }`
  - L102: `private static SpellEffectSpec[] ToArray(IEnumerable<SpellEffectSpec> effects)`

#### `SpellEffectKind.cs`
- namespace: `EmberCrpg.Domain.Magic`
- types:
  - L10: `enum SpellEffectKind`

#### `SpellEffectSpec.cs`
- namespace: `EmberCrpg.Domain.Magic`
- types:
  - L12: `struct SpellEffectSpec`
- members:
  - L28: `public SpellEffectKind Kind { get; }`
  - L29: `public int Magnitude { get; }`
  - L30: `public int DurationTicks { get; }`

#### `SpellTargetKind.cs`
- namespace: `EmberCrpg.Domain.Magic`
- types:
  - L9: `enum SpellTargetKind`

### `Assets/Scripts/Domain/Memory`

#### `ActorMemory.cs`
- namespace: `EmberCrpg.Domain.Memory`
- types:
  - L13: `class ActorMemory`
- members:
  - L26: `public ActorId ActorId { get; }`
  - L31: `public void RecordEvent(InteractionEvent interactionEvent)`
  - L38: `public void MarkDialogueSeen(string topicId)`
  - L44: `public bool HasDialogueSeen(string topicId)`
  - L49: `public int CountEvents(string eventType)`
  - L54: `public void RecordTransaction(TransactionRecord transaction)`
  - L59: `public void ReplaceEvents(IEnumerable<InteractionEvent> events)`
  - L69: `public void ReplaceDialogueSeen(IEnumerable<string> topicIds)`
  - L79: `public void ReplaceTransactions(IEnumerable<TransactionRecord> transactions)`

#### `ActorMemoryEventTypes.cs`
- namespace: `EmberCrpg.Domain.Memory`
- types:
  - L9: `class ActorMemoryEventTypes`

#### `InteractionEvent.cs`
- namespace: `EmberCrpg.Domain.Memory`
- types:
  - L12: `struct InteractionEvent`
- members:
  - L32: `public GameTime Timestamp { get; }`
  - L33: `public string EventType { get; }`
  - L34: `public ActorId ActorSeen { get; }`
  - L35: `public string SubjectId { get; }`
  - L36: `public string ItemTemplateId { get; }`
  - L37: `public int Amount { get; }`
  - L38: `public GridPosition Location { get; }`

#### `NpcMemoryStore.cs`
- namespace: `EmberCrpg.Domain.Memory`
- types:
  - L13: `class NpcMemoryStore`
- members:
  - L19: `public ActorMemory GetOrCreate(ActorId actorId)`
  - L30: `public bool TryGet(ActorId actorId, out ActorMemory memory)`
  - L35: `public IReadOnlyList<ActorMemory> GetAllSorted()`
  - L40: `public void ReplaceAll(IEnumerable<ActorMemory> memories)`

#### `TransactionRecord.cs`
- namespace: `EmberCrpg.Domain.Memory`
- types:
  - L11: `struct TransactionRecord`
- members:
  - L22: `public GameTime Timestamp { get; }`
  - L23: `public string TransactionType { get; }`
  - L24: `public string ItemTemplateId { get; }`
  - L25: `public int Count { get; }`
  - L26: `public int GoldDelta { get; }`

### `Assets/Scripts/Domain/Narrative`

#### `AskAboutTopic.cs`
- namespace: `EmberCrpg.Domain.Narrative`
- types:
  - L11: `class AskAboutTopic`
- members:
  - L27: `public string Id { get; }`
  - L28: `public string Label { get; }`
  - L29: `public string Answer { get; }`

### `Assets/Scripts/Domain/Process`

#### `JobBoard.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L18: `class JobBoard`
  - L161: `class Entry`
- members:
  - L27: `public void Add(JobRequest request)`
  - L39: `public bool Contains(JobId id)`
  - L45: `public bool TryGet(JobId id, out JobRequest request)`
  - L60: `public bool TryPeekNext(out JobRequest request)`
  - L96: `public bool TryClaim(JobId id, ActorId actorId, out JobRequest request)`
  - L112: `public bool IsClaimed(JobId id)`
  - L118: `public ActorId GetClaimedBy(JobId id)`
  - L124: `public bool Complete(JobId id)`
  - L130: `public bool Cancel(JobId id)`
  - L136: `public void Clear()`
  - L152: `private bool Remove(JobId id)`
  - L168: `public JobRequest Request { get; }`
  - L170: `public ActorId ClaimedBy { get; set; }`

#### `JobId.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L14: `struct JobId`
- members:
  - L45: `public bool Equals(JobId other)`
  - L53: `public override bool Equals(object obj)`
  - L61: `public override int GetHashCode()`
  - L69: `public override string ToString()`

#### `JobKind.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L13: `enum JobKind`

#### `JobPriority.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L14: `struct JobPriority`
- members:
  - L34: `public static JobPriority Active(int value)`
  - L61: `public bool Equals(JobPriority other)`
  - L69: `public override bool Equals(object obj)`
  - L77: `public override int GetHashCode()`
  - L86: `public int CompareTo(JobPriority other)`
  - L101: `public override string ToString()`

#### `JobRequest.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L18: `class JobRequest`
- members:
  - L60: `public JobId Id { get; }`
  - L63: `public RecipeId RecipeId { get; }`
  - L66: `public SiteId SiteId { get; }`
  - L69: `public GridPosition WorksitePosition { get; }`
  - L72: `public WorksiteKind WorksiteKind { get; }`
  - L75: `public JobKind Kind { get; }`
  - L78: `public JobPriority Priority { get; }`
  - L81: `public int Quantity { get; }`
  - L84: `public ActorId RequesterId { get; }`

#### `NeedRecoveryRecipe.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L13: `class NeedRecoveryRecipe`
- members:
  - L35: `public string Id { get; }`
  - L36: `public string ActionKind { get; }`
  - L37: `public NeedKind NeedKind { get; }`
  - L38: `public int RecoveryAmount { get; }`
  - L39: `public string ConsumedItemTemplateId { get; }`

#### `PlantComponent.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L9: `class PlantComponent`
- members:
  - L38: `public WorldComponentId Id { get; }`
  - L39: `public SiteId SiteId { get; }`
  - L40: `public GridPosition Position { get; }`
  - L41: `public string SpeciesId { get; }`
  - L42: `public PlantStageId StageId { get; }`
  - L43: `public int DaysInStage { get; }`
  - L45: `public PlantComponent WithStage(PlantStageId stageId)`
  - L50: `public PlantComponent WithDaysInStage(int daysInStage)`

#### `PlantGrowthRule.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L6: `class PlantGrowthRule`
- members:
  - L15: `public Season Season { get; }`
  - L16: `public bool AllowsGrowth { get; }`
  - L17: `public bool BlockedBySnow { get; }`
  - L19: `public bool Matches(Season season)`
  - L24: `public bool CanGrow(bool isSnowing)`

#### `PlantGrowthStageDef.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L6: `class PlantGrowthStageDef`
- members:
  - L23: `public PlantStageId Id { get; }`
  - L24: `public string DisplayName { get; }`
  - L25: `public int DaysToNextStage { get; }`
  - L26: `public bool IsHarvestable { get; }`

#### `PlantSpeciesDef.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L10: `class PlantSpeciesDef`
- members:
  - L54: `public string SpeciesId { get; }`
  - L55: `public string SeedItemTag { get; }`
  - L56: `public string HarvestItemTag { get; }`
  - L57: `public IReadOnlyList<PlantGrowthStageDef> Stages { get { return _stages; } }`
  - L58: `public IReadOnlyList<PlantGrowthRule> GrowthRules { get { return _growthRules; } }`
  - L59: `public PlantGrowthStageDef FirstStage { get { return _stages[0]; } }`
  - L61: `public bool TryGetStage(PlantStageId stageId, out PlantGrowthStageDef stage)`
  - L76: `public bool TryGetNextStage(PlantStageId currentStageId, out PlantGrowthStageDef nextStage)`
  - L91: `public bool CanGrow(Season season, bool isSnowing)`

#### `PlantStageId.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L6: `struct PlantStageId`
- members:
  - L17: `public string Value { get { return _value ?? string.Empty; } }`
  - L20: `public bool Equals(PlantStageId other)`
  - L25: `public override bool Equals(object obj)`
  - L30: `public override int GetHashCode()`
  - L35: `public override string ToString()`

#### `RecipeDef.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L17: `class RecipeDef`
- members:
  - L50: `public RecipeId Id { get; }`
  - L55: `public string WorksiteKind { get; }`
  - L60: `public string SkillTag { get; }`
  - L65: `public int DurationTicks { get; }`

#### `RecipeId.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L14: `struct RecipeId`
- members:
  - L45: `public bool Equals(RecipeId other)`
  - L53: `public override bool Equals(object obj)`
  - L61: `public override int GetHashCode()`
  - L69: `public override string ToString()`

#### `RecipeIngredient.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L14: `class RecipeIngredient`
- members:
  - L30: `public string ItemTag { get; }`
  - L35: `public int Quantity { get; }`

#### `RecipeOutput.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L16: `class RecipeOutput`
- members:
  - L38: `public string ItemTag { get; }`
  - L43: `public ItemMaterial Material { get; }`
  - L48: `public ItemQuality Quality { get; }`
  - L53: `public int Quantity { get; }`

#### `SoilComponent.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L13: `class SoilComponent`
- members:
  - L36: `public WorldComponentId Id { get; }`
  - L37: `public SiteId SiteId { get; }`
  - L38: `public GridPosition Position { get; }`
  - L39: `public int Fertility { get; }`
  - L40: `public int Moisture { get; }`
  - L41: `public WorldComponentId PlantId { get; }`
  - L42: `public bool HasPlant { get { return !PlantId.IsEmpty; } }`
  - L44: `public SoilComponent WithMoisture(int moisture)`
  - L49: `public SoilComponent WithPlant(WorldComponentId plantId)`
  - L56: `public SoilComponent WithoutPlant()`
  - L61: `private static int ClampPercent(int value)`

#### `WorldProcessDef.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L6: `class WorldProcessDef`
- members:
  - L25: `public WorldProcessId Id { get; }`
  - L26: `public string DisplayName { get; }`
  - L27: `public int DurationDays { get; }`
  - L28: `public string OutputEventReason { get; }`

#### `WorldProcessId.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L6: `struct WorldProcessId`
- members:
  - L17: `public string Value { get { return _value ?? string.Empty; } }`
  - L18: `public bool IsEmpty { get { return string.IsNullOrEmpty(Value); } }`
  - L20: `public bool Equals(WorldProcessId other)`
  - L25: `public override bool Equals(object obj)`
  - L30: `public override int GetHashCode()`
  - L35: `public override string ToString()`

#### `WorldProcessInstance.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L8: `class WorldProcessInstance`
- members:
  - L31: `public WorldComponentId Id { get; }`
  - L32: `public WorldProcessDef Definition { get; }`
  - L33: `public SiteId SiteId { get; }`
  - L34: `public WorldComponentId SubjectId { get; }`
  - L35: `public int ElapsedDays { get; }`
  - L36: `public int RemainingDays { get { return Definition.DurationDays - ElapsedDays; } }`
  - L37: `public bool IsComplete { get { return ElapsedDays >= Definition.DurationDays; } }`
  - L39: `public WorldProcessInstance AdvanceOneDay()`

#### `WorksiteKind.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L12: `enum WorksiteKind`

#### `WorksiteRecord.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L16: `class WorksiteRecord`
- members:
  - L32: `public SiteId SiteId { get; }`
  - L35: `public GridPosition Position { get; }`
  - L38: `public WorksiteKind Kind { get; }`
  - L41: `public bool IsActive { get; }`
  - L46: `public WorksiteRecord WithActive(bool isActive)`

#### `WorksiteStore.cs`
- namespace: `EmberCrpg.Domain.Process`
- types:
  - L19: `class WorksiteStore`
  - L118: `struct WorksiteKey`
- members:
  - L31: `public void Add(WorksiteRecord record)`
  - L45: `public WorksiteRecord Get(SiteId siteId, GridPosition position)`
  - L57: `public bool TryGet(SiteId siteId, GridPosition position, out WorksiteRecord record)`
  - L69: `public bool Contains(SiteId siteId, GridPosition position)`
  - L78: `public bool Remove(SiteId siteId, GridPosition position)`
  - L92: `public void Clear()`
  - L111: `private static WorksiteKey MakeKey(SiteId siteId, GridPosition position, string paramName)`
  - L126: `public SiteId SiteId { get; }`
  - L128: `public GridPosition Position { get; }`
  - L130: `public bool Equals(WorksiteKey other)`
  - L135: `public override bool Equals(object obj)`
  - L140: `public override int GetHashCode()`

### `Assets/Scripts/Domain/Time`

#### `Season.cs`
- namespace: `EmberCrpg.Domain.Time`
- types:
  - L4: `enum Season`

#### `SeasonCalendar.cs`
- namespace: `EmberCrpg.Domain.Time`
- types:
  - L9: `class SeasonCalendar`
- members:
  - L40: `public IReadOnlyList<SeasonDefinition> Seasons { get { return _seasons; } }`
  - L43: `public Season GetSeason(GameTime time)`
  - L53: `public bool TryGetSeason(GameTime time, out Season season)`
  - L70: `public bool IsSeasonBoundary(GameTime previous, GameTime current)`

#### `SeasonDefinition.cs`
- namespace: `EmberCrpg.Domain.Time`
- types:
  - L7: `class SeasonDefinition`
- members:
  - L27: `public Season Season { get; }`
  - L30: `public int StartDayOfYear { get; }`
  - L33: `public int EndDayOfYear { get; }`
  - L36: `public bool ContainsDay(int dayOfYear)`

### `Assets/Scripts/Domain/World`

#### `ActorStore.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L19: `class ActorStore`
- members:
  - L31: `public void Add(ActorRecord record)`
  - L45: `public ActorRecord Get(ActorId id)`
  - L58: `public bool TryGet(ActorId id, out ActorRecord record)`
  - L69: `public bool Contains(ActorId id)`
  - L78: `public bool Remove(ActorId id)`
  - L89: `public void Clear()`
  - L122: `public IEnumerable<ActorRecord> RecordsByRole(ActorRole role)`
  - L136: `public ActorRecord FirstByRole(ActorRole role)`
  - L148: `public bool TryFirstByRole(ActorRole role, out ActorRecord record)`

#### `ComponentStore.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L11: `class ComponentStore`
- members:
  - L16: `public int Count { get { return _byId.Count; } }`
  - L18: `public void Add(WorldComponentId id, T component)`
  - L31: `public T Get(WorldComponentId id)`
  - L40: `public bool TryGet(WorldComponentId id, out T component)`
  - L51: `public bool Contains(WorldComponentId id)`
  - L56: `public bool Replace(WorldComponentId id, T component)`
  - L65: `public bool Remove(WorldComponentId id)`

#### `DungeonDoor.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L12: `class DungeonDoor`
- members:
  - L22: `public int OtherRoom(int roomId)`

#### `DungeonDoorState.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L9: `class DungeonDoorState`

#### `DungeonRoom.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L12: `class DungeonRoom`
- members:
  - L22: `public bool IsWalkable(GridPosition position)`

#### `DungeonRoomState.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L9: `class DungeonRoomState`

#### `DungeonSpawnKind.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L9: `enum DungeonSpawnKind`

#### `DungeonSpawnPoint.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L11: `class DungeonSpawnPoint`

#### `FactionRecord.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L17: `class FactionRecord`
- members:
  - L47: `public FactionId Id { get; }`
  - L48: `public string Name { get; }`
  - L57: `public bool HasTag(string tag)`

#### `FactionStore.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L19: `class FactionStore`
- members:
  - L31: `public void Add(FactionRecord record)`
  - L45: `public FactionRecord Get(FactionId id)`
  - L58: `public bool TryGet(FactionId id, out FactionRecord record)`
  - L69: `public bool Contains(FactionId id)`
  - L78: `public bool Remove(FactionId id)`
  - L89: `public void Clear()`

#### `GeneratedDungeonLayout.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L12: `class GeneratedDungeonLayout`
- members:
  - L20: `public DungeonRoom FindRoom(int roomId)`
  - L25: `public DungeonSpawnPoint FindSpawn(DungeonSpawnKind kind)`

#### `IPathfinder.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L8: `interface IPathfinder`
  - L18: `struct PathfinderRequest`
  - L39: `struct PathfinderResult`
  - L61: `struct ActorPathStep`
- members:
  - L30: `public int ActorId { get; }`
  - L31: `public int StartX { get; }`
  - L32: `public int StartY { get; }`
  - L33: `public int GoalX { get; }`
  - L34: `public int GoalY { get; }`
  - L35: `public int ActorSize { get; }`
  - L50: `public bool Success { get; }`
  - L57: `public int TotalCost { get; }`
  - L70: `public int NewX { get; }`
  - L71: `public int NewY { get; }`
  - L72: `public bool Arrived { get; }`

#### `ItemStore.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L20: `class ItemStore`
- members:
  - L32: `public void Add(ItemRecord record)`
  - L46: `public ItemRecord Get(ItemId id)`
  - L59: `public bool TryGet(ItemId id, out ItemRecord record)`
  - L70: `public bool Contains(ItemId id)`
  - L79: `public bool Remove(ItemId id)`
  - L90: `public void Clear()`

#### `ProceduralRoom.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L11: `class ProceduralRoom`
- members:
  - L24: `public bool IsWalkable(GridPosition position)`

#### `ReasonTrace.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L19: `class ReasonTrace`
- members:
  - L72: `public bool HasCause(string cause)`

#### `SiteKind.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L8: `enum SiteKind`

#### `SiteRecord.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L15: `class SiteRecord`
- members:
  - L35: `public SiteId Id { get; }`
  - L36: `public SiteKind Kind { get; }`
  - L37: `public string Name { get; }`
  - L38: `public GridPosition MinBound { get; }`
  - L39: `public GridPosition MaxBound { get; }`
  - L42: `public bool Contains(GridPosition position)`

#### `SiteStore.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L19: `class SiteStore`
- members:
  - L31: `public void Add(SiteRecord record)`
  - L45: `public SiteRecord Get(SiteId id)`
  - L58: `public bool TryGet(SiteId id, out SiteRecord record)`
  - L69: `public bool Contains(SiteId id)`
  - L78: `public bool Remove(SiteId id)`
  - L89: `public void Clear()`

#### `SliceWorldState.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L18: `class SliceWorldState`
- members:
  - L87: `private ActorRecord GetActorView(ActorRole role)`
  - L93: `private void SetActorView(ActorRole expectedRole, ActorRecord record)`
  - L115: `private void EnsureActorStore()`

#### `WorldComponentId.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L10: `struct WorldComponentId`
- members:
  - L19: `public ulong Value { get { return _value; } }`
  - L20: `public bool IsEmpty { get { return _value == 0UL; } }`
  - L22: `public bool Equals(WorldComponentId other)`
  - L27: `public override bool Equals(object obj)`
  - L32: `public override int GetHashCode()`
  - L37: `public override string ToString()`

#### `WorldEvent.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L18: `class WorldEvent`
- members:
  - L37: `public GameTime Tick { get; }`
  - L38: `public WorldEventKind Kind { get; }`
  - L39: `public ActorId ActorId { get; }`
  - L40: `public SiteId SiteId { get; }`
  - L41: `public string Reason { get; }`
  - L42: `public ReasonTrace ReasonTrace { get; }`

#### `WorldEventKind.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L12: `enum WorldEventKind`

#### `WorldEventLog.cs`
- namespace: `EmberCrpg.Domain.World`
- types:
  - L23: `class WorldEventLog`
- members:
  - L49: `public void Append(WorldEvent worldEvent)`

### `Assets/Scripts/Simulation/Combat`

#### `BodyPartHierarchy.cs`
- namespace: `EmberCrpg.Simulation.Combat`
- types:
  - L13: `class BodyPartHierarchy`
- members:
  - L26: `public BodyPartNode GetNode(BodyPart bodyPart)`
  - L37: `public BodyPart Select(IDeterministicRng rng)`

#### `BodyPartSelector.cs`
- namespace: `EmberCrpg.Simulation.Combat`
- types:
  - L12: `class BodyPartSelector`
- members:
  - L38: `public BodyPart Select(IDeterministicRng rng)`

#### `CombatActionTimingProfile.cs`
- namespace: `EmberCrpg.Simulation.Combat`
- types:
  - L12: `struct CombatActionTimingProfile`
- members:
  - L24: `public double WindupSeconds { get; }`
  - L25: `public double ActiveSeconds { get; }`
  - L26: `public double RecoverySeconds { get; }`
  - L28: `public static CombatActionTimingProfile For(CombatActionKind kind)`

#### `CombatMathService.cs`
- namespace: `EmberCrpg.Simulation.Combat`
- types:
  - L14: `class CombatMathService`
- members:
  - L18: `public int CalculateHitChance(ActorRecord attacker, ActorRecord defender, int attackerAccuracyBonus = 0)`
  - L25: `public CombatStrikeResult ResolveAttack(ActorRecord attacker, ActorRecord defender, IDeterministicRng rng, int attackerAccuracyBonus = 0, int attackerDamageBonus = 0)`
  - L57: `public int CalculateArmorMitigation(ActorRecord defender, BodyPart bodyPart)`
  - L62: `private static int GetBodyPartBonus(BodyPart bodyPart)`
  - L73: `private static int ClampPercent(int value)`

#### `EncounterTurnService.cs`
- namespace: `EmberCrpg.Simulation.Combat`
- types:
  - L14: `class EncounterTurnService`
- members:
  - L18: `public CombatStrikeResult Advance(EncounterState encounter, ActorRecord player, ActorRecord enemy, IDeterministicRng rng)`

#### `RealtimeCombatActionScheduler.cs`
- namespace: `EmberCrpg.Simulation.Combat`
- types:
  - L13: `class RealtimeCombatActionScheduler`
- members:
  - L15: `public QueuedCombatAction QueueAction(RealtimeCombatState state, ActorId actorId, CombatActionKind kind, ActorId targetActorId)`
  - L36: `public bool CancelAction(RealtimeCombatState state, int sequence)`
  - L43: `public RealtimeCombatTickResult Tick(RealtimeCombatState state, double deltaSeconds)`
  - L76: `public CombatDefenseIntent GetActiveDefenseIntent(RealtimeCombatState state, ActorId actorId)`
  - L95: `private static double FindActorAvailableAt(RealtimeCombatState state, ActorId actorId)`

#### `RealtimeCombatState.cs`
- namespace: `EmberCrpg.Simulation.Combat`
- types:
  - L13: `class RealtimeCombatState`
- members:
  - L18: `public double ElapsedSeconds { get; private set; }`
  - L19: `public bool IsPaused { get; private set; }`
  - L22: `internal int ReserveSequence()`
  - L27: `internal void Add(QueuedCombatAction action)`
  - L33: `internal bool RemoveBySequence(int sequence)`
  - L42: `internal void Advance(double deltaSeconds)`
  - L49: `public void SetPaused(bool isPaused)`
  - L54: `public void TogglePaused()`
  - L59: `private static int CompareActions(QueuedCombatAction left, QueuedCombatAction right)`

#### `RealtimeCombatTickResult.cs`
- namespace: `EmberCrpg.Simulation.Combat`
- types:
  - L12: `class RealtimeCombatTickResult`
- members:
  - L18: `internal void Add(CombatActionEvent combatEvent)`

#### `RealtimeDamageService.cs`
- namespace: `EmberCrpg.Simulation.Combat`
- types:
  - L14: `class RealtimeDamageService`
- members:
  - L74: `public int CalculateArmorClass(ActorRecord defender, BodyPartNode node, CombatDefenseIntent defenseIntent)`
  - L82: `private static int GetDefenseArmorClass(CombatDefenseIntent defenseIntent, ActorRecord defender)`
  - L92: `private static int GetDefenseMitigation(CombatDefenseIntent defenseIntent, ActorRecord defender)`
  - L97: `private static int ClampPercent(int value)`

### `Assets/Scripts/Simulation/Inventory`

#### `EquipmentActionError.cs`
- namespace: `EmberCrpg.Simulation.Inventory`
- types:
  - L9: `enum EquipmentActionError`

#### `EquipmentActionResult.cs`
- namespace: `EmberCrpg.Simulation.Inventory`
- types:
  - L9: `class EquipmentActionResult`
- members:
  - L18: `public bool Success { get; }`
  - L19: `public EquipmentActionError Error { get; }`
  - L20: `public string Message { get; }`
  - L22: `public static EquipmentActionResult Ok(string message)`
  - L27: `public static EquipmentActionResult Fail(EquipmentActionError error, string message)`

#### `EquipmentCombatStats.cs`
- namespace: `EmberCrpg.Simulation.Inventory`
- types:
  - L9: `struct EquipmentCombatStats`
- members:
  - L17: `public int AccuracyBonus { get; }`
  - L18: `public int DamageBonus { get; }`

#### `EquipmentService.cs`
- namespace: `EmberCrpg.Simulation.Inventory`
- types:
  - L13: `class EquipmentService`
- members:
  - L15: `public EquipmentActionResult TryEquip(InventoryState inventory, EquipmentState equipment, ItemId itemId)`
  - L38: `public EquipmentActionResult TryUnequip(EquipmentState equipment, EquipmentSlot slot)`
  - L53: `public EquipmentCombatStats GetCombatStats(InventoryState inventory, EquipmentState equipment)`
  - L70: `public static string GetSlotLabel(EquipmentSlot slot)`

#### `MerchantTradeService.cs`
- namespace: `EmberCrpg.Simulation.Inventory`
- types:
  - L13: `class MerchantTradeService`
- members:
  - L17: `public string TradeGateWrit(SliceWorldState world)`

#### `PickupService.cs`
- namespace: `EmberCrpg.Simulation.Inventory`
- types:
  - L11: `class PickupService`
- members:
  - L13: `public bool TryCollect(RoomPickup pickup, InventoryState inventory)`

#### `SliceItemCatalog.cs`
- namespace: `EmberCrpg.Simulation.Inventory`
- types:
  - L12: `class SliceItemCatalog`
- members:
  - L18: `public static InventoryItem CreateEmberShard()`
  - L23: `public static InventoryItem CreateGateWrit()`
  - L28: `public static InventoryItem CreateAshTrainingBlade()`

### `Assets/Scripts/Simulation/Living`

#### `NeedMoodEvaluator.cs`
- namespace: `EmberCrpg.Simulation.Living`
- types:
  - L13: `class NeedMoodEvaluator`
- members:
  - L15: `public ActorMood Evaluate(ActorNeeds needs)`
  - L24: `public ActorMood Evaluate(ActorRecord actor)`

#### `NeedRecoverySystem.cs`
- namespace: `EmberCrpg.Simulation.Living`
- types:
  - L17: `class NeedRecoverySystem`
- members:
  - L73: `private static bool CanRecover(ActorRecord actor, NeedRecoveryRecipe recipe)`
  - L114: `private static void ValidateRecipe(NeedRecoveryRecipe recipe, string actionKind, NeedKind needKind, bool requiresInventoryItem)`
  - L126: `private static string NeedLabel(NeedKind kind)`

#### `NeedsSystem.cs`
- namespace: `EmberCrpg.Simulation.Living`
- types:
  - L15: `class NeedsSystem`
- members:
  - L32: `public ActorNeeds TickNeeds(ActorNeeds needs, int ticks = 1)`
  - L42: `public ActorMood RecomputeMood(ActorRecord actor)`
  - L90: `private static int ScaleRate(int rate, int ticks)`

### `Assets/Scripts/Simulation/Magic`

#### `MagicTickDriver.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L16: `class MagicTickDriver`
- members:
  - L27: `public void AdvanceTicks(SpellCooldownState cooldownState, ShieldBuffState shieldBuffState, int elapsedTicks)`
  - L47: `public void AdvanceTicks(SpellCooldownState cooldownState, ShieldBuffStateRegistry shieldBuffStateRegistry, int elapsedTicks)`

#### `ShieldBuffAbsorptionBatchTotals.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L16: `class ShieldBuffAbsorptionBatchTotals`
  - L751: `class GroupAccumulator`
- members:
  - L36: `public int TotalIncomingDamage { get; }`
  - L37: `public int TotalAbsorbedDamage { get; }`
  - L38: `public int TotalRemainingDamage { get; }`
  - L39: `public int ActorCount { get; }`
  - L40: `public int ActorsWithAbsorption { get; }`
  - L41: `public int TotalConsumedBuffEntries { get; }`
  - L42: `public int TotalExpiredBuffEntries { get; }`
  - L55: `public static ShieldBuffAbsorptionBatchTotals Empty { get; } =`

#### `ShieldBuffAbsorptionBatchTotalsPartition.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L14: `class ShieldBuffAbsorptionBatchTotalsPartition`
- members:
  - L29: `public ShieldBuffAbsorptionBatchTotals Included { get; }`
  - L30: `public ShieldBuffAbsorptionBatchTotals Excluded { get; }`

#### `ShieldBuffAbsorptionResult.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L15: `class ShieldBuffAbsorptionResult`
- members:
  - L31: `public int IncomingDamage { get; }`
  - L32: `public int AbsorbedDamage { get; }`
  - L33: `public int RemainingDamage { get; }`
  - L34: `public IReadOnlyList<string> ConsumedSpellTemplateIds { get; }`
  - L35: `public IReadOnlyList<string> ExpiredSpellTemplateIds { get; }`

#### `ShieldBuffApplicationResult.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L13: `class ShieldBuffApplicationResult`
- members:
  - L33: `public bool Success { get; }`
  - L34: `public SpellEffectResolutionError Error { get; }`
  - L35: `public SpellDefinition Spell { get; }`
  - L36: `public int AppliedBuffCount { get; }`
  - L37: `public int TotalAppliedMagnitude { get; }`
  - L38: `public int TotalAppliedDurationTicks { get; }`
  - L39: `public string Message { get; }`

#### `ShieldBuffService.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L17: `class ShieldBuffService`
- members:
  - L19: `public void AdvanceTicks(ShieldBuffState shieldBuffState, int elapsedTicks)`
  - L44: `public void AdvanceTicksForAllActors(ShieldBuffStateRegistry registry, int elapsedTicks)`
  - L70: `public ShieldBuffAbsorptionResult AbsorbDamage(ShieldBuffState shieldBuffState, int incomingDamage)`

#### `SliceSpellCatalog.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L13: `class SliceSpellCatalog`
- members:
  - L24: `public static SpellDefinition Find(string templateId)`
  - L40: `public static SpellDefinition CreateFlameBolt()`
  - L53: `public static SpellDefinition CreateMendingTouch()`
  - L66: `public static SpellDefinition CreateEmberWard()`
  - L79: `private static SpellDefinition[] BuildSpells()`
  - L89: `private static Dictionary<string, SpellDefinition> BuildLookup(SpellDefinition[] spells)`

#### `SpellCastError.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L9: `enum SpellCastError`

#### `SpellCastResult.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L11: `class SpellCastResult`
- members:
  - L22: `public bool Success { get; }`
  - L23: `public SpellCastError Error { get; }`
  - L24: `public SpellDefinition Spell { get; }`
  - L25: `public int ManaSpent { get; }`
  - L26: `public string Message { get; }`
  - L28: `public static SpellCastResult Ok(SpellDefinition spell, int manaSpent, string message)`
  - L33: `public static SpellCastResult Fail(SpellCastError error, SpellDefinition spell, string message)`

#### `SpellCastRollError.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L9: `enum SpellCastRollError`

#### `SpellCastRollResult.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L15: `class SpellCastRollResult`
- members:
  - L35: `public bool Success { get; }`
  - L36: `public SpellCastRollError Error { get; }`
  - L37: `public SpellDefinition Spell { get; }`
  - L39: `public int Roll { get; }`
  - L41: `public int Threshold { get; }`
  - L43: `public SpellSuccessChanceResult Chance { get; }`
  - L44: `public string Message { get; }`

#### `SpellCastRollService.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L18: `class SpellCastRollService`
- members:
  - L32: `public SpellCastRollResult Roll(ActorRecord caster, SpellDefinition spell, IDeterministicRng rng)`

#### `SpellCastingService.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L17: `class SpellCastingService`
- members:
  - L74: `public SpellCastResult CommitPreparedCast(ActorRecord caster, SpellDefinition spell, SpellCooldownState cooldownState = null)`
  - L114: `private static bool ContainsKnown(IReadOnlyCollection<string> knownSpellIds, string templateId)`

#### `SpellCooldownService.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L12: `class SpellCooldownService`
- members:
  - L14: `public int GetRemainingTicks(SpellDefinition spell, SpellCooldownState cooldownState)`
  - L24: `public bool IsOnCooldown(SpellDefinition spell, SpellCooldownState cooldownState)`
  - L29: `public void StartCooldown(SpellDefinition spell, SpellCooldownState cooldownState)`
  - L39: `public void AdvanceTicks(SpellCooldownState cooldownState, int elapsedTicks)`

#### `SpellCostCalculator.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L12: `class SpellCostCalculator`
- members:
  - L17: `public int EstimateTotalManaCost(SpellDefinition spell)`
  - L31: `public int EstimateEffectCost(SpellEffectSpec effect)`
  - L36: `public int ApplyTargetMultiplier(int effectCostTotal, SpellTargetKind targetKind)`
  - L45: `public int GetTargetMultiplierNumerator(SpellTargetKind targetKind)`
  - L59: `private static int CalculateDurationComponent(int durationTicks)`
  - L64: `private static int DivideRoundedUp(int numerator, int denominator)`

#### `SpellEffectResolutionError.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L9: `enum SpellEffectResolutionError`

#### `SpellEffectResolutionResult.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L11: `class SpellEffectResolutionResult`
- members:
  - L39: `public bool Success { get; }`
  - L40: `public SpellEffectResolutionError Error { get; }`
  - L41: `public SpellDefinition Spell { get; }`
  - L42: `public int AppliedEffectCount { get; }`
  - L43: `public int TotalDamage { get; }`
  - L44: `public int TotalHealing { get; }`
  - L45: `public int TotalRestoredFatigue { get; }`
  - L46: `public int TotalRestoredMana { get; }`
  - L47: `public int TotalDirectManaDamage { get; }`
  - L48: `public int TotalDirectFatigueDamage { get; }`
  - L49: `public string Message { get; }`
  - L122: `public static SpellEffectResolutionResult Fail(SpellEffectResolutionError error, SpellDefinition spell, string message)`

#### `SpellEffectResolutionService.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L22: `class SpellEffectResolutionService`
- members:
  - L24: `public SpellEffectResolutionResult CanResolveInstantaneousEffects(SpellDefinition spell, ActorRecord target)`
  - L42: `public SpellEffectResolutionResult ResolveInstantaneousEffects(SpellCastResult castResult, ActorRecord target)`
  - L110: `public ShieldBuffApplicationResult ApplyShieldBuffs(SpellCastResult castResult, ShieldBuffState shieldBuffState)`
  - L150: `public ShieldBuffApplicationResult ApplyShieldBuffs(SpellCastResult castResult, ShieldBuffStateRegistry registry, string actorId)`
  - L163: `private static SpellEffectResolutionResult ValidateInstantaneousEffects(SpellDefinition spell, ActorRecord target)`
  - L183: `private static bool IsSupported(SpellEffectKind kind)`

#### `SpellExecutionError.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L9: `enum SpellExecutionError`

#### `SpellExecutionResult.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L12: `class SpellExecutionResult`
- members:
  - L36: `public bool Success { get; }`
  - L37: `public SpellExecutionError Error { get; }`
  - L38: `public SpellDefinition Spell { get; }`
  - L39: `public ActorRecord RoutedTarget { get; }`
  - L40: `public SpellCastResult CastResult { get; }`
  - L41: `public SpellCastRollResult CastRollResult { get; }`
  - L42: `public SpellTargetValidationResult TargetValidationResult { get; }`
  - L43: `public SpellEffectResolutionResult EffectResolutionResult { get; }`
  - L44: `public string Message { get; }`

#### `SpellExecutionService.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L18: `class SpellExecutionService`

#### `SpellSuccessChanceError.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L9: `enum SpellSuccessChanceError`

#### `SpellSuccessChanceResult.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L12: `class SpellSuccessChanceResult`
- members:
  - L42: `public bool Success { get; }`
  - L43: `public SpellSuccessChanceError Error { get; }`
  - L44: `public SpellDefinition Spell { get; }`
  - L45: `public int ChancePercent { get; }`
  - L46: `public int BaseChance { get; }`
  - L47: `public int PrimaryAttributeBonus { get; }`
  - L48: `public int SecondaryAttributeBonus { get; }`
  - L49: `public int ManaCostPenalty { get; }`
  - L50: `public int EffectComplexityPenalty { get; }`
  - L51: `public int TargetPenalty { get; }`
  - L52: `public int RangePenalty { get; }`
  - L53: `public string Message { get; }`
  - L82: `public static SpellSuccessChanceResult Fail(SpellSuccessChanceError error, SpellDefinition spell, string message)`

#### `SpellSuccessChanceService.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L15: `class SpellSuccessChanceService`
- members:
  - L25: `public SpellSuccessChanceResult Calculate(ActorRecord caster, SpellDefinition spell)`
  - L67: `private static int GetPrimaryAttribute(EmberStatBlock stats, MagicSchool school)`
  - L84: `private static int GetSecondaryAttribute(EmberStatBlock stats, MagicSchool school)`
  - L101: `private static int GetTargetPenalty(SpellTargetKind targetKind)`
  - L115: `private static int GetRangePenalty(SpellDefinition spell)`
  - L123: `private static int ClampPercent(int value)`

#### `SpellTargetValidationError.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L9: `enum SpellTargetValidationError`

#### `SpellTargetValidationResult.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L12: `class SpellTargetValidationResult`
- members:
  - L28: `public bool Success { get; }`
  - L29: `public SpellTargetValidationError Error { get; }`
  - L30: `public SpellDefinition Spell { get; }`
  - L31: `public ActorRecord RoutedTarget { get; }`
  - L32: `public string Message { get; }`
  - L34: `public static SpellTargetValidationResult Ok(SpellDefinition spell, ActorRecord routedTarget, string message)`
  - L39: `public static SpellTargetValidationResult Fail(SpellTargetValidationError error, SpellDefinition spell, string message)`

#### `SpellTargetValidator.cs`
- namespace: `EmberCrpg.Simulation.Magic`
- types:
  - L17: `class SpellTargetValidator`
- members:
  - L19: `public SpellTargetValidationResult Validate(SpellDefinition spell, ActorRecord caster, ActorRecord requestedTarget)`
  - L50: `private static SpellTargetValidationResult ValidateCasterSelf(SpellDefinition spell, ActorRecord caster, ActorRecord requestedTarget)`
  - L61: `private static SpellTargetValidationResult ValidateTouch(SpellDefinition spell, ActorRecord caster, ActorRecord requestedTarget)`
  - L84: `private static SpellTargetValidationResult ValidateSingleTarget(SpellDefinition spell, ActorRecord caster, ActorRecord requestedTarget)`

### `Assets/Scripts/Simulation/Movement`

#### `Sprint4KinematicMotor.cs`
- namespace: `EmberCrpg.Simulation.Movement`
- types:
  - L9: `class Sprint4KinematicMotor`
- members:
  - L13: `public Sprint4MotorStep Plan(Sprint4MotorState state, Sprint4MovementInput input, Sprint4MotorSettings settings, float deltaTime)`
  - L44: `public Sprint4MotorState ResolveGrounding(Sprint4MotorState state, Sprint4Vector3 resolvedPosition, bool isGrounded)`
  - L50: `public static Sprint4Vector3 ToWorldPlanar(Sprint4Vector3 localPlanar, float yawDegrees)`

#### `Sprint4MotorSettings.cs`
- namespace: `EmberCrpg.Simulation.Movement`
- types:
  - L4: `struct Sprint4MotorSettings`
- members:
  - L20: `public float MoveSpeed { get; }`
  - L21: `public float JumpHeight { get; }`
  - L22: `public float Gravity { get; }`
  - L23: `public float GroundedStickVelocity { get; }`

#### `Sprint4MotorState.cs`
- namespace: `EmberCrpg.Simulation.Movement`
- types:
  - L4: `struct Sprint4MotorState`
- members:
  - L13: `public static Sprint4MotorState GroundedAt(Sprint4Vector3 position)`
  - L16: `public Sprint4Vector3 Position { get; }`
  - L17: `public float VerticalVelocity { get; }`
  - L18: `public bool IsGrounded { get; }`

#### `Sprint4MotorStep.cs`
- namespace: `EmberCrpg.Simulation.Movement`
- types:
  - L4: `struct Sprint4MotorStep`
- members:
  - L14: `public Sprint4Vector3 Displacement { get; }`
  - L15: `public Sprint4Vector3 PlanarVelocity { get; }`
  - L16: `public Sprint4MotorState State { get; }`
  - L17: `public bool JumpedThisFrame { get; }`

#### `Sprint4MovementInput.cs`
- namespace: `EmberCrpg.Simulation.Movement`
- types:
  - L4: `struct Sprint4MovementInput`
- members:
  - L15: `public float MoveX { get; }`
  - L18: `public float MoveZ { get; }`
  - L21: `public float YawDegrees { get; }`
  - L24: `public bool JumpPressed { get; }`

#### `Sprint4Vector3.cs`
- namespace: `EmberCrpg.Simulation.Movement`
- types:
  - L6: `struct Sprint4Vector3`
- members:
  - L17: `public float X { get; }`
  - L18: `public float Y { get; }`
  - L19: `public float Z { get; }`
  - L33: `public static Sprint4Vector3 ClampMagnitude(Sprint4Vector3 value, float maxMagnitude)`
  - L55: `public bool Equals(Sprint4Vector3 other)`
  - L58: `public override bool Equals(object obj)`
  - L61: `public override int GetHashCode()`
  - L64: `public override string ToString()`

### `Assets/Scripts/Simulation/Narrative`

#### `AskAboutService.cs`
- namespace: `EmberCrpg.Simulation.Narrative`
- types:
  - L13: `class AskAboutService`
- members:
  - L17: `public string Ask(SliceWorldState world, string topicId)`

#### `AskDmService.cs`
- namespace: `EmberCrpg.Simulation.Narrative`
- types:
  - L11: `class AskDmService`
- members:
  - L13: `public string Ask(SliceWorldState world, string question)`

#### `GuardInteractionService.cs`
- namespace: `EmberCrpg.Simulation.Narrative`
- types:
  - L14: `class GuardInteractionService`
- members:
  - L19: `public string Interact(SliceWorldState world)`

#### `NpcMemoryQueryService.cs`
- namespace: `EmberCrpg.Simulation.Narrative`
- types:
  - L13: `class NpcMemoryQueryService`
  - L88: `struct DialogueMemoryContext`
  - L106: `enum DialogueMemoryState`
  - L113: `struct GuardMemoryContext`
  - L129: `enum GuardStance`
  - L137: `struct MerchantMemoryContext`
  - L153: `enum MerchantFamiliarity`
- members:
  - L15: `public DialogueMemoryContext GetDialogueContext(NpcMemoryStore store, ActorId npcId, string topicId)`
  - L34: `public GuardMemoryContext GetGuardContext(NpcMemoryStore store, ActorId guardId, string passageId)`
  - L54: `public MerchantMemoryContext GetMerchantContext(NpcMemoryStore store, ActorId merchantId)`
  - L72: `private static ActorMemory GetMemory(NpcMemoryStore store, ActorId actorId)`
  - L79: `private static int CountEvents(ActorMemory memory, string eventType, string subjectId)`
  - L99: `public string TopicId { get; }`
  - L100: `public int TopicAskCount { get; }`
  - L101: `public int DistinctTopicsSeen { get; }`
  - L102: `public int TotalDialogueEvents { get; }`
  - L103: `public DialogueMemoryState State { get; }`
  - L123: `public string PassageId { get; }`
  - L124: `public int PassageRequestCount { get; }`
  - L125: `public bool ClearanceGranted { get; }`
  - L126: `public GuardStance Stance { get; }`
  - L147: `public int TransactionCount { get; }`
  - L148: `public int GateWritTransactions { get; }`
  - L149: `public int TradeEventCount { get; }`
  - L150: `public MerchantFamiliarity Familiarity { get; }`

#### `ThinkService.cs`
- namespace: `EmberCrpg.Simulation.Narrative`
- types:
  - L12: `class ThinkService`
- members:
  - L14: `public string Think(SliceWorldState world)`

### `Assets/Scripts/Simulation/Process`

#### `HarvestSystem.cs`
- namespace: `EmberCrpg.Simulation.Process`
- types:
  - L15: `class HarvestSystem`
- members:
  - L17: `public bool TryHarvest(PlantSpeciesDef species, ComponentStore<PlantComponent> plants, ComponentStore<SoilComponent> soils, WorldComponentId plantId, InventoryState stockpile, WorldEventLog eventLog, GameTime now, Func<string, InventoryItem> createHarvestItem)`

#### `JobAssignmentSystem.cs`
- namespace: `EmberCrpg.Simulation.Process`
- types:
  - L21: `class JobAssignmentSystem`
  - L614: `class Candidate`
  - L651: `struct JobRecipeStartResult`
  - L706: `struct JobAssignmentResult`
- members:
  - L32: `public bool TryAssignNext(ActorStore actors, JobBoard board, WorksiteStore worksites, out JobAssignmentResult result)`
  - L209: `public bool CanActorWorkJob(ActorRecord actor, JobRequest request, WorksiteStore worksites)`
  - L319: `public bool TryGetActiveWorkOrder(JobId jobId, out RecipeWorkOrder order)`
  - L501: `private int GetCompletedExecutionCount(JobId jobId)`
  - L506: `private static bool ActorAlreadyHasPendingClaim(ActorRecord actor, JobBoard board)`
  - L542: `private static bool TryGetActivePreference(ActorRecord actor, JobKind kind, out ActorJobPreference preference)`
  - L584: `private static bool TryGetActiveMatchingWorksite(JobRequest request, WorksiteStore worksites, out WorksiteRecord worksite)`
  - L595: `private static bool IsRefusing(ActorRecord actor)`
  - L625: `public ActorRecord Actor { get; }`
  - L626: `public JobRequest Request { get; }`
  - L627: `public JobPriority ActorPriority { get; }`
  - L628: `public int ActorOrder { get; }`
  - L629: `public int JobOrder { get; }`
  - L631: `public int CompareTo(Candidate other)`
  - L668: `public ActorId ActorId { get; }`
  - L671: `public JobId JobId { get; }`
  - L674: `public SiteId SiteId { get; }`
  - L677: `public GridPosition WorksitePosition { get; }`
  - L680: `public RecipeWorkOrder WorkOrder { get; }`
  - L683: `public bool Equals(JobRecipeStartResult other)`
  - L693: `public override bool Equals(object obj)`
  - L699: `public override int GetHashCode()`
  - L717: `public ActorId ActorId { get; }`
  - L720: `public JobId JobId { get; }`
  - L723: `public SiteId SiteId { get; }`
  - L726: `public GridPosition WorksitePosition { get; }`
  - L729: `public bool Equals(JobAssignmentResult other)`
  - L738: `public override bool Equals(object obj)`
  - L744: `public override int GetHashCode()`

#### `PlantingSystem.cs`
- namespace: `EmberCrpg.Simulation.Process`
- types:
  - L13: `class PlantingSystem`

#### `PlantGrowthSystem.cs`
- namespace: `EmberCrpg.Simulation.Process`
- types:
  - L15: `class PlantGrowthSystem`
- members:
  - L17: `public int AdvanceOneDay(PlantSpeciesDef species, ComponentStore<PlantComponent> plants, WorldEventLog eventLog, GameTime now, Season season, bool isSnowing)`

#### `RecipeSystem.cs`
- namespace: `EmberCrpg.Simulation.Process`
- types:
  - L21: `class RecipeSystem`
  - L180: `class RecipeWorkOrder`
- members:
  - L112: `private static IReadOnlyList<InventoryItem> CreateOutputItems(RecipeWorkOrder order, Func<RecipeOutput, InventoryItem> createOutput)`
  - L134: `private static void PreflightOutputs(InventoryState inventory, IReadOnlyList<InventoryItem> outputItems)`
  - L144: `private static bool HasInputs(RecipeDef recipe, InventoryState inventory)`
  - L162: `private static void ConsumeInputs(RecipeDef recipe, InventoryState inventory)`
  - L171: `private static bool MatchesWorksiteKind(string recipeKind, WorksiteKind worksiteKind)`
  - L200: `public static RecipeWorkOrder Resume(RecipeDef recipe, SiteId siteId, GridPosition position, ActorId actorId, int progressTicks)`
  - L205: `public RecipeDef Recipe { get; }`
  - L206: `public SiteId SiteId { get; }`
  - L207: `public GridPosition Position { get; }`
  - L208: `public ActorId ActorId { get; }`
  - L209: `public int ProgressTicks { get; private set; }`
  - L210: `public bool IsComplete { get { return ProgressTicks >= Recipe.DurationTicks; } }`
  - L212: `internal void AdvanceOneTick()`

### `Assets/Scripts/Simulation/Rng`

#### `IDeterministicRng.cs`
- namespace: `EmberCrpg.Simulation.Rng`
- types:
  - L9: `interface IDeterministicRng`

#### `XorShiftRng.cs`
- namespace: `EmberCrpg.Simulation.Rng`
- types:
  - L11: `class XorShiftRng`
- members:
  - L20: `public int NextInt(int exclusiveMax)`
  - L27: `public int RollPercent()`
  - L32: `private uint NextUInt()`

### `Assets/Scripts/Simulation/Time`

#### `GameTimeAdvanceSystem.cs`
- namespace: `EmberCrpg.Simulation.Time`
- types:
  - L13: `class GameTimeAdvanceSystem`
- members:
  - L22: `public GameTime Advance(GameTime current, long minutes)`

### `Assets/Scripts/Simulation/World`

#### `DoorInteractionService.cs`
- namespace: `EmberCrpg.Simulation.World`
- types:
  - L12: `class DoorInteractionService`
- members:
  - L14: `public string Toggle(SliceWorldState world)`

#### `DungeonTraversalService.cs`
- namespace: `EmberCrpg.Simulation.World`
- types:
  - L12: `class DungeonTraversalService`
- members:
  - L14: `public string Traverse(SliceWorldState world, int doorId)`

#### `MultiRoomDungeonGenerator.cs`
- namespace: `EmberCrpg.Simulation.World`
- types:
  - L15: `class MultiRoomDungeonGenerator`
- members:
  - L25: `public GeneratedDungeonLayout Generate(int seed)`
  - L39: `private static void AddConnectedRoom(GeneratedDungeonLayout layout, IDeterministicRng rng)`
  - L55: `private static bool TryAddAdjacent(GeneratedDungeonLayout layout, DungeonRoom anchor, GridPosition direction, IDeterministicRng rng)`
  - L67: `private static DungeonRoom AddRoom(GeneratedDungeonLayout layout, int id, int gridX, int gridY, IDeterministicRng rng)`
  - L82: `private static void AddDoor(GeneratedDungeonLayout layout, DungeonRoom from, DungeonRoom to, GridPosition direction)`
  - L99: `private static GridPosition DoorCell(DungeonRoom room, GridPosition direction)`
  - L110: `private static string SelectTemplate(int id, IDeterministicRng rng)`
  - L116: `private static void AddSpawnPoints(GeneratedDungeonLayout layout)`
  - L127: `private static void AddSpawn(GeneratedDungeonLayout layout, DungeonRoom room, DungeonSpawnKind kind, int x, int y)`

#### `ProceduralRoomGenerator.cs`
- namespace: `EmberCrpg.Simulation.World`
- types:
  - L12: `class ProceduralRoomGenerator`
- members:
  - L14: `public ProceduralRoom Generate(int seed)`

#### `RoomMovementService.cs`
- namespace: `EmberCrpg.Simulation.World`
- types:
  - L12: `class RoomMovementService`
- members:
  - L14: `public GridPosition Move(ProceduralRoom room, GridPosition origin, int deltaX, int deltaY)`
  - L20: `public GridPosition Move(SliceWorldState world, GridPosition origin, int deltaX, int deltaY)`

#### `SliceActorLoadoutFactory.cs`
- namespace: `EmberCrpg.Simulation.World`
- types:
  - L13: `class SliceActorLoadoutFactory`
- members:
  - L15: `public ActorRecord Create(ActorId id, string name, ActorRole role, GridPosition position, IEnumerable<string> topicIds = null)`
  - L28: `private static ActorRecord Build(ActorId id, string name, ActorRole role, GridPosition position, EmberStatBlock stats, ActorVitals vitals, int accuracy, int dodge, int armor, int baseDamage, IEnumerable<string> topicIds)`

#### `SliceWorldFactory.cs`
- namespace: `EmberCrpg.Simulation.World`
- types:
  - L18: `class SliceWorldFactory`
- members:
  - L24: `public SliceWorldState Create(int roomSeed)`

### `Assets/Tests/EditMode/Actors`

#### `ActorJobPreferenceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L12: `class ActorJobPreferenceTests`
- members:
  - L15: `public void Constructor_StoresKindAndPriority()`
  - L25: `public void Constructor_RejectsNoneKind()`
  - L32: `public void Disabled_CreatesOptOutRowForConcreteKind()`
  - L42: `public void SameKindAndPriority_AreEqual()`
  - L53: `public void ToString_ReturnsDebugLabel()`

#### `ActorMoodTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L10: `class ActorMoodTests`
- members:
  - L13: `public void Default_IsNeutralMood()`
  - L20: `public void Constructor_ClampsToZeroToOneHundred()`
  - L27: `public void IsLow_UsesLowMoodThreshold()`
  - L34: `public void EqualityAndComparison_UseMoodValue()`
  - L47: `public void ToString_ReturnsDebugLabel()`

#### `ActorNeedsTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L11: `class ActorNeedsTests`
- members:
  - L14: `public void Comfortable_DefaultsToZeroPressure()`
  - L22: `public void Constructor_StoresNeedPressures()`
  - L32: `public void With_ReplacesOneNeedOnly()`
  - L45: `public void With_RejectsNoneKind()`
  - L52: `public void Equality_UsesAllNeedPressures()`
  - L63: `public void ToString_ReturnsDebugLabel()`

#### `ActorRecordMoodTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L11: `class ActorRecordMoodTests`
- members:
  - L14: `public void Constructor_DefaultsToNeutralMood()`
  - L22: `public void Constructor_CanSeedMood()`
  - L30: `public void ApplyMood_ReplacesMoodWithoutChangingIdentity()`
  - L41: `private static ActorRecord CreateActor(ActorMood mood = default)`

#### `ActorRecordNeedsTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L11: `class ActorRecordNeedsTests`
- members:
  - L14: `public void Constructor_DefaultsToComfortableNeeds()`
  - L22: `public void Constructor_CanSeedNeeds()`
  - L31: `public void ApplyNeeds_ReplacesNeedsWithoutChangingIdentity()`
  - L43: `private static ActorRecord CreateActor(ActorNeeds needs = default)`

#### `ActorRecordTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L12: `class ActorRecordTests`
- members:
  - L15: `public void MoveTo_UpdatesGridPosition()`
  - L23: `public void ApplyVitals_ReplacesHealthSnapshot()`
  - L31: `public void RecordTopic_StoresOnlyUniqueTopicIds()`
  - L40: `public void ApplyJobPreferences_ReplacesRowsWithoutChangingIdentity()`
  - L54: `public void ApplyJobPreferences_RejectsDuplicateKinds()`
  - L64: `public void ApplyJobPreferences_PreservesExistingRowsWhenReplacementIsInvalid()`
  - L77: `public void ApplyScheduleState_ReplacesCurrentJobState()`
  - L90: `private static ActorRecord CreateActor()`

#### `ActorScheduleStateTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L13: `class ActorScheduleStateTests`
- members:
  - L16: `public void Default_IsIdle()`
  - L23: `public void Assigned_StoresCurrentJobAndWorksiteTarget()`
  - L37: `public void Assigned_RejectsEmptyJobOrSite()`
  - L44: `public void SameAssignmentFields_AreEqual()`
  - L55: `public void ToString_ReturnsDebugLabel()`

#### `EmberStatBlockTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L11: `class EmberStatBlockTests`
- members:
  - L14: `public void Constructor_StoresAllSixStats()`
  - L21: `public void Constructor_BelowMinimum_Throws()`
  - L27: `public void Get_ReturnsSelectedAttribute()`

#### `NeedKindTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L10: `class NeedKindTests`
- members:
  - L13: `public void None_IsZeroSentinel()`
  - L19: `public void SeedKinds_AreStable()`

#### `NeedValueTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L10: `class NeedValueTests`
- members:
  - L13: `public void Constructor_ClampsToZeroToOneHundred()`
  - L20: `public void IncreaseAndDecrease_ClampAtBounds()`
  - L27: `public void Increase_SaturatesLargePositiveDeltas()`
  - L33: `public void IsAtLeast_PinsThresholdSemantics()`
  - L40: `public void Comparison_UsesRawPressure()`
  - L48: `public void ToString_ReturnsDebugLabel()`

#### `VitalStatTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Actors`
- types:
  - L11: `class VitalStatTests`
- members:
  - L14: `public void Constructor_CurrentAboveMax_Throws()`
  - L20: `public void Damage_ClampsAtZero()`
  - L26: `public void Restore_ClampsAtMax()`

### `Assets/Tests/EditMode/Combat`

#### `BodyPartSelectorTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Combat`
- types:
  - L12: `class BodyPartSelectorTests`
  - L24: `class SequenceRng`
- members:
  - L15: `public void Select_UsesWeightedArrayOrder()`
  - L29: `public int NextInt(int exclusiveMax) { return _values[_index++] % exclusiveMax; }`
  - L30: `public int RollPercent() { return 1; }`

#### `CombatMathServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Combat`
- types:
  - L13: `class CombatMathServiceTests`
  - L50: `class FixedRng`
- members:
  - L16: `public void CalculateHitChance_ClampsToValidPercentRange()`
  - L26: `public void ResolveAttack_HitAppliesArmorMitigatedDamage()`
  - L35: `private static ActorRecord CreateActor(int accuracy, int dodge, int armor, int baseDamage)`
  - L56: `public int NextInt(int exclusiveMax) { return _values[_index++] % exclusiveMax; }`
  - L57: `public int RollPercent() { return _values[_index++]; }`

#### `EncounterTurnServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Combat`
- types:
  - L14: `class EncounterTurnServiceTests`
  - L54: `class FixedRng`
- members:
  - L17: `public void Advance_FirstCallUsesPlayerTurnThenFlipsToEnemy()`
  - L28: `public void Advance_KillingStrike_FinishesEncounter()`
  - L39: `private static ActorRecord CreateActor(ulong id, int accuracy, int dodge, int armor, int baseDamage)`
  - L59: `public int NextInt(int exclusiveMax) { return _values[_index++] % exclusiveMax; }`
  - L60: `public int RollPercent() { return _values[_index++]; }`

#### `RealtimeCombatActionSchedulerTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Combat`
- types:
  - L13: `class RealtimeCombatActionSchedulerTests`
- members:
  - L16: `public void Tick_QueuesActorActionsInOrderAndEmitsActivationBeforeCompletion()`
  - L40: `public void Tick_WhenPaused_DoesNotAdvanceTimeOrActivateActions()`
  - L55: `public void Queue_CanBeEditedWhilePausedAndResumesDeterministically()`
  - L79: `public void GetActiveDefenseIntent_PrefersDodgeOverBlock()`

#### `RealtimeDamageServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Combat`
- types:
  - L14: `class RealtimeDamageServiceTests`
  - L97: `class FixedRng`
- members:
  - L17: `public void ResolveWeaponHit_WithSameSeed_IsDeterministic()`
  - L30: `public void ResolveWeaponHit_UsesColliderBodyPartWhenProvided()`
  - L45: `public void BodyPartHierarchy_ChestAndFeetExposeDifferentRiskProfiles()`
  - L58: `public void ResolveWeaponHit_BlockingRaisesArmorClassAndReducesDamage()`
  - L73: `private static RealtimeDamageResult ResolveWithSeed(uint seed)`
  - L82: `private static ActorRecord CreateActor(ulong id, string name, int accuracy, int dodge, int armor, int baseDamage, EmberStatBlock stats)`
  - L102: `public int NextInt(int exclusiveMax) { return 0; }`
  - L103: `public int RollPercent() { return _roll; }`

### `Assets/Tests/EditMode/Core`

#### `ActorIdTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Core`
- types:
  - L12: `class ActorIdTests`
- members:
  - L18: `public void Constructor_StoresValue()`
  - L29: `public void SameValue_IsEqual()`
  - L41: `public void DifferentValue_IsNotEqual()`
  - L53: `public void Default_IsEmpty()`
  - L62: `public void SameValue_HasSameHashCode()`
  - L74: `public void Empty_ToString_ReturnsEmptyLabel()`
  - L83: `public void NonEmpty_ToString_ContainsRawValue()`

#### `FactionIdTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Core`
- types:
  - L12: `class FactionIdTests`
- members:
  - L18: `public void Constructor_StoresValue()`
  - L29: `public void SameValue_IsEqual()`
  - L41: `public void DifferentValue_IsNotEqual()`
  - L53: `public void Default_IsEmpty()`
  - L62: `public void SameValue_HasSameHashCode()`
  - L74: `public void Empty_ToString_ReturnsEmptyLabel()`
  - L83: `public void NonEmpty_ToString_ContainsRawValue()`

#### `GameTimeTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Core`
- types:
  - L11: `class GameTimeTests`
- members:
  - L15: `public void Constructor_StoresTotalMinutes() => Assert.That(new GameTime(42).TotalMinutes, Is.EqualTo(42));`
  - L19: `public void Constructor_NegativeTotalMinutes_ThrowsArgumentOutOfRange() =>`
  - L24: `public void Minute_WrapsWithinHour() => Assert.That(new GameTime(61).Minute, Is.EqualTo(1));`
  - L28: `public void Hour_WrapsWithinDay() => Assert.That(new GameTime(25 * 60).Hour, Is.EqualTo(1));`
  - L32: `public void DayOfMonth_IsOneBasedWithinThirtyDayMonth() =>`
  - L37: `public void Month_IsOneBasedWithinYear() =>`
  - L42: `public void Year_IsOneBasedAndAdvancesEveryYear() =>`
  - L47: `public void DayOfYear_IsOneBasedWithinYear() =>`
  - L52: `public void AddMinutes_ReturnsAdvancedTime() =>`
  - L57: `public void AddHours_UsesSixtyMinuteHours() =>`
  - L62: `public void AddDays_Uses1440MinuteDays() =>`
  - L67: `public void AddMonths_UsesThirtyDayMonths() =>`
  - L72: `public void AddYears_Uses518400MinuteYears() =>`
  - L77: `public void Operator_Plus_AddsMinutes() => Assert.That((new GameTime(10) + 5).TotalMinutes, Is.EqualTo(15));`
  - L81: `public void Operator_Minus_ReturnsDeltaMinutes() =>`
  - L86: `public void Operator_LessThan_ComparesByTotalMinutes() =>`
  - L91: `public void Operator_EqualsAndNotEquals_MatchEqualsMethod()`
  - L102: `public void ToString_IncludesYearDayAndTime() =>`

#### `ItemIdTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Core`
- types:
  - L12: `class ItemIdTests`
- members:
  - L18: `public void Constructor_StoresValue()`
  - L29: `public void SameValue_IsEqual()`
  - L41: `public void DifferentValue_IsNotEqual()`
  - L53: `public void Default_IsEmpty()`
  - L62: `public void SameValue_HasSameHashCode()`
  - L74: `public void Empty_ToString_ReturnsEmptyLabel()`
  - L83: `public void NonEmpty_ToString_ContainsRawValue()`

#### `SiteIdTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Core`
- types:
  - L12: `class SiteIdTests`
- members:
  - L18: `public void Constructor_StoresValue()`
  - L29: `public void SameValue_IsEqual()`
  - L41: `public void DifferentValue_IsNotEqual()`
  - L53: `public void Default_IsEmpty()`
  - L62: `public void SameValue_HasSameHashCode()`
  - L74: `public void Empty_ToString_ReturnsEmptyLabel()`
  - L83: `public void NonEmpty_ToString_ContainsRawValue()`

### `Assets/Tests/EditMode/Inventory`

#### `EquipmentServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Inventory`
- types:
  - L15: `class EquipmentServiceTests`
  - L127: `class FixedRng`
- members:
  - L18: `public void TryEquip_WeaponInInventory_EquipsByStableItemId()`
  - L30: `public void TryEquip_NonEquipmentItem_ReturnsDeterministicRefusal()`
  - L45: `public void TryEquip_WhenSlotOccupied_RefusesSecondWeaponUntilUnequipped()`
  - L65: `public void TryEquip_AlreadyEquippedWeapon_ReturnsDedicatedErrorCode()`
  - L80: `public void TryRemove_WithEquipmentState_RefusesToRemoveEquippedItem()`
  - L95: `public void TryUnequip_EquippedWeapon_ClearsSlot()`
  - L109: `public void EquippedWeapon_IncreasesEncounterStrikeDamage()`
  - L132: `public int NextInt(int exclusiveMax) { return 0; }`
  - L133: `public int RollPercent() { return _roll; }`

#### `InventoryStateTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Inventory`
- types:
  - L11: `class InventoryStateTests`
- members:
  - L14: `public void TryAdd_NewItem_UsesOneSlot()`
  - L22: `public void TryAdd_SameTemplate_StacksInsteadOfUsingNewSlot()`
  - L31: `public void TryAdd_WhenFull_ReturnsFalse()`
  - L40: `public void TryRemove_RemovesItemWhenQuantityHitsZero()`

#### `ItemRecordTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Inventory`
- types:
  - L14: `class ItemRecordTests`
- members:
  - L16: `private static ItemRecord MakeRecord()`
  - L27: `public void Constructor_StoresFields()`
  - L39: `public void IsEquipment_TrueForSlottedRecord()`
  - L48: `public void Constructor_AllowsNoneSlot_ForNonEquipmentRecord()`
  - L62: `public void Constructor_RejectsEmptyId()`
  - L73: `public void Constructor_RejectsNoneMaterial()`
  - L84: `public void Constructor_RejectsNoneQuality()`

#### `MerchantTradeServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Inventory`
- types:
  - L14: `class MerchantTradeServiceTests`
- members:
  - L17: `public void TradeGateWrit_WithPayment_TransfersItemAndConsumesStock()`
  - L31: `public void TradeGateWrit_WhenInventoryRemainsFull_RefusesTrade()`
  - L45: `private static SliceWorldState CreateMerchantReadyWorld()`

### `Assets/Tests/EditMode/Living`

#### `NeedMoodEvaluatorTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Living`
- types:
  - L13: `class NeedMoodEvaluatorTests`
- members:
  - L16: `public void NeutralNeeds_PreserveNeutralMood()`
  - L24: `public void NeedPressure_LowersMoodDeterministically()`
  - L37: `public void ThirstContributesWithoutMutatingNeeds()`
  - L47: `public void ActorOverload_ReadsActorNeedsAndRejectsNullActor()`
  - L56: `private static ActorRecord CreateActor(ActorNeeds needs)`

#### `NeedRecoverySystemEatTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Living`
- types:
  - L18: `class NeedRecoverySystemEatTests`
  - L118: `class InventoryStateTestExtensions`
- members:
  - L21: `public void EatMeal_ConsumesFoodLowersHungerAndLogsReasonTrace()`
  - L52: `public void EatMeal_MissingFoodPreservesInventoryActorAndLog()`
  - L68: `public void EatMeal_RejectsInvalidInputs()`
  - L84: `private static NeedRecoveryRecipe MealRecipe()`
  - L89: `private static NeedRecoveryRecipe SleepRecipe()`
  - L94: `private static InventoryState MealInventory(int quantity)`
  - L101: `private static ActorRecord CreateActor(ActorNeeds needs)`
  - L120: `public static bool IsEmpty(this InventoryState inventory)`

#### `NeedRecoverySystemSleepTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Living`
- types:
  - L16: `class NeedRecoverySystemSleepTests`
- members:
  - L19: `public void Sleep_LowersFatigueRecomputesMoodAndLogsReasonTrace()`
  - L47: `public void Sleep_WhenActorIsRestedDoesNotAppendEvent()`
  - L62: `public void Sleep_RejectsMealRecipeAndNullInputs()`
  - L76: `private static ActorRecord CreateActor(ActorNeeds needs)`

#### `NeedsEventLogTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Living`
- types:
  - L15: `class NeedsEventLogTests`
- members:
  - L18: `public void TickActorNeeds_AppendsNeedChangedEventWithReasonTrace()`
  - L48: `public void TickActorNeeds_NonPositiveTicksDoNotAppendEvent()`
  - L62: `public void TickActorNeeds_RejectsNullInputs()`
  - L71: `private static ActorRecord CreateActor()`

#### `NeedsSystemMoodTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Living`
- types:
  - L14: `class NeedsSystemMoodTests`
- members:
  - L17: `public void RecomputeMood_AppliesDerivedMoodToActor()`
  - L28: `public void ThreeUnfedNeedTicks_LowerMoodUnderRefusalThreshold()`
  - L42: `private static ActorRecord CreateActor(ActorNeeds needs = default)`

#### `NeedsSystemTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Living`
- types:
  - L11: `class NeedsSystemTests`
- members:
  - L14: `public void TickNeeds_AdvancesHungerAndFatigueOnly()`
  - L26: `public void TickNeeds_RepeatedTicksClampAtMaximum()`
  - L38: `public void TickNeeds_NonPositiveTicksDoNotChangeNeeds()`

### `Assets/Tests/EditMode/Magic`

#### `MagicTickDriverRegistryTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L17: `class MagicTickDriverRegistryTests`
- members:
  - L19: `private static MagicTickDriver NewDriver()`
  - L25: `public void AdvanceTicks_NullCooldownState_Throws()`
  - L34: `public void AdvanceTicks_NullRegistry_Throws()`
  - L43: `public void AdvanceTicks_NegativeElapsed_Throws()`
  - L52: `public void AdvanceTicks_ZeroElapsed_IsNoOp()`
  - L68: `public void AdvanceTicks_EmptyRegistry_StillAdvancesCooldown()`
  - L81: `public void AdvanceTicks_DecaysCooldownAndEveryActorBagByExactlyElapsedTicks()`
  - L104: `public void AdvanceTicks_ExpiresPerActorEntriesIndependently()`
  - L124: `public void AdvanceTicks_RepeatedCallsAccumulateDecayAcrossCooldownAndEveryActorBag()`
  - L147: `public void AdvanceTicks_MatchesSingleBagOverloadAppliedPerActor()`
  - L181: `public void AdvanceTicks_PerSideParity_MatchesUnderlyingServicesIndependently()`

#### `MagicTickDriverTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L13: `class MagicTickDriverTests`
- members:
  - L15: `private static MagicTickDriver NewDriver()`
  - L21: `public void Constructor_NullCooldownService_Throws()`
  - L27: `public void Constructor_NullShieldBuffService_Throws()`
  - L33: `public void AdvanceTicks_NullCooldownState_Throws()`
  - L41: `public void AdvanceTicks_NullShieldBuffState_Throws()`
  - L49: `public void AdvanceTicks_NegativeElapsed_Throws()`
  - L58: `public void AdvanceTicks_ZeroElapsed_IsNoOp()`
  - L73: `public void AdvanceTicks_DecaysBothBagsByExactlyElapsedTicks()`
  - L88: `public void AdvanceTicks_ExpiresEntriesInBothBagsAtTheSameElapsed()`
  - L104: `public void AdvanceTicks_EmptyBags_IsSafeNoOp()`
  - L115: `public void AdvanceTicks_RepeatedCallsAccumulateDecayAcrossBothBags()`
  - L133: `public void AdvanceTicks_MatchesUnderlyingServicesIndependently()`

#### `ShieldBuffActorKeyedApplicationServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L15: `class ShieldBuffActorKeyedApplicationServiceTests`
- members:
  - L21: `public void ApplyShieldBuffs_RegistryOverload_EmberWardCast_WritesIntoOwnActorBag()`
  - L44: `public void ApplyShieldBuffs_RegistryOverload_OnlyTouchesTargetActorBag()`
  - L66: `public void ApplyShieldBuffs_RegistryOverload_RecastReplacesEntryOnSameActorBag()`
  - L84: `public void ApplyShieldBuffs_RegistryOverload_MixedSpell_OnlyWritesTimedBuffEffectIntoActorBag()`
  - L117: `public void ApplyShieldBuffs_RegistryOverload_FlameBoltCast_ProducesNoBuffWritesAndStillCreatesEmptyBag()`
  - L134: `public void ApplyShieldBuffs_RegistryOverload_NullCast_IsRejectedAndRegistryIsUntouched()`
  - L148: `public void ApplyShieldBuffs_RegistryOverload_FailedCast_IsRejectedAndRegistryIsUntouched()`
  - L162: `public void ApplyShieldBuffs_RegistryOverload_NullRegistry_ThrowsArgumentNull()`
  - L171: `public void ApplyShieldBuffs_RegistryOverload_WhitespaceActorId_ThrowsArgument()`
  - L183: `public void ApplyShieldBuffs_RegistryOverload_ParityWithSingleBagOverloadOnSameInputState()`

#### `ShieldBuffApplicationServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L14: `class ShieldBuffApplicationServiceTests`
- members:
  - L17: `public void ApplyShieldBuffs_EmberWardCast_RecordsBuffWithMagnitudeAndDuration()`
  - L37: `public void ApplyShieldBuffs_EmberWardCastReplacingActiveBuff_OverwritesEntry()`
  - L53: `public void ApplyShieldBuffs_MultipleTimedBuffs_AppliesAllInDefinitionOrder()`
  - L83: `public void ApplyShieldBuffs_MixedSpell_OnlyWritesTimedBuffEffects()`
  - L114: `public void ApplyShieldBuffs_InstantaneousShieldBuffEffect_IsIgnoredAsBuff()`
  - L139: `public void ApplyShieldBuffs_FlameBoltCast_ProducesNoBuffWritesAndStateStaysEmpty()`
  - L153: `public void ApplyShieldBuffs_NullCast_IsRejectedAndStateIsUntouched()`
  - L169: `public void ApplyShieldBuffs_FailedCast_IsRejectedAndStateIsUntouched()`
  - L183: `public void ApplyShieldBuffs_NullShieldBuffState_IsRejectedWithInvalidBuffState()`
  - L196: `public void ApplyShieldBuffs_DoesNotAffectInstantaneousResolutionRejection()`
  - L213: `private static ActorRecord CreateGuard()`

#### `ShieldBuffServiceAbsorptionTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L16: `class ShieldBuffServiceAbsorptionTests`
- members:
  - L19: `public void AbsorbDamage_NullState_Throws()`
  - L27: `public void AbsorbDamage_NegativeDamage_Throws()`
  - L36: `public void AbsorbDamage_ZeroDamage_NoOpReturnsEmptyTrace()`
  - L54: `public void AbsorbDamage_EmptyState_ReturnsRemainingEqualsIncoming()`
  - L69: `public void AbsorbDamage_SingleBuff_PartiallyConsumed_ReducesMagnitudeAndPreservesTicks()`
  - L88: `public void AbsorbDamage_SingleBuff_ExactlyConsumed_RemovesBuffEvenWhenTicksRemain()`
  - L107: `public void AbsorbDamage_SingleBuff_OverConsumed_RemovesBuffAndReturnsRemainingDamage()`
  - L124: `public void AbsorbDamage_MultipleBuffs_DeterministicOrdinalOrder_ConsumesAegisBeforeEmber()`
  - L146: `public void AbsorbDamage_MultipleBuffs_FirstFullyConsumedThenSecondPartial()`
  - L165: `public void AbsorbDamage_MultipleBuffs_ExhaustsAllAndReturnsRemainingDamage()`
  - L182: `public void AbsorbDamage_StopsOnceDamageIsZero_LeavesLaterBuffsUntouched()`
  - L201: `public void AbsorbDamage_SkipsZeroMagnitudeBuffs_WithoutMarkingThemConsumed()`
  - L221: `public void AbsorbDamage_RepeatedCallsAccumulateMagnitudeReduction()`
  - L238: `public void AbsorbDamage_DoesNotChangeRemainingTicks_OnPartialConsume()`
  - L251: `public void AbsorbDamage_AbsorbedPlusRemaining_AlwaysEqualsIncoming()`
  - L267: `public void AbsorbDamage_AfterAdvanceTicksExpiresBuff_BuffNoLongerAbsorbs()`

#### `ShieldBuffServiceBatchAbsorptionTotalsFromStackedFilterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L24: `class ShieldBuffServiceBatchAbsorptionTotalsFromStackedFilterTests`
- members:
  - L27: `public void From_StackedFilter_NullResultMap_Throws()`
  - L37: `public void ComputeBatchTotals_StackedFilter_NullResultMap_Throws()`
  - L49: `public void From_StackedFilter_WhitespaceActorKey_StillThrowsBeforeFilters()`
  - L71: `public void From_StackedFilter_NullActorResult_StillThrowsBeforeFilters()`
  - L86: `public void ComputeBatchTotals_StackedFilter_NullFilter_BehavesLikePredicateOnlyOverload()`
  - L113: `public void From_StackedFilter_BothPredicatesNull_MatchesUnfilteredFrom()`
  - L138: `public void ComputeBatchTotals_StackedFilter_FilterRejectsAll_TotalsAllZero()`
  - L162: `public void ComputeBatchTotals_StackedFilter_FilterAcceptsAll_ParityWithPredicateOnlyOverload()`
  - L189: `public void ComputeBatchTotals_StackedFilter_OnlyAbsorbingAllies_ParityWithFilteredThenIncluded()`
  - L230: `public void ComputeBatchTotals_StackedFilter_FilterConsultedBeforeIncludePredicate()`
  - L261: `public void ComputeBatchTotals_StackedFilter_OrderIndependent()`
  - L298: `public void ComputeBatchTotals_StackedFilter_DoesNotMutateRegistry()`
  - L326: `private static void AssertAllZero(ShieldBuffAbsorptionBatchTotals totals)`

#### `ShieldBuffServiceBatchAbsorptionTotalsGroupByFilterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L20: `class ShieldBuffServiceBatchAbsorptionTotalsGroupByFilterTests`
- members:
  - L23: `public void GroupByFilter_NullResultMap_Throws()`
  - L33: `public void GroupByFilter_NullKeyExtractor_Throws()`
  - L45: `public void GroupBatchTotalsWithFilter_NullResultMap_Throws()`
  - L54: `public void GroupBatchTotalsWithFilter_NullKeyExtractor_Throws()`
  - L64: `public void GroupByFilter_WhitespaceActorKey_StillThrowsBeforePredicate()`
  - L86: `public void GroupByFilter_NullActorResult_StillThrowsBeforePredicate()`
  - L101: `public void GroupBatchTotalsWithFilter_KeyExtractorReturnsNullForIncluded_Throws()`
  - L119: `public void GroupBatchTotalsWithFilter_EmptyMap_ReturnsEmptyGroupDictionary()`
  - L132: `public void GroupBatchTotalsWithFilter_AllExcluded_ReturnsEmptyGroupDictionary()`
  - L157: `public void GroupBatchTotalsWithFilter_NullPredicate_BehavesLikeUnfilteredGroupBy()`
  - L189: `public void GroupBatchTotalsWithFilter_PerBucketParityVsTwoConditionFilteredComputeBatchTotals()`
  - L228: `public void GroupBatchTotalsWithFilter_SumOfAllBuckets_EqualsFilteredComputeBatchTotals()`
  - L285: `public void GroupBatchTotalsWithFilter_DoesNotMutateRegistry()`

#### `ShieldBuffServiceBatchAbsorptionTotalsGroupByManyFilterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L25: `class ShieldBuffServiceBatchAbsorptionTotalsGroupByManyFilterTests`
- members:
  - L28: `public void GroupByMany_NullSequence_ThrowsBeforePredicate()`
  - L38: `public void GroupBatchTotalsByMany_NullSequence_ThrowsBeforePredicate()`
  - L50: `public void GroupByMany_NullKeyExtractor_ThrowsBeforePredicate()`
  - L60: `public void GroupBatchTotalsByMany_NullKeyExtractor_ThrowsBeforePredicate()`
  - L72: `public void GroupByMany_NullElement_ThrowsBeforePredicate()`
  - L91: `public void GroupBatchTotalsByMany_NullElement_ThrowsBeforePredicate()`
  - L111: `public void GroupByMany_WhitespaceGroupKeyOnKeptElement_Throws()`
  - L123: `public void GroupByMany_WhitespaceGroupKeyOnFilteredOutElement_DoesNotThrow()`
  - L139: `public void GroupByMany_NullPredicate_BehavesAsUnfilteredOverload()`
  - L165: `public void GroupByMany_EmptySequence_ReturnsEmptyDictionary()`
  - L177: `public void GroupByMany_FullyFilteredOut_ReturnsEmptyDictionary()`
  - L191: `public void GroupByMany_SingleBucket_MatchesFilteredMergeMany()`
  - L216: `public void GroupByMany_TwoBuckets_MatchPerBucketPreFilteredMergeMany()`
  - L260: `public void GroupByMany_BucketUnionEqualsFilteredMergeMany()`
  - L286: `public void GroupByMany_IsPermutationInvariant()`
  - L317: `public void GroupBatchTotalsByMany_DoesNotMutateRegistry()`
  - L351: `private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()`

#### `ShieldBuffServiceBatchAbsorptionTotalsGroupByManyStackedFilterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L27: `class ShieldBuffServiceBatchAbsorptionTotalsGroupByManyStackedFilterTests`
- members:
  - L30: `public void GroupByMany_NullSequence_ThrowsBeforePredicates()`
  - L41: `public void GroupBatchTotalsByMany_NullSequence_ThrowsBeforePredicates()`
  - L54: `public void GroupByMany_NullKeyExtractor_ThrowsBeforePredicates()`
  - L65: `public void GroupBatchTotalsByMany_NullKeyExtractor_ThrowsBeforePredicates()`
  - L78: `public void GroupByMany_NullElement_ThrowsBeforePredicates()`
  - L98: `public void GroupBatchTotalsByMany_NullElement_ThrowsBeforePredicates()`
  - L119: `public void GroupByMany_WhitespaceGroupKeyOnKeptElement_Throws()`
  - L132: `public void GroupByMany_WhitespaceGroupKeyOnFilterRejectedElement_DoesNotThrow()`
  - L149: `public void GroupByMany_WhitespaceGroupKeyOnIncludeRejectedElement_DoesNotThrow()`
  - L166: `public void GroupByMany_NullFilterPredicate_BehavesAsThreeArgOverload()`
  - L192: `public void GroupByMany_BothPredicatesNull_BehavesAsUnfilteredOverload()`
  - L216: `public void GroupByMany_EmptySequence_ReturnsEmptyDictionary()`
  - L229: `public void GroupByMany_FilterRejectsAll_ReturnsEmptyDictionary()`
  - L244: `public void GroupByMany_IncludeRejectsAll_ReturnsEmptyDictionary()`
  - L259: `public void GroupByMany_FilterConsultedBeforeIncludePredicate()`
  - L277: `public void GroupByMany_SingleBucket_MatchesStackedFilterMergeMany()`
  - L319: `public void GroupByMany_TwoBuckets_MatchPerBucketPreStackedFilterMergeMany()`
  - L367: `public void GroupByMany_BucketUnionEqualsStackedFilterMergeMany()`
  - L398: `public void GroupByMany_IsPermutationInvariant()`
  - L428: `public void GroupBatchTotalsByMany_DoesNotMutateRegistry()`
  - L463: `private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()`

#### `ShieldBuffServiceBatchAbsorptionTotalsGroupByManyTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L24: `class ShieldBuffServiceBatchAbsorptionTotalsGroupByManyTests`
- members:
  - L27: `public void GroupByMany_NullSequence_Throws()`
  - L36: `public void GroupBatchTotalsByMany_NullSequence_Throws()`
  - L45: `public void GroupByMany_NullKeyExtractor_Throws()`
  - L54: `public void GroupBatchTotalsByMany_NullKeyExtractor_Throws()`
  - L65: `public void GroupByMany_NullElement_ThrowsBeforeKeyExtractor()`
  - L83: `public void GroupBatchTotalsByMany_NullElement_ThrowsBeforeKeyExtractor()`
  - L100: `public void GroupByMany_WhitespaceGroupKey_Throws()`
  - L111: `public void GroupByMany_EmptySequence_ReturnsEmptyDictionary()`
  - L122: `public void GroupByMany_SingleBucket_MatchesMergeMany()`
  - L138: `public void GroupByMany_TwoBuckets_MatchPreFilteredMergeManyOnEachBucket()`
  - L174: `public void GroupByMany_BucketUnionEqualsUnfilteredMergeMany()`
  - L199: `public void GroupByMany_IsPermutationInvariant()`
  - L226: `public void GroupBatchTotalsByMany_DoesNotMutateRegistry()`
  - L259: `private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()`

#### `ShieldBuffServiceBatchAbsorptionTotalsGroupByStackedFilterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L25: `class ShieldBuffServiceBatchAbsorptionTotalsGroupByStackedFilterTests`
- members:
  - L28: `public void GroupByStackedFilter_NullResultMap_Throws()`
  - L39: `public void GroupBatchTotalsWithStackedFilter_NullResultMap_Throws()`
  - L52: `public void GroupByStackedFilter_NullKeyExtractor_Throws()`
  - L65: `public void GroupBatchTotalsWithStackedFilter_NullKeyExtractor_Throws()`
  - L79: `public void GroupByStackedFilter_WhitespaceActorKey_StillThrowsBeforePredicates()`
  - L102: `public void GroupByStackedFilter_NullActorResult_StillThrowsBeforePredicates()`
  - L118: `public void GroupByStackedFilter_WhitespaceGroupKeyOnKeptEntry_Throws()`
  - L137: `public void GroupByStackedFilter_WhitespaceGroupKeyOnFilterRejectedEntry_DoesNotThrow()`
  - L158: `public void GroupByStackedFilter_WhitespaceGroupKeyOnIncludeRejectedEntry_DoesNotThrow()`
  - L179: `public void GroupByStackedFilter_NullFilterPredicate_BehavesAsThreeArgOverload()`
  - L213: `public void GroupByStackedFilter_BothPredicatesNull_BehavesAsUnfilteredOverload()`
  - L244: `public void GroupByStackedFilter_EmptyMap_ReturnsEmptyDictionary()`
  - L258: `public void GroupByStackedFilter_FilterRejectsAll_ReturnsEmptyDictionary()`
  - L284: `public void GroupByStackedFilter_IncludeRejectsAll_ReturnsEmptyDictionary()`
  - L310: `public void GroupByStackedFilter_FilterConsultedBeforeIncludePredicate()`
  - L347: `public void GroupByStackedFilter_PerBucketParityVsTwoConditionFilteredComputeBatchTotals()`
  - L388: `public void GroupByStackedFilter_SumOfAllBuckets_EqualsStackedFilterComputeBatchTotals()`
  - L450: `public void GroupBatchTotalsWithStackedFilter_DoesNotMutateRegistry()`

#### `ShieldBuffServiceBatchAbsorptionTotalsGroupByTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L20: `class ShieldBuffServiceBatchAbsorptionTotalsGroupByTests`
- members:
  - L23: `public void GroupBy_NullResultMap_Throws()`
  - L30: `public void GroupBy_NullKeyExtractor_Throws()`
  - L39: `public void GroupBatchTotals_NullResultMap_Throws()`
  - L48: `public void GroupBatchTotals_NullKeyExtractor_Throws()`
  - L58: `public void GroupBy_WhitespaceActorKey_StillThrowsBeforeKeyExtractor()`
  - L77: `public void GroupBy_NullActorResult_StillThrowsBeforeKeyExtractor()`
  - L89: `public void GroupBatchTotals_KeyExtractorReturnsNull_Throws()`
  - L104: `public void GroupBatchTotals_KeyExtractorReturnsWhitespace_Throws()`
  - L119: `public void GroupBatchTotals_EmptyMap_ReturnsEmptyGroupDictionary()`
  - L131: `public void GroupBatchTotals_AllIntoOneKey_SingleBucketEqualsUnfilteredTotals()`
  - L155: `public void GroupBatchTotals_BySidePrefix_BucketsMatchFilteredComputeBatchTotals()`
  - L194: `public void GroupBatchTotals_SumOfAllBuckets_EqualsUnfilteredBatchTotals()`
  - L247: `public void GroupBatchTotals_ParityWithBinaryPartitionFrom()`
  - L278: `public void GroupBatchTotals_DoesNotMutateRegistry()`

#### `ShieldBuffServiceBatchAbsorptionTotalsMergeManyFilterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L22: `class ShieldBuffServiceBatchAbsorptionTotalsMergeManyFilterTests`
- members:
  - L25: `public void MergeMany_Filtered_NullSequence_Throws()`
  - L34: `public void MergeBatchTotalsMany_Filtered_NullSequence_Throws()`
  - L43: `public void MergeMany_Filtered_NullElement_ThrowsBeforePredicate()`
  - L61: `public void MergeBatchTotalsMany_Filtered_NullElement_ThrowsBeforePredicate()`
  - L78: `public void MergeMany_NullPredicate_MatchesUnfilteredOverload()`
  - L92: `public void MergeMany_PredicateAlwaysTrue_MatchesUnfilteredOverload()`
  - L106: `public void MergeMany_PredicateAlwaysFalse_ReturnsEmpty()`
  - L118: `public void MergeMany_Filtered_EmptySequence_ReturnsEmpty()`
  - L128: `public void MergeMany_Filtered_MatchesPreFilteredMergeMany()`
  - L161: `public void MergeMany_Filtered_IsPermutationInvariant()`
  - L183: `public void MergeBatchTotalsMany_Filtered_DoesNotMutateRegistry()`
  - L216: `private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()`

#### `ShieldBuffServiceBatchAbsorptionTotalsMergeManyStackedFilterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L24: `class ShieldBuffServiceBatchAbsorptionTotalsMergeManyStackedFilterTests`
- members:
  - L27: `public void MergeMany_StackedFilter_NullSequence_Throws()`
  - L37: `public void MergeBatchTotalsMany_StackedFilter_NullSequence_Throws()`
  - L49: `public void MergeMany_StackedFilter_NullElement_ThrowsBeforeFilters()`
  - L68: `public void MergeBatchTotalsMany_StackedFilter_NullElement_ThrowsBeforeFilters()`
  - L88: `public void MergeMany_StackedFilter_NullFilterPredicate_MatchesPredicateOnlyOverload()`
  - L106: `public void MergeMany_StackedFilter_BothNull_MatchesUnfilteredOverload()`
  - L121: `public void MergeMany_StackedFilter_FilterAlwaysFalse_ReturnsEmpty()`
  - L134: `public void MergeMany_StackedFilter_FilterAlwaysTrue_MatchesPredicateOnlyOverload()`
  - L152: `public void MergeMany_StackedFilter_MatchesHandPreFilteredMergeMany()`
  - L188: `public void MergeMany_StackedFilter_FilterRejectsBeforeIncludeIsConsulted()`
  - L206: `public void MergeMany_StackedFilter_IsPermutationInvariant()`
  - L233: `public void MergeBatchTotalsMany_StackedFilter_DoesNotMutateRegistry()`
  - L267: `private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()`

#### `ShieldBuffServiceBatchAbsorptionTotalsMergeManyTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L20: `class ShieldBuffServiceBatchAbsorptionTotalsMergeManyTests`
- members:
  - L23: `public void MergeMany_NullSequence_Throws()`
  - L30: `public void MergeBatchTotalsMany_NullSequence_Throws()`
  - L38: `public void MergeMany_NullElement_Throws()`
  - L54: `public void MergeBatchTotalsMany_NullElement_Throws()`
  - L71: `public void MergeMany_EmptySequence_ReturnsEmpty()`
  - L80: `public void MergeBatchTotalsMany_EmptySequence_ReturnsEmpty()`
  - L91: `public void MergeMany_SingletonSequence_EqualsSingleSnapshot()`
  - L102: `public void MergeMany_MatchesChainedPairwiseMerge()`
  - L115: `public void MergeMany_MatchesFromOverUnionOfAllBatches()`
  - L152: `public void MergeMany_IsPermutationInvariant()`
  - L168: `public void MergeMany_EmptyEntriesAreIdentities()`
  - L184: `public void MergeBatchTotalsMany_DoesNotMutateRegistry()`
  - L215: `private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()`

#### `ShieldBuffServiceBatchAbsorptionTotalsMergeTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L18: `class ShieldBuffServiceBatchAbsorptionTotalsMergeTests`
- members:
  - L21: `public void Merge_NullLeft_Throws()`
  - L30: `public void Merge_NullRight_Throws()`
  - L39: `public void MergeBatchTotals_NullLeft_Throws()`
  - L48: `public void MergeBatchTotals_NullRight_Throws()`
  - L57: `public void Empty_AllCountersZero()`
  - L71: `public void Empty_MatchesFromOverEmptyMap()`
  - L80: `public void Merge_EmptyIsLeftIdentity()`
  - L91: `public void Merge_EmptyIsRightIdentity()`
  - L102: `public void Merge_SumsEveryCounterFieldWise()`
  - L138: `public void Merge_MatchesFromOverUnionOfBothBatches()`
  - L169: `public void Merge_IsCommutative()`
  - L190: `public void Merge_IsAssociative()`
  - L218: `public void MergeBatchTotals_DoesNotMutateRegistry()`
  - L245: `private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()`

#### `ShieldBuffServiceBatchAbsorptionTotalsPartitionFromStackedFilterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L22: `class ShieldBuffServiceBatchAbsorptionTotalsPartitionFromStackedFilterTests`
- members:
  - L25: `public void PartitionFrom_StackedFilter_NullResultMap_Throws()`
  - L35: `public void PartitionFrom_StackedFilter_NullIncludePredicate_Throws()`
  - L47: `public void ComputeBatchTotalsPartition_StackedFilter_NullResultMap_Throws()`
  - L59: `public void ComputeBatchTotalsPartition_StackedFilter_NullIncludePredicate_Throws()`
  - L72: `public void PartitionFrom_StackedFilter_WhitespaceActorKey_StillThrowsBeforeFilters()`
  - L94: `public void PartitionFrom_StackedFilter_NullActorResult_StillThrowsBeforeFilters()`
  - L109: `public void ComputeBatchTotalsPartition_StackedFilter_NullFilter_BehavesLikeBinaryPartition()`
  - L137: `public void ComputeBatchTotalsPartition_StackedFilter_FilterRejectsAll_BothBucketsZero()`
  - L162: `public void ComputeBatchTotalsPartition_StackedFilter_FilterAcceptsAll_ParityWithBinaryPartition()`
  - L190: `public void ComputeBatchTotalsPartition_StackedFilter_OnlyAbsorbingAllies_ParityWithFilteredThenPartition()`
  - L232: `public void ComputeBatchTotalsPartition_StackedFilter_IncludedPlusExcluded_EqualsFilteredFrom()`
  - L282: `public void ComputeBatchTotalsPartition_StackedFilter_FilterConsultedBeforeIncludePredicate()`
  - L314: `public void ComputeBatchTotalsPartition_StackedFilter_OrderIndependent()`
  - L352: `public void ComputeBatchTotalsPartition_StackedFilter_DoesNotMutateRegistry()`
  - L380: `private static void AssertAllZero(ShieldBuffAbsorptionBatchTotals totals)`

#### `ShieldBuffServiceBatchAbsorptionTotalsPartitionManyFilterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L29: `class ShieldBuffServiceBatchAbsorptionTotalsPartitionManyFilterTests`
- members:
  - L32: `public void PartitionMany_NullSequence_Throws()`
  - L42: `public void PartitionBatchTotalsMany_NullSequence_Throws()`
  - L54: `public void PartitionMany_NullIncludePredicate_Throws()`
  - L64: `public void PartitionBatchTotalsMany_NullIncludePredicate_Throws()`
  - L76: `public void PartitionMany_NullElement_ThrowsBeforePredicates()`
  - L97: `public void PartitionBatchTotalsMany_NullElement_ThrowsBeforePredicates()`
  - L117: `public void PartitionMany_NullFilterPredicate_BehavesLikeBinaryOverload()`
  - L142: `public void PartitionMany_FilterAlwaysTrue_BehavesLikeBinaryOverload()`
  - L167: `public void PartitionMany_FilterAlwaysFalse_BothBucketsAreEmpty()`
  - L182: `public void PartitionMany_BucketsSumToPreFilteredMergeMany()`
  - L215: `public void PartitionMany_MatchesBinaryPartitionOverPreFilteredSequence()`
  - L251: `public void PartitionMany_IsPermutationInvariant()`
  - L280: `public void PartitionBatchTotalsMany_DoesNotMutateRegistry()`
  - L314: `private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()`

#### `ShieldBuffServiceBatchAbsorptionTotalsPartitionManyTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L25: `class ShieldBuffServiceBatchAbsorptionTotalsPartitionManyTests`
- members:
  - L28: `public void PartitionMany_NullSequence_Throws()`
  - L37: `public void PartitionBatchTotalsMany_NullSequence_Throws()`
  - L46: `public void PartitionMany_NullPredicate_Throws()`
  - L55: `public void PartitionBatchTotalsMany_NullPredicate_Throws()`
  - L66: `public void PartitionMany_NullElement_ThrowsBeforePredicate()`
  - L84: `public void PartitionBatchTotalsMany_NullElement_ThrowsBeforePredicate()`
  - L101: `public void PartitionMany_EmptySequence_BothBucketsAreEmpty()`
  - L112: `public void PartitionMany_PredicateAlwaysTrue_IncludedMatchesMergeMany_ExcludedIsEmpty()`
  - L127: `public void PartitionMany_PredicateAlwaysFalse_ExcludedMatchesMergeMany_IncludedIsEmpty()`
  - L142: `public void PartitionMany_BucketsSumToUnfilteredMergeMany()`
  - L169: `public void PartitionMany_MatchesPreFilteredMergeManyOnEachBucket()`
  - L204: `public void PartitionMany_IsPermutationInvariant()`
  - L228: `public void PartitionBatchTotalsMany_DoesNotMutateRegistry()`
  - L261: `private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()`

#### `ShieldBuffServiceBatchAbsorptionTotalsPartitionTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L19: `class ShieldBuffServiceBatchAbsorptionTotalsPartitionTests`
- members:
  - L22: `public void PartitionFrom_NullResultMap_Throws()`
  - L29: `public void PartitionFrom_NullPredicate_Throws()`
  - L38: `public void ComputeBatchTotalsPartition_NullResultMap_Throws()`
  - L47: `public void ComputeBatchTotalsPartition_NullPredicate_Throws()`
  - L57: `public void PartitionFrom_WhitespaceActorKey_StillThrowsBeforeFilter()`
  - L76: `public void PartitionFrom_NullActorResult_StillThrowsBeforeFilter()`
  - L88: `public void ComputeBatchTotalsPartition_EmptyMap_BothBucketsZero()`
  - L100: `public void ComputeBatchTotalsPartition_PredicateAllTrue_AllInIncludedBucket()`
  - L123: `public void ComputeBatchTotalsPartition_PredicateAllFalse_AllInExcludedBucket()`
  - L146: `public void ComputeBatchTotalsPartition_PredicateBySideKeyPrefix_ReportsBothBucketsFromOnePass()`
  - L188: `public void ComputeBatchTotalsPartition_IncludedPlusExcluded_EqualsUnfilteredBatchTotals()`
  - L234: `public void ComputeBatchTotalsPartition_ParityWithTwoFilteredComputeBatchTotalsCalls()`
  - L263: `public void ComputeBatchTotalsPartition_DoesNotMutateRegistry()`
  - L288: `private static void AssertAllZero(ShieldBuffAbsorptionBatchTotals totals)`

#### `ShieldBuffServiceBatchAbsorptionTotalsTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L17: `class ShieldBuffServiceBatchAbsorptionTotalsTests`
- members:
  - L20: `public void ComputeBatchTotals_NullResultMap_Throws()`
  - L28: `public void From_NullResultMap_Throws()`
  - L34: `public void From_WhitespaceActorKey_Throws()`
  - L52: `public void From_NullActorResult_Throws()`
  - L63: `public void ComputeBatchTotals_EmptyMap_AllZero()`
  - L79: `public void ComputeBatchTotals_SingleActorFullAbsorption_ReportsSingleActorTotals()`
  - L101: `public void ComputeBatchTotals_UntrackedActor_TreatedAsRemainingOnly()`
  - L122: `public void ComputeBatchTotals_MultiActorMixedAbsorption_AggregatesAcrossActors()`
  - L153: `public void ComputeBatchTotals_ParityWithManualWalk()`
  - L197: `public void From_PredicateNullMap_Throws()`
  - L204: `public void From_PredicateOverloadWithNullPredicate_AggregatesAllEntries()`
  - L232: `public void From_PredicateOverloadWhitespaceActorKey_StillThrowsBeforeFilter()`
  - L251: `public void From_PredicateOverloadNullActorResult_StillThrowsBeforeFilter()`
  - L263: `public void ComputeBatchTotals_PredicateNullMap_Throws()`
  - L272: `public void ComputeBatchTotals_PredicateExcludesAllActors_AllZero()`
  - L299: `public void ComputeBatchTotals_PredicateBySideKeyPrefix_ReportsOnlyMatchingSubset()`
  - L345: `public void ComputeBatchTotals_PredicateByAbsorptionFlag_ExcludesNonAbsorbers()`
  - L376: `public void ComputeBatchTotals_PredicateSubsetSumsAddBackToWholeBatch()`
  - L417: `public void ComputeBatchTotals_PredicateDoesNotMutateRegistry()`
  - L443: `public void ComputeBatchTotals_DoesNotMutateRegistry()`

#### `ShieldBuffServiceRegistryAbsorptionTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L16: `class ShieldBuffServiceRegistryAbsorptionTests`
- members:
  - L19: `public void AbsorbDamageForActor_NullRegistry_Throws()`
  - L27: `public void AbsorbDamageForActor_NullActorId_Throws()`
  - L36: `public void AbsorbDamageForActor_WhitespaceActorId_Throws()`
  - L45: `public void AbsorbDamageForActor_NegativeDamage_Throws()`
  - L54: `public void AbsorbDamageForActor_ZeroDamage_ReturnsEmptyTrace()`
  - L73: `public void AbsorbDamageForActor_ZeroDamage_DoesNotAddActorToRegistry()`
  - L85: `public void AbsorbDamageForActor_UntrackedActor_ReturnsFullRemainingAndEmptyTrace()`
  - L100: `public void AbsorbDamageForActor_UntrackedActor_DoesNotAddActorToRegistry()`
  - L112: `public void AbsorbDamageForActor_TrackedActor_PartialConsumeReducesMagnitudeAndPreservesTicks()`
  - L131: `public void AbsorbDamageForActor_TrackedActor_ExactConsumeClearsBuffEvenWithTicksLeft()`
  - L149: `public void AbsorbDamageForActor_TrackedActor_OverConsumeReturnsLeftover()`
  - L166: `public void AbsorbDamageForActor_TrackedActor_MultiBuffOrdinalOrderingMirrorsSingleBag()`
  - L186: `public void AbsorbDamageForActor_OnlyAffectsTargetActor_LeavesOtherActorsUntouched()`
  - L205: `public void AbsorbDamageForActor_RepeatedCalls_AccumulateMagnitudeReductionWithoutTickChange()`
  - L220: `public void AbsorbDamageForActor_AfterTickSweepExpiresBuff_NoLongerAbsorbs()`
  - L237: `public void AbsorbDamageForActor_TrackedActorEmptyBag_ReturnsFullRemainingAndEmptyTrace()`
  - L253: `public void AbsorbDamageForActor_TrackedActor_ParityWithDirectAbsorbDamage()`

#### `ShieldBuffServiceRegistryBatchAbsorptionTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L17: `class ShieldBuffServiceRegistryBatchAbsorptionTests`
- members:
  - L20: `public void AbsorbDamageForActors_NullRegistry_Throws()`
  - L30: `public void AbsorbDamageForActors_NullIncomingMap_Throws()`
  - L39: `public void AbsorbDamageForActors_NullActorKey_Throws()`
  - L51: `public void AbsorbDamageForActors_EmptyActorKey_Throws()`
  - L61: `public void AbsorbDamageForActors_NegativeDamageValue_Throws()`
  - L71: `public void AbsorbDamageForActors_EmptyMap_ReturnsEmptyResultAndDoesNotMutateRegistry()`
  - L87: `public void AbsorbDamageForActors_ZeroDamageEntry_ProducesZeroResultAndPreservesBag()`
  - L110: `public void AbsorbDamageForActors_UntrackedActor_ReturnsFullRemainingAndEmptyTrace()`
  - L129: `public void AbsorbDamageForActors_UntrackedActor_DoesNotAddActorToRegistry()`
  - L148: `public void AbsorbDamageForActors_MixedTrackedAndUntracked_DispatchesPerActor()`
  - L193: `public void AbsorbDamageForActors_OnlyAffectsActorsInInputMap()`
  - L215: `public void AbsorbDamageForActors_ParityWithIndividualAbsorbDamageForActorCalls()`
  - L261: `public void AbsorbDamageForActors_ResultKeysMirrorInputKeys()`
  - L284: `public void AbsorbDamageForActors_AfterTickSweepExpiresBuffs_NoLongerAbsorbs()`
  - L311: `public void AbsorbDamageForActors_TrackedActorEmptyBag_ReturnsFullRemainingAndEmptyTrace()`
  - L331: `public void AbsorbDamageForActors_DoesNotEnumerateUnreferencedRegistryActors()`

#### `ShieldBuffServiceRegistrySweepTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L15: `class ShieldBuffServiceRegistrySweepTests`
- members:
  - L18: `public void AdvanceTicksForAllActors_NullRegistry_Throws()`
  - L26: `public void AdvanceTicksForAllActors_NegativeElapsed_Throws()`
  - L35: `public void AdvanceTicksForAllActors_ZeroElapsed_IsNoOp()`
  - L49: `public void AdvanceTicksForAllActors_EmptyRegistry_IsNoOp()`
  - L59: `public void AdvanceTicksForAllActors_DecaysEachActorIndependently()`
  - L77: `public void AdvanceTicksForAllActors_ExpiresOnlyTheActorsBuffsThatHitZero()`
  - L96: `public void AdvanceTicksForAllActors_IsParityWithPerActorAdvanceTicks()`
  - L126: `public void AdvanceTicksForAllActors_RepeatedCallsAccumulateDecayPerActor()`
  - L146: `public void AdvanceTicksForAllActors_ActorWithEmptyBag_StaysEmpty()`

#### `ShieldBuffServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L13: `class ShieldBuffServiceTests`
- members:
  - L16: `public void AdvanceTicks_ReducesRemainingTicksAndPreservesMagnitude()`
  - L30: `public void AdvanceTicks_ExactExpiry_RemovesEntry()`
  - L45: `public void AdvanceTicks_OverExpiry_ClampsToZeroAndRemovesEntry()`
  - L58: `public void AdvanceTicks_MultipleBuffs_DecaysIndependentlyAndExpiresOnlyExpired()`
  - L74: `public void AdvanceTicks_ZeroElapsed_IsNoOp()`
  - L87: `public void AdvanceTicks_EmptyState_IsNoOp()`
  - L97: `public void AdvanceTicks_NegativeElapsed_Throws()`
  - L106: `public void AdvanceTicks_NullState_Throws()`
  - L114: `public void AdvanceTicks_ZeroMagnitudeBuff_DecaysAndExpiresLikeAnyOther()`
  - L132: `public void AdvanceTicks_RepeatedCallsAccumulateDecay()`
  - L147: `public void AdvanceTicks_DoesNotResurrectAlreadyExpiredEntries()`

#### `ShieldBuffStateRegistryTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L12: `class ShieldBuffStateRegistryTests`
- members:
  - L15: `public void HasState_UntrackedActor_ReturnsFalse()`
  - L23: `public void HasState_NullOrWhitespaceActorId_ReturnsFalse()`
  - L33: `public void GetOrCreate_NullOrWhitespaceActorId_Throws()`
  - L43: `public void GetOrCreate_NewActor_ReturnsFreshState()`
  - L55: `public void GetOrCreate_SameActorTwice_ReturnsSameInstance()`
  - L66: `public void GetOrCreate_DifferentActors_ReturnsDistinctInstances()`
  - L79: `public void GetOrCreate_PreservesPerActorBuffs_AcrossLookups()`
  - L91: `public void GetOrCreate_DoesNotLeakStateBetweenActors()`
  - L104: `public void GetOrNull_UntrackedActor_ReturnsNull()`
  - L112: `public void GetOrNull_NullOrWhitespaceActorId_ReturnsNull()`
  - L122: `public void GetOrNull_TrackedActor_ReturnsExistingInstance()`
  - L133: `public void GetOrNull_DoesNotCreateState()`
  - L144: `public void GetTrackedActorIds_EmptyRegistry_ReturnsEmpty()`
  - L152: `public void GetTrackedActorIds_ListsAllActorsWithState()`
  - L165: `public void Remove_TrackedActor_DropsState()`
  - L178: `public void Remove_UntrackedActor_IsNoOp()`
  - L188: `public void Remove_OneActor_LeavesOthersIntact()`
  - L202: `public void GetOrCreate_AfterRemove_CreatesFreshState()`

#### `ShieldBuffStateTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L11: `class ShieldBuffStateTests`
- members:
  - L14: `public void GetRemainingTicks_UntrackedSpell_ReturnsZero()`
  - L23: `public void GetMagnitude_UntrackedSpell_ReturnsZero()`
  - L31: `public void SetActiveBuff_PositiveTicks_StoresTicksAndMagnitude()`
  - L43: `public void SetActiveBuff_ZeroTicks_RemovesEntry()`
  - L55: `public void SetActiveBuff_ReplacesExistingEntry()`
  - L67: `public void SetActiveBuff_NegativeTicks_Throws()`
  - L76: `public void SetActiveBuff_NegativeMagnitude_Throws()`
  - L85: `public void SetActiveBuff_BlankSpellId_Throws()`
  - L96: `public void Clear_RemovesTrackedEntry()`
  - L110: `public void Clear_UnknownSpell_IsNoOp()`
  - L123: `public void GetTrackedSpellTemplateIds_ReturnsActiveEntries()`

#### `SliceSpellCatalogTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L11: `class SliceSpellCatalogTests`
- members:
  - L14: `public void All_ListsThreeStarterSpellsInStableOrder()`
  - L33: `public void Find_ReturnsSpellByTemplateId()`
  - L47: `public void Find_ReturnsNullForUnknownOrEmptyId()`
  - L55: `public void EmberWard_HasNonZeroDuration_AndShieldEffect()`
  - L66: `public void StarterSpells_ExposeDeterministicCooldownTicks()`

#### `SpellCastRollServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L15: `class SpellCastRollServiceTests`
- members:
  - L18: `public void Roll_NullCaster_FailsWithStableError()`
  - L32: `public void Roll_NullSpell_FailsWithStableError()`
  - L45: `public void Roll_NullRng_FailsWithStableError()`
  - L58: `public void Roll_IncapacitatedCaster_PropagatesUpstreamRefusal()`
  - L75: `public void Roll_InvalidSpellSchool_PropagatesAsChanceCalculationFailed()`
  - L97: `public void Roll_HealthyCaster_ForwardsChanceBreakdownAsThreshold()`
  - L119: `public void Roll_RollWithinThreshold_ReportsSuccess()`
  - L141: `public void Roll_RollAboveThreshold_ReportsMiss()`
  - L170: `public void Roll_SameSeed_ProducesSameRollAndThreshold()`
  - L185: `public void Roll_DifferentSeeds_CanProduceDifferentRolls()`
  - L199: `public void Roll_DoesNotMutateCasterMana()`
  - L211: `private static ActorRecord CreateActor(string name, int mind, int insight, int health = 16)`

#### `SpellCastingServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L13: `class SpellCastingServiceTests`
- members:
  - L16: `public void TryCast_KnownSpellWithEnoughMana_SpendsManaAndReturnsSuccess()`
  - L34: `public void TryCast_CooldownSpellSuccess_StartsCooldown()`
  - L54: `public void TryCast_ActiveCooldown_IsRejectedWithoutSpendingMana()`
  - L76: `public void TryCast_InsufficientMana_DoesNotSpendMana()`
  - L93: `public void TryCast_UnlearnedSpell_DoesNotSpendMana()`
  - L110: `public void TryCast_NullCaster_IsRejected()`
  - L125: `public void TryCast_BlankSpellId_ReturnsSpellNotFound()`
  - L139: `public void TryCast_NullKnownSpellSet_ReturnsSpellNotKnownWithoutSpendingMana()`
  - L153: `public void TryCast_UnknownSpell_ReturnsSpellNotFound()`
  - L167: `public void TryCast_IncapacitatedCaster_IsRejected()`
  - L182: `private static ActorRecord CreateCaster(int mana, int health)`
  - L197: `private static SpellDefinition CreateCooldownSpell()`

#### `SpellCooldownServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L12: `class SpellCooldownServiceTests`
- members:
  - L15: `public void StartCooldown_CooldownSpell_PersistsDeclaredTicks()`
  - L28: `public void StartCooldown_ZeroCooldownSpell_LeavesStateReady()`
  - L41: `public void AdvanceTicks_ReducesRemainingTicksAndExpiresAtZero()`
  - L57: `public void AdvanceTicks_NegativeElapsed_Throws()`
  - L66: `public void State_SetRemainingTicks_ZeroRemovesTrackedSpell()`
  - L76: `private static SpellDefinition CreateSpell(int cooldownTicks)`

#### `SpellCostCalculatorTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L11: `class SpellCostCalculatorTests`
- members:
  - L14: `public void TargetMultipliers_OrderFromSelfTouchToRangedArea()`
  - L26: `public void EstimateTotalManaCost_DurationIncreasesCostWithDeterministicRounding()`
  - L42: `public void EstimateTotalManaCost_MultipleEffectsSumBeforeTargetMultiplier()`
  - L57: `public void EstimateTotalManaCost_CatalogManaCostsRemainAtOrAboveEstimator()`
  - L68: `private static SpellDefinition CreateSpell(SpellTargetKind targetKind, SpellEffectSpec[] effects)`

#### `SpellDefinitionTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L11: `class SpellDefinitionTests`
- members:
  - L14: `public void EffectSpec_RejectsNoneKind()`
  - L20: `public void EffectSpec_RejectsNegativeMagnitudeOrDuration()`
  - L27: `public void Definition_RejectsEmptyTemplateIdOrDisplayName()`
  - L35: `public void Definition_RejectsNoneSchoolTargetKindNegativeManaCostNegativeRangeAndNegativeCooldown()`
  - L46: `public void Definition_ExistingConstructorDefaultsToSingleTargetUnboundedRangeAndZeroCooldown()`
  - L61: `public void Definition_RangeAwareConstructor_PersistsRangeAndCooldown()`
  - L79: `public void Definition_RequiresAtLeastOneEffect()`
  - L85: `public void Definition_EffectsCollectionIsReadOnlySnapshot()`

#### `SpellEffectResolutionServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L13: `class SpellEffectResolutionServiceTests`
- members:
  - L16: `public void ResolveInstantaneousEffects_DirectDamage_DamagesTargetWithoutExtraManaSpend()`
  - L40: `public void ResolveInstantaneousEffects_DirectDamage_ClampsAtZeroHealth()`
  - L54: `public void ResolveInstantaneousEffects_DirectDamage_OvershootMagnitudeClampsAtZeroHealth()`
  - L74: `public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealthAggregatesIndependently()`
  - L100: `public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_OvershootDamageClampsThenRestoreApplies()`
  - L126: `public void ResolveInstantaneousEffects_DirectDamage_ZeroMagnitudeLeavesHealthUnchanged()`
  - L147: `public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_ZeroMagnitudeLeavesHealthUnchanged()`
  - L175: `public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_ZeroRestoreLeavesDamageApplied()`
  - L201: `public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_ZeroDamageLeavesRestoreApplied()`
  - L227: `public void ResolveInstantaneousEffects_RestoreHealth_HealsTargetUpToMax()`
  - L244: `public void ResolveInstantaneousEffects_RestoreHealth_ZeroMagnitudeLeavesHealthUnchanged()`
  - L265: `public void ResolveInstantaneousEffects_RestoreHealth_ClampsAtMaxHealth()`
  - L285: `public void ResolveInstantaneousEffects_RestoreFatigue_RestoresTargetFatigue()`
  - L309: `public void ResolveInstantaneousEffects_RestoreFatigue_ClampsAtMaxFatigue()`
  - L329: `public void ResolveInstantaneousEffects_RestoreFatigue_ZeroMagnitudeLeavesFatigueUnchanged()`
  - L350: `public void ResolveInstantaneousEffects_RestoreMana_RestoresTargetMana()`
  - L376: `public void ResolveInstantaneousEffects_RestoreMana_ClampsAtMaxMana()`
  - L396: `public void ResolveInstantaneousEffects_RestoreMana_BundledWithOtherEffectsAggregatesPerKind()`
  - L427: `public void ResolveInstantaneousEffects_RestoreMana_ZeroMagnitudeLeavesManaUnchanged()`
  - L447: `public void ResolveInstantaneousEffects_DirectMana_DrainsTargetMana()`
  - L474: `public void ResolveInstantaneousEffects_DirectMana_ClampsAtZeroMana()`
  - L494: `public void ResolveInstantaneousEffects_DirectMana_OvershootMagnitudeClampsAtZeroMana()`
  - L515: `public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreManaAggregatesIndependently()`
  - L541: `public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_OvershootDrainClampsThenRestoreApplies()`
  - L567: `public void ResolveInstantaneousEffects_DirectMana_ZeroMagnitudeLeavesManaUnchanged()`
  - L587: `public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroMagnitudeLeavesManaUnchanged()`
  - L615: `public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroRestoreLeavesDrainApplied()`
  - L641: `public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroDrainLeavesRestoreApplied()`
  - L667: `public void ResolveInstantaneousEffects_DirectFatigue_DrainsTargetFatigue()`
  - L695: `public void ResolveInstantaneousEffects_DirectFatigue_ClampsAtZeroFatigue()`
  - L715: `public void ResolveInstantaneousEffects_DirectFatigue_OvershootMagnitudeClampsAtZeroFatigue()`
  - L736: `public void ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigueAggregatesIndependently()`
  - L762: `public void ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_OvershootDrainClampsThenRestoreApplies()`
  - L788: `public void ResolveInstantaneousEffects_DirectFatigue_ZeroMagnitudeLeavesFatigueUnchanged()`
  - L808: `public void ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_ZeroMagnitudeLeavesFatigueUnchanged()`
  - L836: `public void ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_ZeroDrainLeavesRestoreApplied()`
  - L862: `public void ResolveInstantaneousEffects_MultipleSupportedEffects_AppliesInDefinitionOrder()`
  - L891: `public void ResolveInstantaneousEffects_NullCast_IsRejectedWithoutMutatingTarget()`
  - L905: `public void ResolveInstantaneousEffects_FailedCast_IsRejectedWithoutMutatingTarget()`
  - L919: `public void ResolveInstantaneousEffects_NullOrIncapacitatedTarget_IsRejected()`
  - L936: `public void ResolveInstantaneousEffects_NonInstantaneousEffect_IsRejectedWithoutMutatingTarget()`
  - L951: `public void ResolveInstantaneousEffects_UnsupportedInstantaneousShieldBuff_IsRejectedWithoutMutatingTarget()`
  - L980: `public void ResolveInstantaneousEffects_AllSixSupportedKindsBundledAggregateIndependently()`
  - L1018: `private static ActorRecord CreateActor(int id, string name, ActorRole role, int health, int mana, int fatigue = 12)`

#### `SpellExecutionServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L14: `class SpellExecutionServiceTests`
- members:
  - L17: `public void TryExecute_InRangeDamageSpell_SpendsManaAndDamagesTarget()`
  - L39: `public void TryExecute_CatalogFlameBolt_WithCooldownState_StartsCatalogCooldownAndRejectsRecast()`
  - L74: `public void TryExecute_CooldownSpellSuccess_StartsCooldownAfterCommittedCast()`
  - L97: `public void TryExecute_ActiveCooldown_RejectsBeforeTargetValidationOrManaSpend()`
  - L124: `public void TryExecute_TargetRejected_DoesNotStartCooldown()`
  - L149: `public void TryExecuteWithRoll_RollFizzle_DoesNotSpendManaMutateTargetOrStartCooldown()`
  - L175: `public void TryExecute_OutOfRangeTarget_DoesNotSpendManaOrMutateTarget()`
  - L196: `public void TryExecute_TimedUnsupportedEffect_DoesNotSpendManaOrMutateCaster()`
  - L216: `public void TryExecute_AdjacentHealingSpell_SpendsManaAndRestoresHealth()`
  - L236: `public void TryExecuteWithRoll_RollSuccess_SpendsManaAndDamagesTarget()`
  - L261: `public void TryExecuteWithRoll_RollFizzle_DoesNotSpendManaOrMutateTarget()`
  - L285: `public void TryExecuteWithRoll_PrecheckRefusal_HappensBeforeAnyRollOrManaSpend()`
  - L307: `public void TryExecute_InsufficientMana_DoesNotSpendManaOrMutateTarget()`
  - L328: `public void TryExecute_UnlearnedSpell_DoesNotSpendMana()`
  - L348: `private static ActorRecord CreateActor(int id, string name, ActorRole role, int x, int y, int health, int mana)`
  - L363: `private static SpellDefinition CreateCooldownSpell()`
  - L376: `private static SpellExecutionService CreateCooldownExecutionService(SpellDefinition cooldownSpell)`

#### `SpellSuccessChanceServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L12: `class SpellSuccessChanceServiceTests`
- members:
  - L15: `public void Calculate_NullCaster_FailsWithStableError()`
  - L27: `public void Calculate_NullSpell_FailsWithStableError()`
  - L39: `public void Calculate_IncapacitatedCaster_FailsWithStableError()`
  - L52: `public void Calculate_InvalidSchool_FailsWithStableError()`
  - L72: `public void Calculate_InvalidTargetKind_FailsWithStableError()`
  - L92: `public void Calculate_DestructionSpell_UsesMindAsPrimaryAttribute()`
  - L108: `public void Calculate_RestorationSpell_UsesInsightAsPrimaryAttribute()`
  - L124: `public void Calculate_SingleTargetRangeSpell_IncludesExpectedPenaltyBreakdown()`
  - L144: `public void Calculate_NonSingleTargetSpell_DoesNotPayRangePenalty()`
  - L168: `public void Calculate_ResultClampsToMinimumAndMaximumBounds()`
  - L201: `private static ActorRecord CreateActor(string name, int mind, int insight, int health = 16)`

#### `SpellTargetValidatorTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Magic`
- types:
  - L14: `class SpellTargetValidatorTests`
- members:
  - L17: `public void Validate_CasterSelfWithNullTarget_RoutesToCaster()`
  - L30: `public void Validate_CasterSelfWithCasterAsTarget_RoutesToCaster()`
  - L42: `public void Validate_CasterSelfWithOtherTarget_IsRefused()`
  - L56: `public void Validate_TouchWithOrthogonallyAdjacentTarget_RoutesToTarget()`
  - L70: `public void Validate_TouchWithDiagonalTarget_IsRefusedAsNotAdjacent()`
  - L83: `public void Validate_TouchWithSelfTile_IsRefusedAsNotAdjacent()`
  - L95: `public void Validate_TouchWithFarTarget_IsRefusedAsNotAdjacent()`
  - L108: `public void Validate_TouchWithNullTarget_IsRefused()`
  - L120: `public void Validate_TouchWithDeadTarget_IsRefused()`
  - L133: `public void Validate_SingleTargetWithLivingTargetInsideConfiguredRange_RoutesToTarget()`
  - L147: `public void Validate_SingleTargetBeyondConfiguredRange_IsRefused()`
  - L161: `public void Validate_SingleTargetWithUnboundedRangeZero_AllowsFarTarget()`
  - L183: `public void Validate_SingleTargetWithNullTarget_IsRefused()`
  - L195: `public void Validate_SingleTargetWithDeadTarget_IsRefused()`
  - L208: `public void Validate_AreaAroundCaster_IsRefusedUntilAreaLands()`
  - L228: `public void Validate_AreaAtRange_IsRefusedUntilAreaLands()`
  - L248: `public void Validate_NullSpell_IsRefused()`
  - L260: `public void Validate_NullCaster_IsRefused()`
  - L271: `public void Validate_DeadCaster_IsRefused()`
  - L284: `public void Validate_RoutedTarget_FlowsCleanlyIntoEffectResolver()`
  - L300: `private static ActorRecord CreateActor(int id, string name, ActorRole role, int x, int y, int health = 16)`

### `Assets/Tests/EditMode/Movement`

#### `Sprint4KinematicMotorTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Movement`
- types:
  - L6: `class Sprint4KinematicMotorTests`
- members:
  - L9: `public void Plan_ForwardInput_ProducesExpectedPositionDelta()`
  - L25: `public void Plan_DiagonalInput_IsClampedToMoveSpeed()`
  - L37: `public void Plan_JumpPressedWhileGrounded_AddsUpwardVelocity()`
  - L50: `public void ToWorldPlanar_YawNinety_MapsForwardToPositiveX()`

### `Assets/Tests/EditMode/Narrative`

#### `GuardInteractionServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Narrative`
- types:
  - L13: `class GuardInteractionServiceTests`
- members:
  - L16: `public void Interact_WithoutGateWrit_EscalatesWarnings()`
  - L26: `public void Interact_WithGateWrit_GrantsDoorClearance()`
  - L37: `private static SliceWorldState CreateGuardReadyWorld()`

#### `NarrativeShellTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Narrative`
- types:
  - L11: `class NarrativeShellTests`
- members:
  - L14: `public void AskAbout_FirstQuestion_UsesTopicAnswer()`
  - L22: `public void AskDm_ReturnsGroundedWorldSummary()`
  - L30: `public void Think_PrefersNearbyPickupWhenInventoryHasSpace()`

#### `NpcMemoryQueryServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Narrative`
- types:
  - L14: `class NpcMemoryQueryServiceTests`
- members:
  - L17: `public void AskAbout_ThirdRepeatedTopic_UsesWellWornMemoryState()`
  - L32: `public void GuardInteract_WithPersistedPassageRequests_UsesClosedStanceEvenWhenCounterIsReset()`
  - L49: `public void GuardInteract_WithPersistedClearance_RemembersAccessWithoutTransientFlag()`
  - L69: `public void MerchantTrade_WithPriorTransaction_UsesRecognizedCustomerFlavor()`
  - L97: `public void QueryService_DerivesContextsFromNpcMemoryStore()`
  - L112: `private static InteractionEvent CreateGuardPassageRequest(EmberCrpg.Domain.World.SliceWorldState world, int amount)`

#### `PersistentNpcMemoryTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Narrative`
- types:
  - L13: `class PersistentNpcMemoryTests`
- members:
  - L16: `public void AskAbout_FirstInteraction_RecordsDialogueMemory()`
  - L32: `public void AskAbout_RepeatedInteraction_ChangesOutputFromPersistentMemory()`

### `Assets/Tests/EditMode/Presentation`

#### `InventoryEquipmentFormatterTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Presentation`
- types:
  - L13: `class InventoryEquipmentFormatterTests`
- members:
  - L16: `public void FormatInspect_ShowsInventoryItemAndEmptyWeaponSlot()`
  - L27: `public void FormatEquipmentLine_ShowsEquippedWeaponBonuses()`

#### `SliceAtmosphereSelectorTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Presentation`
- types:
  - L12: `class SliceAtmosphereSelectorTests`
- members:
  - L15: `public void Select_StartRoom_UsesRoomTemplateAndClosedDoorCues()`
  - L29: `public void Select_CurrentEnemyRoomWithCombatActive_UsesCombatMusicAndSfx()`
  - L42: `public void Select_UnvisitedAndClearedRoomStates_VaryAmbienceDeterministically()`
  - L61: `public void FormatHud_ShowsCurrentAtmosphereForDebugValidation()`
  - L74: `private static string ExpectedAmbiencePrefix(string templateId)`

### `Assets/Tests/EditMode/Process`

#### `JobAssignmentCompetitionTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L15: `class JobAssignmentCompetitionTests`
- members:
  - L23: `public void HigherPrioritySmithWinsFurnace()`
  - L51: `public void BreadJobWaitsWithoutBakery()`
  - L88: `private static WorksiteStore ActiveFurnaceAndBakeryStore()`
  - L95: `private static WorksiteStore ActiveFurnaceStore()`
  - L102: `private static ActorRecord CreateActor(ulong id, string name, ActorJobPreference preference)`

#### `JobAssignmentSystemTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L18: `class JobAssignmentSystemTests`
- members:
  - L25: `public void TryAssignNext_AssignsTwoSmithsDeterministically()`
  - L52: `public void TryAssignNext_UsesActorPriorityBeforeActorOrder()`
  - L73: `public void TryAssignNext_SkipsIdleActorsThatAlreadyHoldPendingClaims()`
  - L99: `public void TryAssignNext_IgnoresDisabledOrUnavailableActors()`
  - L120: `public void CanActorWorkJob_RequiresAliveIdlePreferenceAndActiveWorksite()`
  - L138: `public void CanActorWorkJob_WithRecipeInputs_ReturnsTrueWithoutMutatingInventory()`
  - L153: `public void CanActorWorkJob_WithMissingRecipeInputs_ReturnsFalseWithoutMutatingInventory()`
  - L168: `public void CanActorWorkJob_WithBatchRecipeInputs_RequiresInputsForEveryExecution()`
  - L183: `public void CanActorWorkJob_WithBatchRecipeInputs_ReturnsTrueWhenStockCoversEveryExecution()`
  - L198: `public void StartRecipeForClaim_ConsumesInputsAndStoresActiveWorkOrder()`
  - L232: `public void StartRecipeForClaim_WithMissingInputs_DoesNotStartOrMutateInventory()`
  - L256: `public void StartRecipeForClaim_WithBatchMissingInputs_DoesNotStartOrMutateInventory()`
  - L280: `public void StartRecipeForClaim_WithBatchInputs_PreflightsFullQuantityThenStartsOneWorkOrder()`
  - L304: `public void StartRecipeForClaim_RequiresClaimWithoutMutatingInventory()`
  - L326: `public void StartRecipeForClaim_RejectsDuplicateStartsWithoutDoubleConsumingInputs()`
  - L351: `public void TickAssignedJobs_AdvancesActiveWorkOrderWithoutCompletingEarly()`
  - L380: `public void TickAssignedJobs_CompletesBoardEntryAndClearsActorScheduleWhenRecipeCompletes()`
  - L411: `public void TickAssignedJobs_ContinuesBatchJobUntilRequestedQuantityCompletes()`
  - L453: `public void CanActorWorkJob_WithRecipeMismatch_ReturnsFalseWithoutMutatingInventory()`
  - L471: `private static JobRequest MakeRequest(ulong jobId, int priority, int quantity = 1)`
  - L485: `private static WorksiteStore ActiveFurnaceStore()`
  - L492: `private static WorksiteStore InactiveFurnaceStore()`
  - L499: `private static RecipeDef SmeltIronRecipe(RecipeId recipeId, string worksiteKind = "furnace")`
  - L510: `private static InventoryState InventoryWithInputs(int ore, int fuel)`
  - L520: `private static InventoryItem CreateOutput(RecipeOutput output)`
  - L525: `private static int CountTemplate(InventoryState inventory, string templateId)`
  - L537: `private static ActorRecord CreateActor(ulong id, string name, ActorJobPreference preference = default, bool alive = true)`

#### `JobBoardTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L14: `class JobBoardTests`
- members:
  - L22: `private static JobRequest MakeRequest(ulong jobId, int priority = 1, ulong recipeId = 100UL)`
  - L38: `public void Add_StoresRequestsInInsertionOrder()`
  - L54: `public void Add_RejectsNullOrDuplicateRequest()`
  - L66: `public void TryGet_ReturnsRequestWhenPresent()`
  - L82: `public void TryPeekNext_OrdersByPriorityThenInsertionOrder()`
  - L100: `public void TryClaim_ClaimsJobAndSkipsClaimedRows()`
  - L120: `public void TryClaim_RejectsDuplicateOrInvalidClaims()`
  - L138: `public void CompleteAndCancel_RemoveTerminalJobs()`
  - L158: `public void Clear_DropsAllJobs()`

#### `JobEventLogTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L18: `class JobEventLogTests`
- members:
  - L25: `public void TryAssignNext_WithEventLog_AppendsJobAssignedReasonTrace()`
  - L51: `public void TickAssignedJobs_WithGameTime_AppendsJobCompletedAfterRecipeCompleted()`
  - L84: `private static JobRequest MakeRequest(ulong jobId, int priority)`
  - L98: `private static WorksiteStore ActiveFurnaceStore()`
  - L105: `private static InventoryState InventoryWithInputs(int ore, int fuel)`
  - L115: `private static InventoryItem CreateOutput(RecipeOutput output)`
  - L120: `private static ActorRecord CreateActor(ulong id, string name, ActorJobPreference preference)`

#### `JobIdTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L13: `class JobIdTests`
- members:
  - L19: `public void Constructor_StoresValue()`
  - L30: `public void SameValue_IsEqual()`
  - L43: `public void DifferentValue_IsNotEqual()`
  - L56: `public void Default_IsEmpty()`
  - L65: `public void SameValue_HasSameHashCode()`
  - L77: `public void Empty_ToString_ReturnsEmptyLabel()`
  - L86: `public void NonEmpty_ToString_ContainsRawValue()`

#### `JobNeedsRefusalTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L10: `class JobNeedsRefusalTests`
- members:
  - L16: `public void TryAssignNext_RejectsHungryLowMoodActorAndEmitsJobRefused()`

#### `JobPriorityTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L14: `class JobPriorityTests`
- members:
  - L18: `public void Active_StoresValue()`
  - L28: `public void Default_IsDisabled()`
  - L36: `public void Active_RejectsZeroOrNegativeValues()`
  - L44: `public void SameValue_IsEqual()`
  - L56: `public void CompareTo_LowerActiveNumberWins()`
  - L67: `public void CompareTo_ActiveSortsBeforeDisabled()`
  - L77: `public void ToString_ReturnsDebugLabel()`

#### `JobRequestTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L14: `class JobRequestTests`
- members:
  - L46: `public void Constructor_StoresFields()`
  - L63: `public void Constructor_RejectsEmptyIds()`
  - L73: `public void Constructor_RejectsMissingWorksite()`
  - L80: `public void Constructor_RejectsNoneJobKind()`
  - L87: `public void Constructor_RejectsInactivePriorityOrQuantity()`

#### `NeedRecoveryRecipeTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L12: `class NeedRecoveryRecipeTests`
- members:
  - L15: `public void Constructor_StoresNormalizedRecoveryShape()`
  - L28: `public void Constructor_AllowsInventoryFreeRecovery()`
  - L37: `public void Constructor_RejectsEmptyIdsAndMissingActionKind()`
  - L46: `public void Constructor_RejectsNonRecoveryDeltasAndMissingNeed()`
  - L54: `public void Constructor_RejectsBlankConsumedItemTemplate()`

#### `PlantComponentTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L11: `class PlantComponentTests`
- members:
  - L14: `public void Constructor_StoresPlantState()`
  - L27: `public void WithStage_ChangesStageAndResetsAge()`
  - L39: `public void Constructor_RejectsInvalidValues()`
  - L48: `private static PlantComponent CreatePlant()`

#### `HarvestSystemTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L15: `class HarvestSystemTests`
- members:
  - L18: `public void TryHarvest_ConvertsRipePlantToStockpileOutputAndClearsSoil()`
  - L57: `public void TryHarvest_ReturnsFalseForUnripePlantWithoutMutation()`
  - L72: `public void TryHarvest_ReturnsFalseWhenStockpileCannotAcceptOutputWithoutMutation()`
  - L88: `public void TryHarvest_RejectsNullInputsAndBadFactoryOutput()`
  - L108: `private static ComponentStore<PlantComponent> CreatePlants(PlantStageId stageId)`
  - L115: `private static ComponentStore<SoilComponent> CreateSoils()`
  - L122: `private static InventoryItem CreateHarvestItem(string templateId)`
  - L127: `private static int Quantity(InventoryState inventory, string templateId)`
  - L132: `private static PlantSpeciesDef CreateWheat()`

#### `PlantDefinitionTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L9: `class PlantDefinitionTests`
- members:
  - L12: `public void WheatDefinition_ProvidesOrderedStagesAndItemTags()`
  - L27: `public void GrowthRules_AllowSpringAndSummerButSnowBlocks()`
  - L38: `public void Constructor_RejectsInvalidRows()`
  - L48: `private static PlantSpeciesDef CreateWheat()`
  - L69: `private static PlantGrowthStageDef Stage(string id, bool harvestable)`

#### `PlantingSystemTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L15: `class PlantingSystemTests`
- members:
  - L18: `public void TryPlant_ConsumesSeedAddsPlantUpdatesSoilAndLogsEvent()`
  - L60: `public void TryPlant_ReturnsFalseWithoutMutatingWhenSeedMissingOrSoilOccupied()`
  - L84: `public void TryPlant_RejectsNullInputs()`
  - L102: `private static ComponentStore<SoilComponent> CreateSoils()`
  - L109: `private static InventoryState CreateInventory(int seedQuantity)`
  - L117: `private static int Quantity(InventoryState inventory, string templateId)`
  - L122: `private static PlantSpeciesDef CreateWheat()`

#### `PlantGrowthSystemTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L14: `class PlantGrowthSystemTests`
- members:
  - L17: `public void AdvanceOneDay_IncrementsAgeUntilStageBoundaryThenLogsAdvance()`
  - L51: `public void AdvanceOneDay_BlocksGrowthWhenSeasonOrSnowRuleDisallowsIt()`
  - L67: `public void AdvanceOneDay_SkipsOtherSpeciesAndHarvestableFinalStage()`
  - L82: `public void AdvanceOneDay_RejectsNullInputsAndUnknownStage()`
  - L98: `private static ComponentStore<PlantComponent> CreatePlants(int daysInStage)`
  - L105: `private static PlantSpeciesDef CreateWheat()`

#### `WorldProcessDefinitionTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L11: `class WorldProcessDefinitionTests`
- members:
  - L14: `public void Definition_StoresProcessData()`
  - L25: `public void Instance_AdvancesUntilCompleteWithoutOvershoot()`
  - L42: `public void Constructors_RejectInvalidRows()`
  - L57: `private static WorldProcessDef CreateDef()`

#### `RecipeDefTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L16: `class RecipeDefTests`
- members:
  - L22: `public void Constructor_StoresRecipeShape()`
  - L41: `public void Constructor_TrimsWorksiteAndSkillTags()`
  - L53: `public void Constructor_RejectsEmptyRecipeId()`
  - L62: `public void Constructor_RejectsBlankWorksiteOrSkillTags()`
  - L76: `public void Constructor_RejectsNonPositiveDuration()`
  - L89: `public void Constructor_RejectsNullRowCollections()`
  - L111: `public void Constructor_RejectsEmptyRowCollections()`
  - L121: `public void Constructor_RejectsNullRows()`
  - L131: `public void Constructor_DefensivelyCopiesRows()`
  - L150: `public void RowCollections_AreReadOnly()`
  - L164: `public void SmeltIronIngotShape_IsPinned()`

#### `RecipeEventLogTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L18: `class RecipeEventLogTests`
- members:
  - L21: `public void CompletingRecipe_AppendsOrderedRecipeCompletedEventWithReasonTrace()`

#### `RecipeFixtureCatalog.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L12: `class RecipeFixtureCatalog`
- members:
  - L15: `public static RecipeDef SmeltIronIngot(RecipeId recipeId)`
  - L27: `public static RecipeDef BakeBread(RecipeId recipeId)`

#### `RecipeIdTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L13: `class RecipeIdTests`
- members:
  - L19: `public void Constructor_StoresValue()`
  - L30: `public void SameValue_IsEqual()`
  - L43: `public void DifferentValue_IsNotEqual()`
  - L56: `public void Default_IsEmpty()`
  - L65: `public void SameValue_HasSameHashCode()`
  - L77: `public void Empty_ToString_ReturnsEmptyLabel()`
  - L86: `public void NonEmpty_ToString_ContainsRawValue()`

#### `RecipeIngredientTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L14: `class RecipeIngredientTests`
- members:
  - L20: `public void Constructor_StoresTagAndQuantity()`
  - L32: `public void Constructor_TrimsItemTag()`
  - L43: `public void Constructor_RejectsBlankItemTag()`
  - L54: `public void Constructor_RejectsNonPositiveQuantity()`

#### `RecipeOutputTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L15: `class RecipeOutputTests`
- members:
  - L21: `public void Constructor_StoresOutputShape()`
  - L35: `public void Constructor_TrimsItemTag()`
  - L46: `public void Constructor_RejectsBlankItemTag()`
  - L57: `public void Constructor_RejectsMaterialOrQualitySentinel()`
  - L67: `public void Constructor_RejectsNonPositiveQuantity()`

#### `RecipeSystemTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L19: `class RecipeSystemTests`
- members:
  - L26: `public void TryStart_ConsumesInputsAtActiveMatchingFurnace()`
  - L49: `public void TryStart_ReturnsFalseAndKeepsInventoryWhenInputsAreMissing()`
  - L72: `public void TryStart_ReturnsFalseForInactiveOrMissingWorksite()`
  - L100: `public void Tick_CompletesAfterFortyTicksAndProducesIronIngot()`
  - L132: `public void Tick_RejectsFactoriesThatReturnBundledOutputQuantities()`
  - L154: `public void Tick_WhenOutputPlacementFails_DoesNotCompleteWorkOrder()`
  - L189: `public void TryStart_ConsumesOnlyStackableInputsWhenEquipmentSharesTemplate()`
  - L214: `private static RecipeDef CreateSmeltIronIngotRecipe()`
  - L232: `private static RecipeDef CreateDoubleIngotRecipe()`
  - L250: `private static WorksiteStore CreateActiveFurnaceStore()`
  - L257: `private static InventoryState CreateSmeltingInventory(int capacity = 8)`
  - L265: `private static InventoryItem CreateOutputItem(RecipeOutput output)`
  - L270: `private static InventoryItem CreateBundledOutputItem(RecipeOutput output)`
  - L275: `private static int StackableQuantity(InventoryState inventory, string templateId)`
  - L281: `private static int Quantity(InventoryState inventory, string templateId)`

#### `SoilComponentTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L11: `class SoilComponentTests`
- members:
  - L14: `public void Constructor_StoresSitePositionAndClampedSoilValues()`
  - L33: `public void WithPlantAndWithoutPlant_UpdatePlantHandleImmutably()`
  - L47: `public void Constructor_RejectsEmptyIdentityOrSite()`
  - L54: `private static SoilComponent CreateSoil()`

#### `WorksiteRecordTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L15: `class WorksiteRecordTests`
- members:
  - L17: `private static WorksiteRecord MakeRecord(bool isActive = true)`
  - L28: `public void Constructor_StoresFields()`
  - L40: `public void Constructor_RejectsEmptySiteId()`
  - L51: `public void Constructor_RejectsNoneKind()`
  - L62: `public void Constructor_AllowsInactiveState()`
  - L71: `public void WithActive_ReturnsCopyWithRequestedState()`

#### `WorksiteStoreTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Process`
- types:
  - L17: `class WorksiteStoreTests`
- members:
  - L22: `private static WorksiteRecord MakeRecord(SiteId siteId, int x, int y, bool isActive = true)`
  - L33: `public void Add_StoresRecordBySiteAndPosition()`
  - L47: `public void Add_RejectsNullRecord()`
  - L56: `public void Add_RejectsDuplicateSiteCell()`
  - L66: `public void Add_AllowsSamePositionInDifferentSites()`
  - L82: `public void Get_RejectsEmptySiteId()`
  - L91: `public void Get_MissingSiteCell_ThrowsKeyNotFound()`
  - L100: `public void TryGet_KnownSiteCell_ReturnsStoredRecord()`
  - L114: `public void TryGet_ReturnsFalseForEmptyOrMissingKey()`
  - L126: `public void Contains_ReturnsFalseForEmptyOrMissingKey()`
  - L137: `public void Remove_ReturnsFalseForEmptyOrMissingKey()`
  - L151: `public void Remove_DropsRecordAndOrderEntry()`
  - L168: `public void Records_EnumerateInInsertionOrder()`
  - L186: `public void Clear_DropsAllRecords()`

### `Assets/Tests/EditMode/Save`

#### `ActorNeedsRoundTripTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Save`
- types:
  - L9: `class ActorNeedsRoundTripTests`
- members:
  - L14: `public void ActorNeeds_RoundTrip_PreservesNeedsAndMood()`

#### `JobAssignmentRoundTripTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Save`
- types:
  - L18: `class JobAssignmentRoundTripTests`
- members:
  - L26: `public void JsonDto_RoundTripsClaimedJobBoardAndActorScheduleState()`
  - L77: `private static RecipeDef ResolveRecipe(RecipeId recipeId)`
  - L83: `private static RecipeDef CreateSmeltIronIngotRecipe()`
  - L94: `private static WorksiteStore CreateActiveFurnaceStore()`

#### `JsonSliceSaveServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Save`
- types:
  - L18: `class JsonSliceSaveServiceTests`
- members:
  - L21: `public void SaveAndLoad_RoundTripsDoorMerchantGuardAndEnemyState()`
  - L89: `public void SaveAndLoad_FreshWorld_StartsWithNoSpellCooldowns()`
  - L101: `public void SaveAndLoad_FreshWorld_StartsWithNoShieldBuffs()`

#### `RecipeWorksiteRoundTripTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Save`
- types:
  - L20: `class RecipeWorksiteRoundTripTests`
- members:
  - L27: `public void JsonDto_RoundTripsActiveWorksiteProgressAndProducedStock()`
  - L70: `private static RecipeDef ResolveRecipe(RecipeId recipeId)`
  - L76: `private static RecipeDef CreateSmeltIronIngotRecipe()`
  - L94: `private static WorksiteStore CreateActiveFurnaceStore()`
  - L101: `private static InventoryState CreateSmeltingInventory()`
  - L109: `private static InventoryItem CreateOutputItem(RecipeOutput output)`
  - L114: `private static int Quantity(InventoryState inventory, string templateId)`

#### `ShieldBuffSaveMapperTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Save`
- types:
  - L13: `class ShieldBuffSaveMapperTests`
- members:
  - L16: `public void ToData_NullState_ReturnsEmptyEntries()`
  - L26: `public void ToData_EmptyState_ReturnsEmptyEntries()`
  - L34: `public void ToData_NonEmptyState_OrdersEntriesBySpellTemplateId()`
  - L58: `public void ToState_NullDto_ReturnsEmptyState()`
  - L67: `public void ToState_NullEntriesArray_ReturnsEmptyState()`
  - L75: `public void ToState_SkipsNullEmptyZeroTickAndNegativeMagnitudeEntries()`
  - L99: `public void ToState_PreservesZeroMagnitudeEntries()`
  - L117: `public void RoundTrip_PreservesEveryActiveShieldBuff()`
  - L137: `public void RoundTrip_ZeroOnlyState_RebuildsEmpty()`

#### `SpellCooldownSaveMapperTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Save`
- types:
  - L13: `class SpellCooldownSaveMapperTests`
- members:
  - L16: `public void ToData_NullState_ReturnsEmptyEntries()`
  - L26: `public void ToData_EmptyState_ReturnsEmptyEntries()`
  - L34: `public void ToData_NonEmptyState_OrdersEntriesBySpellTemplateId()`
  - L55: `public void ToState_NullDto_ReturnsEmptyState()`
  - L64: `public void ToState_NullEntriesArray_ReturnsEmptyState()`
  - L72: `public void ToState_SkipsNullEmptyAndZeroTickEntries()`
  - L93: `public void RoundTrip_PreservesEveryActiveCooldown()`
  - L110: `public void RoundTrip_ZeroOnlyState_RebuildsEmpty()`

#### `StoreRoundTripTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Save`
- types:
  - L18: `class StoreRoundTripTests`
- members:
  - L21: `public void SaveAndLoad_RoundTripsCanonicalStoreRootsAndEventLog()`
  - L77: `public void Mapper_PrefersCanonicalActorStoreWhenSaveDataCarriesLegacyAndStoreActors()`
  - L95: `public void Mapper_TreatsEmptyCanonicalActorStoreAsExplicitAndSkipsLegacyActors()`
  - L107: `public void Mapper_SkipsMalformedLegacyActorsWhenCanonicalActorStoreIsPresent()`
  - L121: `private static ActorRecord MakeRecord(ulong id, string name, ActorRole role, GridPosition position)`

### `Assets/Tests/EditMode/Time`

#### `GameTimeAdvanceSystemTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Time`
- types:
  - L16: `class GameTimeAdvanceSystemTests`
- members:
  - L19: `public void Advance_ReturnsTimeAdvancedByMinutesWithoutSideEffects()`
  - L32: `public void Advance_AppendsDayAdvancedEventWhenDayChanges()`
  - L61: `public void Advance_AppendsSeasonChangedAfterDayEventWhenBoundaryCrosses()`
  - L86: `public void Advance_DoesNotAppendEventsWhenStillSameDayAndSeason()`
  - L97: `public void Advance_RejectsInvalidInputs()`
  - L107: `private static SeasonCalendar CreateCalendar()`

#### `SeasonCalendarTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.Time`
- types:
  - L9: `class SeasonCalendarTests`
- members:
  - L12: `public void GetSeason_ResolvesDayOfYearFromDataRows()`
  - L23: `public void IsSeasonBoundary_ReturnsTrueOnlyWhenSeasonChanges()`
  - L32: `public void Constructor_RejectsOverlappingRows()`
  - L42: `public void TryGetSeason_ReturnsFalseForCalendarGap()`
  - L54: `private static SeasonCalendar CreateFourSeasonCalendar()`

### `Assets/Tests/EditMode/World`

#### `ActorStoreTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L17: `class ActorStoreTests`
- members:
  - L20: `public void Add_ThenGet_ReturnsSameRecord()`
  - L32: `public void Add_NullRecord_Throws()`
  - L39: `public void Add_EmptyId_Throws()`
  - L47: `public void Add_DuplicateId_Throws()`
  - L56: `public void Get_MissingId_ThrowsKeyNotFound()`
  - L63: `public void Get_EmptyId_Throws()`
  - L70: `public void TryGet_MissingId_ReturnsFalseAndNull()`
  - L81: `public void TryGet_EmptyId_ReturnsFalseAndNull()`
  - L93: `public void TryGet_KnownId_ReturnsRecord()`
  - L106: `public void Contains_RespectsRegistrationAndEmptyId()`
  - L118: `public void Remove_KnownId_DropsRecordAndDecrementsCount()`
  - L132: `public void Remove_MissingOrEmptyId_ReturnsFalse()`
  - L140: `public void Clear_DropsEveryRecord()`
  - L153: `public void Records_EnumeratesInInsertionOrder()`
  - L168: `public void Records_AfterRemove_PreservesRemainingOrder()`
  - L191: `public void RecordsByRole_OnlyMatchingRoleInInsertionOrder()`
  - L209: `public void RecordsByRole_NoMatch_ReturnsEmpty()`
  - L218: `public void RecordsByRole_EmptyStore_ReturnsEmpty()`
  - L225: `public void FirstByRole_ReturnsFirstInInsertionOrder()`
  - L237: `public void FirstByRole_NoMatch_Throws()`
  - L246: `public void TryFirstByRole_KnownRole_ReturnsFirstAndTrue()`
  - L263: `public void TryFirstByRole_NoMatch_ReturnsFalseAndNull()`
  - L275: `public void TryFirstByRole_EmptyStore_ReturnsFalseAndNull()`
  - L285: `private static ActorRecord MakeRecord(ulong id, string name, ActorRole role)`

#### `ComponentStoreTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L13: `class ComponentStoreTests`
- members:
  - L16: `public void AddGetAndRows_PreserveInsertionOrderForSoilComponents()`
  - L34: `public void Replace_UpdatesComponentWithoutChangingOrder()`
  - L48: `public void RejectsInvalidRows()`
  - L60: `private static SoilComponent CreateSoil(ulong id, int x)`

#### `DoorInteractionServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L12: `class DoorInteractionServiceTests`
- members:
  - L15: `public void Toggle_WithoutClearance_RefusesToOpen()`
  - L24: `public void Toggle_WithClearance_OpensThenClosesDoor()`
  - L34: `private static SliceWorldState CreateDoorReadyWorld()`

#### `FactionRecordTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L13: `class FactionRecordTests`
- members:
  - L15: `private static FactionRecord MakeRecord()`
  - L25: `public void Constructor_StoresFields()`
  - L36: `public void Constructor_RejectsEmptyId()`
  - L46: `public void Constructor_RejectsBlankName()`
  - L56: `public void Constructor_RejectsNullTags()`
  - L66: `public void Constructor_RejectsBlankTag()`
  - L76: `public void Constructor_AcceptsEmptyTags()`
  - L89: `public void Tags_PreserveInsertionOrder()`
  - L101: `public void Tags_DefensiveCopyAtConstruction()`
  - L114: `public void Tags_ProjectionIsNotBackingArray()`
  - L124: `public void Tags_ProjectionCannotBeMutatedViaDowncast()`
  - L138: `public void HasTag_KnownTag_IsTrue()`
  - L148: `public void HasTag_UnknownOrInvalid_IsFalse()`

#### `FactionStoreTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L17: `class FactionStoreTests`
- members:
  - L20: `public void Add_ThenGet_ReturnsSameRecord()`
  - L32: `public void Add_NullRecord_Throws()`
  - L39: `public void Add_DuplicateId_Throws()`
  - L48: `public void Get_MissingId_ThrowsKeyNotFound()`
  - L55: `public void Get_EmptyId_Throws()`
  - L62: `public void TryGet_MissingId_ReturnsFalseAndNull()`
  - L73: `public void TryGet_EmptyId_ReturnsFalseAndNull()`
  - L85: `public void TryGet_KnownId_ReturnsRecord()`
  - L98: `public void Contains_RespectsRegistrationAndEmptyId()`
  - L110: `public void Remove_KnownId_DropsRecordAndDecrementsCount()`
  - L124: `public void Remove_MissingOrEmptyId_ReturnsFalse()`
  - L132: `public void Clear_DropsEveryRecord()`
  - L145: `public void Records_EnumeratesInInsertionOrder()`
  - L160: `public void Records_AfterRemove_PreservesRemainingOrder()`
  - L177: `private static FactionRecord MakeRecord(ulong id, string name)`

#### `Faz1AcceptanceReplayTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L18: `class Faz1AcceptanceReplayTests`
- members:
  - L21: `public void Replay_GuardTalkMemoryAndSecondSiteSurviveSaveLoad()`

#### `IPathfinderTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L10: `class IPathfinderTests`
  - L53: `class FixturePathfinder`
- members:
  - L13: `public void TryFindPath_ForFixtureMap_ReturnsDeterministicSteps()`
  - L30: `public void PathfinderResult_Steps_AreReadOnlyAndCopied()`
  - L48: `private static int Pack(int x, int y)`
  - L55: `public bool TryFindPath(PathfinderRequest request, out PathfinderResult result)`
  - L68: `public ActorPathStep StepActor(int actorId, PathfinderResult path)`

#### `ItemStoreTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L18: `class ItemStoreTests`
- members:
  - L21: `public void Add_ThenGet_ReturnsSameRecord()`
  - L33: `public void Add_NullRecord_Throws()`
  - L40: `public void Add_DuplicateId_Throws()`
  - L49: `public void Get_MissingId_ThrowsKeyNotFound()`
  - L56: `public void Get_EmptyId_Throws()`
  - L63: `public void TryGet_MissingId_ReturnsFalseAndNull()`
  - L74: `public void TryGet_EmptyId_ReturnsFalseAndNull()`
  - L86: `public void TryGet_KnownId_ReturnsRecord()`
  - L99: `public void Contains_RespectsRegistrationAndEmptyId()`
  - L111: `public void Remove_KnownId_DropsRecordAndDecrementsCount()`
  - L125: `public void Remove_MissingOrEmptyId_ReturnsFalse()`
  - L133: `public void Clear_DropsEveryRecord()`
  - L146: `public void Records_EnumeratesInInsertionOrder()`
  - L161: `public void Records_AfterRemove_PreservesRemainingOrder()`
  - L178: `private static ItemRecord MakeRecord(ulong id, ItemMaterial material, ItemQuality quality, EquipmentSlot slot)`

#### `MultiRoomDungeonGeneratorTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L13: `class MultiRoomDungeonGeneratorTests`
- members:
  - L16: `public void Generate_CreatesFiveToTenConnectedRoomsWithoutOrphans()`
  - L26: `public void Generate_SameSeed_RepeatsTopologyDoorsAndSpawns()`
  - L36: `public void Generate_PlacesRequiredArchetypesInWalkableCells()`
  - L47: `public void Traverse_ClosedGuardedDoorBlocksUntilSprint2DoorOpens()`
  - L61: `private static HashSet<int> ReachableRoomIds(GeneratedDungeonLayout dungeon)`

#### `ProceduralRoomGeneratorTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L10: `class ProceduralRoomGeneratorTests`
- members:
  - L13: `public void Generate_SameSeed_RepeatsDimensionsAndSpawns()`
  - L22: `public void Generate_PlacesEnemyInsideRoom()`

#### `ReasonTraceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L12: `class ReasonTraceTests`
- members:
  - L14: `private static ReasonTrace MakeTrace()`
  - L21: `public void Constructor_StoresCausesInOrder()`
  - L33: `public void Constructor_AcceptsSingleCause()`
  - L44: `public void Constructor_RejectsNullCauses()`
  - L51: `public void Constructor_RejectsEmptyCauses()`
  - L58: `public void Constructor_RejectsBlankCauseEntry()`
  - L65: `public void Constructor_TakesDefensiveCopyOfCauses()`
  - L78: `public void Causes_ViewIsReadOnly()`
  - L87: `public void HasCause_MatchesCaseSensitively()`
  - L99: `public void HasCause_RejectsBlankQuery()`

#### `RoomMovementServiceTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L11: `class RoomMovementServiceTests`
- members:
  - L14: `public void Move_InsideRoom_AdvancesToCandidate()`
  - L22: `public void Move_IntoWall_StaysInPlace()`
  - L30: `public void Move_ClosedDoorCell_StaysInPlace()`
  - L38: `public void Move_OpenDoorCell_AllowsThresholdStep()`

#### `SiteRecordTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L14: `class SiteRecordTests`
- members:
  - L16: `private static SiteRecord MakeRecord()`
  - L28: `public void Constructor_StoresFields()`
  - L41: `public void Constructor_RejectsEmptyId()`
  - L53: `public void Constructor_RejectsNoneKind()`
  - L65: `public void Constructor_RejectsBlankName()`
  - L77: `public void Constructor_RejectsInvertedBounds()`
  - L89: `public void Contains_InsidePoint_IsTrue()`
  - L98: `public void Contains_BoundaryPoint_IsTrue()`
  - L108: `public void Contains_OutsidePoint_IsFalse()`

#### `SiteStoreTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L18: `class SiteStoreTests`
- members:
  - L21: `public void Add_ThenGet_ReturnsSameRecord()`
  - L33: `public void Add_NullRecord_Throws()`
  - L40: `public void Add_DuplicateId_Throws()`
  - L49: `public void Get_MissingId_ThrowsKeyNotFound()`
  - L56: `public void Get_EmptyId_Throws()`
  - L63: `public void TryGet_MissingId_ReturnsFalseAndNull()`
  - L74: `public void TryGet_EmptyId_ReturnsFalseAndNull()`
  - L86: `public void TryGet_KnownId_ReturnsRecord()`
  - L99: `public void Contains_RespectsRegistrationAndEmptyId()`
  - L111: `public void Remove_KnownId_DropsRecordAndDecrementsCount()`
  - L125: `public void Remove_MissingOrEmptyId_ReturnsFalse()`
  - L133: `public void Clear_DropsEveryRecord()`
  - L146: `public void Records_EnumeratesInInsertionOrder()`
  - L161: `public void Records_AfterRemove_PreservesRemainingOrder()`
  - L178: `private static SiteRecord MakeRecord(ulong id, SiteKind kind, string name)`

#### `SliceWorldFactoryTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L10: `class SliceWorldFactoryTests`
- members:
  - L13: `public void Create_AssignsDistinctRoleVitalsAndCombatFields()`

#### `SliceWorldStateActorViewTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L14: `class SliceWorldStateActorViewTests`
- members:
  - L17: `public void SettingNamedActorView_RegistersRecordInActorStore()`
  - L29: `public void SettingNamedActorViewTwice_ReplacesPreviousRoleRecord()`
  - L44: `public void SettingNamedActorViewWithWrongRole_Throws()`
  - L53: `public void SettingNamedActorView_RemovesAllExistingRoleRecordsBeforeAddingNew()`
  - L71: `public void NewWorldStateStartsWithEmptyCoreStoreRoots()`
  - L88: `public void FactoryPopulatesStoreBackedNamedViewsAndKeepsOtherStoreRootsReady()`
  - L108: `private static ActorRecord MakeRecord(ulong id, string name, ActorRole role)`

#### `WorldComponentIdTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L7: `class WorldComponentIdTests`
- members:
  - L10: `public void DefaultId_IsEmptySentinel()`
  - L17: `public void Equality_UsesRawValue()`

#### `WorldEventLogTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L16: `class WorldEventLogTests`
- members:
  - L21: `private static WorldEvent MakeEvent(long tick, WorldEventKind kind, string reason)`
  - L33: `public void NewLog_IsEmpty()`
  - L44: `public void Append_StoresEvent()`
  - L59: `public void Append_PreservesInsertionOrder()`
  - L76: `public void Append_PreservesInsertionOrderEvenWhenTicksDecrease()`
  - L91: `public void Append_PreservesReasonTraceOnEvent()`
  - L111: `public void Append_RejectsNullEvent()`
  - L121: `public void Events_ViewReflectsLaterAppends()`
  - L135: `public void Events_ViewIsReadOnly()`

#### `WorldEventTests.cs`
- namespace: `EmberCrpg.Tests.EditMode.World`
- types:
  - L13: `class WorldEventTests`
- members:
  - L19: `private static WorldEvent MakeEvent()`
  - L31: `public void Constructor_StoresFields()`
  - L46: `public void Constructor_StoresReasonTraceWhenProvided()`
  - L65: `public void Constructor_AcceptsNullReasonTrace()`
  - L80: `public void Constructor_RejectsNoneKind()`
  - L92: `public void Constructor_RejectsEmptyActorAndSite()`
  - L104: `public void Constructor_AcceptsEmptySiteWhenActorPresent()`
  - L119: `public void Constructor_AcceptsEmptyActorWhenSitePresent()`
  - L134: `public void Constructor_RejectsBlankReason()`
  - L146: `public void Constructor_RejectsNullReason()`
