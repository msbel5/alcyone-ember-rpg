using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    public sealed class CaravanSystemTests
    {
        private static readonly TradeRouteId RouteId = new TradeRouteId(101UL);
        private static readonly CaravanId CaravanId = new CaravanId(7UL);
        private static readonly SiteId Origin = new SiteId(1UL);
        private static readonly SiteId Destination = new SiteId(2UL);

        private static TradeRouteDef Route() =>
            new TradeRouteDef(RouteId, Origin, Destination, "iron_ingot", 5, 3);

        [Test]
        public void Tick_AdvancesEnRouteCaravan_NoArrivalUntilCadenceReached()
        {
            var caravan = new CaravanInstance(CaravanId, RouteId, Origin, 5, 0, CaravanState.EnRoute);
            var route = Route();
            var stockpile = new StockpileComponent(Destination);
            var events = new WorldEventLog();
            var system = new CaravanSystem();

            system.Tick(new[] { caravan }, _ => route, _ => stockpile, default, events);
            system.Tick(new[] { caravan }, _ => route, _ => stockpile, default, events);

            Assert.That(caravan.StepsSinceDeparture, Is.EqualTo(2));
            Assert.That(caravan.State, Is.EqualTo(CaravanState.EnRoute));
            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void Tick_AtCadence_ArrivesAndDeliversToStockpile()
        {
            var caravan = new CaravanInstance(CaravanId, RouteId, Origin, 5, 2, CaravanState.EnRoute);
            var route = Route();
            var stockpile = new StockpileComponent(Destination);
            var events = new WorldEventLog();
            var system = new CaravanSystem();

            system.Tick(new[] { caravan }, _ => route, _ => stockpile, default, events);

            Assert.That(caravan.State, Is.EqualTo(CaravanState.Idle));
            Assert.That(caravan.CurrentSiteId, Is.EqualTo(Destination));
            Assert.That(stockpile.Get("iron_ingot"), Is.EqualTo(5));
            Assert.That(caravan.PayloadRemaining, Is.EqualTo(0));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(WorldEventKind.CaravanArrived));
            Assert.That(events.Events[0].SiteId, Is.EqualTo(Destination));
        }

        [Test]
        public void Tick_IdleCaravan_NoOp()
        {
            var caravan = new CaravanInstance(CaravanId, RouteId, Origin, 0, 0, CaravanState.Idle);
            var route = Route();
            var stockpile = new StockpileComponent(Destination);
            var events = new WorldEventLog();
            var system = new CaravanSystem();

            system.Tick(new[] { caravan }, _ => route, _ => stockpile, default, events);

            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(caravan.StepsSinceDeparture, Is.EqualTo(0));
        }

        [Test]
        public void Tick_NullRoute_SkipsCaravan()
        {
            var caravan = new CaravanInstance(CaravanId, RouteId, Origin, 5, 0, CaravanState.EnRoute);
            var stockpile = new StockpileComponent(Destination);
            var events = new WorldEventLog();
            var system = new CaravanSystem();

            system.Tick(new[] { caravan }, _ => null, _ => stockpile, default, events);

            Assert.That(events.Count, Is.EqualTo(0));
        }

        // Codex audit Batch 5 / A-P2 regression: a caravan with zero payload
        // (origin stockpile empty or item missing) used to "arrive" with
        // delivered:0 — a successful-looking CaravanArrived event for nothing.
        // The caravan must stall instead, leaving its state intact so the
        // next tick can retry once origin restocks.
        [Test]
        public void Tick_AtCadence_OriginEmpty_StallsInsteadOfArriving()
        {
            // PayloadRemaining=0 → caravan will try to Load from origin.
            // Origin stockpile resolves but has no "iron_ingot" → Load(0).
            var caravan = new CaravanInstance(CaravanId, RouteId, Origin, 0, 2, CaravanState.EnRoute);
            var route = Route();
            var originStock = new StockpileComponent(Origin); // empty on purpose
            var destinationStock = new StockpileComponent(Destination);
            var events = new WorldEventLog();
            var system = new CaravanSystem();

            System.Func<SiteId, StockpileComponent> resolver = siteId =>
                siteId.Equals(Origin) ? originStock :
                siteId.Equals(Destination) ? destinationStock : null;

            system.Tick(new[] { caravan }, _ => route, resolver, default, events);

            // No goods delivered to destination.
            Assert.That(destinationStock.Get("iron_ingot"), Is.EqualTo(0));
            // Caravan remains EnRoute (not Idle / Arrived) — next tick can retry.
            Assert.That(caravan.State, Is.EqualTo(CaravanState.EnRoute));
            Assert.That(caravan.PayloadRemaining, Is.EqualTo(0));
            // One stuck event emitted at the ORIGIN site, not the destination.
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(WorldEventKind.CaravanArrived));
            Assert.That(events.Events[0].SiteId, Is.EqualTo(Origin));
            Assert.That(events.Events[0].Reason, Does.Contain("caravan_stuck"));
            Assert.That(events.Events[0].Reason, Does.Contain("origin_empty"));
        }
    }
}
