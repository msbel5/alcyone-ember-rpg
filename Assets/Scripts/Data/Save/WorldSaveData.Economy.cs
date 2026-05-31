using System;

// REF-f (LEFT/59-LOC): Economy DTOs split out of WorldSaveData.cs (same namespace, zero behaviour change).
namespace EmberCrpg.Data.Save
{
    [Serializable]
    public sealed class FactionRecordSaveData
    {
        public long id;
        public string name;
        public string[] tags;
    }

    [Serializable]
    public sealed class FactionReputationSaveData
    {
        public long a;
        public long b;
        public int reputation;
    }

    [Serializable]
    public sealed class PriceLedgerSaveData
    {
        public long siteId;
        public string itemTag;
        public int price;
    }

    [Serializable]
    public sealed class StockpileSaveData
    {
        public long siteId;
        public StockpileEntrySaveData[] entries;
    }

    [Serializable]
    public sealed class StockpileEntrySaveData
    {
        public string itemTag;
        public int count;
    }

    [Serializable]
    public sealed class TradeRouteSaveData
    {
        public long id;
        public long originSiteId;
        public long destinationSiteId;
        public string itemTag;
        public int quantityPerCaravan;
        public int cadenceDays;
    }

    [Serializable]
    public sealed class CaravanSaveData
    {
        public long id;
        public long routeId;
        public long currentSiteId;
        public int payloadRemaining;
        public int stepsSinceDeparture;
        public string stateCode;
    }
}
