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

        private static RegionId SelectStartingRegion(
            EmberCrpg.Simulation.Worldgen.GeneratedWorld generated,
            string startLocation)
        {
            if (generated.Regions.Count == 0)
                return default;
            if (!string.IsNullOrWhiteSpace(startLocation))
            {
                for (int i = 0; i < generated.Regions.Count; i++)
                {
                    var r = generated.Regions[i];
                    if (r.Name.IndexOf(startLocation, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return r.Id;
                }
            }
            return generated.Regions[0].Id;
        }

        private static SettlementId SelectStartingSettlement(
            EmberCrpg.Simulation.Worldgen.GeneratedWorld generated,
            SettlementSize preferredSize,
            string startLocation)
        {
            if (!string.IsNullOrWhiteSpace(startLocation))
            {
                foreach (var settlement in generated.Settlements)
                {
                    if (settlement.Name.IndexOf(startLocation, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return settlement.Id;
                }
            }

            foreach (var settlement in generated.Settlements)
            {
                if (settlement.Size == preferredSize)
                    return settlement.Id;
            }
            return generated.Settlements.Count == 0 ? default : generated.Settlements[0].Id;
        }

        private static FactionId SelectStartingFaction(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated, string calling)
        {
            if (generated.Factions.Count == 0) return default;
            string normalized = (calling ?? string.Empty).Trim().ToLowerInvariant();
            for (int i = 0; i < generated.Factions.Count; i++)
            {
                var name = generated.Factions[i].Name.ToLowerInvariant();
                if ((normalized.Contains("mage") || normalized.Contains("scholar")) && (name.Contains("order") || name.Contains("circle")))
                    return generated.Factions[i].Id;
                if ((normalized.Contains("merchant") || normalized.Contains("trader") || normalized.Contains("smith")) && name.Contains("league"))
                    return generated.Factions[i].Id;
                if ((normalized.Contains("war") || normalized.Contains("guard") || normalized.Contains("soldier")) && (name.Contains("house") || name.Contains("pact")))
                    return generated.Factions[i].Id;
            }

            uint hash = FoldSeed(calling, string.Empty, string.Empty);
            return generated.Factions[(int)(hash % (uint)generated.Factions.Count)].Id;
        }

        private void HydrateGeneratedWorld(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated, SettlementSize preferredSize)
        {
            HydrateSites(generated);
            HydrateFactions(generated);
            HydrateNpcs(generated);
            HydrateHistory(generated);
            MovePlayerToStartingSettlement();
            SeedStartingQuest();
        }

        private void HydrateSites(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated)
        {
            if (_world.Sites == null) _world.Sites = new SiteStore();
            for (int i = 0; i < generated.Regions.Count; i++)
            {
                var region = generated.Regions[i];
                var id = RegionSiteId(region.Id);
                if (_world.Sites.Contains(id)) continue;
                int x = (i % 10) * 96;
                int y = (i / 10) * 96;
                _world.Sites.Add(new SiteRecord(id, SiteKind.Region, region.Name, new GridPosition(x, y), new GridPosition(x + 80, y + 80)));
            }

            for (int i = 0; i < generated.Settlements.Count; i++)
            {
                var settlement = generated.Settlements[i];
                var id = SettlementSiteId(settlement.Id);
                if (_world.Sites.Contains(id)) continue;
                int x = (i % 32) * 12;
                int y = (i / 32) * 12;
                // Site spans the whole town (~1 cell ≈ 1 m) so NPC homes/day-spots spread across the settlement
                // and align with the building ring (8-24 m), instead of clumping inside a 2-6 m dot at the centre.
                int radius = settlement.Size == SettlementSize.Capital ? 28 : settlement.Size == SettlementSize.City ? 24 : settlement.Size == SettlementSize.Town ? 18 : 14;
                _world.Sites.Add(new SiteRecord(id, SiteKind.Settlement, settlement.Name, new GridPosition(x, y), new GridPosition(x + radius, y + radius)));
            }

            SeedStartingProductionSites();
        }

        /// <summary>
        /// SOUL-01: give the running world something the per-tick economy systems can actually advance —
        /// a farm plot (soil + a seeded crop that PlantGrowthSystem grows each game-day), a forge
        /// worksite, and one pending JobRequest on the JobBoard. Everything is derived deterministically
        /// from the starting settlement's stable SiteId so the same seed always produces the same setup.
        /// Without this, the newly-wired growth/job systems would tick over empty stores forever.
        /// </summary>
        private void SeedStartingProductionSites()
        {
            if (_world.Sites == null) return;

            if (!TryGetStartingProductionAnchor(out var anchor, out var anchorSite))
                return;

            // Worksite cells: anchored to the site's min corner so they never collide with the
            // center worksite the adapter ctor may have already registered for this site.
            var farmPos = anchorSite.MinBound;
            var forgePos = anchorSite.MinBound.Translate(1, 0);

            // Farm plot: soil + a seeded "wheat" crop at its first growth stage. Growth advances daily
            // via WorldTickComposer -> PlantGrowthSystem (species catalog lives on the composer).
            if (!_world.Worksites.Contains(anchor, farmPos))
                _world.Worksites.Add(new WorksiteRecord(anchor, farmPos, WorksiteKind.Field, isActive: true));

            ulong baseId = anchor.Value;
            var soilId = new WorldComponentId(baseId * 10UL + 1UL);
            var plantId = new WorldComponentId(baseId * 10UL + 2UL);
            if (!_world.Plants.TryGet(plantId, out _))
                _world.Plants.Add(plantId, new PlantComponent(plantId, anchor, farmPos, "wheat", new PlantStageId("seed"), 0));
            if (!_world.Soils.TryGet(soilId, out _))
                _world.Soils.Add(soilId, new SoilComponent(soilId, anchor, farmPos, fertility: 70, moisture: 60, plantId: plantId));

            // Forge worksite + one pending smelting job. JobKind.Smith / WorksiteKind.Furnace /
            // SmeltIronIngot recipe matches the production registry so the job is workable in-game
            // once an actor with a Smith preference is present (see HydrateNpcs).
            if (!_world.Worksites.Contains(anchor, forgePos))
                _world.Worksites.Add(new WorksiteRecord(anchor, forgePos, WorksiteKind.Furnace, isActive: true));

            var jobId = new JobId(baseId * 10UL + 3UL);
            if (!_world.Jobs.Contains(jobId))
            {
                _world.Jobs.Add(new JobRequest(
                    jobId,
                    EmberCrpg.Data.Recipes.ProductionRecipeRegistry.SmeltIronIngotId,
                    anchor,
                    forgePos,
                    WorksiteKind.Furnace,
                    JobKind.Smith,
                    JobPriority.Active(1),
                    quantity: 1,
                    requesterId: anchor.Value == 0UL ? new ActorId(1UL) : new ActorId(anchor.Value)));
            }

            // The live production loop currently consumes from world.PlayerInventory. Seed the exact
            // recipe inputs there so the claimed smelting job can actually start and produce output.
            EnsureInventoryContains(
                _world.PlayerInventory ?? (_world.PlayerInventory = new InventoryState(10)),
                new ItemId(9_000_000UL + baseId * 10UL + 4UL),
                "iron_ore",
                "Iron Ore",
                requiredQuantity: 2);
            EnsureInventoryContains(
                _world.PlayerInventory,
                new ItemId(9_000_000UL + baseId * 10UL + 5UL),
                "fuel",
                "Fuel",
                requiredQuantity: 1);
        }

        private void HydrateFactions(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated)
        {
            _world.Factions = new FactionStore();
            foreach (var faction in generated.Factions)
                _world.Factions.Add(faction);

            foreach (var relation in generated.FactionRelations)
                _world.Factions.WithReputation(relation.FactionA, relation.FactionB, relation.Reputation);

            if (!StartingFaction.IsEmpty)
            {
                foreach (var faction in generated.Factions)
                {
                    if (faction.Id.Equals(StartingFaction)) continue;
                    _world.Factions.WithReputation(StartingFaction, faction.Id, new FactionReputation(15));
                }
            }
        }

        private void HydrateNpcs(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated)
        {
            if (_world.Actors == null) _world.Actors = new ActorStore();
            _world.NpcSeeds = generated.Npcs.ToList();
            foreach (var npc in generated.Npcs)
            {
                var actorId = new ActorId(GeneratedNpcActorOffset + npc.Id.Value);
                if (_world.Actors.Contains(actorId)) continue;

                // LIVING WORLD: every NPC gets a HOME cell (spread across its settlement) + a daytime ANCHOR
                // (near the settlement centre) so ScheduleSystem walks it to work/anchor by day and home by
                // night, plus a JOB PREFERENCE from its worldgen role so blacksmiths smith and farmers farm
                // where a matching worksite exists. Spawns at home rather than all stacked on the centre tile.
                var siteId = SettlementSiteId(npc.Home);
                var home = HomeCellFor(siteId, npc.Id.Value);
                var dayAnchor = DayAnchorFor(siteId, npc.Id.Value);

                var actor = new ActorRecord(
                    actorId,
                    npc.Name,
                    ToActorRole(npc.Role),
                    StatsFor(npc.Role),
                    VitalsFor(npc.Role),
                    home,
                    accuracy: npc.Role == NpcRole.Guard || npc.Role == NpcRole.Outlaw ? 55 : 35,
                    dodge: npc.Role == NpcRole.Outlaw ? 55 : 30,
                    armor: npc.Role == NpcRole.Guard ? 12 : 4,
                    baseDamage: npc.Role == NpcRole.Outlaw ? 10 : 4,
                    topicIds: new[] { "rumors", "work", "trade" },
                    home: home,
                    dayAnchor: dayAnchor);

                var jobKind = NpcRoleJobMapper.ToJobKind(npc.Role);
                if (jobKind.HasValue)
                    actor.ApplyJobPreferences(new[] { new ActorJobPreference(jobKind.Value, JobPriority.Active(1)) });

                _world.Actors.Add(actor);
            }

            GrantStartingJobPreference();
        }

        // A deterministic HOME cell spread across the NPC's home-settlement site, so the crowd doesn't stack on
        // one tile and each NPC has its own place to return to at night.
        private GridPosition HomeCellFor(SiteId siteId, ulong npcId)
        {
            if (_world.Sites != null && _world.Sites.TryGet(siteId, out var site))
            {
                int w = System.Math.Max(1, (site.MaxBound.X - site.MinBound.X) + 1);
                int h = System.Math.Max(1, (site.MaxBound.Y - site.MinBound.Y) + 1);
                ulong k = (npcId * 2654435761UL) + 1013904223UL;
                return new GridPosition(site.MinBound.X + (int)(k % (ulong)w), site.MinBound.Y + (int)((k / (ulong)w) % (ulong)h));
            }
            return CenterOfSite(siteId);
        }

        // A daytime gathering anchor near the settlement centre (small per-NPC spread) for NPCs without a
        // claimed production job — so they walk to the "square" by day and home by night.
        private GridPosition DayAnchorFor(SiteId siteId, ulong npcId)
        {
            // A DISTINCT daytime spot per NPC, spread across the WHOLE settlement (a different hash from the home
            // cell), so by day the townsfolk disperse to their own places and walk there - instead of every NPC
            // converging on the centre into one frozen clump.
            if (_world.Sites != null && _world.Sites.TryGet(siteId, out var site))
            {
                int w = System.Math.Max(1, (site.MaxBound.X - site.MinBound.X) + 1);
                int h = System.Math.Max(1, (site.MaxBound.Y - site.MinBound.Y) + 1);
                ulong k = (npcId * 40503UL) + 2246822519UL;
                return new GridPosition(site.MinBound.X + (int)(k % (ulong)w), site.MinBound.Y + (int)((k / (ulong)w) % (ulong)h));
            }
            return CenterOfSite(siteId);
        }

        /// <summary>
        /// SOUL-01: give exactly one deterministic worker an active Smith preference so the pending
        /// smelting job seeded in <see cref="SeedStartingProductionSites"/> actually gets claimed and
        /// worked by JobAssignmentSystem in the live game. The first generated NPC homed at the
        /// starting settlement is chosen; falls back to the first NPC overall. Idle, alive NPCs only.
        /// </summary>
        private void GrantStartingJobPreference()
        {
            if (_world.Actors == null) return;

            ActorRecord worker = null;
            GridPosition preferredSmithPosition = default;
            var hasPreferredPosition = false;
            var preferredHomePosition = default(GridPosition);
            if (TryGetStartingProductionAnchor(out var anchor, out var anchorSite))
            {
                preferredHomePosition = CenterOfSite(anchor);
                preferredSmithPosition = anchorSite.MinBound.Translate(1, 1);
                hasPreferredPosition = true;
            }

            foreach (var actor in _world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive || actor.Id.Value < GeneratedNpcActorOffset)
                    continue;
                if (worker == null)
                    worker = actor; // deterministic fallback: first generated NPC, by insertion order
                if (hasPreferredPosition && actor.Position.Equals(preferredHomePosition))
                {
                    worker = actor;
                    break;
                }
            }

            if (worker == null)
                return;

            worker.ApplyJobPreferences(new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });
            if (hasPreferredPosition)
                worker.MoveTo(preferredSmithPosition);
        }

        private bool TryGetStartingProductionAnchor(out SiteId anchor, out SiteRecord anchorSite)
        {
            anchorSite = default; // CS0177: assign on every path; the success branch overwrites via TryGet below.
            anchor = StartingSettlement.IsEmpty ? default : SettlementSiteId(StartingSettlement);
            if (anchor.IsEmpty || !_world.Sites.TryGet(anchor, out _))
            {
                foreach (var site in _world.Sites.Records)
                {
                    if (site.Kind == SiteKind.Settlement)
                    {
                        anchor = site.Id;
                        break;
                    }
                }
            }

            if (anchor.IsEmpty || !_world.Sites.TryGet(anchor, out _))
                anchor = FirstSiteId();

            return !anchor.IsEmpty && _world.Sites.TryGet(anchor, out anchorSite);
        }

        private static void EnsureInventoryContains(
            InventoryState inventory,
            ItemId seedItemId,
            string templateId,
            string displayName,
            int requiredQuantity)
        {
            var existingQuantity = 0;
            foreach (var item in inventory.Items)
            {
                if (!item.IsEquipment && string.Equals(item.TemplateId, templateId, System.StringComparison.Ordinal))
                    existingQuantity += item.Quantity;
            }

            if (existingQuantity >= requiredQuantity)
                return;

            if (!inventory.TryAdd(new InventoryItem(seedItemId, templateId, displayName, requiredQuantity - existingQuantity)))
                throw new System.InvalidOperationException($"Starting inventory could not accept required smithing input {templateId}.");
        }

        private void HydrateHistory(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated)
        {
            if (_world.Events == null) _world.Events = new WorldEventLog();
            var fallbackSite = StartingSettlement.IsEmpty ? FirstSiteId() : SettlementSiteId(StartingSettlement);
            var minYear = generated.History.Count == 0 ? 0 : generated.History.Min(history => history.Year);
            foreach (var history in generated.History)
            {
                _world.Events.Append(new WorldEvent(
                    new GameTime((long)(history.Year - minYear) * GameTime.MinutesPerYear),
                    ToRuntimeEventKind(history.Kind),
                    default,
                    fallbackSite,
                    history.Detail));
            }
        }

        private void MovePlayerToStartingSettlement()
        {
            if (StartingSettlement.IsEmpty || _world.Actors == null) return;
            if (!_world.Actors.TryFirstByRole(ActorRole.Player, out var player) || player == null) return;
            player.MoveTo(CenterOfSite(SettlementSiteId(StartingSettlement)));
        }

        private void SeedStartingQuest()
        {
            var quest = QuestCatalog.ForgeIronIngot();
            _world.Quests ??= new QuestStore();
            if (_world.Quests.Contains(quest.Id))
                return;

            _world.Quests.Add(quest.Id, new QuestState(quest.Tasks.Count, _world.Time));
        }

        private GridPosition CenterOfSite(SiteId siteId)
        {
            if (_world.Sites != null && _world.Sites.TryGet(siteId, out var site))
                return CenterOf(site);
            return default;
        }

        private SiteId FirstSiteId()
        {
            if (_world.Sites != null)
            {
                foreach (var site in _world.Sites.Records)
                    return site.Id;
            }
            return new SiteId(1UL);
        }

        private static SiteId RegionSiteId(RegionId id) => new SiteId(RegionSiteOffset + id.Value);
        private static SiteId SettlementSiteId(SettlementId id) => new SiteId(SettlementSiteOffset + id.Value);

        private static ActorRole ToActorRole(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Merchant: return ActorRole.Merchant;
                case NpcRole.Guard: return ActorRole.Guard;
                case NpcRole.Outlaw: return ActorRole.Enemy;
                default: return ActorRole.Talker;
            }
        }

        private static EmberStatBlock StatsFor(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Guard: return new EmberStatBlock(55, 45, 55, 30, 35, 35);
                case NpcRole.Merchant: return new EmberStatBlock(35, 40, 35, 45, 45, 60);
                case NpcRole.Scholar: return new EmberStatBlock(25, 35, 35, 70, 60, 45);
                case NpcRole.Outlaw: return new EmberStatBlock(45, 60, 40, 35, 55, 40);
                default: return new EmberStatBlock(35, 35, 40, 35, 40, 45);
            }
        }

        private static ActorVitals VitalsFor(NpcRole role)
        {
            var stats = StatsFor(role);
            return new ActorVitals(
                new VitalStat(20 + stats.End / 2, 20 + stats.End / 2),
                new VitalStat(20 + stats.Mig / 2, 20 + stats.Mig / 2),
                new VitalStat(10 + stats.Mnd / 2, 10 + stats.Mnd / 2));
        }

        private static WorldEventKind ToRuntimeEventKind(WorldHistoryKind kind)
        {
            switch (kind)
            {
                case WorldHistoryKind.FactionWar:
                case WorldHistoryKind.FactionAlliance:
                    return WorldEventKind.FactionReputationChanged;
                case WorldHistoryKind.TradeRouteOpened:
                    return WorldEventKind.TradeCompleted;
                case WorldHistoryKind.Calamity:
                    return WorldEventKind.ShortageDetected;
                default:
                    return WorldEventKind.StorytellerCheckpoint;
            }
        }

        private static WorldStyle ParseStyle(string mood)
        {
            return WorldGenesisMapper.ToStyle(mood);
        }

        private static WorldGenre ParseGenre(string mood, string calling, string startLocation)
        {
            return WorldGenesisMapper.ToGenre(mood, calling, startLocation);
        }

        private static SettlementSize ParsePreferredSettlementSize(string startLocation)
        {
            return WorldGenesisMapper.ToPreferredSettlementSize(startLocation);
        }

        private static uint FoldSeed(string mood, string calling, string startLocation)
        {
            // FNV-1a-32 over the three strings concatenated with a unit
            // separator so "ab|c" and "a|bc" do not fold to the same seed.
            const uint Prime = 16777619u;
            uint hash = 2166136261u;
            FoldString(ref hash, mood, Prime);
            FoldString(ref hash, "", Prime);
            FoldString(ref hash, calling, Prime);
            FoldString(ref hash, "", Prime);
            FoldString(ref hash, startLocation, Prime);
            // Avoid the XorShiftRng zero-seed reroute by nudging the result
            // when it lands on 0 — preserves determinism (the same inputs
            // still fold to the same seed) without losing entropy.
            if (hash == 0u) hash = 2463534242u;
            return hash;
        }

        private static void FoldString(ref uint hash, string s, uint prime)
        {
            if (s == null) return;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= prime;
            }
        }
    }
}
