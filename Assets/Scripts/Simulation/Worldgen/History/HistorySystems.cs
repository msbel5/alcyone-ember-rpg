using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.History
{
    public sealed class LifeEmergenceSystem : IHistorySystem
    {
        public int SystemKey { get { return HistorySystemKeys.LifeEmergence; } }
        public string Name { get { return "LifeEmergence"; } }

        public void Tick(HistoryState state, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            if (state.LifeEmerged)
                return;

            int cradle = 0;
            for (int i = 1; i < state.Regions.Length; i++)
            {
                if (state.Regions[i].Suitability > state.Regions[cradle].Suitability)
                {
                    cradle = i;
                }
                else if (state.Regions[i].Suitability == state.Regions[cradle].Suitability
                    && state.Regions[i].Record.Id.Value < state.Regions[cradle].Record.Id.Value)
                {
                    cradle = i;
                }
            }

            state.LifeEmerged = true;
            state.LifeCradleRegionIndex = cradle;

            var cradleRegion = state.Regions[cradle];
            cradleRegion.Population = Math.Max(cradleRegion.Population, cradleRegion.CarryingCapacity * 0.045 + rng.NextInt(800));

            var neighbors = state.NeighborsOf(cradle);
            for (int i = 0; i < neighbors.Count; i++)
            {
                var neighbor = state.Regions[neighbors[i]];
                double seedPopulation = neighbor.CarryingCapacity * 0.010;
                if (neighbor.Population < seedPopulation)
                    neighbor.Population = seedPopulation;
            }

            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.LifeEmerged,
                cradleRegion.Record.Name,
                "Life first takes root in " + cradleRegion.Record.Name + ", where " + cradleRegion.Record.Biome + " becomes the cradle of the world.",
                cradleRegion.Record.Id));
        }
    }

    public sealed class PopulationGrowthSystem : IHistorySystem
    {
        private readonly HistorySimulationTuning _tuning;

        public PopulationGrowthSystem(HistorySimulationTuning tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public int SystemKey { get { return HistorySystemKeys.PopulationGrowth; } }
        public string Name { get { return "PopulationGrowth"; } }

        public void Tick(HistoryState state, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (sink == null) throw new ArgumentNullException(nameof(sink));

            for (int i = 0; i < state.Regions.Length; i++)
            {
                var region = state.Regions[i];
                if (region.Population <= 0.5)
                    continue;

                double capacity = region.CarryingCapacity;
                double adjustedRate = _tuning.GrowthRate * (0.90 + (region.Suitability * 0.20));
                double next = region.Population + (adjustedRate * region.Population * (1.0 - (region.Population / capacity)));
                if (next < 0.0)
                    next = Math.Max(1.0, capacity * 0.35);

                region.Population = next;
                EmitPopulationMilestone(region, year, sink);
                ApplyShock(state, region, year, sink);
            }
        }

        private void EmitPopulationMilestone(HistoryRegionState region, int year, IHistoryEventSink sink)
        {
            double ratio = region.Population / region.CarryingCapacity;
            int milestone = 0;
            if (ratio >= 0.20) milestone = 1;
            if (ratio >= 0.45) milestone = 2;
            if (ratio >= 0.70) milestone = 3;
            if (ratio >= 0.90) milestone = 4;

            if (milestone <= region.PopulationMilestone)
                return;

            region.PopulationMilestone = milestone;
            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.PopulationBoom,
                region.Record.Name,
                region.Record.Name + " enters population boom stage " + milestone.ToString() + " as food, roads, and settlement pressure compound.",
                region.Record.Id));
        }

        private void ApplyShock(HistoryState state, HistoryRegionState region, int year, IHistoryEventSink sink)
        {
            var shockRng = HistoryRandom.Create(state.WorldSeed, SystemKey, region.Index + 1, year);
            if (shockRng.NextInt(1000) >= _tuning.ShockChancePerThousand)
                return;

            double temporaryCapacity = region.CarryingCapacity * (0.58 + (shockRng.NextInt(28) / 100.0));
            if (region.Population <= temporaryCapacity)
                return;

            int lost = Math.Max(1, (int)(region.Population - (temporaryCapacity * 0.94)));
            region.Population = Math.Max(1.0, region.Population - lost);

            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.Famine,
                region.Record.Name,
                "A famine in " + region.Record.Name + " kills " + lost.ToString() + " people after population outruns the shocked carrying capacity.",
                region.Record.Id));

            if (lost > 400 || shockRng.NextInt(100) < 35)
            {
                sink.Emit(new WorldHistoryEvent(
                    year,
                    WorldHistoryKind.Calamity,
                    region.Record.Name,
                    region.Record.Name + " records the famine as a generational calamity.",
                    region.Record.Id));
            }
        }
    }

    public sealed class MigrationSystem : IHistorySystem
    {
        private readonly HistorySimulationTuning _tuning;

        public MigrationSystem(HistorySimulationTuning tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public int SystemKey { get { return HistorySystemKeys.Migration; } }
        public string Name { get { return "Migration"; } }

        public void Tick(HistoryState state, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (sink == null) throw new ArgumentNullException(nameof(sink));

            var deltas = new double[state.Regions.Length];
            for (int i = 0; i < state.Regions.Length; i++)
            {
                var neighbors = state.NeighborsOf(i);
                for (int n = 0; n < neighbors.Count; n++)
                {
                    int j = neighbors[n];
                    if (j <= i)
                        continue;

                    double flowIJ = CalculateFlow(state, i, j, year);
                    double flowJI = CalculateFlow(state, j, i, year);
                    double net = flowIJ - flowJI;
                    if (Math.Abs(net) < 1.0)
                        continue;

                    if (net > 0.0)
                    {
                        deltas[i] -= net;
                        deltas[j] += net;
                        EmitMigrationWaveIfLarge(state, i, j, net, year, sink);
                    }
                    else
                    {
                        double reverse = -net;
                        deltas[j] -= reverse;
                        deltas[i] += reverse;
                        EmitMigrationWaveIfLarge(state, j, i, reverse, year, sink);
                    }
                }
            }

            for (int i = 0; i < state.Regions.Length; i++)
                state.Regions[i].Population = Math.Max(0.0, state.Regions[i].Population + deltas[i]);
        }

        private double CalculateFlow(HistoryState state, int fromIndex, int toIndex, int year)
        {
            var from = state.Regions[fromIndex];
            var to = state.Regions[toIndex];
            if (from.Population < 50.0)
                return 0.0;

            double fromPressure = from.Population / from.CarryingCapacity;
            double toPressure = to.Population / to.CarryingCapacity;
            double attractiveness = (to.Suitability - from.Suitability) + (fromPressure - toPressure);
            if (attractiveness <= 0.0)
                return 0.0;

            var localRng = HistoryRandom.Create(state.WorldSeed, SystemKey, (fromIndex + 1) * 257 + toIndex + 1, year);
            double weatherNoise = 0.92 + (localRng.NextInt(17) / 100.0);
            double cost = state.RegionStepCost(fromIndex, toIndex);
            double roadBias = to.RoadLevel > 0 ? 1.12 : 1.0;
            return from.Population * _tuning.MigrationRate * attractiveness * to.Suitability * roadBias * weatherNoise / (1.0 + (cost * 0.18));
        }

        private void EmitMigrationWaveIfLarge(HistoryState state, int fromIndex, int toIndex, double flow, int year, IHistoryEventSink sink)
        {
            if (flow < 180.0 || state.WasRecentMigrationWave(fromIndex, toIndex, year, 9))
                return;

            state.MarkMigrationWave(fromIndex, toIndex, year);
            var from = state.Regions[fromIndex];
            var to = state.Regions[toIndex];
            int people = Math.Max(1, (int)flow);

            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.MigrationWave,
                to.Record.Name,
                people.ToString() + " people migrate from " + from.Record.Name + " toward " + to.Record.Name + " along the lowest-cost frontier.",
                to.Record.Id,
                from.Record.Id));
        }
    }

    public sealed class SettlementFoundingSystem : IHistorySystem
    {
        private readonly HistorySimulationTuning _tuning;

        public SettlementFoundingSystem(HistorySimulationTuning tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public int SystemKey { get { return HistorySystemKeys.SettlementFounding; } }
        public string Name { get { return "SettlementFounding"; } }

        public void Tick(HistoryState state, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (sink == null) throw new ArgumentNullException(nameof(sink));

            for (int i = 0; i < state.Settlements.Length; i++)
            {
                var settlement = state.Settlements[i];
                var region = state.Regions[settlement.RegionIndex];

                if (!settlement.Founded)
                {
                    TryFoundSettlement(state, settlement, region, year, sink);
                    continue;
                }

                TryGrowSettlement(state, settlement, region, year, sink);
                TryAbandonSettlement(settlement, region, year, sink);
            }
        }

        private void TryFoundSettlement(HistoryState state, HistoricalSettlementState settlement, HistoryRegionState region, int year, IHistoryEventSink sink)
        {
            double threshold = FoundingThreshold(settlement.Record.Size);
            threshold += state.FoundedSettlementsInRegion(settlement.RegionIndex) * 900.0;
            threshold *= 1.12 - (region.Suitability * 0.24);
            threshold /= FoundingBonus(region.Record.Biome);

            bool isLocalPressure = state.IsLocalSuitabilityMaximum(settlement.RegionIndex)
                || region.Population >= threshold * 1.25
                || settlement.Record.Size == SettlementSize.Hamlet
                || settlement.Record.Size == SettlementSize.Village;

            if (region.Population < threshold
                || !isLocalPressure
                || !state.HasMinimumSpacing(settlement.RegionIndex, settlement.Record.Size, region.Population, threshold))
                return;

            settlement.Founded = true;
            settlement.FoundedYear = year;
            settlement.CurrentTier = SettlementSize.Hamlet;

            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.SettlementFounded,
                settlement.Record.Name,
                settlement.Record.Name + " is founded in " + region.Record.Name + " after regional population crosses " + ((int)threshold).ToString() + ".",
                region.Record.Id,
                secondaryRegion: null,
                primarySettlement: settlement.Record.Id,
                secondarySettlement: null,
                primaryFaction: state.Factions[settlement.FactionIndex].Record.Id));

            var faction = state.Factions[settlement.FactionIndex];
            if (!faction.CivilizationFounded)
            {
                faction.CivilizationFounded = true;
                sink.Emit(new WorldHistoryEvent(
                    year,
                    WorldHistoryKind.CivilizationFounded,
                    faction.Record.Name,
                    faction.Record.Name + " rises around " + settlement.Record.Name + " and claims its first durable territory.",
                    region.Record.Id,
                    secondaryRegion: null,
                    primarySettlement: settlement.Record.Id,
                    secondarySettlement: null,
                    primaryFaction: faction.Record.Id));
            }
        }

        private void TryGrowSettlement(HistoryState state, HistoricalSettlementState settlement, HistoryRegionState region, int year, IHistoryEventSink sink)
        {
            SettlementSize targetTier = EligibleTier(settlement.Record.Size, region.Population);
            if ((int)targetTier <= (int)settlement.CurrentTier)
                return;

            SettlementSize next = NextTier(settlement.CurrentTier);
            if ((int)next > (int)settlement.Record.Size)
                next = settlement.Record.Size;
            if ((int)next > (int)targetTier)
                next = targetTier;

            if ((int)next <= (int)settlement.CurrentTier)
                return;

            settlement.CurrentTier = next;
            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.SiteGrew,
                settlement.Record.Name,
                settlement.Record.Name + " grows into a " + next.ToString() + " as " + region.Record.Name + " fills with farms, workshops, and roads.",
                region.Record.Id,
                secondaryRegion: null,
                primarySettlement: settlement.Record.Id,
                secondarySettlement: null,
                primaryFaction: state.Factions[settlement.FactionIndex].Record.Id));
        }

        private static void TryAbandonSettlement(HistoricalSettlementState settlement, HistoryRegionState region, int year, IHistoryEventSink sink)
        {
            if (!settlement.Founded || (int)settlement.CurrentTier > (int)SettlementSize.Village)
                return;

            double floor = settlement.CurrentTier == SettlementSize.Hamlet ? 180.0 : 420.0;
            if (region.Population >= floor)
                return;

            settlement.Founded = false;
            settlement.CurrentTier = SettlementSize.None;
            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.SiteAbandoned,
                settlement.Record.Name,
                settlement.Record.Name + " is abandoned when " + region.Record.Name + " can no longer feed its smallest sites.",
                region.Record.Id,
                secondaryRegion: null,
                primarySettlement: settlement.Record.Id));
        }

        private static double FoundingThreshold(SettlementSize size)
        {
            switch (size)
            {
                case SettlementSize.Capital: return 52_000.0;
                case SettlementSize.City: return 34_000.0;
                case SettlementSize.Town: return 8_000.0;
                case SettlementSize.Village: return 1_900.0;
                case SettlementSize.Hamlet: return 650.0;
                default: return 1_000.0;
            }
        }

        private static double FoundingBonus(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.TemperatePlain: return 1.22;
                case BiomeKind.CoastalMarsh: return 1.16;
                case BiomeKind.BorealForest: return 1.04;
                case BiomeKind.AridSteppe: return 0.94;
                case BiomeKind.TropicalJungle: return 0.90;
                case BiomeKind.MountainHighland: return 0.82;
                case BiomeKind.DesertWaste: return 0.70;
                case BiomeKind.FrozenTundra: return 0.68;
                default: return 1.0;
            }
        }

        private static SettlementSize EligibleTier(SettlementSize finalSize, double regionPopulation)
        {
            SettlementSize target = SettlementSize.Hamlet;
            if (regionPopulation >= 2_200.0) target = SettlementSize.Village;
            if (regionPopulation >= 10_000.0) target = SettlementSize.Town;
            if (regionPopulation >= 38_000.0) target = SettlementSize.City;
            if (regionPopulation >= 68_000.0) target = SettlementSize.Capital;
            return (int)target > (int)finalSize ? finalSize : target;
        }

        private static SettlementSize NextTier(SettlementSize current)
        {
            switch (current)
            {
                case SettlementSize.None: return SettlementSize.Hamlet;
                case SettlementSize.Hamlet: return SettlementSize.Village;
                case SettlementSize.Village: return SettlementSize.Town;
                case SettlementSize.Town: return SettlementSize.City;
                case SettlementSize.City: return SettlementSize.Capital;
                default: return current;
            }
        }
    }

    public sealed class RoadNetworkSystem : IHistorySystem
    {
        private readonly HistorySimulationTuning _tuning;

        public RoadNetworkSystem(HistorySimulationTuning tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public int SystemKey { get { return HistorySystemKeys.RoadNetwork; } }
        public string Name { get { return "RoadNetwork"; } }

        public void Tick(HistoryState state, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            if (state.FoundedSettlementCount < 2)
                return;
            if (state.YearOffset(year) % _tuning.RoadCadenceYears != 0)
                return;

            int budget = Math.Max(1, state.FoundedSettlementCount / 24);
            int built = 0;

            for (int i = 0; i < state.Settlements.Length && built < budget; i++)
            {
                var source = state.Settlements[i];
                if (!source.Founded || (int)source.CurrentTier < (int)SettlementSize.Village)
                    continue;

                int targetIndex = FindRoadTarget(state, source, year);
                if (targetIndex < 0)
                {
                    EmitIsolationIfNeeded(state, source, year, sink);
                    continue;
                }

                var target = state.Settlements[targetIndex];
                var path = state.FindLeastCostPath(source.RegionIndex, target.RegionIndex, 170.0);
                if (!path.Found)
                {
                    EmitIsolationIfNeeded(state, source, year, sink);
                    continue;
                }

                state.ReinforceRoadPath(path.Regions);
                state.AddRoadConnection(source.Index, target.Index);
                built++;

                sink.Emit(new WorldHistoryEvent(
                    year,
                    WorldHistoryKind.RoadBuilt,
                    source.Record.Name,
                    "Road crews connect " + source.Record.Name + " to " + target.Record.Name + " across " + path.Regions.Length.ToString() + " regions at cost " + ((int)path.Cost).ToString() + ".",
                    state.Regions[source.RegionIndex].Record.Id,
                    state.Regions[target.RegionIndex].Record.Id,
                    source.Record.Id,
                    target.Record.Id,
                    state.Factions[source.FactionIndex].Record.Id,
                    state.Factions[target.FactionIndex].Record.Id));

                if (source.FactionIndex != target.FactionIndex || (int)source.CurrentTier >= (int)SettlementSize.Town || (int)target.CurrentTier >= (int)SettlementSize.Town)
                {
                    sink.Emit(new WorldHistoryEvent(
                        year,
                        WorldHistoryKind.TradeRouteOpened,
                        target.Record.Name,
                        "A trade route opens between " + source.Record.Name + " and " + target.Record.Name + ".",
                        state.Regions[source.RegionIndex].Record.Id,
                        state.Regions[target.RegionIndex].Record.Id,
                        source.Record.Id,
                        target.Record.Id,
                        state.Factions[source.FactionIndex].Record.Id,
                        state.Factions[target.FactionIndex].Record.Id));
                }
            }
        }

        private int FindRoadTarget(HistoryState state, HistoricalSettlementState source, int year)
        {
            int best = -1;
            int bestScore = int.MaxValue;
            for (int i = 0; i < state.Settlements.Length; i++)
            {
                var target = state.Settlements[i];
                if (target.Index == source.Index
                    || !target.Founded
                    || state.HasRoadConnection(source.Index, target.Index)
                    || target.RegionIndex == source.RegionIndex)
                {
                    continue;
                }

                double relation = state.Relations[source.FactionIndex, target.FactionIndex];
                if (source.FactionIndex != target.FactionIndex && relation < -0.55)
                    continue;

                int distance = state.ManhattanDistance(source.RegionIndex, target.RegionIndex);
                int tierBonus = TierRoadBonus(source.CurrentTier) + TierRoadBonus(target.CurrentTier);
                var tieRng = HistoryRandom.Create(state.WorldSeed, SystemKey, HistoryState.PairAgent(source.Index, target.Index), year);
                int score = (distance * 120) - tierBonus - (int)(relation * 50.0) + tieRng.NextInt(7);
                if (score < bestScore)
                {
                    best = target.Index;
                    bestScore = score;
                }
            }
            return best;
        }

        private void EmitIsolationIfNeeded(HistoryState state, HistoricalSettlementState settlement, int year, IHistoryEventSink sink)
        {
            if (settlement.Isolated || (int)settlement.CurrentTier < (int)SettlementSize.Town)
                return;

            settlement.Isolated = true;
            var region = state.Regions[settlement.RegionIndex];
            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.ImpassableZoneFormed,
                settlement.Record.Name,
                settlement.Record.Name + " is marked isolated after roads fail to cross the high-cost terrain around " + region.Record.Name + ".",
                region.Record.Id,
                secondaryRegion: null,
                primarySettlement: settlement.Record.Id,
                secondarySettlement: null,
                primaryFaction: state.Factions[settlement.FactionIndex].Record.Id));
        }

        private static int TierRoadBonus(SettlementSize tier)
        {
            switch (tier)
            {
                case SettlementSize.Capital: return 180;
                case SettlementSize.City: return 130;
                case SettlementSize.Town: return 75;
                case SettlementSize.Village: return 30;
                default: return 10;
            }
        }
    }

    public sealed class DiplomacySystem : IHistorySystem
    {
        private readonly HistorySimulationTuning _tuning;

        public DiplomacySystem(HistorySimulationTuning tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public int SystemKey { get { return HistorySystemKeys.Diplomacy; } }
        public string Name { get { return "Diplomacy"; } }

        public void Tick(HistoryState state, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (sink == null) throw new ArgumentNullException(nameof(sink));

            state.RecalculateFactionStrengths();
            for (int a = 0; a < state.Factions.Length; a++)
            {
                if (!state.Factions[a].Active || !state.Factions[a].CivilizationFounded)
                    continue;

                for (int b = a + 1; b < state.Factions.Length; b++)
                {
                    if (!state.Factions[b].Active || !state.Factions[b].CivilizationFounded)
                        continue;

                    double previous = state.Relations[a, b];
                    double next = previous * 0.985;
                    int border = state.BorderScore(a, b);
                    int competition = state.SharedRegionCompetition(a, b);
                    next -= border * _tuning.BorderFriction;
                    next -= competition * 0.022;
                    next += Math.Min(0.12, state.TradeLinks[a, b] * _tuning.TradeBenefit);

                    var pairRng = HistoryRandom.Create(state.WorldSeed, SystemKey, HistoryState.PairAgent(a, b), year);
                    next += (pairRng.NextInt(41) - 20) / 2000.0;
                    if (state.AtWar[a, b])
                        next -= 0.045;

                    state.SetRelation(a, b, next);
                    EmitDiplomacyEvents(state, a, b, previous, state.Relations[a, b], year, pairRng, sink);
                }
            }
        }

        private void EmitDiplomacyEvents(HistoryState state, int a, int b, double previous, double next, int year, IDeterministicRng pairRng, IHistoryEventSink sink)
        {
            var factionA = state.Factions[a];
            var factionB = state.Factions[b];

            if (next <= -0.34 && previous > -0.34 && !state.WasRecentBorderDispute(a, b, year))
            {
                state.MarkBorderDispute(a, b, year);
                sink.Emit(new WorldHistoryEvent(
                    year,
                    WorldHistoryKind.BorderDispute,
                    factionA.Record.Name,
                    factionA.Record.Name + " and " + factionB.Record.Name + " dispute roads, fields, and borders.",
                    primaryRegion: null,
                    secondaryRegion: null,
                    primarySettlement: null,
                    secondarySettlement: null,
                    primaryFaction: factionA.Record.Id,
                    secondaryFaction: factionB.Record.Id));
            }

            if (next >= 0.62 && previous < 0.62 && !state.WasRecentAlliance(a, b, year))
            {
                state.MarkAlliance(a, b, year);
                sink.Emit(new WorldHistoryEvent(
                    year,
                    WorldHistoryKind.FactionAlliance,
                    factionA.Record.Name,
                    factionA.Record.Name + " allies with " + factionB.Record.Name + " after years of trade and border trust.",
                    primaryRegion: null,
                    secondaryRegion: null,
                    primarySettlement: null,
                    secondarySettlement: null,
                    primaryFaction: factionA.Record.Id,
                    secondaryFaction: factionB.Record.Id));
            }

            int marriageRoll = pairRng.NextInt(1000);
            if (next >= 0.54 && !state.HasMarriagePair(a, b) && marriageRoll < (int)(_tuning.CourtEventChance * 1000.0))
            {
                state.MarkMarriagePair(a, b);
                sink.Emit(new WorldHistoryEvent(
                    year,
                    WorldHistoryKind.NobleMarriage,
                    factionA.Record.Name,
                    "A noble marriage binds " + factionA.Record.Name + " to " + factionB.Record.Name + ".",
                    primaryRegion: null,
                    secondaryRegion: null,
                    primarySettlement: null,
                    secondarySettlement: null,
                    primaryFaction: factionA.Record.Id,
                    secondaryFaction: factionB.Record.Id));
            }
        }
    }

    public sealed class WarSystem : IHistorySystem
    {
        private readonly HistorySimulationTuning _tuning;

        public WarSystem(HistorySimulationTuning tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public int SystemKey { get { return HistorySystemKeys.War; } }
        public string Name { get { return "War"; } }

        public void Tick(HistoryState state, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (sink == null) throw new ArgumentNullException(nameof(sink));

            state.RecalculateFactionStrengths();
            for (int a = 0; a < state.Factions.Length; a++)
            {
                if (!CanFight(state.Factions[a]))
                    continue;

                for (int b = a + 1; b < state.Factions.Length; b++)
                {
                    if (!CanFight(state.Factions[b]))
                        continue;

                    var pairRng = HistoryRandom.Create(state.WorldSeed, SystemKey, HistoryState.PairAgent(a, b), year);
                    if (!state.AtWar[a, b])
                    {
                        TryDeclareWar(state, a, b, year, pairRng, sink);
                    }
                    else
                    {
                        ResolveWarYear(state, a, b, year, pairRng, sink);
                    }
                }
            }
        }

        private void TryDeclareWar(HistoryState state, int a, int b, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            double relation = state.Relations[a, b];
            double pressure = -relation + _tuning.WarPressure + (state.BorderScore(a, b) * 0.05);
            if (relation > -0.70 || rng.NextInt(1000) >= (int)(pressure * 430.0))
                return;

            state.AtWar[a, b] = true;
            state.AtWar[b, a] = true;
            state.WarStartedYear[a, b] = year;
            state.WarStartedYear[b, a] = year;

            var factionA = state.Factions[a];
            var factionB = state.Factions[b];
            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.FactionWar,
                factionA.Record.Name,
                factionA.Record.Name + " declares war against " + factionB.Record.Name + ".",
                primaryRegion: null,
                secondaryRegion: null,
                primarySettlement: null,
                secondarySettlement: null,
                primaryFaction: factionA.Record.Id,
                secondaryFaction: factionB.Record.Id));

            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.WarDeclared,
                factionA.Record.Name,
                "War is declared after relations fall to " + ((int)(relation * 100.0)).ToString() + " between " + factionA.Record.Name + " and " + factionB.Record.Name + ".",
                primaryRegion: null,
                secondaryRegion: null,
                primarySettlement: null,
                secondarySettlement: null,
                primaryFaction: factionA.Record.Id,
                secondaryFaction: factionB.Record.Id));
        }

        private void ResolveWarYear(HistoryState state, int a, int b, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            int started = state.WarStartedYear[a, b];
            int age = Math.Max(0, year - started);
            if (age % 2 != 0 && rng.NextInt(100) >= 35)
                return;

            int attacker = rng.NextInt(2) == 0 ? a : b;
            int defender = attacker == a ? b : a;
            double attackStrength = state.Factions[attacker].Strength + rng.NextInt(700);
            double defenseStrength = state.Factions[defender].Strength + rng.NextInt(700);

            int winner = attackStrength >= defenseStrength ? attacker : defender;
            int loser = winner == attacker ? defender : attacker;
            var target = state.FindWarTarget(winner, loser);
            if (target == null)
                return;

            var region = state.Regions[target.RegionIndex];
            int casualties = Math.Max(12, (int)(region.Population * (0.010 + (rng.NextInt(45) / 1000.0))));
            region.Population = Math.Max(1.0, region.Population - casualties);

            var winningFaction = state.Factions[winner];
            var losingFaction = state.Factions[loser];
            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.BattleFought,
                target.Record.Name,
                winningFaction.Record.Name + " defeats " + losingFaction.Record.Name + " near " + target.Record.Name + ", with " + casualties.ToString() + " casualties.",
                region.Record.Id,
                secondaryRegion: null,
                primarySettlement: target.Record.Id,
                secondarySettlement: null,
                primaryFaction: winningFaction.Record.Id,
                secondaryFaction: losingFaction.Record.Id));

            bool sack = casualties > 240 || rng.NextInt(100) < 28;
            if (sack)
            {
                sink.Emit(new WorldHistoryEvent(
                    year,
                    WorldHistoryKind.SiteSacked,
                    target.Record.Name,
                    target.Record.Name + " is sacked by " + winningFaction.Record.Name + ".",
                    region.Record.Id,
                    secondaryRegion: null,
                    primarySettlement: target.Record.Id,
                    secondarySettlement: null,
                    primaryFaction: winningFaction.Record.Id,
                    secondaryFaction: losingFaction.Record.Id));
            }

            if (attackStrength - defenseStrength > 280.0 || sack)
            {
                target.FactionIndex = winner;
                state.SetRelation(a, b, Math.Max(-1.0, state.Relations[a, b] - 0.08));
            }

            state.RecalculateFactionStrengths();
            if (state.Factions[loser].FoundedSettlementCount == 0 && !state.WasCivilizationDestroyed(winner, loser))
            {
                losingFaction.Active = false;
                state.MarkCivilizationDestroyed(winner, loser);
                sink.Emit(new WorldHistoryEvent(
                    year,
                    WorldHistoryKind.CivilizationDestroyed,
                    losingFaction.Record.Name,
                    losingFaction.Record.Name + " is destroyed after losing its last held settlement.",
                    region.Record.Id,
                    secondaryRegion: null,
                    primarySettlement: target.Record.Id,
                    secondarySettlement: null,
                    primaryFaction: losingFaction.Record.Id,
                    secondaryFaction: winningFaction.Record.Id));
            }

            if (age > 8 && rng.NextInt(100) < 38)
            {
                state.AtWar[a, b] = false;
                state.AtWar[b, a] = false;
                state.SetRelation(a, b, Math.Max(state.Relations[a, b], -0.44));
            }
        }

        private static bool CanFight(HistoryFactionState faction)
        {
            return faction.Active && faction.CivilizationFounded && faction.FoundedSettlementCount > 0;
        }
    }

    public sealed class HistoricalFigureSystem : IHistorySystem
    {
        private readonly HistorySimulationTuning _tuning;

        public HistoricalFigureSystem(HistorySimulationTuning tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public int SystemKey { get { return HistorySystemKeys.HistoricalFigure; } }
        public string Name { get { return "HistoricalFigure"; } }

        public void Tick(HistoryState state, int year, IDeterministicRng rng, IHistoryEventSink sink)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (sink == null) throw new ArgumentNullException(nameof(sink));

            for (int f = 0; f < state.Factions.Length; f++)
            {
                var faction = state.Factions[f];
                if (!faction.Active || !faction.CivilizationFounded)
                    continue;

                EnsureLeader(state, faction, year, sink);
                MaybeBirthHeir(state, faction, year, sink);
                MaybeEndLeader(state, faction, year, sink);
            }

            MaybePoliticalMarriage(state, year, sink);
        }

        private void EnsureLeader(HistoryState state, HistoryFactionState faction, int year, IHistoryEventSink sink)
        {
            var leader = state.CurrentLeader(faction.Index);
            if (leader != null)
                return;

            var heir = state.FindLivingHeir(faction.Index);
            if (heir == null)
            {
                var rng = HistoryRandom.Create(state.WorldSeed, SystemKey, faction.Index + 1, year);
                heir = state.AddFigure(FigureName(rng, faction), faction.Index, year - 24 - rng.NextInt(22), faction.Index + 1);
            }

            heir.IsHeir = false;
            faction.LeaderFigureId = heir.Id;
            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.LeaderCrowned,
                heir.Name,
                heir.Name + " is crowned leader of " + faction.Record.Name + ".",
                primaryRegion: null,
                secondaryRegion: null,
                primarySettlement: null,
                secondarySettlement: null,
                primaryFaction: faction.Record.Id,
                secondaryFaction: null,
                primaryFigureId: heir.Id));
        }

        private void MaybeBirthHeir(HistoryState state, HistoryFactionState faction, int year, IHistoryEventSink sink)
        {
            bool hasLivingHeir = state.FindLivingHeir(faction.Index) != null;
            var rng = HistoryRandom.Create(state.WorldSeed, SystemKey, (faction.Index + 1) * 11, year);
            int chance = hasLivingHeir ? 34 : 210;
            if (rng.NextInt(1000) >= chance)
                return;

            var heir = state.AddFigure(FigureName(rng, faction), faction.Index, year, faction.Index + 1);
            heir.IsHeir = true;
            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.HeirBorn,
                heir.Name,
                heir.Name + " is born into the dynasty of " + faction.Record.Name + ".",
                primaryRegion: null,
                secondaryRegion: null,
                primarySettlement: null,
                secondarySettlement: null,
                primaryFaction: faction.Record.Id,
                secondaryFaction: null,
                primaryFigureId: heir.Id));
        }

        private void MaybeEndLeader(HistoryState state, HistoryFactionState faction, int year, IHistoryEventSink sink)
        {
            var leader = state.CurrentLeader(faction.Index);
            if (leader == null)
                return;

            int age = year - leader.BirthYear;
            var rng = HistoryRandom.Create(state.WorldSeed, SystemKey, (faction.Index + 1) * 23, year);
            int deathChance = age < 42 ? 4 : Math.Min(260, (age - 38) * 7);
            if (rng.NextInt(1000) >= deathChance)
                return;

            bool assassination = rng.NextInt(100) < 16 + (int)(_tuning.WarPressure * 400.0);
            leader.Alive = false;
            leader.DeathYear = year;
            faction.LeaderFigureId = -1;

            if (assassination)
            {
                sink.Emit(new WorldHistoryEvent(
                    year,
                    WorldHistoryKind.FigureAssassinated,
                    leader.Name,
                    leader.Name + " of " + faction.Record.Name + " is assassinated during a court crisis.",
                    primaryRegion: null,
                    secondaryRegion: null,
                    primarySettlement: null,
                    secondarySettlement: null,
                    primaryFaction: faction.Record.Id,
                    secondaryFaction: null,
                    primaryFigureId: leader.Id));
            }

            sink.Emit(new WorldHistoryEvent(
                year,
                WorldHistoryKind.NobleDeath,
                leader.Name,
                leader.Name + " of " + faction.Record.Name + " dies at age " + age.ToString() + ".",
                primaryRegion: null,
                secondaryRegion: null,
                primarySettlement: null,
                secondarySettlement: null,
                primaryFaction: faction.Record.Id,
                secondaryFaction: null,
                primaryFigureId: leader.Id));
        }

        private void MaybePoliticalMarriage(HistoryState state, int year, IHistoryEventSink sink)
        {
            for (int a = 0; a < state.Factions.Length; a++)
            {
                if (!state.Factions[a].Active || !state.Factions[a].CivilizationFounded)
                    continue;

                for (int b = a + 1; b < state.Factions.Length; b++)
                {
                    if (!state.Factions[b].Active || !state.Factions[b].CivilizationFounded)
                        continue;
                    if (state.Relations[a, b] < 0.50 || state.HasMarriagePair(a, b))
                        continue;

                    var rng = HistoryRandom.Create(state.WorldSeed, SystemKey, HistoryState.PairAgent(a, b) * 31, year);
                    if (rng.NextInt(1000) >= (int)(_tuning.CourtEventChance * 700.0))
                        continue;

                    state.MarkMarriagePair(a, b);
                    sink.Emit(new WorldHistoryEvent(
                        year,
                        WorldHistoryKind.NobleMarriage,
                        state.Factions[a].Record.Name,
                        "A dynastic marriage joins " + state.Factions[a].Record.Name + " and " + state.Factions[b].Record.Name + ".",
                        primaryRegion: null,
                        secondaryRegion: null,
                        primarySettlement: null,
                        secondarySettlement: null,
                        primaryFaction: state.Factions[a].Record.Id,
                        secondaryFaction: state.Factions[b].Record.Id));
                }
            }
        }

        private static string FigureName(IDeterministicRng rng, HistoryFactionState faction)
        {
            string given = SyllableNameForge.Forge(rng, 2);
            string house = faction.Record.Name;
            int space = house.LastIndexOf(' ');
            if (space >= 0 && space + 1 < house.Length)
                house = house.Substring(space + 1);
            return given + " " + house;
        }
    }
}
