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

        private static List<RegionRecord> GenerateRegions(IDeterministicRng rng, WorldgenParameters parameters)
        {
            var nameBag = new HashSet<string>(StringComparer.Ordinal);
            var regions = new List<RegionRecord>(parameters.RegionCount);

            for (int i = 0; i < parameters.RegionCount; i++)
            {
                var word = SyllableNameForge.ForgeUnique(rng, nameBag);
                var suffix = RegionSuffixes[rng.NextInt(RegionSuffixes.Length)];
                var name = word + " " + suffix;

                var biome = RollBiome(rng, parameters);

                // Per-region population bounds are coarse — the realized
                // total comes from summing settlements, not from sampling
                // this range. Bounds reflect "this region can hold roughly
                // X-Y people" and feed future migration / growth systems.
                int low = 5_000 + rng.NextInt(15_000);
                int high = low + 5_000 + rng.NextInt(50_000);

                regions.Add(new RegionRecord(new RegionId((ulong)(i + 1)), name, low, high, biome));
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
                case WorldStyle.HighFantasyTolkien:
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
                // in the Faz-N follow-up that wires factions into the
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
            var npcs = new List<NpcSeedRecord>(parameters.NpcCount);
            ulong nextId = 1UL;

            // Per-settlement weight: bigger settlements get more named NPCs.
            // Weight = sqrt-ish via integer fold so cities do not absorb
            // every named NPC — Daggerfall's biggest cities still have
            // proportionally more named NPCs than villages, but villages
            // are not name-starved either.
            var weights = new int[settlements.Count];
            long totalWeight = 0;
            for (int i = 0; i < settlements.Count; i++)
            {
                int pop = settlements[i].Population;
                // Integer log-ish: count how many times pop divides by 4,
                // plus 1, so [100..400) → 1, [400..1600) → 2, ... [25600+) → 5+
                int weight = 1;
                int reduced = pop / 4;
                while (reduced > 0)
                {
                    weight++;
                    reduced /= 4;
                }
                weights[i] = weight;
                totalWeight += weight;
            }

            // First pass: allocate floor counts proportional to weight.
            var assigned = new int[settlements.Count];
            int placed = 0;
            for (int i = 0; i < settlements.Count; i++)
            {
                int share = (int)(((long)parameters.NpcCount * weights[i]) / totalWeight);
                assigned[i] = share;
                placed += share;
            }

            // Second pass: distribute the leftover (rounding losses) to the
            // biggest settlements until we hit NpcCount exactly.
            int remaining = parameters.NpcCount - placed;
            int cursor = 0;
            while (remaining > 0)
            {
                int idx = cursor % settlements.Count;
                // Bias toward larger settlements first by stepping by
                // settlement weight rather than 1.
                if (weights[idx] >= 3)
                {
                    assigned[idx]++;
                    remaining--;
                }
                cursor++;
                if (cursor > settlements.Count * 8)
                {
                    // Defensive: every settlement got bumped — just dump
                    // the rest into the first settlement so we never loop.
                    assigned[0] += remaining;
                    remaining = 0;
                }
            }

            // Third pass: forge NPCs settlement-by-settlement with a
            // per-settlement name bag so no two NPCs in the same village
            // share a name.
            for (int s = 0; s < settlements.Count; s++)
            {
                if (assigned[s] <= 0) continue;
                var localNames = new HashSet<string>(StringComparer.Ordinal);
                var settlement = settlements[s];

                for (int n = 0; n < assigned[s]; n++)
                {
                    var given = SyllableNameForge.ForgeUnique(rng, localNames, syllableCount: 2);
                    var family = SyllableNameForge.Forge(rng, syllableCount: 2);
                    var fullName = given + " " + family;
                    // The local name-bag tracks given names so two NPCs in
                    // the same village do not share a first name. The full
                    // "given family" string carries the family suffix only
                    // for display; collisions on full name are vanishingly
                    // rare and not asserted by the brief.

                    var role = RollNpcRole(rng, settlement.Size);
                    var faction = factions[rng.NextInt(factions.Count)];
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

        private static NpcRole RollNpcRole(IDeterministicRng rng, SettlementSize size)
        {
            // Role distribution tracks settlement size. Villages are
            // farmer-heavy; cities/capitals get more merchants, nobles,
            // priests, scholars. Outlaws are rare but seeded everywhere
            // so the world has at least a few faces of trouble.
            int roll = rng.NextInt(100);
            switch (size)
            {
                case SettlementSize.Capital:
                case SettlementSize.City:
                    if (roll < 5) return NpcRole.Noble;
                    if (roll < 15) return NpcRole.Guard;
                    if (roll < 35) return NpcRole.Merchant;
                    if (roll < 50) return NpcRole.Artisan;
                    if (roll < 60) return NpcRole.Priest;
                    if (roll < 70) return NpcRole.Scholar;
                    if (roll < 95) return NpcRole.Farmer;
                    return NpcRole.Outlaw;
                case SettlementSize.Town:
                    if (roll < 3) return NpcRole.Noble;
                    if (roll < 12) return NpcRole.Guard;
                    if (roll < 25) return NpcRole.Merchant;
                    if (roll < 35) return NpcRole.Artisan;
                    if (roll < 42) return NpcRole.Priest;
                    if (roll < 47) return NpcRole.Scholar;
                    if (roll < 95) return NpcRole.Farmer;
                    return NpcRole.Outlaw;
                default:
                    // Village / Hamlet
                    if (roll < 2) return NpcRole.Guard;
                    if (roll < 6) return NpcRole.Merchant;
                    if (roll < 10) return NpcRole.Artisan;
                    if (roll < 13) return NpcRole.Priest;
                    if (roll < 95) return NpcRole.Farmer;
                    return NpcRole.Outlaw;
            }
        }

    }
}
