using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace EmberCrpg.Data.Content
{
    public sealed class FactionsDocumentDto
    {
        public FactionCatalogDto factions = new FactionCatalogDto();
    }

    public sealed class FactionCatalogDto
    {
        public Dictionary<string, int> reaction_levels = new Dictionary<string, int>();
        public List<string> action_types = new List<string>();
        public Dictionary<string, Dictionary<string, string>> ethics = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, Dictionary<string, int>> values = new Dictionary<string, Dictionary<string, int>>();
        public Dictionary<string, string> consequences = new Dictionary<string, string>();

#if UNITY_5_3_OR_NEWER
        [JsonExtensionData] public IDictionary<string, JToken> extension_data;
#else
        [JsonExtensionData] public IDictionary<string, JsonElement> extension_data;
#endif
    }

    public sealed class FactionDto
    {
        public Dictionary<string, string> ethics = new Dictionary<string, string>();
        public Dictionary<string, int> values = new Dictionary<string, int>();
    }

    public sealed class ClassesDocumentDto
    {
        public Dictionary<string, ClassDto> classes = new Dictionary<string, ClassDto>();
    }

    public sealed class ClassDto
    {
        public string name;
        public string description;
        public string hit_die;
        public int hit_die_size;
        public int ap_per_turn;
        public List<string> ability_priority = new List<string>();
        public List<string> save_proficiencies = new List<string>();
        public List<string> skill_pool = new List<string>();
        public int skill_pick_count;
        public List<string> default_skills = new List<string>();
        public List<ClassEquipmentDto> starting_equipment = new List<ClassEquipmentDto>();
        public int starting_gold;
        public string armor_type;
        public Dictionary<string, int> default_stats = new Dictionary<string, int>();
        public int default_hp;
        public int default_spell_points;
    }

    public sealed class ClassEquipmentDto
    {
        public string id;
        public string name;
        public string type;
        public int damage;
        public int ac_bonus;
        public int restore_hp;
        public int restore_sp;
        public int qty;
        public string material;
        public string slot;
    }
}
