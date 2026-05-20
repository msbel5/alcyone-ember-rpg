using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// Ticks caravans along their trade routes. Each tick advances a step;
    /// when StepsSinceDeparture reaches the route's CadenceDays, the caravan
    /// arrives at the destination, unloads into the destination stockpile,
    /// and emits CaravanArrived. Faz 6 Atom 9.
    /// </summary>
    public sealed class CaravanSystem
    {
        public void Tick(
            IReadOnlyList<CaravanInstance> caravans,
            Func<TradeRouteId, TradeRouteDef> resolveRoute,
            Func<SiteId, StockpileComponent> resolveStockpile,
            GameTime now,
            WorldEventLog events)
        {
            if (caravans == null) throw new ArgumentNullException(nameof(caravans));
            if (resolveRoute == null) throw new ArgumentNullException(nameof(resolveRoute));
            if (resolveStockpile == null) throw new ArgumentNullException(nameof(resolveStockpile));
            if (events == null) throw new ArgumentNullException(nameof(events));

            foreach (var caravan in caravans)
            {
                if (caravan == null) continue;
                if (caravan.State.Equals(CaravanState.Idle)) continue;
                if (caravan.State.Equals(CaravanState.Arrived)) continue;

                var route = resolveRoute(caravan.RouteId);
                if (route == null) continue;

                caravan.AdvanceStep();
                if (caravan.StepsSinceDeparture < route.CadenceDays)
                    continue;

                caravan.Arrive(route.DestinationSiteId);
                var destination = resolveStockpile(route.DestinationSiteId);
                var delivered = 0;
                if (destination != null)
                {
                    destination.Add(route.ItemTag, caravan.PayloadRemaining);
                    delivered = caravan.PayloadRemaining;
                    caravan.Unload(caravan.PayloadRemaining);
                }

                events.Append(new WorldEvent(
                    now,
                    WorldEventKind.CaravanArrived,
                    default,
                    route.DestinationSiteId,
                    $"caravan_arrived id:{caravan.Id} route:{caravan.RouteId} item:{route.ItemTag} delivered:{delivered}"));
            }
        }
    }
}
