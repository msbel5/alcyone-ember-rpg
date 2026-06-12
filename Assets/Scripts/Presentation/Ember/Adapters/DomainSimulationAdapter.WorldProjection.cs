using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Views;
using WorldEventInterest = EmberCrpg.Presentation.Visual.WorldEventInterest;
using WorldEventRow = EmberCrpg.Presentation.Visual.WorldEventRow;
using WorldEventTailSnapshot = EmberCrpg.Presentation.Visual.WorldEventTailSnapshot;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // Why: reuse the shared snapshot projection so host/UI read the exact same deterministic tail rows.
        public IReadOnlyList<WorldEventRow> RecentWorldEvents(int maxRows)
        {
            return WorldEventTailSnapshot.FromLog(_world?.Events, maxRows, WorldEventInterest.IsHudWorthy).Rows;
        }

        public bool TryReadActor(string actorName, out ActorViewState state)
        {
            state = default;
            if (string.IsNullOrEmpty(actorName) || _world.Actors == null) return false;
            foreach (var actor in _world.Actors.Records)
            {
                if (string.Equals(actor.Name, actorName, System.StringComparison.Ordinal))
                {
                    state = ProjectActor(actor);
                    return true;
                }
            }
            return false;
        }

        // SOUL-04: id-keyed read so the host can sync a billboard from the actor's stable id and see
        // SOUL-03 (ScheduleSystem) grid movement without depending on name uniqueness.
        public bool TryReadActor(ActorId id, out ActorViewState state)
        {
            state = default;
            if (id.IsEmpty || _world.Actors == null) return false;
            if (!_world.Actors.TryGet(id, out var actor) || actor == null) return false;
            state = ProjectActor(actor);
            return true;
        }

        // Billboard grid->world origin: FIXED at the player's STARTING-settlement centre. WorldSceneDirector
        // realises that one settlement with the player rig + building ring sitting at world (0,0,0), but NPC
        // grid positions live at their raw site coords ((i%32)*12, (i/32)*12) — tens of metres away. Without
        // this offset every NPC projected into a distant clump that the player saw "cluster and do nothing";
        // subtracting the starting-site centre maps it to world origin so the crowd surrounds the plaza. The
        // origin is fixed (NOT player-relative), so NPCs hold their world position as the player walks. Resolved
        // lazily once the site exists (sites are hydrated during SeedWorld); identity (0,0) until then.
        private EmberCrpg.Domain.Actors.GridPosition _billboardOrigin;
        private bool _billboardOriginResolved;

        private EmberCrpg.Domain.Actors.GridPosition BillboardOrigin()
        {
            if (_billboardOriginResolved) return _billboardOrigin;
            // CURRENT settlement (fast travel re-bases the crowd on the destination; TryTravelToSettlement
            // resets _billboardOriginResolved so this re-resolves).
            var anchor = CurrentSettlementOrStart;
            if (!anchor.IsEmpty && _world?.Sites != null)
            {
                var siteId = SettlementSiteId(anchor);
                if (_world.Sites.TryGet(siteId, out _))
                {
                    _billboardOrigin = CenterOfSite(siteId);
                    _billboardOriginResolved = true;
                }
            }
            return _billboardOrigin;
        }

        // Diagnostic hook: the FIXED billboard grid->world origin (starting-settlement centre) that
        // ProjectActor/GetSpawnableActors subtract, so proofs can render every position in the same
        // player-centric frame (an idle NPC then reads home==midday, not a phantom offset jump).
        public EmberCrpg.Domain.Actors.GridPosition BillboardOriginCell() => BillboardOrigin();

        // Single projection so the name and id read paths can never drift: domain grid (X,Y) maps to the
        // world-space XZ plane (Y up stays 0), re-based on the starting-settlement centre (see BillboardOrigin);
        // actors are always visible while alive in the store.
        private ActorViewState ProjectActor(ActorRecord actor)
        {
            var origin = BillboardOrigin();
            return new ActorViewState(
                new UnityEngine.Vector3(actor.Position.X - origin.X, 0f, actor.Position.Y - origin.Y),
                UnityEngine.Quaternion.identity,
                visible: true);
        }

        // SOUL-04 (spawn-from-worldgen): hand the host a flat, Domain-free list of candidate
        // billboards. Reuse the SAME grid->world XZ projection as ProjectActor so a spawned view
        // lands exactly where the per-tick sync will then push it (no first-frame jump). The Player
        // actor is excluded — the player is the rig/camera, not a billboard. Deterministic order
        // (ActorStore.Records is insertion-ordered); the host caps/culls, so we never pre-truncate.
        public IReadOnlyList<SpawnableActor> GetSpawnableActors()
        {
            if (_world.Actors == null) return System.Array.Empty<SpawnableActor>();
            var origin = BillboardOrigin(); // SAME re-basing as ProjectActor, so a spawned billboard lands exactly where the per-tick sync will push it
            // SETTLEMENT-MEMBERSHIP FILTER ("o NPC 11 tile ötedeki şehrin adamı ama burada belirdi"): domain
            // site placement is compact — every town's local grid overlaps in world units — so projecting ALL
            // actors made other cities' people materialize as ghosts in whichever town the player stands.
            // Only residents of the CURRENT settlement spawn here; everyone else stays data until you travel.
            var here = CurrentSettlementOrStart;
            var list = new List<SpawnableActor>();
            foreach (var actor in _world.Actors.Records)
            {
                if (actor == null || actor.Role == ActorRole.Player) continue;
                // F10: the dead don't respawn standing — a felled haunter/outlaw stays a corpse (its
                // record blocks re-creation; its billboard never re-materializes on travel/reload).
                if (!actor.IsAlive) continue;
                var seed = ResolveNpcForActor(actor);
                if (seed != null && !seed.Home.Equals(here)) continue;
                list.Add(new SpawnableActor(
                    actor.Id.Value,
                    actor.Name ?? string.Empty,
                    ResolveGeneratedSpriteRole(actor),
                    actor.Position.X - origin.X,
                    actor.Position.Y - origin.Y,
                    StableSeed(actor.Id.Value)));
            }
            return list;
        }

        private string ResolveGeneratedSpriteRole(ActorRecord actor)
        {
            if (actor == null) return string.Empty;
            // F29 BESTIARY: dungeon dwellers wear their TYPE, not the generic outlaw coat. The name
            // encodes the type ("Fen Wolf of X"); the Warden wears the live delve's apex type.
            var bestiary = EmberCrpg.Simulation.Bestiary.WorldBestiaryCatalog.FromActorName(actor.Name);
            if (bestiary != null) return bestiary.SpriteRole;
            if (actor.Name != null && actor.Name.StartsWith("Warden of", System.StringComparison.Ordinal))
            {
                var apex = EmberCrpg.Simulation.Bestiary.WorldBestiaryCatalog.Find(
                    EmberCrpg.Simulation.Bestiary.WorldBestiaryCatalog.ApexKeyFor(
                        EmberCrpg.Presentation.Ember.WorldDirector.RuntimeDungeonLayoutInfo.ArchetypeName));
                if (apex != null) return apex.SpriteRole;
            }
            if (_world?.NpcSeeds != null && actor.Id.Value >= GeneratedNpcActorOffset)
            {
                var npcId = new EmberCrpg.Domain.Worldgen.NpcId(actor.Id.Value - GeneratedNpcActorOffset);
                var npc = _world.NpcSeeds.FirstOrDefault(candidate => candidate != null && candidate.Id.Equals(npcId));
                if (npc != null)
                    return "npc_" + npc.Role.ToString().ToLowerInvariant();
            }

            switch (actor.Role)
            {
                case ActorRole.Merchant: return "npc_merchant";
                case ActorRole.Guard: return "npc_guard";
                case ActorRole.Enemy: return "npc_bandit";
                case ActorRole.Talker: return "npc_sage";
                default: return "npc_rogue";
            }
        }

        private static int StableSeed(ulong value)
        {
            unchecked
            {
                var folded = value ^ (value >> 32);
                var seed = (int)(folded & 0x7fffffffUL);
                return seed == 0 ? 1 : seed;
            }
        }

        public bool TryReadWorksite(string siteName, out WorksiteViewState state)
        {
            state = default;
            // Codex audit (fifth pass A-P1): previously returned the
            // synthetic `(isActive: true, queueDepth: 0)` for any site
            // name match — the view never reflected the actual worksite
            // store. Now derive isActive from the WorksiteStore and
            // queueDepth from the JobBoard's request count at that site.
            if (string.IsNullOrEmpty(siteName)) return false;
            EmberCrpg.Domain.Core.SiteId siteId = default;
            foreach (var site in _world.Sites.Records)
            {
                if (string.Equals(site.Name, siteName, System.StringComparison.Ordinal))
                {
                    siteId = site.Id;
                    break;
                }
            }
            if (siteId.IsEmpty) return false;

            var worksites = _saveService.Worksites;
            bool isActive = false;
            if (worksites != null)
            {
                foreach (var record in worksites.Records)
                {
                    if (record.SiteId.Equals(siteId) && record.IsActive)
                    {
                        isActive = true;
                        break;
                    }
                }
            }

            int queueDepth = 0;
            var jobs = _saveService.Jobs;
            if (jobs != null)
            {
                foreach (var req in jobs.Requests)
                {
                    if (req.SiteId.Equals(siteId)) queueDepth++;
                }
            }

            state = new WorksiteViewState(isActive: isActive, queueDepth: queueDepth);
            return true;
        }
    }
}
