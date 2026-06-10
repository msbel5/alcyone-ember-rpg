using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;

// Design note:
// GeneratedWorld is the immutable output bundle of WorldgenService.Generate.
// Five read-only views plus the seed used to build them. Insertion-ordered
// IReadOnlyList projections are wrapped in ReadOnlyCollection so callers
// cannot downcast back to List<T> and mutate — matches the live-view
// immutability contract used by WorldEventLog.
//
// FactionRelationSeed lives in this file because it is a worldgen-internal
// shape: a triple of (factionA, factionB, FactionReputation) that the
// generator emits, with a canonical low-then-high ordering so duplicate
// pairs collapse. The runtime FactionStore has its own private FactionPair
// for the same job, but exposing it would leak an internal type.
namespace EmberCrpg.Simulation.Worldgen
{
    /// <summary>Initial reputation seed for a faction pair emitted by WorldgenService.</summary>
    public readonly struct FactionRelationSeed : IEquatable<FactionRelationSeed>
    {
        public FactionRelationSeed(FactionId factionA, FactionId factionB, FactionReputation reputation)
        {
            if (factionA.IsEmpty)
                throw new ArgumentException("factionA cannot be FactionId.Empty.", nameof(factionA));
            if (factionB.IsEmpty)
                throw new ArgumentException("factionB cannot be FactionId.Empty.", nameof(factionB));
            if (factionA == factionB)
                throw new ArgumentException("FactionRelationSeed requires two distinct factions.", nameof(factionB));

            // Canonical ordering: low.Value <= high.Value so (a,b) and (b,a)
            // round-trip to the same seed without callers tracking order.
            if (factionA.Value <= factionB.Value)
            {
                FactionA = factionA;
                FactionB = factionB;
            }
            else
            {
                FactionA = factionB;
                FactionB = factionA;
            }

            Reputation = reputation;
        }

        public FactionId FactionA { get; }
        public FactionId FactionB { get; }
        public FactionReputation Reputation { get; }

        public bool Equals(FactionRelationSeed other)
        {
            return FactionA.Equals(other.FactionA)
                && FactionB.Equals(other.FactionB)
                && Reputation.Equals(other.Reputation);
        }

        public override bool Equals(object obj)
        {
            return obj is FactionRelationSeed other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + FactionA.GetHashCode();
                hash = (hash * 31) + FactionB.GetHashCode();
                hash = (hash * 31) + Reputation.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(FactionRelationSeed left, FactionRelationSeed right) => left.Equals(right);
        public static bool operator !=(FactionRelationSeed left, FactionRelationSeed right) => !left.Equals(right);
    }

    /// <summary>
    /// Immutable bundle of records produced by <see cref="WorldgenService.Generate"/>.
    /// Pairs the original seed with the generated regions, settlements,
    /// factions, faction relations, NPCs, and the multi-century history events
    /// so callers can persist or replay the exact world.
    /// </summary>
    public sealed class GeneratedWorld
    {
        public GeneratedWorld(
            uint seed,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<SettlementRecord> settlements,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<FactionRelationSeed> factionRelations,
            IReadOnlyList<NpcSeedRecord> npcs,
            IReadOnlyList<WorldHistoryEvent> history)
            : this(seed, regions, settlements, factions, factionRelations, npcs, history, Array.Empty<NotableFigureRecord>(), null)
        {
        }

        public GeneratedWorld(
            uint seed,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<SettlementRecord> settlements,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<FactionRelationSeed> factionRelations,
            IReadOnlyList<NpcSeedRecord> npcs,
            IReadOnlyList<WorldHistoryEvent> history,
            IReadOnlyList<NotableFigureRecord> notableFigures)
            : this(seed, regions, settlements, factions, factionRelations, npcs, history, notableFigures, null)
        {
        }

        public GeneratedWorld(
            uint seed,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<SettlementRecord> settlements,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<FactionRelationSeed> factionRelations,
            IReadOnlyList<NpcSeedRecord> npcs,
            IReadOnlyList<WorldHistoryEvent> history,
            IReadOnlyList<NotableFigureRecord> notableFigures,
            WorldGeography geography)
        {
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            if (settlements == null) throw new ArgumentNullException(nameof(settlements));
            if (factions == null) throw new ArgumentNullException(nameof(factions));
            if (factionRelations == null) throw new ArgumentNullException(nameof(factionRelations));
            if (npcs == null) throw new ArgumentNullException(nameof(npcs));
            if (history == null) throw new ArgumentNullException(nameof(history));
            if (notableFigures == null) throw new ArgumentNullException(nameof(notableFigures));

            Seed = seed;
            Regions = Wrap(regions);
            Settlements = Wrap(settlements);
            Factions = Wrap(factions);
            FactionRelations = Wrap(factionRelations);
            Npcs = Wrap(npcs);
            History = Wrap(history);
            NotableFigures = Wrap(notableFigures);
            Geography = geography;
        }

        public uint Seed { get; }
        public IReadOnlyList<RegionRecord> Regions { get; }
        public IReadOnlyList<SettlementRecord> Settlements { get; }
        public IReadOnlyList<FactionRecord> Factions { get; }
        public IReadOnlyList<FactionRelationSeed> FactionRelations { get; }
        public IReadOnlyList<NpcSeedRecord> Npcs { get; }
        public IReadOnlyList<WorldHistoryEvent> History { get; }
        public IReadOnlyList<NotableFigureRecord> NotableFigures { get; }
        public WorldGeography Geography { get; }

        /// <summary>
        /// The full-resolution planet simulation this world was projected from (icosphere tiles carrying
        /// elevation / climate / hydrology, incl. rivers and lakes). Optional sidecar: null on the legacy
        /// non-planet path and NOT part of any save payload — it exists so renderers and terrain samplers
        /// can use the rich source instead of the flattened 128x64 geography raster.
        /// </summary>
        public EmberCrpg.Simulation.Worldgen.Planet.PlanetField PlanetData { get; set; }

        public int TotalPopulation
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Settlements.Count; i++)
                    total += Settlements[i].Population;
                return total;
            }
        }

        private static IReadOnlyList<T> Wrap<T>(IReadOnlyList<T> source)
        {
            // Materialize then wrap in ReadOnlyCollection so the published
            // surface cannot be downcast to a mutable list, matching the
            // immutability contract used by WorldEventLog.Events.
            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++)
                copy.Add(source[i]);
            return new ReadOnlyCollection<T>(copy);
        }
    }
}
