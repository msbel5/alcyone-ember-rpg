using System;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Domain.Forge
{
    public sealed class PromptTemplate
    {
        private readonly string _template;

        public PromptTemplate(string template)
        {
            if (string.IsNullOrWhiteSpace(template)) throw new ArgumentException("Template is required.", nameof(template));
            _template = template;
        }

        public string Interpolate(WorldProfile profile, NpcSeedRecord npc)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (npc == null) throw new ArgumentNullException(nameof(npc));
            return ReplaceCommon(profile)
                .Replace("{NpcName}", npc.Name)
                .Replace("{NpcRole}", npc.Role.ToString())
                .Replace("{BirthYear}", npc.BirthYear.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public string Interpolate(WorldProfile profile, RegionRecord region)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (region == null) throw new ArgumentNullException(nameof(region));
            return ReplaceCommon(profile)
                .Replace("{RegionName}", region.Name)
                .Replace("{Biome}", region.Biome.ToString());
        }

        public string Interpolate(WorldProfile profile, ItemRecord item)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (item == null) throw new ArgumentNullException(nameof(item));
            return ReplaceCommon(profile)
                .Replace("{ItemId}", item.Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Replace("{Material}", item.Material.ToString())
                .Replace("{Quality}", item.Quality.ToString())
                .Replace("{Slot}", item.Slot.ToString());
        }

        private string ReplaceCommon(WorldProfile profile)
        {
            return _template
                .Replace("{Style}", profile.Style.ToString())
                .Replace("{Genre}", profile.Genre.ToString())
                .Replace("{Mood}", profile.MoodKeyword ?? string.Empty)
                .Replace("{Calling}", profile.PlayerCallingKeyword ?? string.Empty)
                .Replace("{Start}", profile.StartLocationKeyword ?? string.Empty)
                .Replace("{Seed}", profile.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
