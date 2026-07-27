using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Rng;

// Design note:
// W36 GUARD+COMBAT: the SLEEP/WORK/FARM "Operations" pattern applied to the enemy hunt path.
// One home for scan predicates + deterministic strike resolution — HuntAdvancer and
// StrikeQuarryAdvancer share these helpers exactly the way MoveToBed and Sleep share
// SleepOperations. PredationSystem.Strike/Nearest are the ancestor (`internal static`); this
// class is that body extracted for reuse under the action strip. When PredationSystem is
// eventually retired, its call sites collapse to CombatOperations verbatim.
// CONSTRAINT (determinism constitution): pure Domain/Simulation, no Unity/IO, deterministic
// XorShiftRng seeded from stamp + attacker + target — same world, same strikes.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Prey scan + deterministic strike resolution for enemy Hunt and guard Chase.</summary>
    internal static class CombatOperations
    {
        /// <summary>Enemy prey filter — civilians only (Merchant/Talker/etc.); never enemies,
        /// guards, or the player. Mirror of PredationSystem's hunter loop filter (CascadeSystems.cs
        /// prey line) so a retirement of PredationSystem drops this filter into that seat.</summary>
        public static bool IsPrey(ActorRecord actor)
            => actor != null && actor.IsAlive
               && actor.Role != ActorRole.Enemy
               && actor.Role != ActorRole.Player
               && actor.Role != ActorRole.Guard;

        /// <summary>Nearest actor (Chebyshev) within radius that passes filter, or null.
        /// Deterministic first-wins on ties (ActorStore.Records is insertion-ordered).</summary>
        public static ActorRecord Nearest(WorldState world, GridPosition from, int radius,
            System.Func<ActorRecord, bool> filter)
        {
            ActorRecord best = null;
            var bestDist = int.MaxValue;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive || !filter(actor)) continue;
                var d = from.ChebyshevDistanceTo(actor.Position);
                if (d <= radius && d < bestDist) { bestDist = d; best = actor; }
            }
            return best;
        }

        /// <summary>Deterministic strike resolution: dice seeded from (stamp, attacker, target).
        /// PredationSystem.Strike's body verbatim, minus the mauled-survives clamp — StrikeQuarry
        /// callers apply the clamp themselves when preserving town population is the intent (the
        /// guard-vs-enemy path drops the clamp so predators still self-cap).</summary>
        public static void ResolveStrike(WorldState world, ActorRecord attacker, ActorRecord target,
            GameTime stamp, string actionId = "predation", string damageDeck = "maul")
        {
            var resolver = new CombatActionResolver(new CombatHitRollService(), new CombatDamageService());
            var action = new EmberCrpg.Domain.Combat.CombatActionDef(
                new EmberCrpg.Domain.Combat.CombatActionId(actionId), 0, "accuracy_vs_dodge", "base_minus_armor", damageDeck);
            var rng = new XorShiftRng((uint)(
                (stamp.TotalMinutes * 2654435761L)
                ^ (long)(attacker.Id.Value * 97L) ^ (long)(target.Id.Value * 193L)) | 1u);
            resolver.Resolve(action, attacker, target,
                damageBandWidth: System.Math.Max(1, attacker.BaseDamage / 2),
                rng: rng, now: stamp, siteId: PredationSystem.FallbackSite(world, target.Position),
                events: world.Events);
        }

        /// <summary>PLAYTEST FIX ("vardigimda kimse yoktu"): a civilian dropped to 0 survives at 1 HP
        /// and receives a mauled_survives event. Predator-vs-predator (Enemy/Guard) is NOT clamped
        /// — the population still self-caps. PredationSystem.Strike's tail applied to the
        /// StrikeQuarry seat, so retiring PredationSystem's hunter loop does not lose the mercy.</summary>
        public static void MaybeMaulClamp(WorldState world, ActorRecord attacker, ActorRecord target, GameTime stamp)
        {
            if (target.IsAlive) return;
            if (target.Role == ActorRole.Enemy || target.Role == ActorRole.Guard) return;
            target.ApplyVitals(new ActorVitals(
                new VitalStat(1, target.Vitals.Health.Max), target.Vitals.Fatigue, target.Vitals.Mana));
            world.Events?.Append(new WorldEvent(stamp, WorldEventKind.NeedChanged, target.Id,
                PredationSystem.FallbackSite(world, target.Position),
                $"mauled_survives by:{attacker.Id.Value}"));
        }

        /// <summary>Reach for a Hunt→StrikeQuarry adjacency test. Mirror of PredationSystem.StrikeReach.</summary>
        public const int StrikeReach = 1;

        /// <summary>Enemy hunt scan radius. Owned by PredationSystem.HuntRadius —
        /// the two decides used to hand-roll the same 6 with a "mirror of" comment.</summary>
        public const int HuntRadius = PredationSystem.HuntRadius;

        /// <summary>Bounded hunt-target TTL: an unresolved target expires and clears the row
        /// (dead-quarry / lost-prey pruning is the advancer's job on the same call). Matches
        /// PursuitRecord's PursuitMinutes so the two ledgers share a rhythm.</summary>
        public const long HuntMinutes = 120;
    }
}
