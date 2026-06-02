// EMB-034: WorldgenService region/settlement/faction/npc generation phases (partial).
using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen
{
    public static partial class WorldgenService
    {
        // ---------------- regions ----------------

        private static List<RegionRecord> GenerateRegions(IDeterministicRng rng, WorldgenParameters parameters, WorldGeographyBuild geography)
        {
            if (geography == null) throw new ArgumentNullException(nameof(geography));

            var nameBag = new HashSet<string>(StringComparer.Ordinal);
            var regions = new List<RegionRecord>(parameters.RegionCount);

            for (int i = 0; i < parameters.RegionCount; i++)
            {
                var regionGeography = geography.Regions[i];
                var word = SyllableNameForge.ForgeUnique(rng, nameBag);
                var suffix = RegionSuffixes[rng.NextInt(RegionSuffixes.Length)];
                var name = word + " " + suffix;

                var biome = regionGeography.WorldBiome;

                // Per-region population bounds are coarse — the realized
                // total comes from summing settlements, not from sampling
                // this range. Bounds reflect "this region can hold roughly
                // X-Y people" and feed future migration / growth systems.
                int low = 5_000 + rng.NextInt(15_000);
                int high = low + 5_000 + rng.NextInt(50_000);

                regions.Add(new RegionRecord(new RegionId((ulong)(i + 1)), name, low, high, biome, regionGeography.CenterX, regionGeography.CenterY));
            }

            return regions;
        }

        private static BiomeKind[] BiomeValues()
        {
            return new[]
            {
                BiomeKind.TemperatePlain,
                BiomeKind.BorealForest,
                BiomeKind.CoastalMarsh,
                BiomeKind.AridSteppe,
                BiomeKind.MountainHighland,
                BiomeKind.DesertWaste,
                BiomeKind.TropicalJungle,
                BiomeKind.FrozenTundra,
            };
        }

        private static BiomeKind RollBiome(IDeterministicRng rng, WorldgenParameters parameters)
        {
            var values = BiomeValues();
            int total = 0;
            for (int i = 0; i < values.Length; i++)
                total += BiomeWeight(values[i], parameters.Style, parameters.Genre);

            int roll = rng.NextInt(total);
            int cursor = 0;
            for (int i = 0; i < values.Length; i++)
            {
                cursor += BiomeWeight(values[i], parameters.Style, parameters.Genre);
                if (roll < cursor) return values[i];
            }
            return values[0];
        }

        private static int BiomeWeight(BiomeKind biome, WorldStyle style, WorldGenre genre)
        {
            int weight = 10;
            switch (style)
            {
                case WorldStyle.HighFantasy:
                    if (biome == BiomeKind.BorealForest || biome == BiomeKind.TemperatePlain || biome == BiomeKind.MountainHighland) weight += 8;
                    break;
                case WorldStyle.DarkFantasyGrim:
                    if (biome == BiomeKind.FrozenTundra || biome == BiomeKind.CoastalMarsh || biome == BiomeKind.MountainHighland) weight += 9;
                    if (biome == BiomeKind.TropicalJungle) weight -= 5;
                    break;
                case WorldStyle.SteampunkRevolution:
                    if (biome == BiomeKind.TemperatePlain || biome == BiomeKind.CoastalMarsh) weight += 10;
                    if (biome == BiomeKind.FrozenTundra || biome == BiomeKind.TropicalJungle) weight -= 4;
                    break;
                case WorldStyle.AncientMythology:
                    if (biome == BiomeKind.DesertWaste || biome == BiomeKind.MountainHighland || biome == BiomeKind.AridSteppe) weight += 10;
                    break;
            }

            switch (genre)
            {
                case WorldGenre.Survival:
                    if (biome == BiomeKind.FrozenTundra || biome == BiomeKind.DesertWaste || biome == BiomeKind.AridSteppe) weight += 5;
                    break;
                case WorldGenre.MerchantEmpire:
                    if (biome == BiomeKind.CoastalMarsh || biome == BiomeKind.TemperatePlain) weight += 5;
                    break;
                case WorldGenre.Pilgrimage:
                    if (biome == BiomeKind.MountainHighland || biome == BiomeKind.DesertWaste) weight += 4;
                    break;
            }

            return weight < 1 ? 1 : weight;
        }

        // ---------------- settlements ----------------

        private static List<SettlementRecord> GenerateSettlements(
            IDeterministicRng rng,
            WorldgenParameters parameters,
            IReadOnlyList<RegionRecord> regions)
        {
            var nameBag = new HashSet<string>(StringComparer.Ordinal);
            var settlements = new List<SettlementRecord>(parameters.SettlementCount);
            ulong nextId = 1UL;

            // Settlements are emitted in deterministic size order
            // (capitals first, then cities, towns, villages) so a single
            // RNG sequence yields the same bytes for the same seed.
            for (int i = 0; i < parameters.CapitalCount; i++)
            {
                settlements.Add(MakeSettlement(rng, ref nextId, regions, nameBag, SettlementSize.Capital, 120_000, 180_000));
            }
            for (int i = 0; i < parameters.CityCount; i++)
            {
                settlements.Add(MakeSettlement(rng, ref nextId, regions, nameBag, SettlementSize.City, 50_000, 100_000));
            }
            for (int i = 0; i < parameters.TownCount; i++)
            {
                settlements.Add(MakeSettlement(rng, ref nextId, regions, nameBag, SettlementSize.Town, 2_000, 10_000));
            }
            for (int i = 0; i < parameters.VillageCount; i++)
            {
                // Hamlet for the smaller half of the village band so the
                // SettlementSize enum gets all five buckets populated.
                bool isHamlet = rng.NextInt(4) == 0;
                if (isHamlet)
                {
                    settlements.Add(MakeSettlement(rng, ref nextId, regions, nameBag, SettlementSize.Hamlet, 100, 300));
                }
                else
                {
                    settlements.Add(MakeSettlement(rng, ref nextId, regions, nameBag, SettlementSize.Village, 300, 1_000));
                }
            }

            return NormalizePopulation(settlements, parameters.TargetPopulation);
        }

        private static List<SettlementRecord> NormalizePopulation(List<SettlementRecord> settlements, int targetPopulation)
        {
            int total = 0;
            for (int i = 0; i < settlements.Count; i++)
                total += settlements[i].Population;
            if (total == targetPopulation) return settlements;

            var normalized = new List<SettlementRecord>(settlements.Count);
            int scaledTotal = 0;
            for (int i = 0; i < settlements.Count; i++)
            {
                var s = settlements[i];
                int population = Math.Max(1, (int)(((long)s.Population * targetPopulation) / total));
                scaledTotal += population;
                normalized.Add(new SettlementRecord(s.Id, s.Region, s.Name, population, s.Size));
            }

            int delta = targetPopulation - scaledTotal;
            int step = delta >= 0 ? 1 : -1;
            int remaining = Math.Abs(delta);
            int cursor = 0;
            while (remaining > 0)
            {
                int index = cursor % normalized.Count;
                var s = normalized[index];
                int population = s.Population + step;
                if (population > 0)
                {
                    normalized[index] = new SettlementRecord(s.Id, s.Region, s.Name, population, s.Size);
                    remaining--;
                }
                cursor++;
            }

            return normalized;
        }

        private static SettlementRecord MakeSettlement(
            IDeterministicRng rng,
            ref ulong nextId,
            IReadOnlyList<RegionRecord> regions,
            HashSet<string> nameBag,
            SettlementSize size,
            int popLow,
            int popHighExclusive)
        {
            var word = SyllableNameForge.ForgeUnique(rng, nameBag);
            string name;
            if (size == SettlementSize.Capital || size == SettlementSize.City)
            {
                name = word;
            }
            else
            {
                var suffix = SettlementSuffixes[rng.NextInt(SettlementSuffixes.Length)];
                name = word + suffix;
                if (!nameBag.Add(name))
                {
                    // Suffixed name collided — fall back to the bare unique
                    // word so the FOUNDATION's distinct-name invariant holds.
                    name = word;
                }
            }

            int range = popHighExclusive - popLow;
            int population = popLow + rng.NextInt(range);

            // Region assignment is round-robin with RNG jitter so the
            // capitals do not all bunch in the first few regions.
            int regionIndex = rng.NextInt(regions.Count);
            var region = regions[regionIndex];

            var id = new SettlementId(nextId++);
            return new SettlementRecord(id, region.Id, name, population, size);
        }

        // ---------------- factions ----------------

        private static List<FactionRecord> GenerateFactions(IDeterministicRng rng, WorldgenParameters parameters)
        {
            var nameBag = new HashSet<string>(StringComparer.Ordinal);
            var factions = new List<FactionRecord>(parameters.FactionCount);

            for (int i = 0; i < parameters.FactionCount; i++)
            {
                var prefix = FactionPrefixes[rng.NextInt(FactionPrefixes.Length)];
                var word = SyllableNameForge.ForgeUnique(rng, nameBag);
                var name = prefix + " " + word;

                // Tags are empty for the FOUNDATION pass — tags get richer
                // in the Phase-N follow-up that wires factions into the
                // economy / military / religion subsystems.
                factions.Add(new FactionRecord(new FactionId((ulong)(i + 1)), name, Array.Empty<string>()));
            }

            return factions;
        }

        // ---------------- faction relations ----------------

        private static List<FactionRelationSeed> GenerateFactionRelations(IDeterministicRng rng, IReadOnlyList<FactionRecord> factions)
        {
            // All distinct pairs (i, j) with i<j. For 20 factions that is
            // 20*19/2 = 190 entries — small enough to materialize fully.
            var relations = new List<FactionRelationSeed>((factions.Count * (factions.Count - 1)) / 2);
            for (int i = 0; i < factions.Count; i++)
            {
                for (int j = i + 1; j < factions.Count; j++)
                {
                    // Reputations cluster around neutral with occasional
                    // strong allies / enemies. Roll a normal-ish value by
                    // averaging three uniform draws — central-limit gives
                    // a soft bell curve without a Math.Sqrt path.
                    int a = rng.NextInt(201) - 100;
                    int b = rng.NextInt(201) - 100;
                    int c = rng.NextInt(201) - 100;
                    int rep = (a + b + c) / 3;
                    relations.Add(new FactionRelationSeed(factions[i].Id, factions[j].Id, new FactionReputation(rep)));
                }
            }
            return relations;
        }

        // ---------------- NPCs ----------------

        private static List<NpcSeedRecord> GenerateNpcs(
            IDeterministicRng rng,
            WorldgenParameters parameters,
            IReadOnlyList<SettlementRecord> settlements,
            IReadOnlyList<FactionRecord> factions)
        {
            if (settlements.Count == 0)
                return new List<NpcSeedRecord>(0);

            var assignment = AllocateNpcCounts(parameters, settlements);
            int targetNpcCount = 0;
            for (int i = 0; i < assignment.Length; i++)
                targetNpcCount += assignment[i];

            var npcs = new List<NpcSeedRecord>(targetNpcCount);
            ulong nextId = 1UL;

            // Forge NPCs settlement-by-settlement with a
            // per-settlement name bag so no two NPCs in the same village
            // share a name.
            for (int s = 0; s < settlements.Count; s++)
            {
                if (assignment[s] <= 0) continue;
                var localNames = new HashSet<string>(StringComparer.Ordinal);
                var settlement = settlements[s];

                for (int n = 0; n < assignment[s]; n++)
                {
                    var given = SyllableNameForge.ForgeUnique(rng, localNames, syllableCount: 2);
                    var family = SyllableNameForge.Forge(rng, syllableCount: 2);
                    var fullName = given + " " + family;
                    // The local name-bag tracks given names so two NPCs in
                    // the same village do not share a first name. The full
                    // "given family" string carries the family suffix only
                    // for display; collisions on full name are vanishingly
                    // rare and not asserted by the brief.

                    var faction = factions[rng.NextInt(factions.Count)];
                    var role = RollNpcRole(rng, settlement.Size, faction.Id);
                    int birthYear = parameters.WorldStartYear - 18 - rng.NextInt(48);

                    npcs.Add(new NpcSeedRecord(
                        new NpcId(nextId++),
                        settlement.Id,
                        faction.Id,
                        fullName,
                        birthYear,
                        role));
                }
            }

            return npcs;
        }

        private static int[] AllocateNpcCounts(WorldgenParameters parameters, IReadOnlyList<SettlementRecord> settlements)
        {
            var assigned = new int[settlements.Count];
            var caps = new int[settlements.Count];
            var weights = new long[settlements.Count];
            int totalCap = 0;

            for (int i = 0; i < settlements.Count; i++)
            {
                caps[i] = NpcCapacity(settlements[i]);
                weights[i] = NpcWeight(settlements[i]);
                totalCap += caps[i];
            }

            int target = parameters.NpcCount < totalCap ? parameters.NpcCount : totalCap;
            int remaining = target;

            if (target >= settlements.Count)
            {
                for (int i = 0; i < settlements.Count; i++)
                {
                    assigned[i] = 1;
                    remaining--;
                }
            }

            while (remaining > 0)
            {
                int index = ChooseNpcAssignmentIndex(settlements, weights, caps, assigned);
                if (index < 0)
                    break;

                assigned[index]++;
                remaining--;
            }

            return assigned;
        }

        private static int ChooseNpcAssignmentIndex(
            IReadOnlyList<SettlementRecord> settlements,
            long[] weights,
            int[] caps,
            int[] assigned)
        {
            int best = -1;
            long bestScore = long.MinValue;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (assigned[i] >= caps[i])
                    continue;

                long score = weights[i] / (assigned[i] + 1L);
                if (score > bestScore)
                {
                    best = i;
                    bestScore = score;
                    continue;
                }

                if (score == bestScore && best >= 0)
                {
                    if (settlements[i].Population > settlements[best].Population)
                        best = i;
                    else if (settlements[i].Population == settlements[best].Population
                        && settlements[i].Id.Value < settlements[best].Id.Value)
                        best = i;
                }
            }

            return best;
        }

        private static long NpcWeight(SettlementRecord settlement)
        {
            return Math.Max(1, settlement.Population) + (NpcTierWeight(settlement.Size) * 1000L);
        }

        private static int NpcTierWeight(SettlementSize size)
        {
            switch (size)
            {
                case SettlementSize.Capital: return 120;
                case SettlementSize.City: return 70;
                case SettlementSize.Town: return 18;
                case SettlementSize.Village: return 5;
                default: return 2;
            }
        }

        private static int NpcCapacity(SettlementRecord settlement)
        {
            int byPopulation = Math.Max(1, settlement.Population / 900);
            int baseline;
            int cap;
            switch (settlement.Size)
            {
                case SettlementSize.Capital:
                    baseline = 24;
                    cap = 90;
                    break;
                case SettlementSize.City:
                    baseline = 16;
                    cap = 64;
                    break;
                case SettlementSize.Town:
                    baseline = 6;
                    cap = 24;
                    break;
                case SettlementSize.Village:
                    baseline = 2;
                    cap = 8;
                    break;
                default:
                    baseline = 1;
                    cap = 4;
                    break;
            }

            int capacity = baseline + byPopulation;
            if (capacity < 1)
                capacity = 1;
            return capacity > cap ? cap : capacity;
        }

        private static NpcRole RollNpcRole(IDeterministicRng rng, SettlementSize size, FactionId faction)
        {
            // Role distribution tracks settlement size and a tiny faction
            // bias. Villages are farmer-heavy; cities/capitals get courts,
            // guilds, mages, clergy, artists, and visible poverty.
            int roll = rng.NextInt(100);
            int factionBias = (int)(faction.Value % 5UL);
            switch (size)
            {
                case SettlementSize.Capital:
                case SettlementSize.City:
                    if (roll < 5) return NpcRole.Noble;
                    if (roll < 10) return factionBias == 0 ? NpcRole.Knight : NpcRole.Guard;
                    if (roll < 25) return NpcRole.Merchant;
                    if (roll < 34) return NpcRole.Blacksmith;
                    if (roll < 42) return NpcRole.Innkeeper;
                    if (roll < 50) return NpcRole.Priest;
                    if (roll < 57) return factionBias == 1 ? NpcRole.Mage : NpcRole.Healer;
                    if (roll < 65) return NpcRole.Sage;
                    if (roll < 72) return NpcRole.Bard;
                    if (roll < 80) return NpcRole.Rogue;
                    if (roll < 88) return NpcRole.Beggar;
                    if (roll < 96) return NpcRole.Farmer;
                    return factionBias == 2 ? NpcRole.Bandit : NpcRole.Outlaw;
                case SettlementSize.Town:
                    if (roll < 3) return NpcRole.Noble;
                    if (roll < 10) return NpcRole.Guard;
                    if (roll < 22) return NpcRole.Merchant;
                    if (roll < 32) return NpcRole.Blacksmith;
                    if (roll < 42) return NpcRole.Innkeeper;
                    if (roll < 49) return NpcRole.Priest;
                    if (roll < 56) return NpcRole.Healer;
                    if (roll < 61) return factionBias == 1 ? NpcRole.Mage : NpcRole.Sage;
                    if (roll < 67) return NpcRole.Bard;
                    if (roll < 73) return NpcRole.Rogue;
                    if (roll < 82) return NpcRole.Beggar;
                    if (roll < 96) return NpcRole.Farmer;
                    return factionBias == 2 ? NpcRole.Bandit : NpcRole.Outlaw;
                default:
                    // Village / Hamlet
                    if (roll < 3) return NpcRole.Guard;
                    if (roll < 8) return NpcRole.Merchant;
                    if (roll < 14) return NpcRole.Blacksmith;
                    if (roll < 20) return NpcRole.Innkeeper;
                    if (roll < 25) return NpcRole.Healer;
                    if (roll < 29) return NpcRole.Priest;
                    if (roll < 32) return NpcRole.Bard;
                    if (roll < 36) return NpcRole.Beggar;
                    if (roll < 94) return NpcRole.Farmer;
                    if (roll < 98) return NpcRole.Rogue;
                    return factionBias == 2 ? NpcRole.Bandit : NpcRole.Outlaw;
            }
        }

    }
}
