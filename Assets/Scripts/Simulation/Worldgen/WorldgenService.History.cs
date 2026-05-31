// EMB-034: WorldgenService history generation phase (partial).
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
                WorldHistoryKind kind = RollHistoryKind(rng, parameters);

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
                        // Codex review (PR #203 P1): WorldgenParameters allows
                        // factionCount=1. The previous reroll path called
                        // rng.NextInt(factions.Count - 1), which becomes
                        // NextInt(0) and throws ArgumentOutOfRangeException
                        // when only one faction exists. Take a one-faction
                        // fast path that emits a self-referential history
                        // entry (civil schism for War / internal reconciliation
                        // for Alliance) instead of crashing.
                        if (factions.Count == 1)
                        {
                            subject = f1.Name;
                            detail = (kind == WorldHistoryKind.FactionWar)
                                ? f1.Name + " splinters into civil war"
                                : f1.Name + " reunites after internal strife";
                            break;
                        }
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

        private static WorldHistoryKind RollHistoryKind(IDeterministicRng rng, WorldgenParameters parameters)
        {
            var kinds = new[]
            {
                WorldHistoryKind.SettlementFounded,
                WorldHistoryKind.FactionWar,
                WorldHistoryKind.FactionAlliance,
                WorldHistoryKind.NobleMarriage,
                WorldHistoryKind.NobleDeath,
                WorldHistoryKind.Calamity,
                WorldHistoryKind.TradeRouteOpened,
                WorldHistoryKind.Migration,
            };

            int total = 0;
            for (int i = 0; i < kinds.Length; i++)
                total += HistoryWeight(kinds[i], parameters.Style, parameters.Genre);

            int roll = rng.NextInt(total);
            int cursor = 0;
            for (int i = 0; i < kinds.Length; i++)
            {
                cursor += HistoryWeight(kinds[i], parameters.Style, parameters.Genre);
                if (roll < cursor) return kinds[i];
            }
            return WorldHistoryKind.Migration;
        }

        private static int HistoryWeight(WorldHistoryKind kind, WorldStyle style, WorldGenre genre)
        {
            int weight;
            switch (kind)
            {
                case WorldHistoryKind.SettlementFounded: weight = 20; break;
                case WorldHistoryKind.FactionWar: weight = 15; break;
                case WorldHistoryKind.FactionAlliance: weight = 10; break;
                case WorldHistoryKind.NobleMarriage: weight = 15; break;
                case WorldHistoryKind.NobleDeath: weight = 12; break;
                case WorldHistoryKind.Calamity: weight = 8; break;
                case WorldHistoryKind.TradeRouteOpened: weight = 12; break;
                default: weight = 8; break;
            }

            if (style == WorldStyle.DarkFantasyGrim && (kind == WorldHistoryKind.FactionWar || kind == WorldHistoryKind.Calamity || kind == WorldHistoryKind.NobleDeath))
                weight += 12;
            if (style == WorldStyle.HighFantasy && (kind == WorldHistoryKind.FactionAlliance || kind == WorldHistoryKind.SettlementFounded))
                weight += 8;
            if (style == WorldStyle.SteampunkRevolution && kind == WorldHistoryKind.TradeRouteOpened)
                weight += 12;
            if (style == WorldStyle.AncientMythology && (kind == WorldHistoryKind.Migration || kind == WorldHistoryKind.Calamity))
                weight += 7;

            if (genre == WorldGenre.PoliticalIntrigue && (kind == WorldHistoryKind.FactionAlliance || kind == WorldHistoryKind.NobleMarriage || kind == WorldHistoryKind.NobleDeath))
                weight += 10;
            if (genre == WorldGenre.MonsterHunt && (kind == WorldHistoryKind.Calamity || kind == WorldHistoryKind.Migration))
                weight += 9;
            if (genre == WorldGenre.MerchantEmpire && kind == WorldHistoryKind.TradeRouteOpened)
                weight += 10;
            if (genre == WorldGenre.Pilgrimage && (kind == WorldHistoryKind.Migration || kind == WorldHistoryKind.SettlementFounded))
                weight += 8;

            return weight < 1 ? 1 : weight;
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
