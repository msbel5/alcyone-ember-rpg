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
using EmberCrpg.Simulation.WorldDirector;

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
            // B10 §A6: sim-blocker projection. Wrap-catch is LOAD-BEARING - a broken layout strategy
            // used to silently kill the adapter register (marathon-FAIL 2026-07-26). Over-blocking is
            // a UX regression; a nulled adapter is a crash. Always keep the sim on its feet.
            try { HydrateBlockedCells(generated); }
            catch (System.Exception ex) { UnityEngine.Debug.LogError($"[B10] HydrateBlockedCells failed: {ex.Message}"); }
        }

        /// <summary>
        /// B10 §A6: sim-blocked-cell hydration. For each generated settlement, ask the deterministic
        /// layout strategy for its building plan and project each building's XZ metre box into integer
        /// sim cells around the site centre. Pure integer math against deterministic float inputs, so
        /// the same seed yields the same blocker set every run (chunking-invariance holds).
        ///
        /// CONSTRAINT (docs risk §UX): worksite bench cells are NOT added — the action strip TARGETS
        /// those cells, and blocking them would freeze the worker on approach. Building footprints only.
        /// DERIVED — never serialized; rehydrated after every load via the same call path.
        /// </summary>
        private void HydrateBlockedCells(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated)
        {
            if (_world == null || _world.Sites == null || generated == null) return;
            _world.Blocked ??= new BlockedCellSet();
            _world.Blocked.Clear(); // idempotent — re-realize / reload must not accumulate stale cells.

            foreach (var settlement in generated.Settlements)
            {
                if (settlement == null) continue;
                var siteId = SettlementSiteId(settlement.Id);
                if (!_world.Sites.TryGet(siteId, out var site) || site == null) continue;

                var kind = SettlementSizeToKind(settlement.Size);
                // Deterministic per-settlement seed: SettlementId is unique + stable, so the layout
                // never differs between runs. Non-zero (SettlementContext expects seed > 0 conceptually).
                uint seed = unchecked((uint)((settlement.Id.Value * 2654435761UL + 17UL) | 1UL));
                var context = new SettlementContext(settlement.Name ?? string.Empty, kind,
                    EmberCrpg.Domain.Overland.BiomeKind.Plains, seed);
                SettlementLayout layout;
                try { layout = SettlementLayoutStrategyFactory.For(kind).Plan(context); }
                catch { continue; } // a broken strategy for one settlement must not kill hydration.
                if (layout?.Buildings == null) continue;

                // Site centre in sim-grid coords (site.MinBound is the origin corner; MaxBound is
                // the opposite corner). Buildings are LOCAL metre offsets centred at (0,0).
                int centreX = (site.MinBound.X + site.MaxBound.X) / 2;
                int centreY = (site.MinBound.Y + site.MaxBound.Y) / 2;

                foreach (var b in layout.Buildings)
                {
                    // XZ metre box → integer cell rectangle. Floor/Ceil so partial-cell overlaps
                    // still block (safer to over-block than to leave a walkable slit through a wall).
                    int xMin = (int)System.Math.Floor(centreX + b.OriginX - (b.SizeX * 0.5f));
                    int xMax = (int)System.Math.Ceiling(centreX + b.OriginX + (b.SizeX * 0.5f));
                    int yMin = (int)System.Math.Floor(centreY + b.OriginZ - (b.SizeZ * 0.5f));
                    int yMax = (int)System.Math.Ceiling(centreY + b.OriginZ + (b.SizeZ * 0.5f));
                    for (int y = yMin; y < yMax; y++)
                        for (int x = xMin; x < xMax; x++)
                            _world.Blocked.Add(new GridPosition(x, y));
                }
            }
        }

        // SettlementSize → SettlementKind: deterministic, and independent of Overland (which is set
        // AFTER hydration). Capital falls to City for layout purposes (both use the Streets strategy).
        private static EmberCrpg.Domain.Overland.SettlementKind SettlementSizeToKind(SettlementSize size)
        {
            switch (size)
            {
                case SettlementSize.Capital:
                case SettlementSize.City: return EmberCrpg.Domain.Overland.SettlementKind.City;
                case SettlementSize.Town: return EmberCrpg.Domain.Overland.SettlementKind.Town;
                case SettlementSize.Village: return EmberCrpg.Domain.Overland.SettlementKind.Village;
                case SettlementSize.Hamlet: return EmberCrpg.Domain.Overland.SettlementKind.Hamlet;
                default: return EmberCrpg.Domain.Overland.SettlementKind.Village;
            }
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
            string[] axisTags =
            {
                EmberCrpg.Simulation.World.RuntimeHistorySystem.CraftTag,
                EmberCrpg.Simulation.World.RuntimeHistorySystem.TradeTag,
                EmberCrpg.Simulation.World.RuntimeHistorySystem.LawTag,
            };
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
