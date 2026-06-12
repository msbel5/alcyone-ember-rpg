using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // Synthetic haunter NpcIds live FAR above worldgen's sequential ids (a planet seeds ~750) so the
        // two id spaces can never collide; the offset is also what makes EnsureDungeonHaunters idempotent.
        private const ulong HaunterNpcIdBase = 9_000_000UL;

        /// <summary>
        /// F10 ("savaşamadım" — the F2 haunters debt): the CURRENT settlement, when it is a Dungeon, gets
        /// two Outlaw residents pinned to the chamber spots the director passes in (home == dayAnchor ==
        /// chamber, so ScheduleSystem never walks them out). They ride the EXISTING paths end to end:
        /// GetSpawnableActors projects them, the spawner billboards them, E binds TryBeginWorldEncounter.
        /// Synthesized presentation-side instead of in worldgen so planet/NPC goldens stay untouched.
        /// Idempotent per settlement (deterministic ids); a felled haunter's corpse-actor blocks respawn,
        /// and both seed + actor persist through WorldSaveMapper like any other NPC.
        /// </summary>
        public int EnsureDungeonHaunters(UnityEngine.Vector3 chamberSpotA, UnityEngine.Vector3 chamberSpotB)
        {
            var here = CurrentSettlementOrStart;
            var map = _world?.Overland;
            if (map == null || _world.Actors == null || _world.NpcSeeds == null) return 0;

            string settlementName = null;
            bool isDungeon = false;
            for (int i = 0; i < map.Settlements.Count; i++)
            {
                if (!map.Settlements[i].Id.Equals(here)) continue;
                isDungeon = map.Settlements[i].Kind == EmberCrpg.Domain.Overland.SettlementKind.Dungeon;
                settlementName = map.Settlements[i].Name;
                break;
            }
            if (!isDungeon || string.IsNullOrEmpty(settlementName)) return 0;

            var faction = _world.NpcSeeds.Count > 0 ? _world.NpcSeeds[0].Faction : new FactionId(1UL);
            var origin = BillboardOrigin();
            var spots = new[] { chamberSpotA, chamberSpotB };
            var titles = new[] { "Haunter of ", "Stalker of " };
            int created = 0;
            for (int k = 0; k < spots.Length; k++)
            {
                var npcId = new NpcId(HaunterNpcIdBase + (here.Value * 2UL) + (ulong)k);
                var actorId = new ActorId(GeneratedNpcActorOffset + npcId.Value);
                if (_world.Actors.Contains(actorId)) continue; // alive or corpse — never duplicate

                bool seedExists = false;
                for (int i = 0; i < _world.NpcSeeds.Count; i++)
                    if (_world.NpcSeeds[i] != null && _world.NpcSeeds[i].Id.Equals(npcId)) { seedExists = true; break; }
                if (!seedExists)
                    _world.NpcSeeds.Add(new NpcSeedRecord(
                        npcId, here, faction, titles[k] + settlementName, 980, NpcRole.Outlaw));

                // ProjectActor world = grid - origin, 1 unit = 1 cell → grid = origin + rounded world XZ.
                var grid = new GridPosition(
                    origin.X + UnityEngine.Mathf.RoundToInt(spots[k].x),
                    origin.Y + UnityEngine.Mathf.RoundToInt(spots[k].z));
                var actor = new ActorRecord(
                    actorId,
                    titles[k] + settlementName,
                    ToActorRole(NpcRole.Outlaw),
                    StatsFor(NpcRole.Outlaw),
                    VitalsFor(NpcRole.Outlaw),
                    grid,
                    accuracy: 30, // matches the v0.3 outlaw rebalance in HydrateNpcs (50-base hit curve)
                    dodge: 20,
                    armor: 4,
                    baseDamage: 10,
                    topicIds: new[] { "rumors", "work", "trade" },
                    home: grid,
                    dayAnchor: grid);
                _world.Actors.Add(actor);
                created++;
            }
            return created;
        }
    }
}
