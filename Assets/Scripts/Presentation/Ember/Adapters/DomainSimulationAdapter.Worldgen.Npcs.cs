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
                    // v0.3 rebalance for the 50-base hit curve: vs the fresh player (acc 18 / dodge 12)
                    // an outlaw is hit ~48% (50+18-20) and lands ~68% (50+30-12) — dangerous, not a wall.
                    // The old 55/55 pinned the player to the hit floor ("nadiren vuruyorum").
                    accuracy: npc.Role == NpcRole.Guard ? 45 : npc.Role == NpcRole.Outlaw ? 30 : 35,
                    dodge: npc.Role == NpcRole.Outlaw ? 20 : 30,
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

    }
}
