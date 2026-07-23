using EmberCrpg.Domain.Actors;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // PLAYTEST FIX ("baslangic hikayesi yok"): the intro narrative existed only in a log
        // line. A NEW GAME raises this flag; the in-game UI shows the story overlay ONCE.
        public static bool JustCreatedWorld;
        public static string OpeningHook = string.Empty;

        /// <summary>F31: configure the three-act spine at world seed — the FINAL delve is the
        /// LAST Dungeon in the overland list (deterministic per seed), the piece requirement
        /// adapts to the world's delve count, and the intro lands in the journal narrative.</summary>
        private void ConfigureMainQuest()
        {
            var map = _world?.Overland;
            if (map == null || _world.MainQuest == null) return;
            int delveCount = 0;
            ulong finalId = 0UL;
            string finalName = "?";
            for (int i = 0; i < map.Settlements.Count; i++)
            {
                if (map.Settlements[i].Kind != EmberCrpg.Domain.Overland.SettlementKind.Dungeon) continue;
                delveCount++;
                finalId = map.Settlements[i].Id.Value;
                finalName = map.Settlements[i].Name;
            }
            _world.MainQuest.Configure(delveCount, finalId);
            JustCreatedWorld = true; // PLAYTEST: the intro overlay consumes this once
            _world.LastNarrative =
                "The ember under the world has a name, and the old stones remember it. Gather the " +
                $"inscription pieces from the delves ({_world.MainQuest.RequiredInscriptions}), carry them " +
                "to the capital's sage, and face what wards the final dark.";
            OpeningHook = _world.LastNarrative;
            UnityEngine.Debug.Log(
                $"[MainQuest] intro: three acts armed — pieces={_world.MainQuest.RequiredInscriptions}, " +
                $"finalDelve='{finalName}' (id={finalId}), delves={delveCount}.");
        }

        /// <summary>F31: the capital — the first CITY in the overland list (deterministic);
        /// falls back to the starting settlement's name when no city rolled.</summary>
        public string CapitalSettlementName()
        {
            var map = _world?.Overland;
            if (map == null) return null;
            for (int i = 0; i < map.Settlements.Count; i++)
                if (map.Settlements[i].Kind == EmberCrpg.Domain.Overland.SettlementKind.City)
                    return map.Settlements[i].Name;
            return StartingSettlementName;
        }

        /// <summary>F31: the final delve's NAME for the driver's travel leg.</summary>
        public string FinalDelveName()
        {
            var map = _world?.Overland;
            var quest = _world?.MainQuest;
            if (map == null || quest == null) return null;
            for (int i = 0; i < map.Settlements.Count; i++)
                if (map.Settlements[i].Id.Value == quest.FinalDelveId)
                    return map.Settlements[i].Name;
            return null;
        }

        /// <summary>F31 ACT 2: the sage reads the joined inscription (the proof driver speaks for
        /// the E-key dialog; the act gate refuses early consultations honestly).</summary>
        public string ProofConsultSage()
        {
            var quest = _world?.MainQuest;
            if (quest == null) return "MAINQUEST: no spine.";
            if (!quest.TryConsultSage(out var line))
                return $"MAINQUEST: the sage has nothing for you (act={quest.Act}, " +
                       $"pieces={quest.InscriptionsFound}/{quest.RequiredInscriptions}).";
            _world.LastNarrative = line;
            UnityEngine.Debug.Log($"[MainQuest] sage: {line} (act={quest.Act})");
            return "MAINQUEST: " + line;
        }

        /// <summary>F34: every settlement name — the marathon's travel roulette wheel.</summary>
        public System.Collections.Generic.List<string> ProofListSettlementNames()
        {
            var names = new System.Collections.Generic.List<string>();
            var map = _world?.Overland;
            if (map == null) return names;
            for (int i = 0; i < map.Settlements.Count; i++)
                names.Add(map.Settlements[i].Name);
            return names;
        }

        public string ProofMainQuestSnapshot()
        {
            var quest = _world?.MainQuest;
            return quest == null
                ? "MAINQUEST: none"
                : $"MAINQUEST act={quest.Act} pieces={quest.InscriptionsFound}/{quest.RequiredInscriptions} " +
                  $"final={quest.FinalDelveId} complete={quest.IsComplete}";
        }
    }
}
