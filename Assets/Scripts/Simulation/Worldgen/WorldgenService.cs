using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;

// Design note:
// WorldgenService is the FOUNDATION's deterministic seed-to-world function.
// Inputs: a uint seed and a WorldgenParameters knob set.
// Outputs: a GeneratedWorld bundle (regions, settlements, factions, faction
// relations, NPCs, 100-year history) shaped to the brief's Daggerfall-style
// targets — ~50 regions, ~200 settlements (1 capital + a few cities + dozens
// of towns + hundreds of villages), ~20 factions, ~750 NPCs, total
// population in [900K, 1.1M], 100-year history of macro events.
//
// Determinism contract: the same (seed, parameters) pair produces a
// byte-identical GeneratedWorld. The implementation uses ONE XorShiftRng
// drawn through a strict call order (regions → settlements → factions →
// relations → npcs → history) so the deterministic-replay test can pin
// the first NPC's name and the history-event count to a fixed expectation.
// No Unity, no I/O, no LINQ in the hot path (HashSet membership only).
//
// Population math: 1 Capital (~150K avg) + 8 Cities (~75K avg × 8 = 600K)
// + 40 Towns (~6K avg × 40 = 240K) + 151 Villages (~575 avg × 151 = ~87K)
// ≈ 1.077M expected total — comfortably inside [900K, 1.1M].
namespace EmberCrpg.Simulation.Worldgen
{
    /// <summary>Pure deterministic worldgen entry point.</summary>
    public static class WorldgenService
    {
        // Region name decorators. The brief specifies bag-of-syllables for
        // names; these suffixes turn raw syllable words into region-flavored
        // labels ("Brytharlm Vale", "Vhinaurd Marches") without dragging a
        // full word-list in.
        private static readonly string[] RegionSuffixes =
        {
            "Vale", "Reach", "Marches", "Wilds", "Holds", "Steppe", "Coast", "Lowlands",
        };

        // Settlement name decorators. Capitals/Cities get the bare forged
        // word; towns/villages get a small qualifier so saying "Vhinaurd"
        // (a city) does not look identical to "Vhinaurd" (a village).
        private static readonly string[] SettlementSuffixes =
        {
            "ford", "haven", "vale", "stead", "wick", "hollow", "bridge", "cross",
        };

        // Faction names get a short noun anchor so they read like factions
        // and not like settlements. House X / Order of X / Y Pact / etc.
        private static readonly string[] FactionPrefixes =
        {
            "House", "Order", "Circle", "League", "Pact", "Hand",
        };

        public static GeneratedWorld Generate(uint seed, WorldgenParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var rng = new XorShiftRng(seed);

            // -- 1. Regions ---------------------------------------------------
            var regions = GenerateRegions(rng, parameters);

            // -- 2. Settlements ----------------------------------------------
            var settlements = GenerateSettlements(rng, parameters, regions);

            // -- 3. Factions -------------------------------------------------
            var factions = GenerateFactions(rng, parameters);

            // -- 4. Faction relations ----------------------------------------
            var relations = GenerateFactionRelations(rng, factions);

            // -- 5. NPCs -----------------------------------------------------
            var npcs = GenerateNpcs(rng, parameters, settlements, factions);

            // -- 6. History --------------------------------------------------
            var history = GenerateHistory(rng, parameters, factions, settlements);

            return new GeneratedWorld(seed, regions, settlements, factions, relations, npcs, history);
        }

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

                // Biome cycles deterministically across the 8 buckets with a
                // small RNG jitter so a fresh seed does not always start in
                // TemperatePlain. NextInt drives the jitter, the cycling
                // anchor keeps the distribution roughly even.
                var biomeValues = BiomeValues();
                var biome = biomeValues[(i + rng.NextInt(biomeValues.Length)) % biomeValues.Length];

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

            return settlements;
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

        // ---------------- history ----------------

        private static List<WorldHistoryEvent> GenerateHistory(
            IDeterministicRng rng,
            WorldgenParameters parameters,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<SettlementRecord> settlements)
        {
            // Exactly historyYears events: one event per year so the
            // HistoryDeterministic test can pin Count == HistoryYears
            // without dragging in branch-count assertions.
            var history = new List<WorldHistoryEvent>(parameters.HistoryYears);
            int startYear = parameters.WorldStartYear - parameters.HistoryYears;

            for (int offset = 0; offset < parameters.HistoryYears; offset++)
            {
                int year = startYear + offset;
                int kindRoll = rng.NextInt(100);
                WorldHistoryKind kind;
                if (kindRoll < 20) kind = WorldHistoryKind.SettlementFounded;
                else if (kindRoll < 35) kind = WorldHistoryKind.FactionWar;
                else if (kindRoll < 45) kind = WorldHistoryKind.FactionAlliance;
                else if (kindRoll < 60) kind = WorldHistoryKind.NobleMarriage;
                else if (kindRoll < 72) kind = WorldHistoryKind.NobleDeath;
                else if (kindRoll < 80) kind = WorldHistoryKind.Calamity;
                else if (kindRoll < 92) kind = WorldHistoryKind.TradeRouteOpened;
                else kind = WorldHistoryKind.Migration;

                string subject;
                string detail;

                switch (kind)
                {
                    case WorldHistoryKind.SettlementFounded:
                    case WorldHistoryKind.Calamity:
                    case WorldHistoryKind.TradeRouteOpened:
                    case WorldHistoryKind.Migration:
                        var settlement = settlements[rng.NextInt(settlements.Count)];
                        subject = settlement.Name;
                        detail = HistoryDetail(kind, settlement.Name);
                        break;
                    case WorldHistoryKind.FactionWar:
                    case WorldHistoryKind.FactionAlliance:
                        var f1 = factions[rng.NextInt(factions.Count)];
                        var f2 = factions[rng.NextInt(factions.Count)];
                        if (f1.Id == f2.Id)
                            f2 = factions[(rng.NextInt(factions.Count - 1) + 1) % factions.Count];
                        subject = f1.Name;
                        detail = (kind == WorldHistoryKind.FactionWar)
                            ? f1.Name + " wars against " + f2.Name
                            : f1.Name + " allies with " + f2.Name;
                        break;
                    default:
                        var noble = factions[rng.NextInt(factions.Count)];
                        subject = noble.Name;
                        detail = HistoryDetail(kind, noble.Name);
                        break;
                }

                history.Add(new WorldHistoryEvent(year, kind, subject, detail));
            }

            return history;
        }

        private static string HistoryDetail(WorldHistoryKind kind, string subject)
        {
            switch (kind)
            {
                case WorldHistoryKind.SettlementFounded:
                    return subject + " is founded.";
                case WorldHistoryKind.Calamity:
                    return subject + " is struck by calamity.";
                case WorldHistoryKind.TradeRouteOpened:
                    return "A trade route opens through " + subject + ".";
                case WorldHistoryKind.Migration:
                    return "Settlers migrate to " + subject + ".";
                case WorldHistoryKind.NobleMarriage:
                    return "A noble of " + subject + " marries.";
                case WorldHistoryKind.NobleDeath:
                    return "A noble of " + subject + " dies.";
                default:
                    return subject + " is mentioned in the chronicles.";
            }
        }
    }
}
