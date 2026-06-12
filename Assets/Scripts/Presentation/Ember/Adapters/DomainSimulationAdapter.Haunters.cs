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
        // F23: the WATCH's synthetic band — clear of the haunter band (9_000_000 + settlement*16 + slot
        // tops out far below 9_500_000 for real settlement counts).
        private const ulong WatchNpcIdBase = 9_500_000UL;

        /// <summary>F23: crime summons the WATCH. Not every settlement rolls Guard seeds in worldgen,
        /// but "muhafız saldırır" must hold EVERYWHERE — so the first crime in a town synthesizes two
        /// watch officers at the plaza edge (same presentation-side pattern as the dungeon dwellers:
        /// deterministic ids, idempotent, corpses persist, ride the normal spawn/sync/combat paths).
        /// They hunt through TickHostileAi while a bounty stands.</summary>
        public int EnsureWatchOfficers()
        {
            var here = CurrentSettlementOrStart;
            var map = _world?.Overland;
            if (map == null || _world.Actors == null || _world.NpcSeeds == null) return 0;

            string settlementName = null;
            for (int i = 0; i < map.Settlements.Count; i++)
                if (map.Settlements[i].Id.Equals(here)) { settlementName = map.Settlements[i].Name; break; }
            if (string.IsNullOrEmpty(settlementName)) return 0;

            var faction = _world.NpcSeeds.Count > 0 ? _world.NpcSeeds[0].Faction : new FactionId(1UL);
            var origin = BillboardOrigin();
            int created = 0;
            for (int slot = 0; slot < 2; slot++)
            {
                var npcId = new NpcId(WatchNpcIdBase + (here.Value * 4UL) + (ulong)slot);
                var actorId = new ActorId(GeneratedNpcActorOffset + npcId.Value);
                if (_world.Actors.Contains(actorId)) continue; // alive or corpse — never duplicate

                bool seedExists = false;
                for (int i = 0; i < _world.NpcSeeds.Count; i++)
                    if (_world.NpcSeeds[i] != null && _world.NpcSeeds[i].Id.Equals(npcId)) { seedExists = true; break; }
                if (!seedExists)
                    _world.NpcSeeds.Add(new NpcSeedRecord(
                        npcId, here, faction, $"Watch of {settlementName} {(slot == 0 ? "I" : "II")}", 970, NpcRole.Guard));

                var grid = new GridPosition(
                    origin.X + (slot == 0 ? 9 : -8),
                    origin.Y + (slot == 0 ? 7 : -6));
                _world.Actors.Add(new ActorRecord(
                    actorId,
                    $"Watch of {settlementName} {(slot == 0 ? "I" : "II")}",
                    ToActorRole(NpcRole.Guard),
                    StatsFor(NpcRole.Guard),
                    VitalsFor(NpcRole.Guard),
                    grid,
                    accuracy: 45, // worldgen guard baseline
                    dodge: 30,
                    armor: 12,
                    baseDamage: 4,
                    topicIds: new[] { "rumors", "work" },
                    home: grid,
                    dayAnchor: grid));
                created++;
            }
            if (created > 0)
                UnityEngine.Debug.Log($"[Crime] the watch arrives: +{created} officers at {settlementName}.");
            return created;
        }

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

        /// <summary>F18: populate the multi-room delve — 0-2 dwellers per room (spots from the
        /// realize step) + ONE boss (2× health, 1.5× damage) guarding the hoard. Deterministic ids
        /// (settlement*16 + index) keep it idempotent; corpses persist and block respawn.
        /// F29: dwellers are BESTIARY types now — the archetype picks the type rotation (cave runs
        /// beasts, crypt the dead, ruin squatters) and the boss wears the archetype's apex type.</summary>
        public int EnsureDungeonDwellers(
            System.Collections.Generic.IReadOnlyList<UnityEngine.Vector3> spots, UnityEngine.Vector3? bossSpot)
        {
            return EnsureDungeonDwellers(spots, bossSpot, "Mağara"); // compat shim (tests + old callers)
        }

        public int EnsureDungeonDwellers(
            System.Collections.Generic.IReadOnlyList<UnityEngine.Vector3> spots, UnityEngine.Vector3? bossSpot,
            string archetypeName)
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
            int created = 0;

            int CreateDweller(int slotIndex, UnityEngine.Vector3 world,
                EmberCrpg.Simulation.Bestiary.BestiaryEntry entry, string name, bool boss)
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
                // F29: health from the bestiary row; the boss keeps its 2× rule on top.
                int healthMax = boss ? entry.HealthMax * 2 : entry.HealthMax;
                var vitals = new ActorVitals(
                    new VitalStat(healthMax, healthMax), baseVitals.Fatigue, baseVitals.Mana);
                var actor = new ActorRecord(
                    actorId,
                    name,
                    ToActorRole(NpcRole.Outlaw),
                    StatsFor(NpcRole.Outlaw),
                    vitals,
                    grid,
                    accuracy: boss ? entry.Accuracy + 8 : entry.Accuracy,
                    dodge: entry.Dodge,
                    armor: boss ? entry.Armor + 2 : entry.Armor,
                    baseDamage: boss ? (entry.BaseDamage * 3) / 2 : entry.BaseDamage, // 1.5× boss damage
                    topicIds: new[] { "rumors", "work", "trade" },
                    home: grid,
                    dayAnchor: grid);
                _world.Actors.Add(actor);
                UnityEngine.Debug.Log($"[Bestiary] dweller spawned: '{name}' type={entry.Key} " +
                    $"archetype={archetypeName} hp={healthMax} dmg={actor.BaseDamage} boss={boss}");
                return 1;
            }

            int cap = System.Math.Min(spots.Count, 9);
            for (int k = 0; k < cap; k++)
            {
                var entry = EmberCrpg.Simulation.Bestiary.WorldBestiaryCatalog.EntryForSlot(archetypeName, k);
                created += CreateDweller(k, spots[k], entry, entry.DisplayPrefix + settlementName, boss: false);
            }
            if (bossSpot.HasValue)
            {
                // The boss keeps its Warden NAME (quests + proofs bind it by that) but wears the
                // archetype's apex TYPE — the "şef-varyantı": a great wolf in the cave, a wisp in
                // the crypt, a bandit chief in the ruin.
                var apex = EmberCrpg.Simulation.Bestiary.WorldBestiaryCatalog.Find(
                    EmberCrpg.Simulation.Bestiary.WorldBestiaryCatalog.ApexKeyFor(archetypeName));
                created += CreateDweller(15, bossSpot.Value, apex, "Warden of " + settlementName, boss: true);
            }
            return created;
        }
    }
}
