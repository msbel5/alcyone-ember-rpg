using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Rng;

// V3 YOLDAŞ: companions are recruited CIVILIANS, not a new actor role — they keep their
// identity, their sprite, and crucially their ActorMemory, so the dialogue pipeline recalls
// the journey you shared ("witnessed_attack" beside you means something now). Membership
// lives on WorldState.CompanionIds; behavior splits into a per-tick FOLLOW (heel distance)
// and an hourly GUARD strike (an adjacent hostile near the player or the companion gets hit
// with the same deterministic dice predation uses). Stateless step instances (H1 lesson).
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Recruit/dismiss rules — the party's front door.</summary>
    public static class CompanionService
    {
        public const int MaxCompanions = 2;
        public const int RecruitReachCells = 3;

        public static bool TryRecruit(WorldState world, ActorId actorId)
        {
            var player = FindPlayer(world);
            if (player == null || world.CompanionIds == null) return false;
            if (world.CompanionIds.Count >= MaxCompanions) return false;
            if (world.CompanionIds.Contains(actorId.Value)) return false;
            if (!world.Actors.TryGet(actorId, out var actor) || actor == null || !actor.IsAlive) return false;
            if (actor.Role == ActorRole.Player || actor.Role == ActorRole.Enemy) return false;
            if (Chebyshev(actor.Position, player.Position) > RecruitReachCells) return false;

            world.CompanionIds.Add(actorId.Value);
            world.Events?.Append(new WorldEvent(world.Time, WorldEventKind.ActorTalked, actorId, default,
                $"companion_joined name:{actor.Name}"));
            return true;
        }

        public static bool TryDismiss(WorldState world, ActorId actorId)
        {
            if (world?.CompanionIds == null || !world.CompanionIds.Remove(actorId.Value)) return false;
            world.Actors.TryGet(actorId, out var actor);
            world.Events?.Append(new WorldEvent(world.Time, WorldEventKind.ActorTalked, actorId, default,
                $"companion_left name:{actor?.Name ?? "?"}"));
            return true;
        }

        public static bool IsCompanion(WorldState world, ActorId actorId)
            => world?.CompanionIds != null && world.CompanionIds.Contains(actorId.Value);

        public static ActorRecord FindPlayer(WorldState world) // public: the proof surface in Presentation also needs it
        {
            if (world?.Actors?.Records == null) return null;
            foreach (var actor in world.Actors.Records)
                if (actor != null && actor.IsAlive && actor.Role == ActorRole.Player) return actor;
            return null;
        }

        internal static int Chebyshev(GridPosition a, GridPosition b)
            => System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y));
    }

    /// <summary>Per-tick heel-follow + hourly guard strike for recruited companions.</summary>
    public sealed class CompanionSystem
    {
        public const int HeelCells = 1;       // at or inside heel range the companion stands easy
        public const int GuardReachCells = 2; // hostiles this close to player OR companion get struck

        /// <summary>PerTick: lagging companions take one Chebyshev step toward the player.</summary>
        public int TickFollow(WorldState world)
        {
            var player = CompanionService.FindPlayer(world);
            if (player == null || world.CompanionIds == null || world.CompanionIds.Count == 0) return 0;

            int moved = 0;
            foreach (var id in world.CompanionIds)
            {
                if (!world.Actors.TryGet(new ActorId(id), out var companion) || companion == null || !companion.IsAlive)
                    continue;
                if (CompanionService.Chebyshev(companion.Position, player.Position) <= HeelCells)
                    continue; // at heel — no jitter
                companion.MoveTo(new GridPosition(
                    companion.Position.X + System.Math.Sign(player.Position.X - companion.Position.X),
                    companion.Position.Y + System.Math.Sign(player.Position.Y - companion.Position.Y)));
                moved++;
            }
            return moved;
        }

        /// <summary>Hourly: each companion strikes one hostile within guard reach of the player
        /// or of itself — same deterministic dice as predation (boundary stamp + both ids).</summary>
        public int TickGuard(WorldState world, GameTime stamp)
        {
            var player = CompanionService.FindPlayer(world);
            if (player == null || world?.CompanionIds == null || world.CompanionIds.Count == 0) return 0;

            int strikes = 0;
            var resolver = new CombatActionResolver(new CombatHitRollService(), new CombatDamageService());
            var action = new EmberCrpg.Domain.Combat.CombatActionDef(
                new EmberCrpg.Domain.Combat.CombatActionId("companion_guard"), 0, "accuracy_vs_dodge", "base_minus_armor", "strike");

            foreach (var id in world.CompanionIds)
            {
                if (!world.Actors.TryGet(new ActorId(id), out var companion) || companion == null || !companion.IsAlive)
                    continue;
                var threat = NearestHostile(world, player.Position, companion.Position);
                if (threat == null) continue;

                var rng = new XorShiftRng((uint)(
                    (stamp.TotalMinutes * 2654435761L)
                    ^ (long)(companion.Id.Value * 97L) ^ (long)(threat.Id.Value * 193L)) | 1u);
                resolver.Resolve(action, companion, threat,
                    damageBandWidth: System.Math.Max(1, companion.BaseDamage / 2),
                    rng: rng, now: stamp, siteId: PredationSystem.FallbackSite(world), events: world.Events);
                strikes++;
            }
            return strikes;
        }

        private static ActorRecord NearestHostile(WorldState world, GridPosition player, GridPosition companion)
        {
            ActorRecord best = null;
            int bestDist = int.MaxValue;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive || actor.Role != ActorRole.Enemy) continue;
                int dist = System.Math.Min(
                    CompanionService.Chebyshev(actor.Position, player),
                    CompanionService.Chebyshev(actor.Position, companion));
                if (dist <= GuardReachCells && dist < bestDist) { bestDist = dist; best = actor; }
            }
            return best;
        }
    }
}
