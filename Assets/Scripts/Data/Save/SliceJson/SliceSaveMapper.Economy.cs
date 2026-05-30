// SliceSaveMapper partial — economy: prices, stockpiles, trade routes, caravans (split from the 961-line monolith, NAME/LOC-split).
using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Data.Save
{
    public static partial class SliceSaveMapper
    {
        private static PriceLedgerSaveData[] ToPriceLedgerData(PriceLedger ledger)
        {
            return (ledger?.Entries ?? Array.Empty<PriceLedgerEntry>())
                .Select(row => new PriceLedgerSaveData
                {
                    siteId = (long)row.SiteId.Value,
                    itemTag = row.ItemTag,
                    price = row.Price,
                })
                .ToArray();
        }

        private static PriceLedger ToPriceLedger(PriceLedgerSaveData[] data)
        {
            var ledger = new PriceLedger();
            foreach (var row in data ?? Array.Empty<PriceLedgerSaveData>())
            {
                if (row == null || row.siteId == 0L || string.IsNullOrWhiteSpace(row.itemTag))
                    continue;
                ledger.SetPrice(new SiteId((ulong)row.siteId), row.itemTag, row.price);
            }

            return ledger;
        }

        private static StockpileSaveData[] ToStockpileData(IEnumerable<StockpileComponent> stockpiles)
        {
            return (stockpiles ?? Array.Empty<StockpileComponent>())
                .Where(stockpile => stockpile != null)
                .Select(stockpile => new StockpileSaveData
                {
                    siteId = (long)stockpile.SiteId.Value,
                    entries = stockpile.Entries.Select(entry => new StockpileEntrySaveData
                    {
                        itemTag = entry.Key,
                        count = entry.Value,
                    }).ToArray(),
                })
                .ToArray();
        }

        private static List<StockpileComponent> ToStockpiles(StockpileSaveData[] data)
        {
            var stockpiles = new List<StockpileComponent>();
            foreach (var row in data ?? Array.Empty<StockpileSaveData>())
            {
                if (row == null || row.siteId == 0L)
                    continue;
                var stockpile = new StockpileComponent(new SiteId((ulong)row.siteId));
                foreach (var entry in row.entries ?? Array.Empty<StockpileEntrySaveData>())
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.itemTag) || entry.count <= 0)
                        continue;
                    stockpile.Add(entry.itemTag, entry.count);
                }
                stockpiles.Add(stockpile);
            }

            return stockpiles;
        }

        private static TradeRouteSaveData[] ToTradeRouteData(IEnumerable<TradeRouteDef> routes)
        {
            return (routes ?? Array.Empty<TradeRouteDef>())
                .Where(route => route != null)
                .Select(route => new TradeRouteSaveData
                {
                    id = (long)route.Id.Value,
                    originSiteId = (long)route.OriginSiteId.Value,
                    destinationSiteId = (long)route.DestinationSiteId.Value,
                    itemTag = route.ItemTag,
                    quantityPerCaravan = route.QuantityPerCaravan,
                    cadenceDays = route.CadenceDays,
                })
                .ToArray();
        }

        private static List<TradeRouteDef> ToTradeRoutes(TradeRouteSaveData[] data)
        {
            return (data ?? Array.Empty<TradeRouteSaveData>())
                .Where(row => row != null && row.id != 0L)
                .Select(row => new TradeRouteDef(
                    new TradeRouteId((ulong)row.id),
                    new SiteId((ulong)row.originSiteId),
                    new SiteId((ulong)row.destinationSiteId),
                    row.itemTag,
                    row.quantityPerCaravan,
                    row.cadenceDays))
                .ToList();
        }

        private static CaravanSaveData[] ToCaravanData(IEnumerable<CaravanInstance> caravans)
        {
            return (caravans ?? Array.Empty<CaravanInstance>())
                .Where(caravan => caravan != null)
                .Select(caravan => new CaravanSaveData
                {
                    id = (long)caravan.Id.Value,
                    routeId = (long)caravan.RouteId.Value,
                    currentSiteId = (long)caravan.CurrentSiteId.Value,
                    payloadRemaining = caravan.PayloadRemaining,
                    stepsSinceDeparture = caravan.StepsSinceDeparture,
                    stateCode = caravan.State.Code,
                })
                .ToArray();
        }

        private static List<CaravanInstance> ToCaravans(CaravanSaveData[] data)
        {
            return (data ?? Array.Empty<CaravanSaveData>())
                .Where(row => row != null && row.id != 0L)
                .Select(row => new CaravanInstance(
                    new CaravanId((ulong)row.id),
                    new TradeRouteId((ulong)row.routeId),
                    new SiteId((ulong)row.currentSiteId),
                    row.payloadRemaining,
                    row.stepsSinceDeparture,
                    CaravanState.FromCode(row.stateCode)))
                .ToList();
        }
    }
}
