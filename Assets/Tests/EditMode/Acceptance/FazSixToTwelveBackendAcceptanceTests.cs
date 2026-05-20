using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.Memory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Acceptance
{
    public sealed class FazSixToTwelveBackendAcceptanceTests
    {
        [Test]
        public void Faz6_CaravanTradePriceReputationShortage_AndSaveRoundTrip()
        {
            var world = NewWorld();
            var origin = new StockpileComponent(new SiteId(20));
            var destination = new StockpileComponent(new SiteId(21));
            origin.Add("iron", 8);
            destination.Add("coin", 100);
            world.Stockpiles.Add(origin);
            world.Stockpiles.Add(destination);
            world.Prices.SetPrice(origin.SiteId, "iron", 10);
            world.Factions.Add(new FactionRecord(new FactionId(1), "Forge", new string[0]));
            world.Factions.Add(new FactionRecord(new FactionId(2), "Harbor", new string[0]));

            var route = new TradeRouteDef(new TradeRouteId(1), origin.SiteId, destination.SiteId, "iron", 5, 1);
            var caravan = new CaravanInstance(new CaravanId(1), route.Id, origin.SiteId, 0, 0, CaravanState.EnRoute);
            world.TradeRoutes.Add(route);
            world.Caravans.Add(caravan);

            new CaravanSystem().Tick(world.Caravans, world.FindTradeRoute, world.FindStockpile, world.Time, world.Events);
            Assert.That(origin.Get("iron"), Is.EqualTo(3));
            Assert.That(destination.Get("iron"), Is.EqualTo(5));

            var traded = new TradeService().TryTrade(
                world.Prices,
                destination,
                origin,
                "iron",
                2,
                world.Time,
                world.Events,
                currencyTag: "coin",
                factions: world.Factions,
                buyerFaction: new FactionId(2),
                sellerFaction: new FactionId(1),
                reputationDelta: 7);
            Assert.That(traded, Is.True);
            Assert.That(destination.Get("coin"), Is.EqualTo(80));
            Assert.That(origin.Get("coin"), Is.EqualTo(20));
            Assert.That(world.Factions.GetReputation(new FactionId(1), new FactionId(2)).Value, Is.EqualTo(7));

            new PriceUpdateSystem().Recompute(world.Prices, origin, "iron", lowThreshold: 2, highThreshold: 20, delta: 3, world.Time, world.Events);
            new ShortageDetector().Check(origin, "iron", threshold: 2, world.Time, world.Events);

            var restored = RoundTrip(world);
            Assert.That(restored.Prices.GetPrice(origin.SiteId, "iron"), Is.EqualTo(13));
            Assert.That(restored.FindStockpile(origin.SiteId).Get("iron"), Is.EqualTo(1));
            Assert.That(restored.FindStockpile(destination.SiteId).Get("iron"), Is.EqualTo(7));
            Assert.That(restored.TradeRoutes.Single().ItemTag, Is.EqualTo("iron"));
            Assert.That(restored.Caravans.Single().State, Is.EqualTo(CaravanState.Idle));
            Assert.That(restored.Factions.GetReputation(new FactionId(1), new FactionId(2)).Value, Is.EqualTo(7));
        }

        [Test]
        public void Faz7_EquipmentCombatMutatesVitals_AndSaveRoundTrip()
        {
            var world = NewWorld();
            var sword = new ItemId(900);
            world.Items.Add(new ItemRecord(sword, ItemMaterial.Iron, ItemQuality.Fine, EquipmentSlot.Weapon));
            world.PlayerEquipment.Equip(EquipmentSlot.Weapon, sword);

            var attacker = Actor("Striker", new ActorId(30), ActorRole.Player, health: 20, fatigue: 10, mana: 5, accuracy: 120, dodge: 0, armor: 0, damage: 8);
            var defender = Actor("Bandit", new ActorId(31), ActorRole.Enemy, health: 20, fatigue: 8, mana: 3, accuracy: 40, dodge: 0, armor: 1, damage: 2);
            var action = new CombatActionDef(new CombatActionId("slash"), staminaCost: 3, "flat", "flat", "slash");

            var outcome = new CombatActionResolver(new CombatHitRollService(), new CombatDamageService())
                .Resolve(action, attacker, defender, damageBandWidth: 0, new XorShiftRng(1), world.Time, new SiteId(30), world.Events);

            Assert.That(outcome.Hit, Is.True);
            Assert.That(outcome.Damage, Is.EqualTo(7));
            Assert.That(attacker.Vitals.Fatigue.Current, Is.EqualTo(7));
            Assert.That(defender.Vitals.Health.Current, Is.EqualTo(13));
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.CombatResolved), Is.True);

            var restored = RoundTrip(world);
            Assert.That(restored.PlayerEquipment.GetEquippedItemId(EquipmentSlot.Weapon), Is.EqualTo(sword));
            Assert.That(EquipmentSlot.Weapon.Code, Is.EqualTo("main_hand"));
        }

        [Test]
        public void Faz8_TerrainSpellMutatesActorAndTerrain_AndSaveRoundTrip()
        {
            var world = NewWorld();
            var terrain = new StockpileComponent(new SiteId(44));
            terrain.Add("oil", 1);
            world.Stockpiles.Add(terrain);

            var target = world.Enemy;
            var before = target.Vitals.Health.Current;
            var handlers = new EffectOperationHandlers();
            handlers.Register(EffectOperationKind.TerrainApply, op => op.Magnitude);
            var spell = new EffectDefinition(
                new EffectId(8),
                "fire",
                new[] { new EffectOperation(EffectOperationKind.TerrainApply, 4, "oil", 0) },
                cost: 0,
                cooldownTicks: 0);

            var result = new SpellResolver(handlers).Resolve(
                spell,
                casterMana: 10,
                world.Time,
                terrain.SiteId,
                world.Events,
                new SpellResolverContext(target, terrain, "oil", "fire"));

            Assert.That(result.Resolved, Is.True);
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(before - 4));
            Assert.That(terrain.Get("oil"), Is.EqualTo(0));
            Assert.That(terrain.Get("fire"), Is.EqualTo(1));

            var restored = RoundTrip(world);
            Assert.That(restored.Enemy.Vitals.Health.Current, Is.EqualTo(before - 4));
            Assert.That(restored.FindStockpile(terrain.SiteId).Get("fire"), Is.EqualTo(1));
        }

        [Test]
        public void Faz9_WorldEventTheftMemoryRefusesTradeAfterTwoDays_AndSaveRoundTrip()
        {
            var thief = Actor("Thief", new ActorId(41), ActorRole.Player, 20, 10, 5, 40, 5, 0, 2);
            var theft = new WorldEvent(new GameTime(0), WorldEventKind.ActorTalked, thief.Id, new SiteId(7), "theft item:coin");
            var world = NewWorld();
            var merchant = world.Merchant;

            var writer = new MemoryWriteSystem();
            Assert.That(writer.RecordFromWorldEvent(merchant.Memory, theft), Is.True);

            var refusal = new TradeRefusalHook(new MemoryRecallService());
            var now = new GameTime(GameTime.MinutesPerDay * 2);
            Assert.That(refusal.ShouldRefuse(
                merchant.Memory,
                default,
                default,
                null,
                now,
                now.AddDays(-2),
                out var reason), Is.True);
            Assert.That(reason, Is.EqualTo("memory_recent_crime"));

            var restored = RoundTrip(world);
            Assert.That(restored.Merchant.Memory.Facts.Single().Detail, Does.Contain("theft"));
        }

        [Test]
        public void Faz10_NpcEscalatesToDm_DmQueriesSnapshot_TracePersists()
        {
            var world = NewWorld();
            var registry = new ToolRegistry();
            DmAgentToolSurface.RegisterAll(registry);
            var router = new ToolCallRouter(new ToolCallValidator());
            var tracer = new ToolCallTracer();
            new DmAgentEscalationService().RegisterHandlers(router, world);

            var request = new ToolCallRequest(new ToolId("query_world_snapshot"), ToolSurfaceKind.Dm, new Dictionary<string, string>());
            var result = router.Invoke(request, registry, world.Time, new SiteId(9), world.Events, tracer);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Payload, Does.Contain("actors:5"));
            Assert.That(tracer.Entries.Count, Is.EqualTo(1));
            world.ToolCallTrace.AddRange(tracer.Entries);

            var restored = RoundTrip(world);
            Assert.That(restored.ToolCallTrace.Single().Request.ToolId.Code, Is.EqualTo("query_world_snapshot"));
            Assert.That(restored.ToolCallTrace.Single().Result.Accepted, Is.True);
        }

        [Test]
        public void Faz12_MockFlavourDoesNotMutateWorld_ConsultFateAppliesOnlyApprovedTools()
        {
            var world = NewWorld();
            var mock = new MockLlmClient();
            var flavourRequest = new LlmRequest("npc_flavour", "tavern", null, 40, 3);
            mock.Script("npc_flavour", "tavern", 3, new LlmResponse("smoke hangs over the tavern", null, 6));
            var flavour = new NpcFlavourService(new LlmRoutingService(mock.Complete, null), new FlavourBudget(1));

            var beforeEvents = world.Events.Count;
            var flavourResponse = flavour.Generate(flavourRequest, world.Time, world, "fallback");
            Assert.That(flavourResponse.Text, Does.Contain("tavern"));
            Assert.That(world.Events.Count, Is.EqualTo(beforeEvents));

            var registry = new ToolRegistry();
            DmAgentToolSurface.RegisterAll(registry);
            var router = new ToolCallRouter(new ToolCallValidator());
            router.RegisterHandler(ToolSurfaceKind.Dm, new ToolId("propose_event"), request =>
            {
                world.Events.Append(new WorldEvent(world.Time, WorldEventKind.ActorTalked, world.Player.Id, new SiteId(9), "approved_tool_call"));
                return ToolCallResult.AcceptedWith("event:approved");
            });
            var tracer = new ToolCallTracer();
            var consult = new ConsultFateService(new LlmProposalValidator(new ToolCallValidator()), router);
            var response = new LlmResponse(
                "fate turns",
                new[]
                {
                    new ToolCallRequest(new ToolId("propose_event"), ToolSurfaceKind.Dm, new Dictionary<string, string>
                    {
                        { "event_kind", "ActorTalked" },
                        { "site", "9" },
                        { "reason", "approved" },
                    }),
                    new ToolCallRequest(new ToolId("unknown_tool"), ToolSurfaceKind.Dm, new Dictionary<string, string>()),
                },
                12);

            var result = consult.Resolve(
                new LlmRequest("consult_fate", "fate", DmAgentToolSurface.Descriptors(), 80, 72),
                response,
                registry,
                world.Time,
                new SiteId(9),
                world.Events,
                tracer,
                seed: 72);

            Assert.That(result.Bucket, Is.EqualTo(ConsultFateOutcomeBucket.Favourable));
            Assert.That(result.AcceptedProposals, Is.EqualTo(1));
            Assert.That(result.RejectedProposals, Is.EqualTo(1));
            Assert.That(result.AppliedToolCalls, Is.EqualTo(1));
            Assert.That(world.Events.Events.Count(e => e.Reason == "approved_tool_call"), Is.EqualTo(1));
        }

        private static SliceWorldState NewWorld()
        {
            return new SliceWorldFactory().Create(1337);
        }

        private static SliceWorldState RoundTrip(SliceWorldState world)
        {
            return SliceSaveMapper.ToWorld(SliceSaveMapper.ToData(world));
        }

        private static ActorRecord Actor(string name, ActorId id, ActorRole role, int health, int fatigue, int mana, int accuracy, int dodge, int armor, int damage)
        {
            return new ActorRecord(
                id,
                name,
                role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(health, health), new VitalStat(fatigue, fatigue), new VitalStat(mana, mana)),
                new GridPosition(1, 1),
                accuracy,
                dodge,
                armor,
                damage);
        }
    }
}
