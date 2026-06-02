using System.Collections.Generic;

namespace EmberCrpg.Data.Content
{
    public sealed class ItemsDocumentDto
    {
        public List<ItemDto> items = new List<ItemDto>();
    }

    public sealed class ItemDto
    {
        public string id;
        public string name;
        public string type;
        public string rarity;
        public int value;
        public float weight;
        public string description;
        public bool stackable;
    }
}
