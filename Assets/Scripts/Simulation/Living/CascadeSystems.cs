using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Rng;

// CAN SUYU H3: EVENT CASCADES. Until now the only reactive behavior lived in the presentation
// adapter, ran on the render pump, and hunted ONLY the player — NPC-vs-NPC was impossible and
// no event ever caused another. These two systems move predation into the SIMULATION and give
// the world its first chain: a hunter mauls a civilian (CombatResolved) → nearby civilians SEE
// it (WitnessRecorded + a real ActorMemory entry — NpcMemory's first runtime writer) → the
// watch CONVERGES and strikes back (GuardResponded → CombatResolved). Deterministic, stateless
// step instances (the H1 lesson), pure Simulation.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Hourly: hostile actors hunt the nearest civilian; guards in reach strike them.</summary>
    public sealed class PredationSystem
    {
        public const int HuntRadius = 6;
        public const int StrikeReach = 2;

        public int Tick(WorldState world) => Tick(world, world?.Time ?? default);

        // Catchup contract: dice and event stamps derive from the boundary stamp.
        public int Tick(WorldState world, GameTime stamp)
        {
            if (world?.Actors == null || world.Events == null) return 0;
            int strikes = 0;
            var resolver = new CombatActionResolver(new CombatHitRollService(), new CombatDamageService());
            var action = new EmberCrpg.Domain.Combat.CombatActionDef(
                new EmberCrpg.Domain.Combat.CombatActionId("predation"), 0, "accuracy_vs_dodge", "base_minus_armor", "maul");

            foreach (var hunter in world.Actors.Records)
            {
                if (hunter == null || !hunter.IsAlive || hunter.Role != ActorRole.Enemy) continue;

                // The watch answers FIRST: a guard within reach of a hunter strikes it — the
                // cascade's third link (and what keeps predation from depopulating the town).
                // W36 GUARD+COMBAT: this branch STILL RUNS for action-driven hunters — the guard
                // is the WRITER on this Actor.Vitals mutation, not the hunter itself, so the
                // action-strip's StrikeQuarry (which mutates its PREY's Vitals) does not race
                // this. Without this the town's mercy-clamped civilians would be mauled forever.
                var guard = Nearest(world, hunter.Position, StrikeReach,
                    a => a.Role == ActorRole.Guard);
                if (guard != null)
                {
                    Strike(world, resolver, action, guard, hunter, stamp);
                    world.Events.Append(new WorldEvent(stamp, WorldEventKind.GuardResponded,
                        guard.Id, FallbackSite(world, hunter.Position), $"guard_strikes_hunter target:{hunter.Id.Value}"));
                    strikes++;
                    if (!hunter.IsAlive) continue;
                }

                // W36 GUARD+COMBAT: a hunter with a live Hunt/StrikeQuarry action is action-strip
                // driven — the PerTick advancer OWNS its legs and its blows. Predation skips the
                // hunter-side (movement + prey strike) below to avoid a double-writer race on
                // Actor.Position and the PREY's Actor.Vitals (guard-first-strike above is safe:
                // it writes the HUNTER's Vitals, which no advancer touches this same tick).
                if (hunter.ActionState.CurrentAction != ActorActionType.None) continue;

                var prey = Nearest(world, hunter.Position, HuntRadius,
                    a => a.Role != ActorRole.Enemy && a.Role != ActorRole.Player && a.Role != ActorRole.Guard);
                if (prey == null) continue;

                if (hunter.Position.ChebyshevDistanceTo(prey.Position) <= StrikeReach)
                {
                    Strike(world, resolver, action, hunter, prey, stamp);
                    strikes++;
                }
                else
                {
                    // B10 §A5: route through the ONE grid stepper — refuses walls & corner-cuts.
                    hunter.MoveTo(MovementService.StepToward(hunter.Position, prey.Position, world.NavView));
                }
            }
            return strikes;
        }

        private static void Strike(WorldState world, CombatActionResolver resolver,
            EmberCrpg.Domain.Combat.CombatActionDef action, ActorRecord attacker, ActorRecord target, GameTime stamp)
        {
            // Deterministic dice: boundary clock + both ids — same world, same bites.
            var rng = new XorShiftRng((uint)(
                (stamp.TotalMinutes * 2654435761L)
                ^ (long)(attacker.Id.Value * 97L) ^ (long)(target.Id.Value * 193L)) | 1u);
            resolver.Resolve(action, attacker, target,
                damageBandWidth: System.Math.Max(1, attacker.BaseDamage / 2),
                rng: rng, now: stamp, siteId: FallbackSite(world, target.Position), events: world.Events);

            // PLAYTEST FIX ("vardigimda kimse yoktu"): predation MAULS civilians, it does not
            // erase settlements — 58 travel days of lethal strikes had depopulated whole towns.
            // A civilian dropped to 0 survives at 1 HP, marked mauled (NeedRecovery heals them);
            // hunters and guards keep killing EACH OTHER — predator population still self-caps.
            if (!target.IsAlive && target.Role != ActorRole.Enemy && target.Role != ActorRole.Guard)
            {
                target.ApplyVitals(new ActorVitals(
                    new VitalStat(1, target.Vitals.Health.Max), target.Vitals.Fatigue, target.Vitals.Mana));
                world.Events?.Append(new WorldEvent(stamp, WorldEventKind.NeedChanged, target.Id,
                    FallbackSite(world, target.Position), $"mauled_survives by:{attacker.Id.Value}"));
            }
        }

        internal static ActorRecord Nearest(WorldState world, GridPosition from, int radius,
            System.Func<ActorRecord, bool> filter)
        {
            ActorRecord best = null;
            int bestDist = int.MaxValue;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive || !filter(actor)) continue;
                int d = from.ChebyshevDistanceTo(actor.Position);
                if (d <= radius && d < bestDist) { bestDist = d; best = actor; }
            }
            return best;
        }

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

                    // DEPTH 4 — the report: a witness RUNS to the watch. Beside a guard they file
                    // the report (memory + event, once per attacker per witness); otherwise they
                    // step toward the nearest guard instead of milling in shock.
                    var nearGuard = PredationSystem.Nearest(world, witness.Position, 16,
                        a => a.Role == ActorRole.Guard);
                    if (nearGuard != null)
                    {
                        if (witness.Position.ChebyshevDistanceTo(nearGuard.Position) <= 2)
                        {
                            bool alreadyReported = false;
                            foreach (var known in witnessMemory.Events)
                                if (known.EventType == "reported_attack" && known.ActorSeen.Equals(attacker.Id))
                                { alreadyReported = true; break; }
                            if (!alreadyReported)
                            {
                                witnessMemory.RecordEvent(new InteractionEvent(
                                    stamp, "reported_attack", attacker.Id, "watch_report", string.Empty, 0, witness.Position));
                                world.Events.Append(new WorldEvent(stamp, WorldEventKind.WitnessRecorded,
                                    witness.Id, evt.SiteId, $"reported attacker:{attacker.Id.Value} guard:{nearGuard.Id.Value}"));
                            }
                        }
                        else
                        {
                            // B10 §A5: route through the ONE grid stepper — refuses walls & corner-cuts.
                            witness.MoveTo(MovementService.StepToward(witness.Position, nearGuard.Position, world.NavView));
                        }
                    }
                }

                // The watch converges: guards in earshot walk a tile toward the trouble.
                foreach (var guard in world.Actors.Records)
                {
                    if (guard == null || !guard.IsAlive || guard.Role != ActorRole.Guard) continue;
                    int d = guard.Position.ChebyshevDistanceTo(attacker.Position);
                    if (d > ResponseRadius) continue;
                    // P0 pursuit: the report ARMS a chase the PerTick schedule will run - the
                    // hourly nudge below alone lost 60:1 to the return-to-post writer.
                    RegisterPursuit(world, guard.Id.Value, attacker.Id.Value, stamp);
                    // P2: every armed response also weighs on the town's ledger.
                    RaiseUnrest(world, evt.SiteId, 2, stamp, attacker.Id.Value);
                    if (d <= 1) continue;
                    // B10 §A5: route through the ONE grid stepper — refuses walls & corner-cuts.
                    guard.MoveTo(MovementService.StepToward(guard.Position, attacker.Position, world.NavView));
                }
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
        private static void RegisterPursuit(WorldState world, ulong guardId, ulong targetId, GameTime stamp)
        {
            world.GuardPursuits ??= new System.Collections.Generic.List<PursuitRecord>();
            PursuitLedgerQuery.UpsertPursuit(world.GuardPursuits, guardId, targetId,
                stamp.TotalMinutes + PursuitMinutes);
        }
    }
}
