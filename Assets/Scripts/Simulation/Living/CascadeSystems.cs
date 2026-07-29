using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living.Actions;

// CAN SUYU H3: EVENT CASCADES. Until now the only reactive behavior lived in the presentation
// adapter, ran on the render pump, and hunted ONLY the player — NPC-vs-NPC was impossible and
// no event ever caused another. These two systems move predation into the SIMULATION and give
// the world its first chain: a hunter mauls a civilian (CombatResolved) → nearby civilians SEE
// it (WitnessRecorded + a real ActorMemory entry — NpcMemory's first runtime writer) → the
// watch CONVERGES and strikes back (GuardResponded → CombatResolved). Deterministic, stateless
// step instances (the H1 lesson), pure Simulation.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Compatibility home for predation constants and site lookup.
    /// Autonomous movement/strike now lives only in the action lifecycle.</summary>
    public sealed class PredationSystem
    {
        public const int HuntRadius = 6;
        public const int StrikeReach = CombatOperations.StrikeReach;

        internal static SiteId FallbackSite(WorldState world, GridPosition position)
        {
            if (world.Sites?.Records != null)
                foreach (var site in world.Sites.Records)
                    if (site != null && site.Contains(position)) return site.Id;
            return FallbackSite(world);
        }

        internal static SiteId FallbackSite(WorldState world)
        {
            if (world.Sites?.Records != null)
                foreach (var site in world.Sites.Records)
                    if (site != null) return site.Id;
            return new SiteId(1UL);
        }
    }

    /// <summary>Hourly: last hour's NPC attacks get WITNESSED (real ActorMemory writes — the
    /// store's first runtime writer) and the watch converges on the attacker.</summary>
    public sealed class WitnessResponseSystem
    {
        public const int WitnessRadius = 8;
        public const int ResponseRadius = 12;

        public int Tick(WorldState world) => Tick(world, world?.Time ?? default);

        // Catchup contract: the witness window derives from the boundary stamp, and the
        // scan may NOT early-break — under multi-day catchup the log is not stamp-monotone
        // (hourly crossings append before daily crossings back-fill earlier stamps).
        public int Tick(WorldState world, GameTime stamp)
        {
            if (world?.Actors == null || world.Events == null || world.NpcMemory == null) return 0;
            long windowStart = stamp.TotalMinutes - 60;
            int recorded = 0;

            // Stateless scan of the LAST HOUR only (hourly cadence → each event seen once).
            // REVIEW FIX (O(history) growth): the full-log rescan cost grows without bound
            // (~50k events in a 6-minute live run). Window scans are depth-capped — 4096 covers
            // any real hour plus catchup interleaving; per-hour volume is ~500 in production.
            var events = world.Events.Events;
            int scanFloor = System.Math.Max(0, events.Count - 4096);
            for (int i = events.Count - 1; i >= scanFloor; i--)
            {
                var evt = events[i];
                if (evt.Tick.TotalMinutes <= windowStart || evt.Tick.TotalMinutes > stamp.TotalMinutes) continue;
                if (evt.Kind != WorldEventKind.CombatResolved) continue;
                if (!world.Actors.TryGet(evt.ActorId, out var attacker) || attacker == null) continue;
                if (attacker.Role != ActorRole.Enemy) continue; // player brawls are the bounty system's beat

                var witnessedThisEvent = false;
                foreach (var witness in world.Actors.Records)
                {
                    if (witness == null || !witness.IsAlive) continue;
                    if (witness.Role == ActorRole.Enemy || witness.Role == ActorRole.Player) continue;
                    if (witness.Id.Equals(evt.ActorId)) continue;
                    if (witness.Position.ChebyshevDistanceTo(attacker.Position) > WitnessRadius) continue;

                    var witnessMemory = world.NpcMemory.GetOrCreate(witness.Id);
                    witnessMemory.RecordEvent(new InteractionEvent(
                        stamp, "witnessed_attack", attacker.Id, "predation", string.Empty, 0, witness.Position));
                    world.Events.Append(new WorldEvent(stamp, WorldEventKind.WitnessRecorded,
                        witness.Id, evt.SiteId, $"witnessed attacker:{attacker.Id.Value}"));
                    recorded++;
                    witnessedThisEvent = true;
                }

                // The event scanner records facts only. ReportCrimeAdvancer later performs the
                // movement and arms a pursuit through the single action writer.
                if (witnessedThisEvent)
                    RaiseUnrest(world, evt.SiteId, 2, stamp, attacker.Id.Value);
            }
            return recorded;
        }

        /// <summary>P2 (DFU LegalRep-lite): raise the site's crime pressure; past the threshold
        /// the WHOLE watch of that settlement sweeps - every guard arms a pursuit at once, a
        /// chronicle line lands, and the ledger resets to a wary simmer.</summary>
        public const int SweepThreshold = 6;
        public const long SweepCooldownMinutes = 1440; // one sweep per site per game day
        private static void RaiseUnrest(WorldState world, SiteId siteId, int amount, GameTime stamp, ulong attackerId)
        {
            if (siteId.IsEmpty) return;
            world.SiteUnrest ??= new System.Collections.Generic.List<SiteUnrestRecord>();
            SiteUnrestRecord row = null;
            foreach (var candidate in world.SiteUnrest)
                if (candidate.SiteId.Equals(siteId)) { row = candidate; break; }
            if (row == null)
            {
                row = new SiteUnrestRecord { SiteId = siteId };
                world.SiteUnrest.Add(row);
            }
            long today = stamp.TotalMinutes / 1440L;
            if (today > row.LastDecayDay)
            {
                row.Unrest = System.Math.Max(0, row.Unrest - (int)(today - row.LastDecayDay));
                row.LastDecayDay = today;
            }
            row.Unrest += amount;
            if (row.Unrest < SweepThreshold) return;

            // TUNING ('sweep spam', 5510 marathon lines): per-guard raises re-cross the threshold
            // within the hour. During the cooldown unrest holds just under the line - the town
            // stays primed but the watch marches at most once per game day per site.
            if (stamp.TotalMinutes < row.SweepCooldownUntilMinutes)
            {
                row.Unrest = SweepThreshold - 1;
                return;
            }
            row.SweepCooldownUntilMinutes = stamp.TotalMinutes + SweepCooldownMinutes;

            row.Unrest = 2; // the sweep clears the air, not the memory
            int swept = 0;
            SiteRecord siteRecord = null;
            foreach (var site in world.Sites.Records)
                if (site != null && site.Id.Equals(siteId)) { siteRecord = site; break; }
            foreach (var guard in world.Actors.Records)
            {
                if (guard == null || !guard.IsAlive || guard.Role != ActorRole.Guard) continue;
                if (siteRecord != null &&
                    (guard.Position.X < siteRecord.MinBound.X - 4 || guard.Position.X > siteRecord.MaxBound.X + 4
                     || guard.Position.Y < siteRecord.MinBound.Y - 4 || guard.Position.Y > siteRecord.MaxBound.Y + 4))
                    continue;
                RegisterPursuit(world, guard.Id.Value, attackerId, stamp);
                swept++;
            }
            world.Events?.Append(new WorldEvent(stamp, WorldEventKind.ChronicleEvent,
                default, siteId, $"watch_sweep guards:{swept} target:{attackerId}"));
        }

        /// <summary>Arm/refresh a chase: one active pursuit per guard, newest trouble wins —
        /// upsert loop lives in Domain.World.PursuitLedgerQuery, shared with RegisterHunt.</summary>
        private const long PursuitMinutes = 120;
        internal static void RegisterPursuit(WorldState world, ulong guardId, ulong targetId, GameTime stamp)
        {
            world.GuardPursuits ??= new System.Collections.Generic.List<PursuitRecord>();
            PursuitLedgerQuery.UpsertPursuit(world.GuardPursuits, guardId, targetId,
                stamp.TotalMinutes + PursuitMinutes);
        }
    }
}
