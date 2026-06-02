using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class EconomyConfigDocumentDto
    {
        public EconomyConfigDto economy_config = new EconomyConfigDto();
    }

    public sealed class EconomyConfigDto : ContentExtensionDto
    {
        public List<string> trade_items = new List<string>();
        public List<string> price_tracking_items = new List<string>();
        public List<StoreInventoryItemDto> default_store_inventory = new List<StoreInventoryItemDto>();
        public List<StoreServiceDto> default_store_services = new List<StoreServiceDto>();
        public List<CommodityDto> commodities = new List<CommodityDto>();
    }

    public sealed class CommodityDto
    {
        public string item_id;
        public string name;
        public string category;
        public int base_price;
        public float volatility;
        public float weight;
        public string description;
    }

    public sealed class StoreInventoryItemDto
    {
        public string item_def_id;
        public int quantity;
    }

    public sealed class StoreServiceDto : ContentExtensionDto
    {
        public string service_id;
        public string service_type;
        public string label;
        public int price;
        public object room_quality;
    }

    public sealed class ColonyConfigDocumentDto
    {
        public ColonyConfigDto colony_config = new ColonyConfigDto();
    }

    public sealed class ColonyConfigDto : ContentExtensionDto
    {
        public Dictionary<string, ColonyNeedDto> needs = new Dictionary<string, ColonyNeedDto>();
        public List<MoraleTierDto> morale_tiers = new List<MoraleTierDto>();
        public Dictionary<string, ColonyShortageQuestRefDto> shortage_quests = new Dictionary<string, ColonyShortageQuestRefDto>();
        public Dictionary<string, object> thresholds = new Dictionary<string, object>();
        public Dictionary<string, PressureTagDto> pressure_tags = new Dictionary<string, PressureTagDto>();
        public Dictionary<string, RoomZoneDto> room_zones = new Dictionary<string, RoomZoneDto>();
    }

    public sealed class ColonyNeedDto { public string label; public float decay_rate; public float fulfillment_base; public float desperate_threshold; public float weight; }
    public sealed class MoraleTierDto : ContentExtensionDto { public string tier; }
    public sealed class ShortageQuestDto { public string item_id; public string title; public string description; public int quantity; public int reward_gold; public int deadline_hours; }
    public sealed class ColonyShortageQuestRefDto { public string kind; public string title; public int priority; }
    public sealed class PressureTagDto : ContentExtensionDto { }
    public sealed class RoomZoneDto : ContentExtensionDto { public List<string> required_furniture = new List<string>(); }

    public sealed class CaravansDocumentDto
    {
        public Dictionary<string, CaravanDto> caravans = new Dictionary<string, CaravanDto>();
    }

    public sealed class CaravanDto
    {
        public string name;
        public string origin;
        public string destination;
        public Dictionary<string, int> goods = new Dictionary<string, int>();
        public int travel_hours;
        public int frequency_hours;
        public int value;
    }
}
