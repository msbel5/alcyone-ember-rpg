// DomainSimulationAdapter — worldgen / character-creation / world-hydration (partial-class split, ARCH-02 structural / LOC).
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using EmberCrpg.Presentation.Ember.Worldgen;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        public void SeedWorld(string mood, string calling, string startLocation, uint? worldSeed = null)
        {
            // Derive a deterministic uint seed from the three wizard strings
            // by FNV-1a-folding their concatenation. The same wizard inputs
            // therefore always produce the same world, which is what makes
            // "share your seed" a viable replay feature down the line.
            uint seed = worldSeed ?? FoldSeed(mood, calling, startLocation);
            var style = ParseStyle(mood);
            var genre = ParseGenre(mood, calling, startLocation);
            var preferredSize = ParsePreferredSettlementSize(startLocation);
            var parameters = EmberCrpg.Simulation.Worldgen.WorldgenParameters.For(style, genre);

            // Spherical-planet worldgen (PRD_planetary_worldgen): the deterministic planet pipeline + the
            // PlanetToWorldMapper produce the SAME GeneratedWorld shape the rest of SeedWorld consumes. Cached
            // per seed in PlanetWorldContext so the char-creation reveal builds it once (streamed) and this
            // reuses it; falls through to a synchronous build (behind the loading screen) when not pre-cached.
            var generated = EmberCrpg.Presentation.Ember.Worldgen.PlanetWorldService.GetOrGenerate(
                seed,
                parameters);
            GeneratedWorld = generated;
            StartingRegion = SelectStartingRegion(generated, startLocation);
            StartingSettlement = SelectStartingSettlement(generated, preferredSize, startLocation);
            StartingFaction = SelectStartingFaction(generated, calling);
            _billboardOriginResolved = false; // re-resolve the billboard grid->world origin for this world's starting settlement (lazily, once its site is hydrated)

            _world.WorldProfile = new WorldProfile(
                style,
                genre,
                seed,
                parameters.TargetPopulation,
                parameters.RegionCount,
                parameters.FactionCount,
                parameters.HistoryYears,
                mood,
                calling,
                startLocation);
            HydrateGeneratedWorld(generated, preferredSize);

            // W1: project the deterministic open-world overland map (PRD_overland_map_v1) from the SAME
            // generated world (not a separate Default regen), so the map's settlements ARE the ones the
            // history simulated + NPCs are homed to (fixes the reveal/map settlement-count mismatch and the
            // starting-settlement resolution miss). OverlandWorldgen.Generate(GeneratedWorld, ...) projects.
            // Project the overland map at the GeneratedWorld's OWN geography dimensions. The planet mapper
            // builds a 128x64 grid (vs the flat worldgen's Default), and OverlandWorldgen requires the params to
            // match the geography it carries. Deriving from generated.Geography works for either worldgen source.
            var overlandGeo = generated.Geography;
            _world.Overland = EmberCrpg.Simulation.Overland.OverlandWorldgen.Generate(
                generated,
                new EmberCrpg.Domain.Overland.OverlandParameters(overlandGeo.Width, overlandGeo.Height));

            UnityEngine.Debug.Log(
                $"Domain Seeded: seed={seed} style={style} genre={genre} mood='{mood}' calling='{calling}' start='{startLocation}' " +
                $"regions={generated.Regions.Count} settlements={generated.Settlements.Count} " +
                $"npcs={generated.Npcs.Count} pop={generated.TotalPopulation:N0} " +
                $"history={generated.History.Count} startingRegion={StartingRegion} startingSettlement={StartingSettlement} startingFaction={StartingFaction}");
        }

        public void ApplyCharacterCreation(string playerName, string classId, string birthsignId)
        {
            if (_world.Actors == null || !_world.Actors.TryFirstByRole(ActorRole.Player, out var player) || player == null)
                return;

            var klass = CharacterCreationCatalog.GetClass(classId);
            var sign = CharacterCreationCatalog.GetBirthsign(birthsignId);
            var stats = sign.ApplyTo(klass.PrimaryStats);
            var vitals = new ActorVitals(
                new VitalStat(30 + stats.End / 2, 30 + stats.End / 2),
                new VitalStat(30 + stats.Mig / 2, 30 + stats.Mig / 2),
                new VitalStat(20 + stats.Mnd / 2, 20 + stats.Mnd / 2));

            var replacement = new ActorRecord(
                player.Id,
                string.IsNullOrWhiteSpace(playerName) ? player.Name : playerName.Trim(),
                ActorRole.Player,
                stats,
                vitals,
                player.Position,
                accuracy: player.Accuracy,
                dodge: player.Dodge,
                armor: player.Armor,
                baseDamage: player.BaseDamage,
                topicIds: player.TopicIds,
                jobPreferences: player.JobPreferences,
                scheduleState: player.ScheduleState,
                needs: player.Needs,
                mood: player.Mood,
                memory: player.Memory);
            _world.ReplaceActorView(ActorRole.Player, replacement);
            _lastCombatLine = $"{replacement.Name} begins as {klass.Name} under {sign.Name}.";
        }

    }
}
