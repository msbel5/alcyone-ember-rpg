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
        private void HydrateGeneratedWorld(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated, SettlementSize preferredSize)
        {
            HydrateSites(generated);
            HydrateFactions(generated);
            HydrateNpcs(generated);
            HydrateHistory(generated);
            SeedWorldQuests(); // F2/quest variety: kill + visit quests join the forge errand
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

                // COORDINATE MERGE (F1, architectural debt paid): the settlement site sits at its overland
                // tile's WORLD offset (tile × 40km, centred in the tile) instead of a compact (i%32)*12m
                // grid where every town overlapped every other. The domain grid and the walkable world now
                // share one coordinate space — cross-city grid distances are real metres, NPCs inherit their
                // town's true position via the site bounds, and the residency filter stops being load-bearing.
                int x, y;
                if (settlement.HasTilePosition)
                {
                    x = (settlement.TileX * 40000) + 20000;
                    y = (settlement.TileY * 40000) + 20000;
                }
                else
                {
                    x = (i % 32) * 12; // legacy worlds without tile data keep the old compact layout
                    y = (i / 32) * 12;
                }
                // Site spans the whole town (~1 cell ≈ 1 m) so NPC homes/day-spots spread across the settlement
                // and align with the building ring (8-24 m), instead of clumping inside a 2-6 m dot at the centre.
                int radius = settlement.Size == SettlementSize.Capital ? 28 : settlement.Size == SettlementSize.City ? 24 : settlement.Size == SettlementSize.Town ? 18 : 14;
                _world.Sites.Add(new SiteRecord(id, SiteKind.Settlement, settlement.Name, new GridPosition(x, y), new GridPosition(x + radius, y + radius)));

                // OYNANABILIRLIK: every settlement eats. Without a local larder the consumption
                // loop was invisible in play — the only wheat pile sat at a far-off anchor site,
                // so no generated town's civilians could ever reach a meal.
                var larder = new StockpileComponent(id);
                larder.Add("wheat", 150);
                _world.Stockpiles.Add(larder);
            }

            SeedStartingProductionSites();
        }

        private void HydrateFactions(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated)
        {
            _world.Factions = new FactionStore();
            // OYNANABILIRLIK: RuntimeHistorySystem steers relations along the law/craft/trade
            // axes; generated factions carry no such tags, which silently killed runtime history
            // in production. Guarantee the axes by tagging the first three generated factions.
            string[] axisTags = { "craft", "trade", "law" };
            for (int i = 0; i < generated.Factions.Count; i++)
            {
                var faction = generated.Factions[i];
                if (i < axisTags.Length && !faction.HasTag(axisTags[i]))
                    faction = new FactionRecord(faction.Id, faction.Name,
                        faction.Tags.Concat(new[] { axisTags[i] }));
                _world.Factions.Add(faction);
            }

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

    }
}
