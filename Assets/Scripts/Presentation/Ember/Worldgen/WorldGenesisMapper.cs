using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    /// <summary>
    /// Single source of truth for mapping world-genesis choice ids (and legacy free-text)
    /// into deterministic worldgen enums.
    /// </summary>
    public static class WorldGenesisMapper
    {
        public static WorldStyle ToStyle(string mood)
        {
            var text = Normalize(mood);
            switch (text)
            {
                case "mythic": return WorldStyle.AncientMythology;
                case "low": return WorldStyle.LowFantasy;
                case "heroic": return WorldStyle.HighFantasy;
            }

            if (text.Contains("grim") || text.Contains("dark") || text.Contains("bleak")) return WorldStyle.DarkFantasyGrim;
            if (text.Contains("high") || text.Contains("heroic") || text.Contains("tolkien")) return WorldStyle.HighFantasy;
            if (text.Contains("steam") || text.Contains("industrial") || text.Contains("revolution")) return WorldStyle.SteampunkRevolution;
            if (text.Contains("ancient") || text.Contains("myth") || text.Contains("bronze")) return WorldStyle.AncientMythology;
            return WorldStyle.LowFantasy;
        }

        public static WorldGenre ToGenre(string mood, string calling, string startLocation)
        {
            var callingId = Normalize(calling);
            switch (callingId)
            {
                case "intrigue": return WorldGenre.PoliticalIntrigue;
                case "hunt": return WorldGenre.MonsterHunt;
                case "merchant": return WorldGenre.MerchantEmpire;
                case "pilgrimage": return WorldGenre.Pilgrimage;
            }

            var text = Normalize((mood ?? string.Empty) + " " + (calling ?? string.Empty) + " " + (startLocation ?? string.Empty));
            if (text.Contains("politic") || text.Contains("diplomat") || text.Contains("court") || text.Contains("noble")) return WorldGenre.PoliticalIntrigue;
            if (text.Contains("monster") || text.Contains("hunt") || text.Contains("beast")) return WorldGenre.MonsterHunt;
            if (text.Contains("merchant") || text.Contains("trade") || text.Contains("caravan") || text.Contains("smith")) return WorldGenre.MerchantEmpire;
            if (text.Contains("pilgrim") || text.Contains("shrine") || text.Contains("temple") || text.Contains("priest")) return WorldGenre.Pilgrimage;
            return WorldGenre.Survival;
        }

        public static SettlementSize ToPreferredSettlementSize(string startLocation)
        {
            var text = Normalize(startLocation);
            if (text.Contains("capital")) return SettlementSize.Capital;
            if (text.Contains("city")) return SettlementSize.City;
            if (text.Contains("hamlet")) return SettlementSize.Hamlet;
            if (text.Contains("village") || text.Contains("farm")) return SettlementSize.Village;
            return SettlementSize.Town;
        }

        private static string Normalize(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}
