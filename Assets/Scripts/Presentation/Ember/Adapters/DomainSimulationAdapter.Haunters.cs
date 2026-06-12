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
            // Compat shim (tests + single-chamber callers): two dwellers, no boss.
            return EnsureDungeonDwellers(
                new System.Collections.Generic.List<UnityEngine.Vector3> { chamberSpotA, chamberSpotB },
                bossSpot: null);
        }

        /// <summary>F18: populate the multi-room delve — 0-2 Outlaw dwellers per room (spots from the
        /// realize step) + ONE boss (2× health, 1.5× damage) guarding the hoard. Deterministic ids
        /// (settlement*16 + index) keep it idempotent; corpses persist and block respawn.</summary>
        public int EnsureDungeonDwellers(
            System.Collections.Generic.IReadOnlyList<UnityEngine.Vector3> spots, UnityEngine.Vector3? bossSpot)
        {
            var here = CurrentSettlementOrStart;
            var map = _world?.Overland;
            if (map == null || _world.Actors == null || _world.NpcSeeds == null || spots == null) return 0;

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
            string[] titles = { "Haunter of ", "Stalker of ", "Lurker of ", "Prowler of " };
            int created = 0;

            int CreateDweller(int slotIndex, UnityEngine.Vector3 world, string name, bool boss)
            {
                var npcId = new NpcId(HaunterNpcIdBase + (here.Value * 16UL) + (ulong)slotIndex);
                var actorId = new ActorId(GeneratedNpcActorOffset + npcId.Value);
                if (_world.Actors.Contains(actorId)) return 0; // alive or corpse — never duplicate

                bool seedExists = false;
                for (int i = 0; i < _world.NpcSeeds.Count; i++)
                    if (_world.NpcSeeds[i] != null && _world.NpcSeeds[i].Id.Equals(npcId)) { seedExists = true; break; }
                if (!seedExists)
                    _world.NpcSeeds.Add(new NpcSeedRecord(npcId, here, faction, name, 980, NpcRole.Outlaw));

                var origin = BillboardOrigin();
                var grid = new GridPosition(
                    origin.X + UnityEngine.Mathf.RoundToInt(world.x),
                    origin.Y + UnityEngine.Mathf.RoundToInt(world.z));
                var baseVitals = VitalsFor(NpcRole.Outlaw);
                var vitals = boss
                    ? new ActorVitals( // F18 boss: 2× health, full pools
                        new VitalStat(baseVitals.Health.Max * 2, baseVitals.Health.Max * 2),
                        baseVitals.Fatigue, baseVitals.Mana)
                    : baseVitals;
                var actor = new ActorRecord(
                    actorId,
                    name,
                    ToActorRole(NpcRole.Outlaw),
                    StatsFor(NpcRole.Outlaw),
                    vitals,
                    grid,
                    accuracy: boss ? 38 : 30, // v0.3 outlaw rebalance baseline; the boss reads meaner
                    dodge: 20,
                    armor: boss ? 6 : 4,
                    baseDamage: boss ? 15 : 10, // 1.5× boss damage
                    topicIds: new[] { "rumors", "work", "trade" },
                    home: grid,
                    dayAnchor: grid);
                _world.Actors.Add(actor);
                return 1;
            }

            int cap = System.Math.Min(spots.Count, 9);
            for (int k = 0; k < cap; k++)
                created += CreateDweller(k, spots[k], titles[k % titles.Length] + settlementName, boss: false);
            if (bossSpot.HasValue)
                created += CreateDweller(15, bossSpot.Value, "Warden of " + settlementName, boss: true);
            return created;
        }
    }
}
