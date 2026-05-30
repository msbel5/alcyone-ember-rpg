// DomainSimulationAdapter — worldgen / character-creation / world-hydration (partial-class split, ARCH-02 structural / LOC).
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        public void SeedWorld(string mood, string calling, string startLocation)
        {
            // Derive a deterministic uint seed from the three wizard strings
            // by FNV-1a-folding their concatenation. The same wizard inputs
            // therefore always produce the same world, which is what makes
            // "share your seed" a viable replay feature down the line.
            uint seed = FoldSeed(mood, calling, startLocation);
            var style = ParseStyle(mood);
            var genre = ParseGenre(mood, calling, startLocation);
            var preferredSize = ParsePreferredSettlementSize(startLocation);
            var parameters = EmberCrpg.Simulation.Worldgen.WorldgenParameters.For(style, genre);

            var generated = EmberCrpg.Simulation.Worldgen.WorldgenService.Generate(
                seed,
                parameters);
            GeneratedWorld = generated;
            StartingRegion = SelectStartingRegion(generated, startLocation);
            StartingSettlement = SelectStartingSettlement(generated, preferredSize, startLocation);
            StartingFaction = SelectStartingFaction(generated, calling);

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
                int radius = settlement.Size == SettlementSize.Capital ? 6 : settlement.Size == SettlementSize.City ? 5 : settlement.Size == SettlementSize.Town ? 3 : 2;
                _world.Sites.Add(new SiteRecord(id, SiteKind.Settlement, settlement.Name, new GridPosition(x, y), new GridPosition(x + radius, y + radius)));
            }
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
                var position = CenterOfSite(SettlementSiteId(npc.Home));
                _world.Actors.Add(new ActorRecord(
                    actorId,
                    npc.Name,
                    ToActorRole(npc.Role),
                    StatsFor(npc.Role),
                    VitalsFor(npc.Role),
                    position,
                    accuracy: npc.Role == NpcRole.Guard || npc.Role == NpcRole.Outlaw ? 55 : 35,
                    dodge: npc.Role == NpcRole.Outlaw ? 55 : 30,
                    armor: npc.Role == NpcRole.Guard ? 12 : 4,
                    baseDamage: npc.Role == NpcRole.Outlaw ? 10 : 4,
                    topicIds: new[] { "rumors", "work", "trade" }));
            }
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
            var text = (mood ?? string.Empty).ToLowerInvariant();
            if (text.Contains("grim") || text.Contains("dark") || text.Contains("bleak")) return WorldStyle.DarkFantasyGrim;
            if (text.Contains("high") || text.Contains("tolkien") || text.Contains("heroic")) return WorldStyle.HighFantasyTolkien;
            if (text.Contains("steam") || text.Contains("industrial") || text.Contains("revolution")) return WorldStyle.SteampunkRevolution;
            if (text.Contains("ancient") || text.Contains("myth") || text.Contains("bronze")) return WorldStyle.AncientMythology;
            return WorldStyle.LowFantasyMorrowind;
        }

        private static WorldGenre ParseGenre(string mood, string calling, string startLocation)
        {
            var text = ((mood ?? string.Empty) + " " + (calling ?? string.Empty) + " " + (startLocation ?? string.Empty)).ToLowerInvariant();
            if (text.Contains("politic") || text.Contains("diplomat") || text.Contains("court") || text.Contains("noble")) return WorldGenre.PoliticalIntrigue;
            if (text.Contains("monster") || text.Contains("hunt") || text.Contains("beast")) return WorldGenre.MonsterHunt;
            if (text.Contains("merchant") || text.Contains("trade") || text.Contains("caravan") || text.Contains("smith")) return WorldGenre.MerchantEmpire;
            if (text.Contains("pilgrim") || text.Contains("shrine") || text.Contains("temple") || text.Contains("priest")) return WorldGenre.Pilgrimage;
            return WorldGenre.Survival;
        }

        private static SettlementSize ParsePreferredSettlementSize(string startLocation)
        {
            var text = (startLocation ?? string.Empty).ToLowerInvariant();
            if (text.Contains("capital")) return SettlementSize.Capital;
            if (text.Contains("city")) return SettlementSize.City;
            if (text.Contains("hamlet")) return SettlementSize.Hamlet;
            if (text.Contains("village") || text.Contains("farm")) return SettlementSize.Village;
            return SettlementSize.Town;
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
