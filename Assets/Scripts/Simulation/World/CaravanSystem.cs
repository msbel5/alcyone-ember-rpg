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

                var delivered = 0;
                if (caravan.PayloadRemaining == 0)
                {
                    var origin = resolveStockpile(route.OriginSiteId);
                    var loaded = origin?.Remove(route.ItemTag, route.QuantityPerCaravan) ?? 0;
                    caravan.Load(loaded);
                }

                // PR#161 bot review fix: previously the caravan was marked Arrived
                // before checking if the destination stockpile resolved. When the
                // destination returned null the payload was never unloaded yet the
                // state was Arrived, so the next tick skipped the caravan and the
                // goods sat in limbo forever. Resolve the destination first; only
                // commit Arrive when delivery actually happens. Otherwise emit a
                // stuck event and leave the caravan in its current state so the
                // next tick can retry.
                var destination = resolveStockpile(route.DestinationSiteId);
                if (destination == null)
                {
                    events.Append(new WorldEvent(
                        now,
                        WorldEventKind.CaravanArrived,
                        default,
                        route.DestinationSiteId,
                        $"caravan_stuck id:{caravan.Id} route:{caravan.RouteId} item:{route.ItemTag} reason:destination_unavailable"));
                    continue;
                }

                caravan.Arrive(route.DestinationSiteId);
                destination.Add(route.ItemTag, caravan.PayloadRemaining);
                delivered = caravan.PayloadRemaining;
                caravan.Unload(caravan.PayloadRemaining);

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
