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
    public sealed class RecipesDocumentDto
    {
        public List<RecipeDto> recipes = new List<RecipeDto>();
    }

    public sealed class RecipeDto
    {
        public string id;
        public string name;
        public string workstation;
        public string skill;
        public int skill_dc;
        public int ap_cost;
        public List<RecipeIngredientDto> ingredients = new List<RecipeIngredientDto>();
        public List<RecipeProductDto> products = new List<RecipeProductDto>();
        public List<string> tools = new List<string>();
        public string failure_result;
        public int xp_reward;
    }

    public sealed class RecipeIngredientDto
    {
        public string item_id;
        public int quantity;
        public string material_class;
    }

    public sealed class RecipeProductDto
    {
        public string item_id;
        public int quantity;
        public bool inherit_material;
    }

    public sealed class MaterialListDocumentDto
    {
        public List<MaterialDto> materials = new List<MaterialDto>();

#if UNITY_5_3_OR_NEWER
        [JsonExtensionData] public IDictionary<string, JToken> extension_data;
#else
        [JsonExtensionData] public IDictionary<string, JsonElement> extension_data;
#endif
    }

    public sealed class MaterialDto
    {
        public string material_id;
        public string label;
        public string category;
        public float density;
        public int impact_yield;
        public int impact_fracture;
        public int shear_yield;
        public int shear_fracture;
        public int max_edge;
        public List<string> tags = new List<string>();
    }
}
