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

        public int Tick(WorldState world)
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
                var guard = Nearest(world, hunter.Position, StrikeReach,
                    a => a.Role == ActorRole.Guard);
                if (guard != null)
                {
                    Strike(world, resolver, action, guard, hunter);
                    world.Events.Append(new WorldEvent(world.Time, WorldEventKind.GuardResponded,
                        guard.Id, FallbackSite(world), $"guard_strikes_hunter target:{hunter.Id.Value}"));
                    strikes++;
                    if (!hunter.IsAlive) continue;
                }

                var prey = Nearest(world, hunter.Position, HuntRadius,
                    a => a.Role != ActorRole.Enemy && a.Role != ActorRole.Player && a.Role != ActorRole.Guard);
                if (prey == null) continue;

                if (Chebyshev(hunter.Position, prey.Position) <= StrikeReach)
                {
                    Strike(world, resolver, action, hunter, prey);
                    strikes++;
                }
                else
                {
                    hunter.MoveTo(new GridPosition(
                        hunter.Position.X + System.Math.Sign(prey.Position.X - hunter.Position.X),
                        hunter.Position.Y + System.Math.Sign(prey.Position.Y - hunter.Position.Y)));
                }
            }
            return strikes;
        }

        private static void Strike(WorldState world, CombatActionResolver resolver,
            EmberCrpg.Domain.Combat.CombatActionDef action, ActorRecord attacker, ActorRecord target)
        {
            // Deterministic dice: world clock + both ids — same world, same bites.
            var rng = new XorShiftRng((uint)(
                (world.Time.TotalMinutes * 2654435761L)
                ^ (long)(attacker.Id.Value * 97L) ^ (long)(target.Id.Value * 193L)) | 1u);
            resolver.Resolve(action, attacker, target,
                damageBandWidth: System.Math.Max(1, attacker.BaseDamage / 2),
                rng: rng, now: world.Time, siteId: FallbackSite(world), events: world.Events);
        }

        internal static ActorRecord Nearest(WorldState world, GridPosition from, int radius,
            System.Func<ActorRecord, bool> filter)
        {
            ActorRecord best = null;
            int bestDist = int.MaxValue;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive || !filter(actor)) continue;
                int d = Chebyshev(from, actor.Position);
                if (d <= radius && d < bestDist) { bestDist = d; best = actor; }
            }
            return best;
        }

        internal static int Chebyshev(GridPosition a, GridPosition b)
            => System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y));

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

        public int Tick(WorldState world)
        {
            if (world?.Actors == null || world.Events == null || world.NpcMemory == null) return 0;
            long windowStart = world.Time.TotalMinutes - 60;
            int recorded = 0;

            // Stateless scan of the LAST HOUR only (hourly cadence → each event seen once).
            var events = world.Events.Events;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                var evt = events[i];
                if (evt.Tick.TotalMinutes <= windowStart) break;
                if (evt.Kind != WorldEventKind.CombatResolved) continue;
                if (!world.Actors.TryGet(evt.ActorId, out var attacker) || attacker == null) continue;
                if (attacker.Role != ActorRole.Enemy) continue; // player brawls are the bounty system's beat

                foreach (var witness in world.Actors.Records)
                {
                    if (witness == null || !witness.IsAlive) continue;
                    if (witness.Role == ActorRole.Enemy || witness.Role == ActorRole.Player) continue;
                    if (witness.Id.Equals(evt.ActorId)) continue;
                    if (PredationSystem.Chebyshev(witness.Position, attacker.Position) > WitnessRadius) continue;

                    world.NpcMemory.GetOrCreate(witness.Id).RecordEvent(new InteractionEvent(
                        world.Time, "witnessed_attack", attacker.Id, "predation", string.Empty, 0, witness.Position));
                    world.Events.Append(new WorldEvent(world.Time, WorldEventKind.WitnessRecorded,
                        witness.Id, evt.SiteId, $"witnessed attacker:{attacker.Id.Value}"));
                    recorded++;
                }

                // The watch converges: guards in earshot walk a tile toward the trouble.
                foreach (var guard in world.Actors.Records)
                {
                    if (guard == null || !guard.IsAlive || guard.Role != ActorRole.Guard) continue;
                    int d = PredationSystem.Chebyshev(guard.Position, attacker.Position);
                    if (d > ResponseRadius || d <= 1) continue;
                    guard.MoveTo(new GridPosition(
                        guard.Position.X + System.Math.Sign(attacker.Position.X - guard.Position.X),
                        guard.Position.Y + System.Math.Sign(attacker.Position.Y - guard.Position.Y)));
                }
            }
            return recorded;
        }
    }
}
