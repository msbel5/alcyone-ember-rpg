using System.Collections.Generic;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.Worldgen;

// Design note:
// WorldQuestGenerator (F21) mints one valid quest from pure worldgen-level inputs — NPC seeds,
// overland settlements, the current settlement, the current world day, a seed. Deterministic:
// same inputs + same seed → the same quest. No guilds yet: PEOPLE give work, picked by role.
// Validity is by construction — every reference is chosen from a filtered non-empty list; when
// a template's raw material is missing (no outlaws, single settlement) the generator rotates to
// the next template, and only a world with no eligible giver returns null.
namespace EmberCrpg.Simulation.Quest
{
    /// <summary>Deterministic single-quest factory for the F21 "görev makinesi".</summary>
    public static class WorldQuestGenerator
    {
        // Items a fetch/deliver quest may ask for — must exist in settlement trade stock so the
        // contract is honestly completable through the live economy.
        private static readonly string[] CargoTemplates = { "ale", "bread" };

        // Roles that hand out work (no guilds in v0.6 — people are the quest board).
        private static bool GivesWork(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Merchant:
                case NpcRole.Noble:
                case NpcRole.Priest:
                case NpcRole.Scholar:
                case NpcRole.Innkeeper:
                case NpcRole.Blacksmith:
                case NpcRole.Healer:
                    return true;
                default:
                    return false;
            }
        }

        public static WorldQuestRecord Generate(
            IReadOnlyList<NpcSeedRecord> npcs,
            IReadOnlyList<OverlandSettlement> settlements,
            SettlementId here,
            int currentDay,
            ulong seed,
            WorldQuestTemplate? force = null)
        {
            if (npcs == null || settlements == null || settlements.Count == 0) return null;

            ulong state = Mix(seed);

            var givers = new List<NpcSeedRecord>();
            for (int i = 0; i < npcs.Count; i++)
                if (npcs[i] != null && npcs[i].Home.Equals(here) && GivesWork(npcs[i].Role))
                    givers.Add(npcs[i]);
            if (givers.Count == 0) return null;
            var giver = givers[(int)(Next(ref state) % (ulong)givers.Count)];

            var elsewhere = new List<OverlandSettlement>();
            for (int i = 0; i < settlements.Count; i++)
                if (!settlements[i].Id.Equals(here))
                    elsewhere.Add(settlements[i]);

            var outlaws = new List<NpcSeedRecord>();
            for (int i = 0; i < npcs.Count; i++)
                if (npcs[i] != null && npcs[i].Role == NpcRole.Outlaw)
                    outlaws.Add(npcs[i]);

            int deadline = currentDay + 3 + (int)(Next(ref state) % 5u); // 3-7 days of slack
            var first = force ?? (WorldQuestTemplate)(Next(ref state) % 4u);

            // Rotate through the four templates starting at the rolled one — the first whose raw
            // material exists wins, so a sparse world still yields a valid contract.
            for (int attempt = 0; attempt < 4; attempt++)
            {
                var template = force ?? (WorldQuestTemplate)(((int)first + attempt) % 4);
                switch (template)
                {
                    case WorldQuestTemplate.Fetch:
                    {
                        string cargo = CargoTemplates[(int)(Next(ref state) % (ulong)CargoTemplates.Length)];
                        return new WorldQuestRecord
                        {
                            Template = WorldQuestTemplate.Fetch,
                            GiverNpcId = giver.Id,
                            GiverName = giver.Name,
                            TargetSettlementId = here,
                            TargetNpcId = giver.Id,
                            TargetNpcName = giver.Name,
                            ItemTemplateId = cargo,
                            RewardGold = 30 + (int)(Next(ref state) % 11u),
                            DeadlineDay = deadline,
                            Title = $"Bring {cargo} to {giver.Name}",
                        };
                    }
                    case WorldQuestTemplate.Kill when outlaws.Count > 0:
                    {
                        var mark = outlaws[(int)(Next(ref state) % (ulong)outlaws.Count)];
                        return new WorldQuestRecord
                        {
                            Template = WorldQuestTemplate.Kill,
                            GiverNpcId = giver.Id,
                            GiverName = giver.Name,
                            TargetNpcId = mark.Id,
                            TargetNpcName = mark.Name,
                            TargetSettlementId = mark.Home,
                            RewardGold = 60 + (int)(Next(ref state) % 21u),
                            DeadlineDay = deadline,
                            Title = $"Hunt {mark.Name}",
                        };
                    }
                    case WorldQuestTemplate.Deliver when elsewhere.Count > 0:
                    {
                        var to = elsewhere[(int)(Next(ref state) % (ulong)elsewhere.Count)];
                        string cargo = CargoTemplates[(int)(Next(ref state) % (ulong)CargoTemplates.Length)];
                        return new WorldQuestRecord
                        {
                            Template = WorldQuestTemplate.Deliver,
                            GiverNpcId = giver.Id,
                            GiverName = giver.Name,
                            TargetSettlementId = to.Id,
                            TargetSettlementName = to.Name,
                            ItemTemplateId = cargo,
                            RewardGold = 50 + (int)(Next(ref state) % 16u),
                            DeadlineDay = deadline,
                            Title = $"Deliver {cargo} to {to.Name}",
                        };
                    }
                    case WorldQuestTemplate.Visit when elsewhere.Count > 0:
                    {
                        var to = elsewhere[(int)(Next(ref state) % (ulong)elsewhere.Count)];
                        return new WorldQuestRecord
                        {
                            Template = WorldQuestTemplate.Visit,
                            GiverNpcId = giver.Id,
                            GiverName = giver.Name,
                            TargetSettlementId = to.Id,
                            TargetSettlementName = to.Name,
                            RewardGold = 40 + (int)(Next(ref state) % 11u),
                            DeadlineDay = deadline,
                            Title = $"Visit {to.Name}",
                        };
                    }
                }
                if (force.HasValue) return null; // a forced template with no raw material is honest null
            }
            return null;
        }

        private static ulong Mix(ulong seed)
        {
            // splitmix64-style scramble so adjacent seeds do not produce adjacent picks.
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private static ulong Next(ref ulong state)
        {
            // xorshift64* — deterministic, allocation-free.
            state ^= state >> 12;
            state ^= state << 25;
            state ^= state >> 27;
            return state * 0x2545F4914F6CDD1DUL;
        }
    }
}
