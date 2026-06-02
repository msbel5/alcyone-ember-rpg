using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Worldgen.History;

namespace EmberCrpg.Simulation.Worldgen
{
    public static partial class WorldgenService
    {
        private static HistoryWorldProjection ProjectHistoryState(WorldHistorySimulationResult historyResult)
        {
            if (historyResult == null) throw new ArgumentNullException(nameof(historyResult));

            var settlements = ProjectSettlements(historyResult.State);
            var figures = ProjectNotableFigures(historyResult.State, settlements);
            return new HistoryWorldProjection(settlements, figures);
        }

        private static List<SettlementRecord> ProjectSettlements(HistoryState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var survivingByRegion = new List<int>[state.Regions.Length];
            for (int i = 0; i < state.Settlements.Length; i++)
            {
                var settlement = state.Settlements[i];
                if (!IsSurvivingSettlement(settlement))
                    continue;

                var regionBucket = survivingByRegion[settlement.RegionIndex];
                if (regionBucket == null)
                {
                    regionBucket = new List<int>();
                    survivingByRegion[settlement.RegionIndex] = regionBucket;
                }

                regionBucket.Add(i);
            }

            var populations = new int[state.Settlements.Length];
            for (int regionIndex = 0; regionIndex < survivingByRegion.Length; regionIndex++)
            {
                var regionSettlements = survivingByRegion[regionIndex];
                if (regionSettlements == null || regionSettlements.Count == 0)
                    continue;

                ProjectRegionPopulation(state, regionIndex, regionSettlements, populations);
            }

            var projected = new List<SettlementRecord>(state.Settlements.Length);
            for (int i = 0; i < state.Settlements.Length; i++)
            {
                int population = populations[i];
                if (population <= 0)
                    continue;

                var settlement = state.Settlements[i];
                projected.Add(new SettlementRecord(
                    settlement.Record.Id,
                    settlement.Record.Region,
                    settlement.Record.Name,
                    population,
                    settlement.CurrentTier));
            }

            return projected;
        }

        private static void ProjectRegionPopulation(
            HistoryState state,
            int regionIndex,
            IReadOnlyList<int> regionSettlements,
            int[] populations)
        {
            int targetPopulation = PositivePopulation(state.Regions[regionIndex].Population, regionSettlements.Count);
            var weights = new long[regionSettlements.Count];
            var remainders = new long[regionSettlements.Count];
            long totalWeight = 0L;

            for (int i = 0; i < regionSettlements.Count; i++)
            {
                var settlement = state.Settlements[regionSettlements[i]];
                long weight = SettlementPopulationWeight(settlement);
                weights[i] = weight;
                totalWeight += weight;
            }

            int assigned = 0;
            for (int i = 0; i < regionSettlements.Count; i++)
            {
                long scaled = targetPopulation * weights[i];
                int share = (int)(scaled / totalWeight);
                if (share < 1)
                    share = 1;

                populations[regionSettlements[i]] = share;
                remainders[i] = scaled % totalWeight;
                assigned += share;
            }

            int delta = targetPopulation - assigned;
            while (delta > 0)
            {
                int localIndex = LargestRemainderIndex(remainders, regionSettlements, populations);
                populations[regionSettlements[localIndex]]++;
                remainders[localIndex] = -1L;
                delta--;
            }

            while (delta < 0)
            {
                int localIndex = LargestPopulationIndex(regionSettlements, populations);
                if (localIndex < 0)
                    break;

                populations[regionSettlements[localIndex]]--;
                delta++;
            }
        }

        private static bool IsSurvivingSettlement(HistoricalSettlementState settlement)
        {
            return settlement.Founded && settlement.CurrentTier != SettlementSize.None;
        }

        private static int PositivePopulation(double simulatedPopulation, int minimum)
        {
            if (double.IsNaN(simulatedPopulation) || double.IsInfinity(simulatedPopulation) || simulatedPopulation <= 0.0)
                return minimum;
            if (simulatedPopulation >= int.MaxValue)
                return int.MaxValue;

            int rounded = (int)Math.Round(simulatedPopulation, MidpointRounding.AwayFromZero);
            return rounded < minimum ? minimum : rounded;
        }

        private static long SettlementPopulationWeight(HistoricalSettlementState settlement)
        {
            return TierPopulationWeight(settlement.CurrentTier) + Math.Max(1, settlement.Record.Population);
        }

        private static long TierPopulationWeight(SettlementSize size)
        {
            switch (size)
            {
                case SettlementSize.Capital: return 180_000L;
                case SettlementSize.City: return 75_000L;
                case SettlementSize.Town: return 6_000L;
                case SettlementSize.Village: return 650L;
                case SettlementSize.Hamlet: return 200L;
                default: return 1L;
            }
        }

        private static int LargestRemainderIndex(long[] remainders, IReadOnlyList<int> regionSettlements, int[] populations)
        {
            int best = 0;
            for (int i = 1; i < remainders.Length; i++)
            {
                if (remainders[i] > remainders[best])
                {
                    best = i;
                    continue;
                }

                if (remainders[i] == remainders[best])
                {
                    int settlementIndex = regionSettlements[i];
                    int bestSettlementIndex = regionSettlements[best];
                    if (populations[settlementIndex] > populations[bestSettlementIndex])
                        best = i;
                    else if (populations[settlementIndex] == populations[bestSettlementIndex]
                        && settlementIndex < bestSettlementIndex)
                        best = i;
                }
            }

            return best;
        }

        private static int LargestPopulationIndex(IReadOnlyList<int> regionSettlements, int[] populations)
        {
            int best = -1;
            for (int i = 0; i < regionSettlements.Count; i++)
            {
                int settlementIndex = regionSettlements[i];
                if (populations[settlementIndex] <= 1)
                    continue;
                if (best < 0 || populations[settlementIndex] > populations[regionSettlements[best]])
                    best = i;
            }

            return best;
        }

        private static List<NotableFigureRecord> ProjectNotableFigures(
            HistoryState state,
            IReadOnlyList<SettlementRecord> projectedSettlements)
        {
            var figures = new List<NotableFigureRecord>(state.Figures.Count);
            if (projectedSettlements.Count == 0)
                return figures;

            for (int i = 0; i < state.Figures.Count; i++)
            {
                var figure = state.Figures[i];
                if (figure.FactionIndex < 0 || figure.FactionIndex >= state.Factions.Length)
                    continue;

                var faction = state.Factions[figure.FactionIndex];
                SettlementId home = FindFigureHomeSettlement(state, figure.FactionIndex);
                if (home.IsEmpty)
                    home = projectedSettlements[0].Id;

                int? deathYear = figure.Alive || figure.DeathYear == int.MinValue
                    ? (int?)null
                    : figure.DeathYear;

                figures.Add(new NotableFigureRecord(
                    figure.Id,
                    figure.Name,
                    FigureTitle(state, figure),
                    figure.BirthYear,
                    deathYear,
                    home,
                    faction.Record.Id));
            }

            return figures;
        }

        private static SettlementId FindFigureHomeSettlement(HistoryState state, int factionIndex)
        {
            int best = -1;
            for (int i = 0; i < state.Settlements.Length; i++)
            {
                var settlement = state.Settlements[i];
                if (!IsSurvivingSettlement(settlement) || settlement.FactionIndex != factionIndex)
                    continue;
                if (best < 0 || CompareFigureHome(settlement, state.Settlements[best]) < 0)
                    best = i;
            }

            if (best >= 0)
                return state.Settlements[best].Record.Id;

            for (int i = 0; i < state.Settlements.Length; i++)
            {
                if (IsSurvivingSettlement(state.Settlements[i]))
                    return state.Settlements[i].Record.Id;
            }

            return default;
        }

        private static int CompareFigureHome(HistoricalSettlementState left, HistoricalSettlementState right)
        {
            int tierCompare = ((int)right.CurrentTier).CompareTo((int)left.CurrentTier);
            if (tierCompare != 0)
                return tierCompare;
            return left.Record.Id.Value.CompareTo(right.Record.Id.Value);
        }

        private static string FigureTitle(HistoryState state, HistoricalFigureState figure)
        {
            if (figure.Alive && figure.FactionIndex >= 0 && figure.FactionIndex < state.Factions.Length
                && state.Factions[figure.FactionIndex].LeaderFigureId == figure.Id)
            {
                return "Ruler";
            }

            if (figure.IsHeir)
                return "Heir";
            if (!figure.Alive)
                return "Late noble";
            return "Noble";
        }

        private readonly struct HistoryWorldProjection
        {
            public HistoryWorldProjection(
                IReadOnlyList<SettlementRecord> settlements,
                IReadOnlyList<NotableFigureRecord> notableFigures)
            {
                Settlements = settlements ?? throw new ArgumentNullException(nameof(settlements));
                NotableFigures = notableFigures ?? throw new ArgumentNullException(nameof(notableFigures));
            }

            public IReadOnlyList<SettlementRecord> Settlements { get; }
            public IReadOnlyList<NotableFigureRecord> NotableFigures { get; }
        }
    }
}
