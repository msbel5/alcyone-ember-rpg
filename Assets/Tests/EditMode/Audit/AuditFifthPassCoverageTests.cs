using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Fifth-pass codex audit — coverage for the SpellResolver counting fix,
    /// melee event-kind, FindTradeRoute, clone independence, and slot labels.
    /// </summary>
    public sealed class AuditFifthPassCoverageTests
    {
        private static ActorRecord NewActor(ulong id = 1UL, ActorRole role = ActorRole.Player, int armor = 2)
        {
            return new ActorRecord(
                new ActorId(id), $"actor-{id}", role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(20, 20), new VitalStat(12, 12), new VitalStat(20, 20)),
                new GridPosition(0, 0),
                accuracy: 10, dodge: 5, armor: armor, baseDamage: 1);
        }

        // ----- SpellResolver no-op counting (A-P1 regression) -----
        [Test]
        public void SpellResolver_TerrainOpMissingRequiredTag_DoesNotCountAsApplied()
        {
            var handlers = new EffectOperationHandlers();
            handlers.Register(EffectOperationKind.TerrainApply, op => op.Magnitude);
            var def = new EffectDefinition(new EffectId(1UL), "scorch",
                new[] { new EffectOperation(EffectOperationKind.TerrainApply, 1, "fuel_dry", 0) },
                cost: 0, cooldownTicks: 0);

            // Stockpile present but lacks the required terrain tag.
            var stockpile = new StockpileComponent(new SiteId(9UL));
            var ctx = new SpellResolverContext(targetActor: null, terrainStockpile: stockpile,
                requiredTerrainTag: "fuel_dry", resultTerrainTag: "scorched");
            var events = new WorldEventLog();

            var result = new SpellResolver(handlers).Resolve(def, casterMana: 100, default, new SiteId(9UL), events, ctx);

            // The operation handler succeeded, but the apply step early-returned
            // because the required terrain tag was missing. After the fifth-pass
            // fix, the result reflects zero confirmed mutations.
            Assert.That(result.OperationsApplied, Is.EqualTo(0));
            Assert.That(result.TotalMagnitude, Is.EqualTo(0));
            Assert.That(stockpile.Get("scorched"), Is.EqualTo(0));
        }

        [Test]
        public void SpellResolver_TerrainOpWithRequiredTagPresent_CountsAsApplied()
        {
            var handlers = new EffectOperationHandlers();
            handlers.Register(EffectOperationKind.TerrainApply, op => op.Magnitude);
            var def = new EffectDefinition(new EffectId(1UL), "scorch",
                new[] { new EffectOperation(EffectOperationKind.TerrainApply, 1, "fuel_dry", 0) },
                cost: 0, cooldownTicks: 0);

            var stockpile = new StockpileComponent(new SiteId(9UL));
            stockpile.Add("fuel_dry", 1);
            var ctx = new SpellResolverContext(targetActor: null, terrainStockpile: stockpile,
                requiredTerrainTag: "fuel_dry", resultTerrainTag: "scorched");
            var events = new WorldEventLog();

            var result = new SpellResolver(handlers).Resolve(def, casterMana: 100, default, new SiteId(9UL), events, ctx);

            Assert.That(result.OperationsApplied, Is.EqualTo(1));
            Assert.That(result.TotalMagnitude, Is.EqualTo(1));
            Assert.That(stockpile.Get("scorched"), Is.EqualTo(1));
        }

        // ----- InventoryState.Clone independence (G-P3) -----
        [Test]
        public void InventoryState_Clone_IsIndependent()
        {
            var inv = new InventoryState(8);
            inv.TryAdd(new InventoryItem(new ItemId(1UL), "ore", "Ore", 5));
            var clone = inv.Clone();
            // Mutating the clone must not affect the original (and vice versa).
            clone.TryRemove("ore", 5);
            Assert.That(inv.Contains("ore"), Is.True);
            Assert.That(clone.Contains("ore"), Is.False);
        }

        // ----- EquipmentState.Clone independence (G-P3) -----
        [Test]
        public void EquipmentState_Clone_IsIndependent()
        {
            var eq = new EquipmentState();
            eq.Equip(EquipmentSlot.Weapon, new ItemId(7UL));
            var clone = eq.Clone();
            clone.Unequip(EquipmentSlot.Weapon);
            Assert.That(eq.GetEquippedItemId(EquipmentSlot.Weapon).Value, Is.EqualTo(7UL));
            Assert.That(clone.GetEquippedItemId(EquipmentSlot.Weapon).Value, Is.EqualTo(0UL));
        }

        // ----- SliceWorldState.FindTradeRoute (G-P3) -----
        [Test]
        public void SliceWorldState_FindTradeRoute_HitAndMiss()
        {
            var world = new SliceWorldState();
            var routeId = new TradeRouteId(42UL);
            world.TradeRoutes.Add(new TradeRouteDef(routeId, new SiteId(1UL), new SiteId(2UL), "iron_ingot", 5, 3));
            Assert.That(world.FindTradeRoute(routeId), Is.Not.Null);
            Assert.That(world.FindTradeRoute(new TradeRouteId(999UL)), Is.Null);
        }

        // ----- EquipmentService.GetSlotLabel (G-P3) -----
        [Test]
        public void EquipmentService_GetSlotLabel_KnownSlotsProduceNonEmpty()
        {
            var weaponLabel = EquipmentService.GetSlotLabel(EquipmentSlot.Weapon);
            Assert.That(weaponLabel, Is.Not.Null);
            Assert.That(weaponLabel, Is.Not.EqualTo(string.Empty));
        }

        // ----- RealtimeDamageService.CalculateArmorClass boundary (G-P3) -----
        [Test]
        public void RealtimeDamageService_CalculateArmorClass_AppliesArmorFloor()
        {
            var defender = NewActor(armor: 5);
            var svc = new RealtimeDamageService();
            // Use the default body-part node + any defense intent; pin a
            // non-negative armor-class output.
            var node = new BodyPartNode(BodyPart.Chest, parent: null, selectionWeight: 50, armorClassModifier: 2, damageMultiplierPercent: 100);
            var ac = svc.CalculateArmorClass(defender, node, default);
            Assert.That(ac, Is.GreaterThanOrEqualTo(0));
        }

        // ----- Faz7 acceptance roundtrips actor vitals (G-P2) -----
        [Test]
        public void Faz7_AcceptanceStyle_ActorVitalsSurviveSaveLoadWhenAddedToStore()
        {
            // Codex audit (fifth pass G-P2): the previous Faz7 acceptance test
            // mutated actors that were never added to the world store, so the
            // round-trip proved nothing about combat-applied damage. Pin the
            // contract here: vitals mutations on actors that ARE in the store
            // survive Save → Load.
            var world = new EmberCrpg.Simulation.World.SliceWorldFactory().Create(roomSeed: 1);
            var defender = world.Actors.Records.FirstOrDefault(a => a.Role == ActorRole.Enemy);
            Assert.That(defender, Is.Not.Null, "factory should seed at least one enemy");
            var startingHp = defender.Vitals.Health.Current;
            defender.ApplyVitals(defender.Vitals.WithHealth(defender.Vitals.Health.Damage(5)));

            var save = new EmberCrpg.Data.Save.JsonSliceSaveService();
            var json = save.SaveToJson(world);
            var loaded = save.LoadFromJson(json);

            var restoredDefender = loaded.Actors.Get(defender.Id);
            Assert.That(restoredDefender.Vitals.Health.Current, Is.EqualTo(startingHp - 5));
        }
    }
}
